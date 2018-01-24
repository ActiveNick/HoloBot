﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using HoloToolkit.Unity.SpatialMapping;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// The SpatialUnderstanding class controls the state and flow of the 
    /// scanning process used in the understanding module. 
    /// </summary>
    [RequireComponent(typeof(SpatialUnderstandingSourceMesh))]
    [RequireComponent(typeof(SpatialUnderstandingCustomMesh))]
    public class SpatialUnderstanding : Singleton<SpatialUnderstanding>
    {
        // Consts
        public const float ScanSearchDistance = 8.0f;

        // Enums
        public enum ScanStates
        {
            None,
            ReadyToScan,
            Scanning,
            Finishing,
            Done
        }

        // Config
        [Tooltip("If set to false, scanning will only begin after RequestBeginScanning is called")]
        public bool AutoBeginScanning = true;
        [Tooltip("Update period used during the scanning process (typically faster than after scanning is completed)")]
        public float UpdatePeriod_DuringScanning = 1.0f;
        [Tooltip("Update period used after the scanning process is completed")]
        public float UpdatePeriod_AfterScanning = 4.0f;

        // Properties
        /// <summary>
        /// Switch used by the entire SpatialUnderstanding module to activate processing.
        /// </summary>
        public bool AllowSpatialUnderstanding
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Reference to the SpatialUnderstandingDLL class (wraps the understanding DLL functions).
        /// </summary>
        public SpatialUnderstandingDll UnderstandingDLL { get; private set; }
        /// <summary>
        /// Reference to the SpatialUnderstandingSourceMesh behavior (input mesh data for the understanding module).
        /// </summary>
        public SpatialUnderstandingSourceMesh UnderstandingSourceMesh { get; private set; }
        /// <summary>
        /// Reference to the UnderstandingCustomMesh behavior (output mesh data from the understanding module).
        /// </summary>
        public SpatialUnderstandingCustomMesh UnderstandingCustomMesh { get; private set; }
        /// <summary>
        /// Indicates the current state of the scan process
        /// </summary>
        public ScanStates ScanState
        {
            get
            {
                return scanState;
            }
            private set
            {
                scanState = value;
                if (ScanStateChanged != null)
                {
                    ScanStateChanged();
                }

                // Update scan period, based on state
                SpatialMappingManager.Instance.GetComponent<SpatialMappingObserver>().TimeBetweenUpdates = (scanState == ScanStates.Done) ? UpdatePeriod_AfterScanning : UpdatePeriod_DuringScanning;
            }
        }
        /// <summary>
        /// Indicates the scanning statistics are still being processed.
        /// Request finish should not be called when this is true. 
        /// </summary>
        public bool ScanStatsReportStillWorking
        {
            get
            {
                if (AllowSpatialUnderstanding)
                {
                    SpatialUnderstandingDll.Imports.PlayspaceStats stats = UnderstandingDLL.GetStaticPlayspaceStats();
                    return (stats.IsWorkingOnStats != 0);
                }
                return false;
            }
        }

        public delegate void OnScanDoneDelegate();

        // Events
        /// <summary>
        /// Event indicating that the scan is done
        /// </summary>
        public event OnScanDoneDelegate OnScanDone;

        /// <summary>
        /// Event indicating that the scan state has changed
        /// </summary>
        public event Action ScanStateChanged;

        // Privates
        private ScanStates scanState;

        private float timeSinceLastUpdate = 0.0f;

        // Functions
        protected override void Awake()
        {
            base.Awake();

            // Cache references to required component
            UnderstandingDLL = new SpatialUnderstandingDll();
            UnderstandingSourceMesh = GetComponent<SpatialUnderstandingSourceMesh>();
            UnderstandingCustomMesh = GetComponent<SpatialUnderstandingCustomMesh>();
        }

        private void Start()
        {
            // Initialize the DLL
            if (AllowSpatialUnderstanding)
            {
                SpatialUnderstandingDll.Imports.SpatialUnderstanding_Init();
            }
        }

        private void Update()
        {
            if (!AllowSpatialUnderstanding)
            {
                return;
            }

            // Only update every few frames, and only if we aren't pulling in a mesh 
            // already.
            timeSinceLastUpdate += Time.deltaTime;
            if ((!UnderstandingCustomMesh.IsImportActive) &&
                (Time.frameCount % 3 == 0))
            {
                // Real-Time scan
                Update_Scan(timeSinceLastUpdate);
                timeSinceLastUpdate = 0;
            }
        }

        protected override void OnDestroy()
        {
            // Term the DLL
            if (AllowSpatialUnderstanding)
            {
                SpatialUnderstandingDll.Imports.SpatialUnderstanding_Term();
            }

            base.OnDestroy();
        }

        /// <summary>
        /// Call to request that scanning should begin. If AutoBeginScanning
        /// is false, this function should be used to initiate the scanning process.
        /// </summary>
        public void RequestBeginScanning()
        {
            if (ScanState == ScanStates.None)
            {
                ScanState = ScanStates.ReadyToScan;
            }
        }

        /// <summary>
        /// Call to request that the scanning progress be finishing. The application must do
        /// this to finalize the playspace. The scan state will not progress pass
        /// Scanning until this is called. The spatial understanding queries will not function
        /// until the playspace is finalized.
        /// </summary>
        public void RequestFinishScan()
        {
            if (AllowSpatialUnderstanding)
            {
                SpatialUnderstandingDll.Imports.GeneratePlayspace_RequestFinish();
                ScanState = ScanStates.Finishing;
            }
        }

        /// <summary>
        /// Update the scan progress. This function will initialize the scan, update it, 
        /// and issue a final mesh import, when the scan is complete.
        /// </summary>
        /// <param name="deltaTime">The amount of time that has passed since the last update (typically Time.deltaTime)</param>
        private void Update_Scan(float deltaTime)
        {
            // If we auto-start scanning, do it now
            if (AutoBeginScanning &&
                (ScanState == ScanStates.None))
            {
                RequestBeginScanning();
            }

            // Update the scan
            bool scanDone = false;
            if (((ScanState == ScanStates.ReadyToScan) ||
                 (ScanState == ScanStates.Scanning) ||
                 (ScanState == ScanStates.Finishing)) &&
                (AllowSpatialUnderstanding))
            {
                // Camera
                Transform cameraTransform = CameraCache.Main.transform;
                Vector3 camPos = cameraTransform.position;
                Vector3 camFwd = cameraTransform.forward;
                Vector3 camUp = cameraTransform.up;

                // If not yet initialized, do that now
                if (ScanState == ScanStates.ReadyToScan)
                {
                    SpatialUnderstandingDll.Imports.GeneratePlayspace_InitScan(
                        camPos.x, camPos.y, camPos.z,
                        camFwd.x, camFwd.y, camFwd.z,
                        camUp.x, camUp.y, camUp.z,
                        ScanSearchDistance, ScanSearchDistance);
                    ScanState = ScanStates.Scanning;
                }

                // Update
                int meshCount;
                IntPtr meshList;
                if (UnderstandingSourceMesh.GetInputMeshList(out meshCount, out meshList))
                {
                    var stopWatch = System.Diagnostics.Stopwatch.StartNew();

                    scanDone = SpatialUnderstandingDll.Imports.GeneratePlayspace_UpdateScan(
                        meshCount, meshList,
                        camPos.x, camPos.y, camPos.z,
                        camFwd.x, camFwd.y, camFwd.z,
                        camUp.x, camUp.y, camUp.z,
                        deltaTime) == 1;

                    stopWatch.Stop();

                    if (stopWatch.Elapsed.TotalMilliseconds > (1000.0 / 30.0))
                    {
                        Debug.LogWarningFormat("SpatialUnderstandingDll.Imports.GeneratePlayspace_UpdateScan took {0,9:N2} ms", stopWatch.Elapsed.TotalMilliseconds);
                    }
                }
            }

            // If it's done, finish up
            if ((ScanState == ScanStates.Finishing) &&
                (scanDone) &&
                (!UnderstandingCustomMesh.IsImportActive) &&
                (UnderstandingCustomMesh != null))
            {
                // Final mesh import
                StartCoroutine(UnderstandingCustomMesh.Import_UnderstandingMesh());

                // Mark it
                ScanState = ScanStates.Done;
                if (OnScanDone != null) OnScanDone.Invoke();
            }
        }
    }
}
