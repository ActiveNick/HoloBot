﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using UnityEngine;
using UnityEditor;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// Sets Force Text Serialization and visible meta files in all projects that use the HoloToolkit.
    /// </summary>
    [InitializeOnLoad]
    public class EnforceEditorSettings
    {
        private const string AssemblyReloadTimestampKey = "_HoloToolkit_Editor_LastAssemblyReload";

        static EnforceEditorSettings()
        {
            #region Editor Settings

            if (!IsNewEditorSession())
            {
                return;
            }

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                if (EditorUtility.DisplayDialog(
                        "Force Text Asset Serialization?",
                        "HoloToolkit is easier to maintain if the asset serialization mode for this project is set to \"Force Text\". Would you like to make this change?",
                        "Force Text Serialization",
                        "Later"))
                {
                    EditorSettings.serializationMode = SerializationMode.ForceText;
                    Debug.Log("Setting Force Text Serialization");
                }
            }

            if (!EditorSettings.externalVersionControl.Equals("Visible Meta Files"))
            {
                if (EditorUtility.DisplayDialog(
                    "Make Meta Files Visible?",
                    "HoloToolkit would like to make meta files visible so they can be more easily handled with common version control systems. Would you like to make this change?",
                    "Enable Visible Meta Files",
                    "Later"))
                {
                    EditorSettings.externalVersionControl = "Visible Meta Files";
                    Debug.Log("Updated external version control mode: " + EditorSettings.externalVersionControl);
                }
            }

            #endregion
        }

        /// <summary>
        /// Returns true the first time it is called within this editor session, and
        /// false for all subsequent calls.
        /// </summary>
        /// <remarks>
        /// The Unity Editor does not provide a callback for when a project is opened.
        /// InitializeOnLoad is triggered for all assembly reloads, including entering
        /// and exiting PlayMode, and whenever a script is modified and recompiled.
        ///
        /// To ensure execution only when opening a project in a new instance of the editor,
        /// store a timestamp in the editor key-value store whenever this function is called. 
        /// The stored timestamp is then compared with the true start time of this editor
        /// instance.
        /// </remarks>
        private static bool IsNewEditorSession()
        {
            // Determine the launch date for this editor session using the current time, and the time since startup.
            DateTime thisLaunchDate = DateTime.UtcNow.AddSeconds(-EditorApplication.timeSinceStartup);

            // Determine the last known launch date of the editor by loading it from the PlayerPrefs cache.
            // If no key exists set the time to this session.
            string dateString = EditorPrefsUtility.GetEditorPref(AssemblyReloadTimestampKey, thisLaunchDate.ToString(CultureInfo.InvariantCulture));

            DateTime lastLaunchDate;
            DateTime.TryParse(dateString, out lastLaunchDate);

            // If the current session was launched later than the last known session start date, then this must be 
            // a new session, and we can display the first-time prompt.
            if ((thisLaunchDate - lastLaunchDate).Seconds > 0)
            {
                EditorPrefsUtility.SetEditorPref(AssemblyReloadTimestampKey, dateString);
                return true;
            }

            return false;
        }
    }
}