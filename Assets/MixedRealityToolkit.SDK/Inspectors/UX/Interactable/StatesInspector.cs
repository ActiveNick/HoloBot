﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities.Editor;
using System;
using UnityEditor;
using UnityEngine;


namespace Microsoft.MixedReality.Toolkit.UI
{
#if UNITY_EDITOR
    [CustomEditor(typeof(States))]
    public class StatesInspector : UnityEditor.Editor
    {
        protected States instance;
        protected SerializedProperty stateList;

        // List of interactable states.
        protected InteractableTypesContainer stateOptions;
        
        // indent tracker
        protected static int indentOnSectionStart = 0;


        protected virtual void OnEnable()
        {
            instance = (States)target;
            
            stateList = serializedObject.FindProperty("StateList");
            instance.SetupStateOptions();
        }

        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();
            serializedObject.Update();

            InspectorUIUtility.DrawTitle("States");
            InspectorUIUtility.DrawNotice("Manage state configurations to drive Interactables or Transitions");

            // get the list of options and InteractableStates
            stateOptions = instance.StateOptions;
            
            SerializedProperty stateLogicName = serializedObject.FindProperty("StateLogicName");
            SerializedProperty assemblyQualifiedName  = serializedObject.FindProperty("AssemblyQualifiedName");
            int option = States.ReverseLookup(stateLogicName.stringValue, stateOptions.ClassNames);

            int newLogic = EditorGUILayout.Popup("State Model", option, stateOptions.ClassNames);
            if (option != newLogic)
            {
                stateLogicName.stringValue = stateOptions.ClassNames[newLogic];
                assemblyQualifiedName.stringValue = stateOptions.AssemblyQualifiedNames[newLogic];
            }

            stateList = serializedObject.FindProperty("StateList");

            int bitCount = 0;
            for (int i = 0; i < stateList.arraySize; i++)
            {
                if (i == 0)
                {
                    bitCount += 1;
                }
                else
                {
                    bitCount += bitCount;
                }

                EditorGUILayout.BeginVertical("Box");
                SerializedProperty stateItem = stateList.GetArrayElementAtIndex(i);

                SerializedProperty name = stateItem.FindPropertyRelative("Name");
                SerializedProperty activeIndex = stateItem.FindPropertyRelative("ActiveIndex");
                SerializedProperty bit = stateItem.FindPropertyRelative("Bit");
                SerializedProperty index = stateItem.FindPropertyRelative("Index");
                
                activeIndex.intValue = i;
                
                EditorGUILayout.BeginHorizontal();
                string[] stateEnums = GetStateOptions();
                int enumIndex = States.ReverseLookup(name.stringValue, stateEnums);

                int newEnumIndex = EditorGUILayout.Popup(name.stringValue + " (" + bitCount + ")", enumIndex, stateEnums);
                
                name.stringValue = stateEnums[newEnumIndex];
                index.intValue = newEnumIndex;

                InspectorUIUtility.SmallButton(new GUIContent(InspectorUIUtility.Minus, "Remove State"), i, RemoveState);
                
                EditorGUILayout.EndHorizontal();

                // assign the bitcount based on location in the list
                bit.intValue = bitCount;
                
                EditorGUILayout.EndVertical();
            }

            InspectorUIUtility.FlexButton(new GUIContent("+", "Add Theme Property"), 0, AddState);

            serializedObject.ApplyModifiedProperties();
        }

        protected void AddState(int index, SerializedProperty prop = null)
        {
            stateList.InsertArrayElementAtIndex(stateList.arraySize);
        }

        protected void RemoveState(int index, SerializedProperty prop = null)
        {
            stateList.DeleteArrayElementAtIndex(index);
        }

        /// <summary>
        /// Get a list of state names
        /// </summary>
        /// <returns></returns>
        protected string[] GetStateOptions()
        {
            return Enum.GetNames(typeof(InteractableStates.InteractableStateEnum));
        }
    }
#endif
}
