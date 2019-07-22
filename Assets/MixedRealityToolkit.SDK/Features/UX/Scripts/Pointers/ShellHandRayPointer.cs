// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    public class ShellHandRayPointer : LinePointer
    {
        [SerializeField]
        [Tooltip("Used when a focus target exists, or when select is pressed")]
        private BaseMixedRealityLineRenderer lineRendererSelected = null;

        [SerializeField]
        [Tooltip("Used when no focus target exists and select is not pressed")]
        private BaseMixedRealityLineRenderer lineRendererNoTarget = null;

        [Header("Inertia Settings")]
        [SerializeField]
        private BezierInertia inertia;

        [Tooltip("Where to place the first control point of the bezier curve")]
        [SerializeField]
        [Range(0f, 0.5f)]
        private float startPointLerp = 0.33f;

        [SerializeField]
        [Tooltip("Where to place the second control point of the bezier curve")]
        [Range(0.5f, 1f)]
        private float endPointLerp = 0.66f;

        protected override void OnEnable()
        {
            base.OnEnable();

            inertia = gameObject.EnsureComponent<BezierInertia>();
        }

        /// <inheritdoc />
        public override void OnPostSceneQuery()
        {
            if (IsSelectPressed)
            {
                InputSystem.RaisePointerDragged(this, MixedRealityInputAction.None, Handedness);
            }

            Gradient lineColor = LineColorNoTarget;
            BaseMixedRealityLineRenderer contextRenderer = null;

            if (!IsActive)
            {
                LineBase.enabled = false;
                BaseCursor?.SetVisibility(false);
                return;
            }

            contextRenderer = lineRendererNoTarget;
            LineBase.enabled = true;
            BaseCursor?.SetVisibility(true);

            float clearWorldLength;
            float cursorOffsetWorldLength = (BaseCursor != null) ? BaseCursor.SurfaceCursorDistance : 0;

            // If we hit something
            if (Result?.CurrentPointerTarget != null)
            {
                clearWorldLength = Result.Details.RayDistance;

                lineColor = LineColorValid;
                contextRenderer = lineRendererSelected;
            }
            else
            {
                clearWorldLength = DefaultPointerExtent;

                lineColor = IsSelectPressed ? LineColorSelected : LineColorNoTarget;
                contextRenderer = IsSelectPressed ? lineRendererSelected : lineRendererNoTarget;
            }

            if (IsFocusLocked)
            {
                lineColor = LineColorLockFocus;
                contextRenderer = lineRendererSelected;
            }

            int maxClampLineSteps = LineCastResolution;

            foreach (BaseMixedRealityLineRenderer lineRenderer in LineRenderers)
            {
                // Otherwise, enable the renderer we chose
                if (lineRenderer == contextRenderer)
                {
                    lineRenderer.enabled = true;
                    maxClampLineSteps = Mathf.Max(maxClampLineSteps, lineRenderer.LineStepCount);
                }
                else
                {
                    lineRenderer.enabled = false;
                }

                // Set colors on all line renderers regardless of context
                lineRenderer.LineColor = lineColor;
            }

            // If focus and target point is locked, we're sticking to the target
            // So don't clamp the world length
            if (IsFocusLocked && IsTargetPositionLockedOnFocusLock)
            {
                float cursorOffsetLocalLength = LineBase.GetNormalizedLengthFromWorldLength(cursorOffsetWorldLength);
                LineBase.LineEndClamp = 1 - cursorOffsetLocalLength;
            }
            else
            {
                // Otherwise clamp the line end by the clear distance
                float clearLocalLength = LineBase.GetNormalizedLengthFromWorldLength(clearWorldLength - cursorOffsetWorldLength, maxClampLineSteps);
                LineBase.LineEndClamp = clearLocalLength;
            }
        }

        protected override void SetLinePoints(Vector3 startPoint, Vector3 endPoint, float distance)
        {
            LineBase.FirstPoint = startPoint;
            LineBase.LastPoint = endPoint;

            if (IsFocusLocked && IsTargetPositionLockedOnFocusLock)
            {
                inertia.enabled = false;
                // Project forward based on pointer direction to get an 'expected' position of the first control point
                Vector3 expectedPoint = startPoint + Rotation * Vector3.forward * distance;
                // Lerp between the expected position and the expected point
                LineBase.SetPoint(1, Vector3.Lerp(startPoint, expectedPoint, startPointLerp));
                // Get our next 'expected' position by lerping between the expected point and the end point
                // The result will be a line that starts moving in the pointer's direction then bends towards the target
                expectedPoint = Vector3.Lerp(expectedPoint, endPoint, endPointLerp);
                LineBase.SetPoint(2, Vector3.Lerp(startPoint, expectedPoint, endPointLerp));
            }
            else
            {
                inertia.enabled = true;
            }
        }
    }
}