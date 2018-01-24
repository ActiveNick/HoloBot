﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoloToolkit.Unity.InputModule
{
    /// <summary>
    /// Xbox Controller support.
    /// <remarks>Only supports one connected device at a time.</remarks>
    /// <remarks>Make sure to enable the <see cref="HumanInterfaceDevice"/> capability before using when targeting HoloLens device.</remarks>
    /// </summary>
    public class XboxControllerInputSource : GamePadInputSource
    {
        [Serializable]
        private class MappingEntry
        {
            public XboxControllerMappingTypes Type = XboxControllerMappingTypes.None;
            public string Value = string.Empty;
        }

        private readonly Dictionary<uint, XboxControllerData> gamePadInputDatas = new Dictionary<uint, XboxControllerData>(0);

        private XboxControllerData controllerData;

        public XboxControllerMappingTypes HorizontalAxis { get { return horizontalAxis; } }
        public XboxControllerMappingTypes VerticalAxis { get { return verticalAxis; } }
        public XboxControllerMappingTypes CancelButton { get { return cancelButton; } }
        public XboxControllerMappingTypes SubmitButton { get { return submitButton; } }

        [SerializeField]
        private XboxControllerMappingTypes horizontalAxis = XboxControllerMappingTypes.XboxDpadHorizontal;

        [SerializeField]
        private XboxControllerMappingTypes verticalAxis = XboxControllerMappingTypes.XboxDpadVertical;

        [SerializeField]
        private XboxControllerMappingTypes submitButton = XboxControllerMappingTypes.XboxA;

        [SerializeField]
        private XboxControllerMappingTypes cancelButton = XboxControllerMappingTypes.XboxB;

        [SerializeField]
        private MappingEntry[] mapping;

        private int motionControllerCount = 0;

        protected override void Awake()
        {
            base.Awake();

            if (mapping != null)
            {
                for (var i = 0; i < Enum.GetNames(typeof(XboxControllerMappingTypes)).Length; i++)
                {
                    XboxControllerMapping.SetMapping((XboxControllerMappingTypes)i, mapping[i].Value);
                }
            }

            PreviousForceActiveState = InputModule.forceModuleActive;
            PreviousHorizontalAxis = InputModule.horizontalAxis;
            PreviousVerticalAxis = InputModule.verticalAxis;
            PreviousSubmitButton = InputModule.submitButton;
            PreviousCancelButton = InputModule.cancelButton;
        }

        protected override void Update()
        {
            base.Update();

            // We will only register the first device we find.  Input is taken from joystick 1.
            // If we have motion controllers connected we will not process Xbox controller input.
            if (gamePadInputDatas.Count != 1 || motionControllerCount > 0) { return; }

            controllerData.XboxLeftStickHorizontalAxis = Input.GetAxis(XboxControllerMapping.XboxLeftStickHorizontal);
            controllerData.XboxLeftStickVerticalAxis = Input.GetAxis(XboxControllerMapping.XboxLeftStickVertical);
            controllerData.XboxRightStickHorizontalAxis = Input.GetAxis(XboxControllerMapping.XboxRightStickHorizontal);
            controllerData.XboxRightStickVerticalAxis = Input.GetAxis(XboxControllerMapping.XboxRightStickVertical);
            controllerData.XboxDpadHorizontalAxis = Input.GetAxis(XboxControllerMapping.XboxDpadHorizontal);
            controllerData.XboxDpadVerticalAxis = Input.GetAxis(XboxControllerMapping.XboxDpadVertical);
            controllerData.XboxLeftTriggerAxis = Input.GetAxis(XboxControllerMapping.XboxLeftTrigger);
            controllerData.XboxRightTriggerAxis = Input.GetAxis(XboxControllerMapping.XboxRightTrigger);
            controllerData.XboxSharedTriggerAxis = Input.GetAxis(XboxControllerMapping.XboxSharedTrigger);

            controllerData.XboxA_Down = Input.GetButtonDown(XboxControllerMapping.XboxA);
            controllerData.XboxB_Down = Input.GetButtonDown(XboxControllerMapping.XboxB);
            controllerData.XboxX_Down = Input.GetButtonDown(XboxControllerMapping.XboxX);
            controllerData.XboxY_Down = Input.GetButtonDown(XboxControllerMapping.XboxY);
            controllerData.XboxView_Down = Input.GetButtonDown(XboxControllerMapping.XboxView);
            controllerData.XboxMenu_Down = Input.GetButtonDown(XboxControllerMapping.XboxMenu);
            controllerData.XboxLeftBumper_Down = Input.GetButtonDown(XboxControllerMapping.XboxLeftBumper);
            controllerData.XboxRightBumper_Down = Input.GetButtonDown(XboxControllerMapping.XboxRightBumper);
            controllerData.XboxLeftStick_Down = Input.GetButtonDown(XboxControllerMapping.XboxLeftStickClick);
            controllerData.XboxRightStick_Down = Input.GetButtonDown(XboxControllerMapping.XboxRightStickClick);

            controllerData.XboxA_Pressed = Input.GetButton(XboxControllerMapping.XboxA);
            controllerData.XboxB_Pressed = Input.GetButton(XboxControllerMapping.XboxB);
            controllerData.XboxX_Pressed = Input.GetButton(XboxControllerMapping.XboxX);
            controllerData.XboxY_Pressed = Input.GetButton(XboxControllerMapping.XboxY);
            controllerData.XboxView_Pressed = Input.GetButton(XboxControllerMapping.XboxView);
            controllerData.XboxMenu_Pressed = Input.GetButton(XboxControllerMapping.XboxMenu);
            controllerData.XboxLeftBumper_Pressed = Input.GetButton(XboxControllerMapping.XboxLeftBumper);
            controllerData.XboxRightBumper_Pressed = Input.GetButton(XboxControllerMapping.XboxRightBumper);
            controllerData.XboxLeftStick_Pressed = Input.GetButton(XboxControllerMapping.XboxLeftStickClick);
            controllerData.XboxRightStick_Pressed = Input.GetButton(XboxControllerMapping.XboxRightStickClick);

            controllerData.XboxA_Up = Input.GetButtonUp(XboxControllerMapping.XboxA);
            controllerData.XboxB_Up = Input.GetButtonUp(XboxControllerMapping.XboxB);
            controllerData.XboxX_Up = Input.GetButtonUp(XboxControllerMapping.XboxX);
            controllerData.XboxY_Up = Input.GetButtonUp(XboxControllerMapping.XboxY);
            controllerData.XboxView_Up = Input.GetButtonUp(XboxControllerMapping.XboxView);
            controllerData.XboxMenu_Up = Input.GetButtonUp(XboxControllerMapping.XboxMenu);
            controllerData.XboxLeftBumper_Up = Input.GetButtonUp(XboxControllerMapping.XboxLeftBumper);
            controllerData.XboxRightBumper_Up = Input.GetButtonUp(XboxControllerMapping.XboxRightBumper);
            controllerData.XboxLeftStick_Up = Input.GetButtonUp(XboxControllerMapping.XboxLeftStickClick);
            controllerData.XboxRightStick_Up = Input.GetButtonUp(XboxControllerMapping.XboxRightStickClick);

            InputManager.Instance.RaiseXboxInputUpdate(this, SourceId, controllerData);
        }

        protected override void RefreshDevices()
        {
            var joystickNames = Input.GetJoystickNames();

            if (joystickNames.Length <= 0) { return; }

            bool devicesChanged = LastDeviceList == null;

            if (LastDeviceList != null && joystickNames.Length == LastDeviceList.Length)
            {
                for (int i = 0; i < LastDeviceList.Length; i++)
                {
                    if (!joystickNames[i].Equals(LastDeviceList[i]))
                    {
                        devicesChanged = true;
                        if (LastDeviceList == null)
                        {
                            LastDeviceList = joystickNames;
                        }
                    }
                }
            }

            if (LastDeviceList != null && devicesChanged)
            {
                foreach (var gamePadInputSource in gamePadInputDatas)
                {
                    InputManager.Instance.RaiseSourceLost(this, gamePadInputSource.Key);
                }

                gamePadInputDatas.Clear();

                if (gamePadInputDatas.Count == 0)
                {
                    // Reset our input module to it's previous state.
                    InputModule.forceModuleActive = PreviousForceActiveState;
                    InputModule.verticalAxis = PreviousVerticalAxis;
                    InputModule.horizontalAxis = PreviousHorizontalAxis;
                    InputModule.submitButton = PreviousSubmitButton;
                    InputModule.cancelButton = PreviousCancelButton;
                }
            }

            motionControllerCount = 0;

            for (var i = 0; i < joystickNames.Length; i++)
            {
                if (joystickNames[i].Contains(MotionControllerLeft) ||
                    joystickNames[i].Contains(MotionControllerRight))
                {
                    // If we don't have any matching joystick types, continue.
                    // If we have motion controllers connected we override the xbox input.
                    motionControllerCount++;
                    continue;
                }

                if (joystickNames[i].Contains(XboxController) ||
                    joystickNames[i].Contains(XboxOneForWindows) ||
                    joystickNames[i].Contains(XboxBluetoothGamePad) ||
                    joystickNames[i].Contains(XboxWirelessController))
                {
                    // We will only register the first device we find.  Input is taken from all joysticks.
                    if (gamePadInputDatas.Count != 0) { return; }

                    SourceId = (uint)i;
                    controllerData = new XboxControllerData { GamePadName = joystickNames[i] };
                    gamePadInputDatas.Add(SourceId, controllerData);

                    InputManager.Instance.RaiseSourceDetected(this, SourceId);

                    // Setup the Input Module to use our custom axis settings.
                    InputModule.forceModuleActive = true;

                    if (verticalAxis != XboxControllerMappingTypes.None)
                    {
                        InputModule.verticalAxis = XboxControllerMapping.GetMapping(verticalAxis);
                    }

                    if (horizontalAxis != XboxControllerMappingTypes.None)
                    {
                        InputModule.horizontalAxis = XboxControllerMapping.GetMapping(horizontalAxis);
                    }

                    if (submitButton != XboxControllerMappingTypes.None)
                    {
                        InputModule.submitButton = XboxControllerMapping.GetMapping(submitButton);
                    }

                    if (cancelButton != XboxControllerMappingTypes.None)
                    {
                        InputModule.cancelButton = XboxControllerMapping.GetMapping(cancelButton);
                    }
                }
            }

            LastDeviceList = joystickNames;
            LastDeviceUpdateCount = joystickNames.Length;
        }
    }
}
