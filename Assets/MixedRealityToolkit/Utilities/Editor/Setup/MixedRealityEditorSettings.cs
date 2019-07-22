﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities.Editor
{
    /// <summary>
    /// Sets Force Text Serialization and visible meta files in all projects that use the Mixed Reality Toolkit.
    /// </summary>
    [InitializeOnLoad]
    public class MixedRealityEditorSettings : IActiveBuildTargetChanged
    {
        public MixedRealityEditorSettings()
        {
            callbackOrder = 0;
        }

        private const string IgnoreKey = "_MixedRealityToolkit_Editor_IgnoreSettingsPrompts";
        private const string SessionKey = "_MixedRealityToolkit_Editor_ShownSettingsPrompts";

        [Obsolete("Use the 'MixedRealityToolkitFiles' APIs.")]
        public static string MixedRealityToolkit_AbsoluteFolderPath
        {
            get
            {
                if (MixedRealityToolkitFiles.AreFoldersAvailable)
                {
#if UNITY_EDITOR
                    if (MixedRealityToolkitFiles.MRTKDirectories.Count() > 1)
                    {
                        Debug.LogError($"A deprecated API '{nameof(MixedRealityEditorSettings)}.{nameof(MixedRealityToolkit_AbsoluteFolderPath)}' " +
                            "is being used, and there are more than one MRTK directory in the project; most likely due to ingestion as NuGet. " +
                            $"Update to use the '{nameof(MixedRealityToolkitFiles)}' APIs.");
                    }
#endif

                    return MixedRealityToolkitFiles.MRTKDirectories.First();
                }

                Debug.LogError("Unable to find the Mixed Reality Toolkit's directory!");
                return null;
            }
        }

        [Obsolete("Use the 'MixedRealityToolkitFiles' APIs.")]
        public static string MixedRealityToolkit_RelativeFolderPath
        {
            get { return MixedRealityToolkitFiles.GetAssetDatabasePath(MixedRealityToolkit_AbsoluteFolderPath); }
        }

        static MixedRealityEditorSettings()
        {
            if (!IsNewSession || Application.isPlaying)
            {
                return;
            }

            bool refresh = false;
            bool restart = false;

            var ignoreSettings = EditorPrefs.GetBool(IgnoreKey, false);

            if (!ignoreSettings)
            {
                var message = "The Mixed Reality Toolkit needs to apply the following settings to your project:\n\n";

                var forceTextSerialization = EditorSettings.serializationMode == SerializationMode.ForceText;

                if (!forceTextSerialization)
                {
                    message += "- Force Text Serialization\n";
                }

                var visibleMetaFiles = EditorSettings.externalVersionControl.Equals("Visible Meta Files");

                if (!visibleMetaFiles)
                {
                    message += "- Visible meta files\n";
                }

                if (!PlayerSettings.virtualRealitySupported)
                {
                    message += "- Enable XR Settings for your current platform\n";
                }

                message += "\nWould you like to make this change?";

                if (!forceTextSerialization || !visibleMetaFiles || !PlayerSettings.virtualRealitySupported)
                {
                    var choice = EditorUtility.DisplayDialogComplex("Apply Mixed Reality Toolkit Default Settings?", message, "Apply", "Ignore", "Later");

                    switch (choice)
                    {
                        case 0:
                            EditorSettings.serializationMode = SerializationMode.ForceText;
                            EditorSettings.externalVersionControl = "Visible Meta Files";
                            PlayerSettings.virtualRealitySupported = true;
                            refresh = true;
                            break;
                        case 1:
                            EditorPrefs.SetBool(IgnoreKey, true);
                            break;
                        case 2:
                            break;
                    }
                }
            }

            if (PlayerSettings.scriptingRuntimeVersion != ScriptingRuntimeVersion.Latest)
            {
                PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
                restart = true;
            }

            if (refresh || restart)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            if (restart)
            {
                EditorApplication.OpenProject(Directory.GetParent(Application.dataPath).ToString());
            }
        }

        /// <summary>
        /// Returns true the first time it is called within this editor session, and false for all subsequent calls.
        /// </summary>
        /// <remarks>A new session is also true if the editor build target group is changed.</remarks>
        private static bool IsNewSession
        {
            get
            {
                if (SessionState.GetBool(SessionKey, false)) { return false; }

                SessionState.SetBool(SessionKey, true);
                return true;
            }
        }

        /// <summary>
        /// Finds the path of a directory relative to the project folder.
        /// </summary>
        /// <param name="directoryPathToSearch">
        /// The subtree's root path to search in.
        /// </param>
        /// <param name="directoryName">
        /// The name of the directory to search for.
        /// </param>
        /// <param name="path"></param>
        internal static bool FindRelativeDirectory(string directoryPathToSearch, string directoryName, out string path)
        {
            string absolutePath;
            if (FindDirectory(directoryPathToSearch, directoryName, out absolutePath))
            {
                path = MixedRealityToolkitFiles.GetAssetDatabasePath(absolutePath);
                return true;
            }

            path = string.Empty;
            return false;
        }

        /// <summary>
        /// Finds the absolute path of a directory.
        /// </summary>
        /// <param name="directoryPathToSearch">
        /// The subtree's root path to search in.
        /// </param>
        /// <param name="directoryName">
        /// The name of the directory to search for.
        /// </param>
        /// <param name="path"></param>
        internal static bool FindDirectory(string directoryPathToSearch, string directoryName, out string path)
        {
            path = string.Empty;

            var directories = Directory.GetDirectories(directoryPathToSearch);

            for (int i = 0; i < directories.Length; i++)
            {
                var name = Path.GetFileName(directories[i]);

                if (name != null && name.Equals(directoryName))
                {
                    path = directories[i];
                    return true;
                }

                if (FindDirectory(directories[i], directoryName, out path))
                {
                    return true;
                }
            }

            return false;
        }

        [Obsolete("Use MixedRealityToolkitFiles.GetAssetDatabasePath instead.")]
        internal static string MakePathRelativeToProject(string absolutePath) => MixedRealityToolkitFiles.GetAssetDatabasePath(absolutePath);

        private static void SetIconTheme()
        {
            if (!MixedRealityToolkitFiles.AreFoldersAvailable)
            {
                Debug.LogError("Unable to find the Mixed Reality Toolkit's directory!");
                return;
            }

            var icons = MixedRealityToolkitFiles.GetFiles("StandardAssets/Icons");
            var icon = new Texture2D(2, 2);
            var iconColor = new Color32(4, 165, 240, 255);

            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = icons[i].Replace("/", "\\");
                if (icons[i].Contains(".meta")) { continue; }

                var imageData = File.ReadAllBytes(icons[i]);
                icon.LoadImage(imageData, false);

                var pixels = icon.GetPixels32();
                for (int j = 0; j < pixels.Length; j++)
                {
                    pixels[j].r = iconColor.r;
                    pixels[j].g = iconColor.g;
                    pixels[j].b = iconColor.b;
                }

                icon.SetPixels32(pixels);
                File.WriteAllBytes(icons[i], icon.EncodeToPNG());
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        /// <inheritdoc />
        public int callbackOrder { get; private set; }

        /// <inheritdoc />
        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            SessionState.SetBool(SessionKey, false);
        }
    }
}
