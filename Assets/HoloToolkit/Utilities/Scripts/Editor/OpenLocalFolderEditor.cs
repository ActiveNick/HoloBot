﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEditor;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// Property drawer for selection of a local input folder with the resultant path stored in a string
    /// </summary>
    [CustomPropertyDrawer(typeof(OpenLocalFolderAttribute))]
    public class OpenLocalFolderEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                throw new ArgumentException();
            }

            position.width -= 30;
            EditorGUI.PropertyField(position, property, label);

            position.x += position.width;
            position.width = 30.0f;

            if (GUI.Button(position, "..."))
            {
                var path = EditorUtility.OpenFolderPanel("Select a folder", Application.dataPath, "");
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                if (path.StartsWith(Application.dataPath))
                {
                    path = path.Substring(Application.dataPath.Length);
                    path = path.Replace("/", "\\");
                }

                property.stringValue = path;
            }
        }
    }
}