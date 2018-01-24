﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_WSA && !UNITY_2017_2_OR_NEWER
using UnityEngine.VR.WSA.Input;
#endif

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Input source for raw interactions sources information, which gives finer details about current source state and position
    /// than the standard GestureRecognizer.
    /// This mostly allows users to access the source up/down and detected/lost events, 
    /// which are not communicated as part of standard Windows gestures.
    /// </summary>
    /// <remarks>
    /// This input source only triggers SourceUp/SourceDown and SourceDetected/SourceLost.
    /// Everything else is handled by InteractionInputSource.
    /// </remarks>
    [Obsolete("Will be removed in a future release")]
    public class RawInteractionInputSource : BaseInputSource
    {
        /// <summary>
        /// Data for an interaction source.
        /// </summary>
        private class SourceData
        {
            public SourceData(uint sourceId)
            {
                SourceId = sourceId;
                HasPosition = false;
                SourcePosition = Vector3.zero;
                IsSourceDown = false;
                IsSourceDownPending = false;
                SourceStateChanged = false;
                SourceStateUpdateTimer = -1;
            }

            public readonly uint SourceId;
            public bool HasPosition;
            public Vector3 SourcePosition;
            public bool IsSourceDown;
            public bool IsSourceDownPending;
            public bool SourceStateChanged;
            public float SourceStateUpdateTimer;
        }

        /// <summary>
        /// Delay before a source press is considered.
        /// This mitigates fake source taps that can sometimes be detected while the input source is moving.
        /// </summary>
        private const float SourcePressDelay = 0.07f;

        [Tooltip("Use unscaled time. This is useful for games that have a pause mechanism or otherwise adjust the game timescale.")]
        public bool UseUnscaledTime = true;

        /// <summary>
        /// Dictionary linking each source ID to its data.
        /// </summary>
        private readonly Dictionary<uint, SourceData> sourceIdToData = new Dictionary<uint, SourceData>(4);
        private readonly List<uint> pendingSourceIdDeletes = new List<uint>();

        // HashSets used to be able to quickly update the sources data when they become visible / not visible
        private readonly HashSet<uint> currentSources = new HashSet<uint>();
        private readonly HashSet<uint> newSources = new HashSet<uint>();

        public override SupportedInputInfo GetSupportedInputInfo(uint sourceId)
        {
            SupportedInputInfo retVal = SupportedInputInfo.None;

            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                if (sourceData.HasPosition)
                {
                    retVal |= SupportedInputInfo.Position;
                }
            }

            return retVal;
        }

        public override bool TryGetMenu(uint sourceId, out bool isPressed)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetPointerPosition(uint sourceId, out Vector3 position)
        {
            SourceData sourceData;
            if (sourceIdToData.TryGetValue(sourceId, out sourceData))
            {
                if (sourceData.HasPosition)
                {
                    position = sourceData.SourcePosition;
                    return true;
                }
            }

            // Else, the source doesn't have positional information
            position = Vector3.zero;
            return false;
        }

        public override bool TryGetPointerRotation(uint sourceId, out Quaternion orientation)
        {
            // Orientation is not supported by any Windows interaction sources
            orientation = Quaternion.identity;
            return false;
        }

        public override bool TryGetSourceKind(uint sourceId, out InteractionSourceInfo sourceKind)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetGripPosition(uint sourceId, out Vector3 position)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetGripRotation(uint sourceId, out Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetThumbstick(uint sourceId, out bool isPressed, out Vector2 position)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetTouchpad(uint sourceId, out bool isPressed, out bool isTouched, out Vector2 position)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetSelect(uint sourceId, out bool isPressed, out double pressedValue)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetGrasp(uint sourceId, out bool isPressed)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetPointingRay(uint sourceId, out Ray pointingRay)
        {
            throw new NotImplementedException();
        }

        private void Update()
        {
            newSources.Clear();
            currentSources.Clear();

            UpdateSourceData();
            SendSourceVisibilityEvents();
        }

        /// <summary>
        /// Update the source data for the currently detected sources.
        /// </summary>
        private void UpdateSourceData()
        {
#if UNITY_WSA && !UNITY_2017_2_OR_NEWER
            // Poll for updated reading from hands
            InteractionSourceState[] sourceStates = InteractionManager.GetCurrentReading();
            if (sourceStates != null)
            {
                for (var i = 0; i < sourceStates.Length; ++i)
                {
                    InteractionSourceState handSource = sourceStates[i];
                    SourceData sourceData = GetOrAddSourceData(handSource.source);
                    currentSources.Add(handSource.source.id);

                    UpdateSourceState(handSource, sourceData);
                }
            }
#endif
        }

#if UNITY_WSA && !UNITY_2017_2_OR_NEWER
        /// <summary>
        /// Gets the source data for the specified interaction source if it already exists, otherwise creates it.
        /// </summary>
        /// <param name="interactionSource">Interaction source for which data should be retrieved.</param>
        /// <returns>The source data requested.</returns>
        private SourceData GetOrAddSourceData(InteractionSource interactionSource)
        {
            SourceData sourceData;
            if (!sourceIdToData.TryGetValue(interactionSource.id, out sourceData))
            {
                sourceData = new SourceData(interactionSource.id);
                sourceIdToData.Add(sourceData.SourceId, sourceData);
                newSources.Add(sourceData.SourceId);
            }

            return sourceData;
        }

        /// <summary>
        /// Updates the source positional information.
        /// </summary>
        /// <param name="interactionSource">Interaction source to use to update the position.</param>
        /// <param name="sourceData">SourceData structure to update.</param>
        private void UpdateSourceState(InteractionSourceState interactionSource, SourceData sourceData)
        {
            // Update source position
            Vector3 sourcePosition;
            if (interactionSource.properties.location.TryGetPosition(out sourcePosition))
            {
                sourceData.HasPosition = true;
                sourceData.SourcePosition = sourcePosition;
            }

            // Check for source presses
            if (interactionSource.pressed != sourceData.IsSourceDownPending)
            {
                sourceData.IsSourceDownPending = interactionSource.pressed;
                sourceData.SourceStateUpdateTimer = SourcePressDelay;
            }

            // Source presses are delayed to mitigate issue with hand position shifting during air tap
            sourceData.SourceStateChanged = false;
            if (sourceData.SourceStateUpdateTimer >= 0)
            {
                float deltaTime = UseUnscaledTime
                    ? Time.unscaledDeltaTime
                    : Time.deltaTime;

                sourceData.SourceStateUpdateTimer -= deltaTime;
                if (sourceData.SourceStateUpdateTimer < 0)
                {
                    sourceData.IsSourceDown = sourceData.IsSourceDownPending;
                    sourceData.SourceStateChanged = true;
                }
            }

            SendSourceStateEvents(sourceData);
        }
#endif

        /// <summary>
        /// Sends the events for source state changes.
        /// </summary>
        /// <param name="sourceData">Source data for which events should be sent.</param>
        private void SendSourceStateEvents(SourceData sourceData)
        {
            // Source pressed/released events
            if (sourceData.SourceStateChanged)
            {
                if (sourceData.IsSourceDown)
                {
                    InputManager.Instance.RaiseSourceDown(this, sourceData.SourceId, InteractionSourcePressInfo.Select);
                }
                else
                {
                    InputManager.Instance.RaiseSourceUp(this, sourceData.SourceId, InteractionSourcePressInfo.Select);
                }
            }
        }

        /// <summary>
        /// Sends the events for source visibility changes.
        /// </summary>
        private void SendSourceVisibilityEvents()
        {
            // Send event for new sources that were added
            foreach (uint newSource in newSources)
            {
                InputManager.Instance.RaiseSourceDetected(this, newSource);
            }

            // Send event for sources that are no longer visible and remove them from our dictionary
            foreach (uint existingSource in sourceIdToData.Keys)
            {
                if (!currentSources.Contains(existingSource))
                {
                    pendingSourceIdDeletes.Add(existingSource);
                    InputManager.Instance.RaiseSourceLost(this, existingSource);
                }
            }

            // Remove pending source IDs
            for (int i = 0; i < pendingSourceIdDeletes.Count; ++i)
            {
                sourceIdToData.Remove(pendingSourceIdDeletes[i]);
            }
            pendingSourceIdDeletes.Clear();
        }
    }
}
