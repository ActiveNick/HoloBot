﻿using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities.Editor;
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// a receiver that listens to toggle events
    /// </summary>
    public class InteractableOnToggleReceiver : ReceiverBase
    {
        [InspectorField(Type = InspectorField.FieldTypes.Event, Label = "On Deselect", Tooltip = "The toggle is deselected")]
        public UnityEvent OnDeselect = new UnityEvent();

        public InteractableOnToggleReceiver(UnityEvent ev) : base(ev)
        {
            Name = "OnSelect";
        }

        public override void OnUpdate(InteractableStates state, Interactable source)
        {
            // using onClick 
        }

        public override void OnClick(InteractableStates state, Interactable source, IMixedRealityPointer pointer = null)
        {
            int currentIndex = source.GetDimensionIndex();
            
            if (currentIndex % 2 == 0)
            {
                OnDeselect.Invoke();
            }
            else
            {
                uEvent.Invoke();
            }
        }
    }
}
