﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityPhysics = UnityEngine.Physics;

namespace Microsoft.MixedReality.Toolkit.UI
{
    public class BoundingBox : MonoBehaviour,
        IMixedRealityPointerHandler,
        IMixedRealitySourceStateHandler,
        IMixedRealityFocusChangedHandler,
        IMixedRealityFocusHandler
    {
        #region Enums
        /// <summary>
        /// Enum which describes how an object's boundingbox is to be flattened.
        /// </summary>
        public enum FlattenModeType
        {
            DoNotFlatten = 0,
            /// <summary>
            /// Flatten the X axis
            /// </summary>
            FlattenX,
            /// <summary>
            /// Flatten the Y axis
            /// </summary>
            FlattenY,
            /// <summary>
            /// Flatten the Z axis
            /// </summary>
            FlattenZ,
            /// <summary>
            /// Flatten the smallest relative axis if it falls below threshold
            /// </summary>
            FlattenAuto,
        }

        /// <summary>
        /// Enum which describes whether a boundingbox handle which has been grabbed, is 
        /// a Rotation Handle (sphere) or a Scale Handle( cube)
        /// </summary>
        private enum HandleType
        {
            None = 0,
            Rotation,
            Scale
        }

        /// <summary>
        /// This enum describes which primitive type the wireframe portion of the boundingbox
        /// consists of. 
        /// </summary>
        /// <remarks>
        /// Wireframe refers to the thin linkage between the handles. When the handles are invisible
        /// the wireframe looks like an outline box around an object.
        /// </remarks> 
        public enum WireframeType
        {
            Cubic = 0,
            Cylindrical
        }

        /// <summary>
        /// This enum defines which of the axes a given rotation handle revolves about.
        /// </summary>
        private enum CardinalAxisType
        {
            X = 0,
            Y,
            Z
        }

        /// <summary>
        /// This enum is used internally to define how an object's bounds are calculated in order to fit the boundingbox
        /// to it.
        /// </summary>
        private enum BoundsCalculationMethod
        {
            Collider = 0,
            Colliders,
            Renderers,
            MeshFilters
        }
        public enum BoundingBoxActivationType
        {
            ActivateOnStart = 0,
            ActivateByProximity,
            ActivateByPointer,
            ActivateByProximityAndPointer,
            ActivateManually
        }
        #endregion Enums

        #region Serialized Fields
        [SerializeField]
        [Tooltip("The object that the bounding box rig will be modifying.")]
        private GameObject targetObject;

        [Tooltip("For complex objects, automatic bounds calculation may not behave as expected. Use an existing Box Collider (even on a child object) to manually determine bounds of Bounding Box.")]
        [SerializeField]
        [FormerlySerializedAs("BoxColliderToUse")]
        private BoxCollider boundsOverride = null;

        [Header("Behavior")]
        [SerializeField]
        private BoundingBoxActivationType activation = BoundingBoxActivationType.ActivateManually;
        public BoundingBoxActivationType BoundingBoxActivation
        {
            get { return activation; }
            set
            {
                if (activation != value)
                {
                    activation = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Maximum scaling allowed relative to the initial size")]
        private float scaleMaximum = 2.0f;
        [SerializeField]
        [Tooltip("Minimum scaling allowed relative to the initial size")]
        private float scaleMinimum = 0.2f;

        /// <summary>
        /// Public property for the scale maximum, in the target's local scale.
        /// Set this value with SetScaleLimits.
        /// </summary>
        public float ScaleMaximum
        {
            get
            {
                return maximumScale != null ? maximumScale.x : scaleMaximum;
            }
        }

        /// <summary>
        /// Public property for the scale minimum, in the target's local scale.
        /// Set this value with SetScaleLimits.
        /// </summary>
        public float ScaleMinimum
        {
            get
            {
                return minimumScale != null ? minimumScale.x : scaleMinimum;
            }
        }

        [Header("Box Display")]
        [SerializeField]
        [Tooltip("Flatten bounds in the specified axis or flatten the smallest one if 'auto' is selected")]
        private FlattenModeType flattenAxis = FlattenModeType.DoNotFlatten;
        public FlattenModeType FlattenAxis
        {
            get { return flattenAxis; }
            set
            {
                if (flattenAxis != value)
                {
                    flattenAxis = value;
                    CreateRig();
                }
            }
        }
        [SerializeField]
        [FormerlySerializedAs("wireframePadding")]
        [Tooltip("Extra padding added to the actual Target bounds")]
        private Vector3 boxPadding = Vector3.zero;
        public Vector3 BoxPadding
        {
            get { return boxPadding; }
            set
            {
                if (Vector3.Distance(boxPadding, value) > float.Epsilon)
                {
                    boxPadding = value;
                    CreateRig();
                }
            }
        }
        [SerializeField]
        [Tooltip("Material used to display the bounding box. If set to null no bounding box will be displayed")]
        private Material boxMaterial = null;
        public Material BoxMaterial
        {
            get { return boxMaterial; }
            set
            {
                if (boxMaterial != value)
                {
                    boxMaterial = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Material used to display the bounding box when grabbed. If set to null no change will occur when grabbed.")]
        private Material boxGrabbedMaterial = null;

        public Material BoxGrabbedMaterial
        {
            get { return boxGrabbedMaterial; }
            set
            {
                if (boxGrabbedMaterial != value)
                {
                    boxGrabbedMaterial = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Show a wireframe around the bounding box when checked. Wireframe parameters below have no effect unless this is checked")]
        private bool showWireframe = true;

        public bool ShowWireFrame
        {
            get { return showWireframe; }
            set
            {
                if (showWireframe != value)
                {
                    showWireframe = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Shape used for wireframe display")]
        private WireframeType wireframeShape = WireframeType.Cubic;
        public WireframeType WireframeShape
        {
            get { return wireframeShape; }
            set
            {
                if (wireframeShape != value)
                {
                    wireframeShape = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Material used for wireframe display")]
        private Material wireframeMaterial;
        public Material WireframeMaterial
        {
            get { return wireframeMaterial; }
            set
            {
                if (wireframeMaterial != value)
                {
                    wireframeMaterial = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [FormerlySerializedAs("linkRadius")]
        [Tooltip("Radius for wireframe edges")]
        private float wireframeEdgeRadius = 0.005f;
        public float WireframeEdgeRadius
        {
            get { return wireframeEdgeRadius; }
            set
            {
                if (wireframeEdgeRadius != value)
                {
                    wireframeEdgeRadius = value;
                    CreateRig();
                }
            }
        }
        [Header("Handles")]
        [SerializeField]
        [Tooltip("Material applied to handles when they are not in a grabbed state")]
        private Material handleMaterial;
        public Material HandleMaterial
        {
            get { return handleMaterial; }
            set
            {
                if (handleMaterial != value)
                {
                    handleMaterial = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Material applied to handles while they are a grabbed")]
        private Material handleGrabbedMaterial;

        public Material HandleGrabbedMaterial
        {
            get { return handleGrabbedMaterial; }
            set
            {
                if (handleGrabbedMaterial != value)
                {
                    handleGrabbedMaterial = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Prefab used to display scale handles in corners. If not set, boxes will be displayed instead")]
        GameObject scaleHandlePrefab = null;

        public GameObject ScaleHandlePrefab
        {
            get { return scaleHandlePrefab; }
            set
            {
                if (scaleHandlePrefab != value)
                {
                    scaleHandlePrefab = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Prefab used to display scale handles in corners for 2D slate. If not set, boxes will be displayed instead")]
        GameObject scaleHandleSlatePrefab = null;

        public GameObject ScaleHandleSlatePrefab
        {
            get { return scaleHandleSlatePrefab; }
            set
            {
                if (scaleHandleSlatePrefab != value)
                {
                    scaleHandleSlatePrefab = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [FormerlySerializedAs("cornerRadius")]
        [Tooltip("Size of the cube collidable used in scale handles")]
        private float scaleHandleSize = 0.03f;

        public float ScaleHandleSize
        {
            get { return scaleHandleSize; }
            set
            {
                if (scaleHandleSize != value)
                {
                    scaleHandleSize = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Prefab used to display rotation handles in the midpoint of each edge. Aligns the Y axis of the prefab with the pivot axis, and the X and Z axes pointing outward. If not set, spheres will be displayed instead")]
        GameObject rotationHandlePrefab = null;
        public GameObject RotationHandleSlatePrefab
        {
            get { return rotationHandlePrefab; }
            set
            {
                if (rotationHandlePrefab != value)
                {
                    rotationHandlePrefab = value;
                    CreateRig();
                }
            }
        }
        [SerializeField]
        [FormerlySerializedAs("ballRadius")]
        [Tooltip("Radius of the sphere collidable used in rotation handles")]
        private float rotationHandleDiameter = 0.035f;
        public float RotationHandleDiameter
        {
            get { return rotationHandleDiameter; }
            set
            {
                if (rotationHandleDiameter != value)
                {
                    rotationHandleDiameter = value;
                    CreateRig();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to show scale handles")]
        private bool showScaleHandles = true;

        /// <summary>
        /// Public property to Set the visibility of the corner cube Scaling handles.
        /// This property can be set independent of the Rotate handles.
        /// </summary>
        public bool ShowScaleHandles
        {
            get
            {
                return showScaleHandles;
            }
            set
            {
                if (showScaleHandles != value)
                {
                    showScaleHandles = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to show rotation handles for the X axis")]
        private bool showRotationHandleForX = true;
        public bool ShowRotationHandleForX
        {
            get
            {
                return showRotationHandleForX;
            }
            set
            {
                if (showRotationHandleForX != value)
                {
                    showRotationHandleForX = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to show rotation handles for the Y axis")]
        private bool showRotationHandleForY = true;
        public bool ShowRotationHandleForY
        {
            get
            {
                return showRotationHandleForY;
            }
            set
            {
                if (showRotationHandleForY != value)
                {
                    showRotationHandleForY = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to show rotation handles for the Z axis")]
        private bool showRotationHandleForZ = true;
        public bool ShowRotationHandleForZ
        {
            get
            {
                return showRotationHandleForZ;
            }
            set
            {
                if (showRotationHandleForZ != value)
                {
                    showRotationHandleForZ = value;
                    ResetHandleVisibility();
                }
            }
        }

        [SerializeField]
        [Tooltip("Check to draw a tether point from the handles to the hand when manipulating.")]
        private bool drawTetherWhenManipulating = true;

        public bool DrawTetherWhenManipulating
        {
            get { return drawTetherWhenManipulating; }
            set { drawTetherWhenManipulating = value; }
        }

        [Header("Debug")]
        [Tooltip("Debug only. Component used to display debug messages")]
        public TextMesh debugText;

        [SerializeField]
        private bool hideElementsInInspector = true;
        public bool HideElementsInInspector
        {
            get { return hideElementsInInspector; }
            set
            {
                if (hideElementsInInspector != value)
                {
                    hideElementsInInspector = value;
                    UpdateRigVisibilityInInspector();
                }
            }
        }

        private void UpdateRigVisibilityInInspector()
        {
            HideFlags desiredFlags = hideElementsInInspector ? HideFlags.HideInHierarchy | HideFlags.HideInInspector : HideFlags.None;
            if (corners != null)
            {
                foreach (var cube in corners)
                {
                    cube.hideFlags = desiredFlags;
                }
            }

            if (boxDisplay != null)
            {
                boxDisplay.hideFlags = desiredFlags;
            }

            if (rigRoot != null)
            {
                rigRoot.hideFlags = desiredFlags;
            }

            if (links != null)
            {
                foreach (var link in links)
                {
                    link.hideFlags = desiredFlags;
                }
            }
        }

        [Header("Events")]
        public UnityEvent RotateStarted;
        public UnityEvent RotateStopped;
        public UnityEvent ScaleStarted;
        public UnityEvent ScaleStopped;
        #endregion Serialized Fields

        #region Private Properties

        // Whether we should be displaying just the wireframe (if enabled) or the handles too
        private bool wireframeOnly = false;

        // Pointer that is being used to manipulate the bounding box
        private IMixedRealityPointer currentPointer;

        private Transform rigRoot;

        // Game object used to display the bounding box. Parented to the rig root
        private GameObject boxDisplay;

        private BoxCollider cachedTargetCollider;
        private Vector3[] boundsCorners;

        // Half the size of the current bounds
        private Vector3 currentBoundsExtents;

        private BoundsCalculationMethod boundsMethod;



        private List<IMixedRealityInputSource> touchingSources = new List<IMixedRealityInputSource>();
        private List<Transform> links;
        private List<Transform> corners;
        private List<Transform> balls;
        private List<Renderer> linkRenderers;
        private List<IMixedRealityController> sourcesDetected;
        private Vector3[] edgeCenters;

        // Current axis of rotation about the center of the rig root
        private Vector3 currentRotationAxis;

        // Scale of the target at the beginning of the current manipulation
        private Vector3 initialScaleOnGrabStart;
        // Position of the target at the beginning of the current manipulation
        private Vector3 initialPositionOnGrabStart;
        // Point that was initially grabbed in OnPointerDown()
        private Vector3 initialGrabPoint;
        // Current position of the grab point
        private Vector3 currentGrabPoint;


        // Scale of the target at startup (in Start())
        private Vector3 initialScaleAtStart;
        private Vector3 maximumScale;
        private Vector3 minimumScale;


        // Grab point position in pointer space. Used to calculate the current grab point from the current pointer pose.
        private Vector3 grabPointInPointer;

        private CardinalAxisType[] edgeAxes;
        private int[] flattenedHandles;

        // Corner opposite to the grabbed one. Scaling will be relative to it.
        private Vector3 oppositeCorner;

        // Direction of the diagonal from the opposite corner to the grabbed one.
        private Vector3 diagonalDir;

        private HandleType currentHandleType;
        private Vector3 lastBounds;

        // TODO Review this, it feels like we should be using Behaviour.enabled instead.
        private bool active = false;
        public bool Active
        {
            get
            {
                return active;
            }
            set
            {
                if (active != value)
                {
                    active = value;
                    rigRoot?.gameObject.SetActive(value);
                    ResetHandleVisibility();
                }
            }
        }

        public GameObject Target
        {
            get
            {
                if (targetObject == null)
                {
                    targetObject = gameObject;
                }

                return targetObject;
            }
        }

        public BoxCollider TargetBounds
        {
            get { return cachedTargetCollider; }
        }

        // True if this game object is a child of the Target one
        private bool isChildOfTarget = false;
        private static readonly string rigRootName = "rigRoot";

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        /// Allows to manually enable wire (edge) highlighting (edges) of the bounding box.
        /// This is useful if connected to the Manipulation events of a
        /// <see cref="Microsoft.MixedReality.Toolkit.UI.ManipulationHandler"/> 
        /// when used in conjunction with this MonoBehavior.
        /// </summary>
        public void HighlightWires()
        {
            SetHighlighted(null);
        }

        public void UnhighlightWires()
        {
            ResetHandleVisibility();
        }

        /// <summary>
        /// Sets the minimum/maximum scale for the bounding box at runtime.
        /// </summary>
        /// <param name="min">Minimum scale</param>
        /// <param name="max">Maximum scale</param>
        /// <param name="relativeToInitialState">If true the values will be multiplied by scale of target at startup. If false they will be in absolute local scale.</param>
        public void SetScaleLimits(float min, float max, bool relativeToInitialState = true)
        {
            scaleMaximum = max;
            scaleMinimum = min;

            // Update the absolute min/max
            var target = Target;
            if (target != null)
            {
                if (relativeToInitialState)
                {
                    maximumScale = initialScaleAtStart * scaleMaximum;
                    minimumScale = initialScaleAtStart * scaleMinimum;
                }
                else
                {
                    maximumScale = new Vector3(scaleMaximum, scaleMaximum, scaleMaximum);
                    minimumScale = new Vector3(scaleMinimum, scaleMinimum, scaleMinimum);
                }
            }
        }

        /// <summary>
        /// Destroys and re-creates the rig around the bounding box
        /// </summary>
        public void CreateRig()
        {
            DestroyRig();
            SetMaterials();
            InitializeDataStructures();
            SetBoundingBoxCollider();
            UpdateBounds();
            AddCorners();
            AddLinks();
            AddBoxDisplay();
            UpdateRigHandles();
            Flatten();
            ResetHandleVisibility();
            rigRoot.gameObject.SetActive(active);
            UpdateRigVisibilityInInspector();
        }
        #endregion

        #region MonoBehaviour Methods
        private void Start()
        {
            CreateRig();
            CaptureInitialState();

            if (activation == BoundingBoxActivationType.ActivateByProximityAndPointer ||
                activation == BoundingBoxActivationType.ActivateByProximity ||
                activation == BoundingBoxActivationType.ActivateByPointer)
            {
                wireframeOnly = true;
                Active = true;
            }
            else if (activation == BoundingBoxActivationType.ActivateOnStart)
            {
                Active = true;
            }
        }

        private void Update()
        {
            if (currentPointer != null)
            {
                TransformTarget();
                UpdateBounds();
                UpdateRigHandles();
            }
            else if (!isChildOfTarget && Target.transform.hasChanged)
            {
                UpdateBounds();
                UpdateRigHandles();
                Target.transform.hasChanged = false;
            }
        }
        #endregion MonoBehaviour Methods

        #region Private Methods

        private void DestroyRig()
        {
            if (boundsOverride == null)
            {
                Destroy(cachedTargetCollider);
            }
            else
            {
                boundsOverride.size -= boxPadding;

                if (cachedTargetCollider != null)
                {
                    if (cachedTargetCollider.gameObject.GetComponent<NearInteractionGrabbable>())
                    {
                        Destroy(cachedTargetCollider.gameObject.GetComponent<NearInteractionGrabbable>());
                    }
                }
            }

            if (balls != null)
            {
                foreach (Transform transform in balls)
                {
                    Destroy(transform.gameObject);
                }
                balls.Clear();
                balls = null;
            }

            if (links != null)
            {
                foreach (Transform transform in links)
                {
                    Destroy(transform.gameObject);
                }
                links.Clear();
                links = null;
            }

            if (corners != null)
            {
                foreach (Transform transform in corners)
                {
                    Destroy(transform.gameObject);
                }
                corners.Clear();
                corners = null;
            }

            if (rigRoot != null)
            {
                Destroy(rigRoot.gameObject);
                rigRoot = null;
            }
        }

        private void TransformTarget()
        {
            if (currentHandleType != HandleType.None)
            {
                Vector3 prevGrabPoint = currentGrabPoint;
                currentGrabPoint = (currentPointer.Rotation * grabPointInPointer) + currentPointer.Position;

                if (currentHandleType == HandleType.Rotation)
                {
                    Vector3 prevDir = Vector3.ProjectOnPlane(prevGrabPoint - rigRoot.transform.position, currentRotationAxis).normalized;
                    Vector3 currentDir = Vector3.ProjectOnPlane(currentGrabPoint - rigRoot.transform.position, currentRotationAxis).normalized;
                    Quaternion q = Quaternion.FromToRotation(prevDir, currentDir);
                    Vector3 axis;
                    float angle;
                    q.ToAngleAxis(out angle, out axis);
                    Target.transform.RotateAround(rigRoot.transform.position, axis, angle);
                }
                else if (currentHandleType == HandleType.Scale)
                {
                    float initialDist = Vector3.Dot(initialGrabPoint - oppositeCorner, diagonalDir);
                    float currentDist = Vector3.Dot(currentGrabPoint - oppositeCorner, diagonalDir);
                    float scaleFactor = 1 + (currentDist - initialDist) / initialDist;

                    Vector3 newScale = initialScaleOnGrabStart * scaleFactor;
                    Vector3 clampedScale = ClampScale(newScale);
                    if (clampedScale != newScale)
                    {
                        scaleFactor = clampedScale[0] / initialScaleOnGrabStart[0];
                    }

                    Target.transform.localScale = clampedScale;
                    Target.transform.position = initialPositionOnGrabStart * scaleFactor + (1 - scaleFactor) * oppositeCorner;
                }
            }
        }

        private Vector3 GetRotationAxis(Transform handle)
        {
            for (int i = 0; i < balls.Count; ++i)
            {
                if (handle == balls[i])
                {
                    if (edgeAxes[i] == CardinalAxisType.X)
                    {
                        return rigRoot.transform.right;
                    }
                    else if (edgeAxes[i] == CardinalAxisType.Y)
                    {
                        return rigRoot.transform.up;
                    }
                    else
                    {
                        return rigRoot.transform.forward;
                    }
                }
            }

            return Vector3.zero;
        }

        private void AddCorners()
        {
            bool isFlattened = (flattenAxis != FlattenModeType.DoNotFlatten);

            // Flattened but missing custom 2D handle prefab OR Not flattened but missing custom 3D handle prefab.
            if ((isFlattened && (scaleHandleSlatePrefab == null)) || (scaleHandlePrefab == null))
            {
                // Use default HoloLens v1 cube style handles
                for (int i = 0; i < boundsCorners.Length; ++i)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = "corner_" + i.ToString();

                    cube.transform.localScale = new Vector3(scaleHandleSize, scaleHandleSize, scaleHandleSize);
                    cube.transform.position = boundsCorners[i];

                    // In order for the cube to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    var g = cube.EnsureComponent<NearInteractionGrabbable>();
                    g.ShowTetherWhenManipulating = drawTetherWhenManipulating;

                    cube.transform.parent = rigRoot.transform;

                    Renderer renderer = cube.GetComponent<Renderer>();

                    BoxCollider collider = cube.GetComponent<BoxCollider>();
                    collider.size *= 1.35f;
                    corners.Add(cube.transform);

                    if (handleMaterial != null)
                    {
                        renderer.material = handleMaterial;
                    }
                }
            }
            else
            {
                // Use custom prefab for the handles
                for (int i = 0; i < boundsCorners.Length; ++i)
                {
                    GameObject corner = new GameObject();
                    corner.name = "corner_" + i.ToString();
                    corner.transform.parent = rigRoot.transform;
                    corner.transform.localPosition = boundsCorners[i];

                    BoxCollider collider = corner.AddComponent<BoxCollider>();
                    collider.size = scaleHandleSize * Vector3.one;

                    // In order for the corner to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    var g = corner.EnsureComponent<NearInteractionGrabbable>();
                    g.ShowTetherWhenManipulating = drawTetherWhenManipulating;

                    GameObject visualsScale = new GameObject();
                    visualsScale.name = "visualsScale";
                    visualsScale.transform.parent = corner.transform;
                    visualsScale.transform.localPosition = Vector3.zero;

                    // Compute mirroring scale
                    {
                        Vector3 p = boundsCorners[i];
                        visualsScale.transform.localScale = new Vector3(Mathf.Sign(p[0]), Mathf.Sign(p[1]), Mathf.Sign(p[2]));
                    }

                    // Instantiate proper prefab based on isFlattened. (2D slate handle vs 3D handle)
                    GameObject cornerVisuals = Instantiate(isFlattened ? scaleHandleSlatePrefab : scaleHandlePrefab, visualsScale.transform);
                    cornerVisuals.name = "visuals";

                    // this is the size of the corner visuals
                    var cornerbounds = GetMaxBounds(cornerVisuals);
                    // we need to multiply by this amount to get to desired scale handle size
                    var invScale = scaleHandleSize / cornerbounds.size.x;
                    cornerVisuals.transform.localScale = new Vector3(invScale, invScale, invScale);

                    if (isFlattened)
                    {
                        // Rotate 2D slate handle asset for proper orientation
                        cornerVisuals.transform.Rotate(0, 0, -90);
                    }

                    ApplyMaterialToAllRenderers(cornerVisuals, handleMaterial);


                    corners.Add(corner.transform);
                }
            }
        }

        private Bounds GetMaxBounds(GameObject g)
        {
            var b = new Bounds();
            foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            {
                if (b.size == Vector3.zero)
                {
                    b = r.bounds;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }
            return b;
        }

        private void AddLinks()
        {
            edgeCenters = new Vector3[12];

            CalculateEdgeCenters();

            edgeAxes = new CardinalAxisType[12];
            edgeAxes[0] = CardinalAxisType.X;
            edgeAxes[1] = CardinalAxisType.Y;
            edgeAxes[2] = CardinalAxisType.X;
            edgeAxes[3] = CardinalAxisType.Y;
            edgeAxes[4] = CardinalAxisType.X;
            edgeAxes[5] = CardinalAxisType.Y;
            edgeAxes[6] = CardinalAxisType.X;
            edgeAxes[7] = CardinalAxisType.Y;
            edgeAxes[8] = CardinalAxisType.Z;
            edgeAxes[9] = CardinalAxisType.Z;
            edgeAxes[10] = CardinalAxisType.Z;
            edgeAxes[11] = CardinalAxisType.Z;

            if (rotationHandlePrefab == null)
            {
                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    ball.name = "midpoint_" + i.ToString();

                    ball.transform.localScale = new Vector3(rotationHandleDiameter, rotationHandleDiameter, rotationHandleDiameter);
                    ball.transform.position = edgeCenters[i];
                    ball.transform.parent = rigRoot.transform;

                    // In order for the ball to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    var g = ball.EnsureComponent<NearInteractionGrabbable>();
                    g.ShowTetherWhenManipulating = drawTetherWhenManipulating;

                    SphereCollider collider = ball.GetComponent<SphereCollider>();
                    collider.radius *= 1.2f;
                    balls.Add(ball.transform);

                    if (handleMaterial != null)
                    {
                        Renderer renderer = ball.GetComponent<Renderer>();
                        renderer.material = handleMaterial;
                    }
                }
            }
            else
            {
                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    GameObject ball = Instantiate(rotationHandlePrefab, rigRoot.transform);
                    ball.name = "midpoint_" + i.ToString();
                    ball.transform.localPosition = edgeCenters[i];

                    SphereCollider collider = ball.AddComponent<SphereCollider>();
                    collider.radius = 0.5f * rotationHandleDiameter;

                    // In order for the ball to be grabbed using near interaction we need
                    // to add NearInteractionGrabbable;
                    var g = ball.EnsureComponent<NearInteractionGrabbable>();
                    g.ShowTetherWhenManipulating = drawTetherWhenManipulating;

                    ApplyMaterialToAllRenderers(ball, handleMaterial);

                    balls.Add(ball.transform);
                }
            }

            // Aligns each rotation handle with the Y axis along the edge and the X and Z axis pointing
            // out from the bounding box.
            // Y axis of the prefab will point toward the positive direction of the pivot axis.
            balls[0].localRotation = Quaternion.Euler(90, 90, 0) * balls[0].localRotation;
            balls[1].localRotation = Quaternion.Euler(0, 180, 0) * balls[1].localRotation;
            balls[2].localRotation = Quaternion.Euler(0, 180, 90) * balls[2].localRotation;
            balls[3].localRotation = Quaternion.Euler(0, 90, 0) * balls[3].localRotation;
            balls[4].localRotation = Quaternion.Euler(0, 0, -90) * balls[4].localRotation;
            balls[5].localRotation = Quaternion.Euler(0, -90, 0) * balls[5].localRotation;
            balls[6].localRotation = Quaternion.Euler(-90, 0, -90) * balls[6].localRotation;
            balls[7].localRotation = Quaternion.Euler(0, 0, 0) * balls[7].localRotation;
            balls[8].localRotation = Quaternion.Euler(180, 90, 90) * balls[8].localRotation;
            balls[9].localRotation = Quaternion.Euler(90, 0, 0) * balls[9].localRotation;
            balls[10].localRotation = Quaternion.Euler(-90, -90, -90) * balls[10].localRotation;
            balls[11].localRotation = Quaternion.Euler(180, -90, -90) * balls[11].localRotation;

            if (links != null)
            {
                GameObject link;
                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    if (wireframeShape == WireframeType.Cubic)
                    {
                        link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(link.GetComponent<BoxCollider>());
                    }
                    else
                    {
                        link = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        Destroy(link.GetComponent<CapsuleCollider>());
                    }
                    link.name = "link_" + i.ToString();


                    Vector3 linkDimensions = GetLinkDimensions();
                    if (edgeAxes[i] == CardinalAxisType.Y)
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.y, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(0.0f, 90.0f, 0.0f));
                    }
                    else if (edgeAxes[i] == CardinalAxisType.Z)
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.z, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(90.0f, 0.0f, 0.0f));
                    }
                    else//X
                    {
                        link.transform.localScale = new Vector3(wireframeEdgeRadius, linkDimensions.x, wireframeEdgeRadius);
                        link.transform.Rotate(new Vector3(0.0f, 0.0f, 90.0f));
                    }

                    link.transform.position = edgeCenters[i];
                    link.transform.parent = rigRoot.transform;
                    Renderer linkRenderer = link.GetComponent<Renderer>();
                    linkRenderers.Add(linkRenderer);

                    if (wireframeMaterial != null)
                    {
                        linkRenderer.material = wireframeMaterial;
                    }

                    links.Add(link.transform);
                }
            }
        }

        private void AddBoxDisplay()
        {
            if (boxMaterial != null)
            {
                boxDisplay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(boxDisplay.GetComponent<BoxCollider>());
                boxDisplay.name = "bounding box";

                ApplyMaterialToAllRenderers(boxDisplay, boxMaterial);

                boxDisplay.transform.localScale = 2.0f * currentBoundsExtents;
                boxDisplay.transform.parent = rigRoot.transform;


            }
        }

        private void SetBoundingBoxCollider()
        {
            // Make sure that the bounds of all child objects are up to date before we compute bounds
            UnityEngine.Physics.SyncTransforms();

            //Collider.bounds is world space bounding volume.
            //Mesh.bounds is local space bounding volume
            //Renderer.bounds is the same as mesh.bounds but in world space coords

            if (boundsOverride != null)
            {
                cachedTargetCollider = boundsOverride;
                cachedTargetCollider.transform.hasChanged = true;
            }
            else
            {
                Bounds bounds = GetTargetBounds();
                cachedTargetCollider = Target.AddComponent<BoxCollider>();
                if (boundsMethod == BoundsCalculationMethod.Renderers)
                {
                    cachedTargetCollider.center = bounds.center;
                    cachedTargetCollider.size = bounds.size;
                }
                else if (boundsMethod == BoundsCalculationMethod.Colliders)
                {
                    // bounds.center is in world space, but cachedTargetCollider.center is in local space
                    cachedTargetCollider.center = Target.transform.InverseTransformPoint(bounds.center);
                    cachedTargetCollider.size = Target.transform.InverseTransformSize(bounds.size);
                }
            }

            Vector3 scale = cachedTargetCollider.transform.lossyScale;
            Vector3 invScale = new Vector3(1.0f / scale[0], 1.0f / scale[1], 1.0f / scale[2]);
            cachedTargetCollider.size += Vector3.Scale(boxPadding, invScale);

            cachedTargetCollider.EnsureComponent<NearInteractionGrabbable>();
        }

        private Bounds GetTargetBounds()
        {
            Bounds bounds = new Bounds();

            List<Transform> toExplore = new List<Transform>();
            for (int i = 0; i < Target.transform.childCount; i++)
            {
                var child = Target.transform.GetChild(i);
                if (!child.name.Equals(rigRootName))
                {
                    toExplore.Add(child);
                }
            }
            if (toExplore.Count == 0)
            {
                bounds = GetSingleObjectBounds(Target);
                boundsMethod = BoundsCalculationMethod.Collider;
                return bounds;
            }
            else
            {
                for (int i = 0; i < toExplore.Count; ++i)
                {
                    var child = toExplore[i];
                    if (bounds.size == Vector3.zero)
                    {
                        bounds = GetSingleObjectBounds(child.gameObject);
                    }
                    else
                    {
                        Bounds childBounds = GetSingleObjectBounds(child.gameObject);
                        if (childBounds.size != Vector3.zero)
                        {
                            bounds.Encapsulate(childBounds);
                        }
                    }
                }

                if (bounds.size != Vector3.zero)
                {
                    boundsMethod = BoundsCalculationMethod.Colliders;
                    return bounds;
                }
            }

            //simple case: sum of existing colliders
            Collider[] colliders = Target.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                //Collider.bounds is in world space.
                bounds = colliders[0].bounds;
                for (int i = 0; i < colliders.Length; ++i)
                {
                    Bounds colliderBounds = colliders[i].bounds;
                    if (colliderBounds.size != Vector3.zero)
                    {
                        bounds.Encapsulate(colliderBounds);
                    }
                }
                if (bounds.size != Vector3.zero)
                {
                    boundsMethod = BoundsCalculationMethod.Colliders;
                    return bounds;
                }
            }

            //Renderer bounds is local. Requires transform to global coordinate system.
            Renderer[] childRenderers = Target.GetComponentsInChildren<Renderer>();
            if (childRenderers.Length > 0)
            {
                bounds = new Bounds();
                bounds = childRenderers[0].bounds;
                for (int i = 0; i < childRenderers.Length; ++i)
                {
                    bounds.Encapsulate(childRenderers[i].bounds);
                }

                GetCornerPositionsFromBounds(bounds, ref boundsCorners);
                for (int c = 0; c < boundsCorners.Length; ++c)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = c.ToString();
                    cube.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                    cube.transform.position = boundsCorners[c];
                }

                boundsMethod = BoundsCalculationMethod.Renderers;
                return bounds;
            }

            MeshFilter[] meshFilters = Target.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length > 0)
            {
                //Mesh.bounds is local space bounding volume
                bounds.size = meshFilters[0].mesh.bounds.size;
                bounds.center = meshFilters[0].mesh.bounds.center;
                for (int i = 0; i < meshFilters.Length; ++i)
                {
                    bounds.Encapsulate(meshFilters[i].mesh.bounds);
                }
                if (bounds.size != Vector3.zero)
                {
                    bounds.center = Target.transform.position;
                    boundsMethod = BoundsCalculationMethod.MeshFilters;
                    return bounds;
                }
            }

            BoxCollider boxCollider = Target.AddComponent<BoxCollider>();
            bounds = boxCollider.bounds;
            Destroy(boxCollider);
            boundsMethod = BoundsCalculationMethod.Collider;
            return bounds;
        }

        private Bounds GetSingleObjectBounds(GameObject gameObject)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            BoxCollider boxCollider;
            boxCollider = gameObject.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                bounds = boxCollider.bounds;
                DestroyImmediate(boxCollider);
            }
            else
            {
                bounds = boxCollider.bounds;
            }

            return bounds;
        }
        private void SetMaterials()
        {
            //ensure materials
            if (wireframeMaterial == null)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

                wireframeMaterial = new Material(shader);
                wireframeMaterial.EnableKeyword("_InnerGlow");
                wireframeMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
                wireframeMaterial.SetFloat("_InnerGlow", 1.0f);
                wireframeMaterial.SetFloatArray("_InnerGlowColor", color);
            }
            if (handleMaterial == null && handleMaterial != wireframeMaterial)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

                handleMaterial = new Material(shader);
                handleMaterial.EnableKeyword("_InnerGlow");
                handleMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
                handleMaterial.SetFloat("_InnerGlow", 1.0f);
                handleMaterial.SetFloatArray("_InnerGlowColor", color);
            }
            if (handleGrabbedMaterial == null && handleGrabbedMaterial != handleMaterial && handleGrabbedMaterial != wireframeMaterial)
            {
                float[] color = { 1.0f, 1.0f, 1.0f, 0.75f };

                Shader shader = Shader.Find("Mixed Reality Toolkit/Standard");

                handleGrabbedMaterial = new Material(shader);
                handleGrabbedMaterial.EnableKeyword("_InnerGlow");
                handleGrabbedMaterial.SetColor("_Color", new Color(0.0f, 0.63f, 1.0f));
                handleGrabbedMaterial.SetFloat("_InnerGlow", 1.0f);
                handleGrabbedMaterial.SetFloatArray("_InnerGlowColor", color);
            }
        }
        private void InitializeDataStructures()
        {
            rigRoot = new GameObject(rigRootName).transform;
            rigRoot.parent = transform;


            boundsCorners = new Vector3[8];

            corners = new List<Transform>();
            balls = new List<Transform>();

            if (showWireframe)
            {
                links = new List<Transform>();
                linkRenderers = new List<Renderer>();
            }

            sourcesDetected = new List<IMixedRealityController>();
        }
        private void CalculateEdgeCenters()
        {
            if (boundsCorners != null && edgeCenters != null)
            {
                edgeCenters[0] = (boundsCorners[0] + boundsCorners[1]) * 0.5f;
                edgeCenters[1] = (boundsCorners[0] + boundsCorners[2]) * 0.5f;
                edgeCenters[2] = (boundsCorners[3] + boundsCorners[2]) * 0.5f;
                edgeCenters[3] = (boundsCorners[3] + boundsCorners[1]) * 0.5f;

                edgeCenters[4] = (boundsCorners[4] + boundsCorners[5]) * 0.5f;
                edgeCenters[5] = (boundsCorners[4] + boundsCorners[6]) * 0.5f;
                edgeCenters[6] = (boundsCorners[7] + boundsCorners[6]) * 0.5f;
                edgeCenters[7] = (boundsCorners[7] + boundsCorners[5]) * 0.5f;

                edgeCenters[8] = (boundsCorners[0] + boundsCorners[4]) * 0.5f;
                edgeCenters[9] = (boundsCorners[1] + boundsCorners[5]) * 0.5f;
                edgeCenters[10] = (boundsCorners[2] + boundsCorners[6]) * 0.5f;
                edgeCenters[11] = (boundsCorners[3] + boundsCorners[7]) * 0.5f;
            }
        }
        private void CaptureInitialState()
        {
            var target = Target;
            if (target != null)
            {
                initialScaleAtStart = target.transform.localScale;

                maximumScale = initialScaleAtStart * scaleMaximum;
                minimumScale = initialScaleAtStart * scaleMinimum;
                isChildOfTarget = transform.IsChildOf(target.transform);
            }
        }

        private Vector3 ClampScale(Vector3 scale)
        {
            if (Vector3.Min(maximumScale, scale) != scale)
            {
                float maxRatio = 0.0f;
                int maxIdx = -1;

                // Find out the component with the maximum ratio to its maximum allowed value
                for (int i = 0; i < 3; ++i)
                {
                    if (maximumScale[i] > 0)
                    {
                        float ratio = scale[i] / maximumScale[i];
                        if (ratio > maxRatio)
                        {
                            maxRatio = ratio;
                            maxIdx = i;
                        }
                    }
                }

                if (maxIdx != -1)
                {
                    scale /= maxRatio;
                }
            }

            if (Vector3.Max(minimumScale, scale) != scale)
            {
                float minRatio = 1.0f;
                int minIdx = -1;

                // Find out the component with the minimum ratio to its minimum allowed value
                for (int i = 0; i < 3; ++i)
                {
                    if (minimumScale[i] > 0)
                    {
                        float ratio = scale[i] / minimumScale[i];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            minIdx = i;
                        }
                    }
                }

                if (minIdx != -1)
                {
                    scale /= minRatio;
                }
            }

            return scale;
        }

        private Vector3 GetLinkDimensions()
        {
            float linkLengthAdjustor = wireframeShape == WireframeType.Cubic ? 2.0f : 1.0f - (6.0f * wireframeEdgeRadius);
            return (currentBoundsExtents * linkLengthAdjustor) + new Vector3(wireframeEdgeRadius, wireframeEdgeRadius, wireframeEdgeRadius);
        }

        private bool ShouldRotateHandleBeVisible(CardinalAxisType axisType)
        {
            return
                (axisType == CardinalAxisType.X && showRotationHandleForX) ||
                (axisType == CardinalAxisType.Y && showRotationHandleForY) ||
                (axisType == CardinalAxisType.Z && showRotationHandleForZ);
        }

        private void ResetHandleVisibility()
        {
            if (currentPointer != null)
            {
                return;
            }

            bool isVisible;

            //set balls visibility
            if (balls != null)
            {
                isVisible = (active == true && wireframeOnly == false);
                for (int i = 0; i < balls.Count; ++i)
                {
                    balls[i].gameObject.SetActive(isVisible && ShouldRotateHandleBeVisible(edgeAxes[i]));
                    ApplyMaterialToAllRenderers(balls[i].gameObject, handleMaterial);
                }
            }

            //set link visibility
            if (links != null)
            {
                isVisible = active == true;
                for (int i = 0; i < linkRenderers.Count; ++i)
                {
                    if (linkRenderers[i] != null)
                    {
                        linkRenderers[i].enabled = isVisible;
                    }
                }
            }

            //set box display visibility
            if (boxDisplay != null)
            {
                boxDisplay.SetActive(active);
                ApplyMaterialToAllRenderers(boxDisplay, boxMaterial);
            }

            //set corner visibility
            if (corners != null)
            {
                isVisible = (active == true && wireframeOnly == false && showScaleHandles == true);

                for (int i = 0; i < corners.Count; ++i)
                {
                    corners[i].gameObject.SetActive(isVisible);
                    ApplyMaterialToAllRenderers(corners[i].gameObject, handleMaterial);
                }
            }

            SetHiddenHandles();
        }
        private void SetHighlighted(Transform activeHandle)
        {
            //turn off all balls
            if (balls != null)
            {
                for (int i = 0; i < balls.Count; ++i)
                {
                    if (balls[i] != activeHandle)
                    {
                        balls[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        ApplyMaterialToAllRenderers(balls[i].gameObject, handleGrabbedMaterial);
                    }
                }
            }

            //turn off all corners
            if (corners != null)
            {
                for (int i = 0; i < corners.Count; ++i)
                {
                    if (corners[i] != activeHandle)
                    {
                        corners[i].gameObject.SetActive(false);
                    }
                    else
                    {
                        ApplyMaterialToAllRenderers(corners[i].gameObject, handleGrabbedMaterial);
                    }
                }
            }

            //update the box material to the grabbed material
            if (boxDisplay != null)
            {
                ApplyMaterialToAllRenderers(boxDisplay, boxGrabbedMaterial);
            }
        }

        private void UpdateBounds()
        {
            if (cachedTargetCollider != null)
            {
                // Store current rotation then zero out the rotation so that the bounds
                // are computed when the object is in its 'axis aligned orientation'.
                Quaternion currentRotation = Target.transform.rotation;
                Target.transform.rotation = Quaternion.identity;
                UnityPhysics.SyncTransforms(); // Update collider bounds

                Vector3 boundsExtents = cachedTargetCollider.bounds.extents;

                // After bounds are computed, restore rotation...
                Target.transform.rotation = currentRotation;
                UnityPhysics.SyncTransforms();

                if (boundsExtents != Vector3.zero)
                {
                    if (flattenAxis == FlattenModeType.FlattenAuto)
                    {
                        float min = Mathf.Min(boundsExtents.x, Mathf.Min(boundsExtents.y, boundsExtents.z));
                        flattenAxis = (min == boundsExtents.x) ? FlattenModeType.FlattenX :
                            ((min == boundsExtents.y) ? FlattenModeType.FlattenY : FlattenModeType.FlattenZ);
                    }

                    boundsExtents.x = (flattenAxis == FlattenModeType.FlattenX) ? 0.0f : boundsExtents.x;
                    boundsExtents.y = (flattenAxis == FlattenModeType.FlattenY) ? 0.0f : boundsExtents.y;
                    boundsExtents.z = (flattenAxis == FlattenModeType.FlattenZ) ? 0.0f : boundsExtents.z;
                    currentBoundsExtents = boundsExtents;

                    GetCornerPositionsFromBounds(new Bounds(Vector3.zero, boundsExtents * 2.0f), ref boundsCorners);
                    CalculateEdgeCenters();
                }
            }
        }

        private void UpdateRigHandles()
        {
            if (rigRoot != null && Target != null)
            {
                // We move the rigRoot to the scene root to ensure that non-uniform scaling performed
                // anywhere above the rigRoot does not impact the position of rig corners / edges
                rigRoot.parent = null;

                rigRoot.rotation = Quaternion.identity;
                rigRoot.position = Vector3.zero;

                for (int i = 0; i < corners.Count; ++i)
                {
                    corners[i].position = boundsCorners[i];
                }

                Vector3 rootScale = rigRoot.lossyScale;
                Vector3 invRootScale = new Vector3(1.0f / rootScale[0], 1.0f / rootScale[1], 1.0f / rootScale[2]);

                // Compute the local scale that produces the desired world space dimensions
                Vector3 linkDimensions = Vector3.Scale(GetLinkDimensions(), invRootScale);

                for (int i = 0; i < edgeCenters.Length; ++i)
                {
                    balls[i].position = edgeCenters[i];

                    if (links != null)
                    {
                        links[i].position = edgeCenters[i];

                        if (edgeAxes[i] == CardinalAxisType.X)
                        {
                            links[i].localScale = new Vector3(wireframeEdgeRadius, linkDimensions.x, wireframeEdgeRadius);
                        }
                        else if (edgeAxes[i] == CardinalAxisType.Y)
                        {
                            links[i].localScale = new Vector3(wireframeEdgeRadius, linkDimensions.y, wireframeEdgeRadius);
                        }
                        else//Z
                        {
                            links[i].localScale = new Vector3(wireframeEdgeRadius, linkDimensions.z, wireframeEdgeRadius);
                        }
                    }
                }

                if (boxDisplay != null)
                {
                    // Compute the local scale that produces the desired world space size
                    boxDisplay.transform.localScale = Vector3.Scale(2.0f * currentBoundsExtents, invRootScale);
                }

                //move rig into position and rotation
                rigRoot.position = cachedTargetCollider.bounds.center;
                rigRoot.rotation = Target.transform.rotation;

                rigRoot.parent = transform;
            }
        }
        private HandleType GetHandleType(Transform handle)
        {
            for (int i = 0; i < balls.Count; ++i)
            {
                if (handle == balls[i])
                {
                    return HandleType.Rotation;
                }
            }
            for (int i = 0; i < corners.Count; ++i)
            {
                if (handle == corners[i])
                {
                    return HandleType.Scale;
                }
            }

            return HandleType.None;
        }

        private void Flatten()
        {
            if (flattenAxis == FlattenModeType.FlattenX)
            {
                flattenedHandles = new int[] { 0, 4, 2, 6 };
            }
            else if (flattenAxis == FlattenModeType.FlattenY)
            {
                flattenedHandles = new int[] { 1, 3, 5, 7 };
            }
            else if (flattenAxis == FlattenModeType.FlattenZ)
            {
                flattenedHandles = new int[] { 9, 10, 8, 11 };
            }

            if (flattenedHandles != null && linkRenderers != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    linkRenderers[flattenedHandles[i]].enabled = false;
                }
            }
        }

        private void SetHiddenHandles()
        {
            if (flattenedHandles != null)
            {
                for (int i = 0; i < flattenedHandles.Length; ++i)
                {
                    balls[flattenedHandles[i]].gameObject.SetActive(false);
                }
            }
        }

        private void GetCornerPositionsFromBounds(Bounds bounds, ref Vector3[] positions)
        {
            int numCorners = 1 << 3;
            if (positions == null || positions.Length != numCorners)
            {
                positions = new Vector3[numCorners];
            }

            // Permutate all axes using minCorner and maxCorner.
            Vector3 minCorner = bounds.center - bounds.extents;
            Vector3 maxCorner = bounds.center + bounds.extents;
            for (int c = 0; c < numCorners; c++)
            {
                positions[c] = new Vector3(
                    (c & (1 << 0)) == 0 ? minCorner[0] : maxCorner[0],
                    (c & (1 << 1)) == 0 ? minCorner[1] : maxCorner[1],
                    (c & (1 << 2)) == 0 ? minCorner[2] : maxCorner[2]);
            }
        }

        private static void ApplyMaterialToAllRenderers(GameObject root, Material material)
        {
            if (material != null)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();

                for (int i = 0; i < renderers.Length; ++i)
                {
                    renderers[i].material = material;
                }
            }
        }

        private bool DoesActivationMatchFocus(FocusEventData eventData)
        {
            switch (activation)
            {
                case BoundingBoxActivationType.ActivateOnStart:
                case BoundingBoxActivationType.ActivateManually:
                    return false;
                case BoundingBoxActivationType.ActivateByProximity:
                    return eventData.Pointer is IMixedRealityNearPointer;
                case BoundingBoxActivationType.ActivateByPointer:
                    return eventData.Pointer is IMixedRealityPointer;
                case BoundingBoxActivationType.ActivateByProximityAndPointer:
                    return true;
                default:
                    return false;
            }
        }
        #endregion Private Methods

        #region Used Event Handlers

        void IMixedRealityFocusChangedHandler.OnFocusChanged(FocusEventData eventData)
        {
            if (activation == BoundingBoxActivationType.ActivateManually || activation == BoundingBoxActivationType.ActivateOnStart)
            {
                return;
            }

            if (!DoesActivationMatchFocus(eventData))
            {
                return;
            }

            bool handInProximity = eventData.NewFocusedObject != null && eventData.NewFocusedObject.transform.IsChildOf(transform);
            if (handInProximity == wireframeOnly)
            {
                wireframeOnly = !handInProximity;
                ResetHandleVisibility();
            }
        }

        void IMixedRealityFocusHandler.OnFocusExit(FocusEventData eventData)
        {
            if (currentPointer != null && eventData.Pointer == currentPointer)
            {
                DropController();
            }
        }

        void IMixedRealityFocusHandler.OnFocusEnter(FocusEventData eventData)
        {
        }

        void IMixedRealityPointerHandler.OnPointerUp(MixedRealityPointerEventData eventData)
        {
            if (currentPointer != null && eventData.Pointer == currentPointer)
            {
                DropController();

                eventData.Use();
            }
        }

        void DropController()
        {
            HandleType lastHandleType = currentHandleType;
            currentPointer = null;
            currentHandleType = HandleType.None;
            ResetHandleVisibility();

            if (lastHandleType == HandleType.Scale)
            {
                if (debugText != null) debugText.text = "OnPointerUp:ScaleStopped";
                ScaleStopped?.Invoke();
            }
            else if (lastHandleType == HandleType.Rotation)
            {
                if (debugText != null) debugText.text = "OnPointerUp:RotateStopped";
                RotateStopped?.Invoke();
            }
        }

        void IMixedRealityPointerHandler.OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (currentPointer == null && !eventData.used)
            {
                GameObject grabbedHandle = eventData.Pointer.Result.CurrentPointerTarget;
                Transform grabbedHandleTransform = grabbedHandle.transform;
                currentHandleType = GetHandleType(grabbedHandleTransform);

                if (currentHandleType != HandleType.None)
                {
                    currentPointer = eventData.Pointer;
                    initialGrabPoint = currentPointer.Result.Details.Point;
                    currentGrabPoint = initialGrabPoint;
                    initialScaleOnGrabStart = Target.transform.localScale;
                    initialPositionOnGrabStart = Target.transform.position;
                    grabPointInPointer = Quaternion.Inverse(eventData.Pointer.Rotation) * (initialGrabPoint - currentPointer.Position);

                    SetHighlighted(grabbedHandleTransform);

                    if (currentHandleType == HandleType.Scale)
                    {
                        // Will use this to scale the target relative to the opposite corner
                        oppositeCorner = rigRoot.transform.TransformPoint(-grabbedHandle.transform.localPosition);
                        diagonalDir = (grabbedHandle.transform.position - oppositeCorner).normalized;

                        ScaleStarted?.Invoke();

                        if (debugText != null)
                        {
                            debugText.text = "OnPointerDown:ScaleStarted";
                        }
                    }
                    else if (currentHandleType == HandleType.Rotation)
                    {
                        currentRotationAxis = GetRotationAxis(grabbedHandleTransform);

                        RotateStarted?.Invoke();

                        if (debugText != null)
                        {
                            debugText.text = "OnPointerDown:RotateStarted";
                        }
                    }

                    eventData.Use();
                }
            }

            if (currentPointer != null)
            {
                // Always mark the pointer data as used to prevent any other behavior to handle pointer events
                // as long as BoundingBox manipulation is active.
                // This is due to us reacting to both "Select" and "Grip" events.
                eventData.Use();
            }
        }

        void IMixedRealityPointerHandler.OnPointerDragged(MixedRealityPointerEventData eventData) { }

        public void OnSourceDetected(SourceStateEventData eventData)
        {
            if (eventData.Controller != null)
            {
                if (sourcesDetected.Count == 0 || sourcesDetected.Contains(eventData.Controller) == false)
                {
                    sourcesDetected.Add(eventData.Controller);
                }
            }
        }

        public void OnSourceLost(SourceStateEventData eventData)
        {
            sourcesDetected.Remove(eventData.Controller);

            if (currentPointer != null && currentPointer.InputSourceParent.SourceId == eventData.SourceId)
            {
                HandleType lastHandleType = currentHandleType;

                currentPointer = null;
                currentHandleType = HandleType.None;
                ResetHandleVisibility();

                if (lastHandleType == HandleType.Scale)
                {
                    if (debugText != null) debugText.text = "OnSourceLost:ScaleStopped";
                    ScaleStopped?.Invoke();
                }
                else if (lastHandleType == HandleType.Rotation)
                {
                    if (debugText != null) debugText.text = "OnSourceLost:RotateStopped";
                    RotateStopped?.Invoke();
                }
            }
        }
        #endregion Used Event Handlers

        #region Unused Event Handlers
        void IMixedRealityPointerHandler.OnPointerClicked(MixedRealityPointerEventData eventData) { }

        void IMixedRealityFocusChangedHandler.OnBeforeFocusChange(FocusEventData eventData) { }
        #endregion Unused Event Handlers
    }
}
