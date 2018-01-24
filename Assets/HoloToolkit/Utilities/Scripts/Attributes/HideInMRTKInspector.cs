// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HoloToolkit.Unity
{
    // Hides a field in an MRDL inspector
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HideInMRTKInspector : ShowIfAttribute
    {
        public HideInMRTKInspector() { }

#if UNITY_EDITOR
        public override bool ShouldShow(object target)
        {
            return false;
        }
#endif
    }
}