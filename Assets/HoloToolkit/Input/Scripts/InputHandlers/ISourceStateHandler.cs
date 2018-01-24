﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine.EventSystems;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Interface to implement to react to source state changes, such as when an input source is detected or lost.
    /// </summary>
    public interface ISourceStateHandler : IEventSystemHandler
    {
        void OnSourceDetected(SourceStateEventData eventData);
        void OnSourceLost(SourceStateEventData eventData);
    }
}
