﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Events;

namespace Microsoft.MixedReality.Toolkit.UI
{
    /// <summary>
    /// A basic receiver for detecting clicks
    /// </summary>
    public class InteractableOnClickReceiver : ReceiverBase
    {
        public InteractableOnClickReceiver(UnityEvent ev): base(ev)
        {
            Name = "OnClick";
        }

        public override void OnUpdate(InteractableStates state, Interactable source)
        {
            // using onClick
        }

        public override void OnClick(InteractableStates state, Interactable source, IMixedRealityPointer pointer = null)
        {
            uEvent.Invoke();
        }
    }
}
