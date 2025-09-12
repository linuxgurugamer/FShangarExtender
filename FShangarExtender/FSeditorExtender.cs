using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolbarControl_NS;
using UnityEngine;

namespace FShangarExtender
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    class FSeditorExtender : MonoBehaviour
    {

        private Camera sceneCamera;
        private List<VABCamera> _vabCameras = new List<VABCamera>();
        private List<SPHCamera> _sphCameras = new List<SPHCamera>();
        private List<Light> _sceneLights = new List<Light>();
        private List<Node> _sceneNodes = new List<Node>();
        private List<Node> _hangarNodes = new List<Node>();
        private List<Node> _nonScalingNodes = new List<Node>();
        private Transform _tempParent;
        private Vector3 _originalConstructionBoundExtends;
        private Vector3 _originalCameraOffsetBoundExtends;
        private bool _sceneScaled = false;
        private static float _scalingFactor = Constants.defaultScaleFactor;
        //private static bool _hideHangars = false;
        private static bool _advancedDebug = false;
        private bool _hangarExtenderReady = false;
        //private ApplicationLauncherButton _toolbarButton;
        static ToolbarControl toolbarControl;
        private static Texture2D _shrinkIcon;
        private static Texture2D _extendIcon;
        private bool _isFirstUpdate;


        private class Node
        {
            public Transform transform;
            public Transform originalParent;
            public Vector3 defaultScaling;
        }


        /// <summary>
        /// method to check a NodeList for a certain contained Transform
        /// </summary>
        /// <param name="nodeList"></param>
        /// <param name="controlObject"></param>
        /// <returns></returns>
        private static bool doesListContain(List<Node> nodeList, Transform controlObject)
        {
            if (nodeList.Count > 0)
            {
                foreach (Node n in nodeList)
                {
                    if (n.transform == controlObject)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// default mono awake method which is called very first of the class
        /// </summary>
        public void Awake()
        {
        }


        /// <summary>
        /// default mono start method which is called each time the thing is started
        /// </summary>
        public void Start()
        {
            if (_sceneScaled)
            {
                StartCoroutine(toggleScaling());
            }
            resetMod();
            StartCoroutine(initFSHangarExtender());
            loadToolbarButton();
            EditorLogic.fetch.switchEditorBtn.onClick.AddListener(() => { switchEditorBtn(); });

        }

        void switchEditorBtn()
        {
            _sceneNodes.Clear();
            _hangarNodes.Clear();
            _nonScalingNodes.Clear();
            _sceneLights.Clear();
            _vabCameras.Clear();
            _sphCameras.Clear();
            _sceneScaled = false;
            _hangarExtenderReady = false;
            _isFirstUpdate = true;
            StartCoroutine(initFSHangarExtender(2f));
        }

        static KeyCode _lastKeyPressed = KeyCode.None;
        /// <summary>
        /// default mono update method which is called once each frame
        /// </summary>
        public void Update()
        {
            if (_hangarExtenderReady)
            {
                if (Event.current.isKey)
                    _lastKeyPressed = Event.current.keyCode;

                bool gotKeyPress = rescaleKeyPressed();
                if ((_isFirstUpdate && HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().BuildingStartMaxSize) || gotKeyPress)
                {
                    Debugger.advancedDebug("Scaling Hangar Geometry", _advancedDebug);
                    StartCoroutine(toggleScaling());
                }
                _isFirstUpdate = false;
            }
        }
#if false
		void OnGUI()
        {
            //if (toolbarControl != null)
            //    toolbarControl.UseBlizzy(HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().useBlizzy);
        }
#endif

        /// <summary>
        /// default mono OnDestroy when the object gets removed from scene
        /// </summary>
        public void OnDestroy()
        {
            if (_sceneScaled)
            {
                StartCoroutine(toggleScaling());
            }
            resetMod();
            Debugger.advancedDebug("cleared the lists", _advancedDebug);
        }


        public void prepareTransforms()
        {
            List<Transform> tList = new List<Transform>();
            foreach (Node n in _hangarNodes)
            {
                tList.Add(n.transform);
            }
            deStaticObjectList(tList);
            tList.Clear();

            foreach (Node n in _sceneNodes)
            {
                tList.Add(n.transform);
            }
            deStaticObjectList(tList);
            tList.Clear();

        }


        public void deStaticObjectList(List<Transform> list)
        {
            foreach (Transform t in list)
            {
                Transform[] childs = t.GetComponentsInChildren<Transform>();
                foreach (Transform c in childs)
                {
                    c.gameObject.isStatic = false;
                }
            }
        }


        private void updateMeshes()
        {
            if (_hangarNodes != null && _hangarNodes.Count > 0)
            {
                foreach (Node n in _hangarNodes)
                {
                    n.transform.gameObject.isStatic = false;
                    List<MeshFilter> listedMeshFilters = new List<MeshFilter>();
                    n.transform.GetComponentsInChildren<MeshFilter>(listedMeshFilters);
                    foreach (MeshFilter mf in listedMeshFilters)
                    {
                        Mesh mesh = mf.mesh;
                        mesh.MarkDynamic();
                        Vector3[] verticies = mesh.vertices;

                        for (int i = 0; i < verticies.Length; i++)
                        {
                            Vector3 v = verticies[i];
                            Vector3 target = new Vector3(v.x * _scalingFactor, v.y * _scalingFactor, v.z * _scalingFactor);
                            verticies[i] = target;
                        }
                        mf.mesh.UploadMeshData(false);
                    }
                }
            }
        }



        //int scaler;
        //public void OnGUI()
        //{
        //	if (_extraScaleNodes != null && _extraScaleNodes.Count > 0)
        //	{
        //		Debugger.advancedDebug("Pre - " + _extraScaleNodes[0].transform.localScale.x + " - " + _extraScaleNodes[0].transform.localScale.y + " - " + _extraScaleNodes[0].transform.localScale.z, true);
        //		scaler = (int)GUI.HorizontalSlider(new Rect(500, 500, 500, 50), scaler, 1, 5);
        //		if (Input.GetKey(KeyCode.Return))
        //		{
        //			_extraScaleNodes[0].transform.localScale = new Vector3(scaler, scaler, scaler);
        //		}
        //		Debugger.advancedDebug("Post - " + _extraScaleNodes[0].transform.localScale.x + " - " + _extraScaleNodes[0].transform.localScale.y + " - " + _extraScaleNodes[0].transform.localScale.z, true);
        //	}
        //}


        /// <summary>
        /// public method to load the icons into the stock toolbar button.
        /// </summary>
        public void loadToolbarButton()
        {
            if (_extendIcon == null)
            {
                _extendIcon = GameDatabase.Instance.GetTexture(Constants.extentIconFileName + "_38", false);

            }
            if (_shrinkIcon == null)
            {
                _shrinkIcon = GameDatabase.Instance.GetTexture(Constants.shrinkIconFileName + "_38", false);

            }

            if (toolbarControl == null)
            {
                toolbarControl = gameObject.AddComponent<ToolbarControl>();
                toolbarControl.AddToAllToolbars(ToggleScalingRoutine, ToggleScalingRoutine,
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
                    MODID,
                    "fsEditorExtenderButton",
                    Constants.extentIconFileName + "_38",
                    Constants.extentIconFileName + "_24",
                    MODNAME
                );
            }
        }

        internal const string MODID = "FSeditorExtender_NS";
        internal const string MODNAME = "FS Editor Extender";

        /// <summary>
        /// method to completly reset the whole mod
        /// </summary>
        private void resetMod()
        {
            if (HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().hideHangars)
            {
                foreach (Node n in _hangarNodes)
                {
                    if (n.transform != null)
                    {
                        List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
                        n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
                        foreach (SkinnedMeshRenderer r in skinRenderers)
                        {
                            if (r != null)
                            {
                                r.enabled = true;
                            }
                        }
                        List<MeshRenderer> renderers = new List<MeshRenderer>();
                        n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
                        foreach (MeshRenderer r in renderers)
                        {
                            if (r != null)
                            {
                                r.enabled = true;
                            }
                        }
                    }
                }
            }
            _sceneNodes.Clear();
            _hangarNodes.Clear();
            _nonScalingNodes.Clear();
            _sceneLights.Clear();
            _vabCameras.Clear();
            _sphCameras.Clear();
            _sceneScaled = false;
            _hangarExtenderReady = false;
            _isFirstUpdate = true;
        }


        /// <summary>
        /// method to initalize the whole Extender
        /// </summary>
        /// <returns></returns>
        private IEnumerator<YieldInstruction> initFSHangarExtender(float delay = 0)
        {
            if (delay != 0)
                yield return new WaitForSeconds(delay);
            getSettings();
            while ((object)EditorBounds.Instance == null && HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                _hangarExtenderReady = false;
                yield return null;
            }
            while (_hangarNodes.Count < 1 || _sceneNodes.Count < 1)
            {
                fetchSceneNodes();
                yield return null;
            }
            prepareTransforms();
            sceneCamera = EditorCamera.Instance.cam;
            _originalConstructionBoundExtends = EditorBounds.Instance.constructionBounds.extents * 2;
            _originalCameraOffsetBoundExtends = EditorBounds.Instance.cameraOffsetBounds.extents * 2;
            EditorBounds.Instance.cameraMinDistance /= _scalingFactor;
            fetchCameras();
            fetchLights();
            listNodes();
            _hangarExtenderReady = true;
            _sceneScaled = false;
            Debugger.advancedDebug("Attempting to init successful", true);
            Debugger.advancedDebug("Editor camera set to Min = " + EditorBounds.Instance.cameraMinDistance + " Max = " + EditorBounds.Instance.cameraMaxDistance + " Start = " + EditorBounds.Instance.cameraStartDistance, _advancedDebug);
            Debugger.advancedDebug("EditorBounds.Instance.constructionBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.constructionBounds.extents = (" + EditorBounds.Instance.constructionBounds.extents.x + " , " + EditorBounds.Instance.constructionBounds.extents.y + " , " + EditorBounds.Instance.constructionBounds.extents.z + ")", _advancedDebug);
            Debugger.advancedDebug("EditorBounds.Instance.cameraOffsetBounds.center = " + EditorBounds.Instance.cameraOffsetBounds.center + " EditorBounds.Instance.cameraOffsetBounds.extents = (" + EditorBounds.Instance.cameraOffsetBounds.extents.x + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.y + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.z + ")", _advancedDebug);


            if (HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().startInExternalView)
                StartCoroutine(toggleScaling());

        }

        void ToggleScalingRoutine()
        {
            StartCoroutine(toggleScaling());
        }

        /// <summary>
        /// method to update the camera bounds and scale the scene
        /// </summary>
        /// <returns></returns>
        private IEnumerator<YieldInstruction> toggleScaling() // code taken from NathanKell, https://github.com/NathanKell/RealSolarSystem/blob/master/Source/CameraFixer.cs
        {
            Debugger.advancedDebug("Attempting work area scaling", _advancedDebug);
            while ((object)EditorBounds.Instance == null)
            {
                yield return null;
            }
            if ((object)(EditorBounds.Instance) != null)
            {
                if (_sceneScaled)
                {
                    Debugger.advancedDebug("shrink scene", _advancedDebug);

                    EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (_originalConstructionBoundExtends));
                    EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (_originalCameraOffsetBoundExtends));
                    EditorBounds.Instance.cameraMaxDistance /= _scalingFactor;
                    Debugger.advancedDebug("Bounds scaled", _advancedDebug);


                    sceneCamera.farClipPlane /= _scalingFactor * 2;
                    for (int i = 0; i < sceneCamera.layerCullDistances.Length; i++)
                    {
                        sceneCamera.layerCullDistances[i] /= _scalingFactor * 2;
                    }
                    foreach (VABCamera c in _vabCameras)
                    {
                        c.maxHeight /= _scalingFactor;
                        c.maxDistance /= _scalingFactor;
                    }
                    Debugger.advancedDebug("vabCameras scaled", _advancedDebug);
                    foreach (SPHCamera c in _sphCameras)
                    {
                        c.maxHeight /= _scalingFactor;
                        c.maxDistance /= _scalingFactor;
                        c.maxDisplaceX /= _scalingFactor;
                        c.maxDisplaceZ /= _scalingFactor;
                    }
                    Debugger.advancedDebug("sphCameras scaled", _advancedDebug);


                    RenderSettings.fogStartDistance /= _scalingFactor;
                    RenderSettings.fogEndDistance /= _scalingFactor;

                    Debugger.advancedDebug("scale Hangars", _advancedDebug);
                    if (_hangarNodes != null && _hangarNodes.Count > 0)
                    {
                        foreach (Node n in _hangarNodes)
                        {
                            n.transform.localScale = n.defaultScaling;
                            Debugger.advancedDebug("scaleing Hangar" + n.transform.name, _advancedDebug);
                        }
                    }
                    //Debugger.advancedDebug("scale Scene", _advancedDebug);
                    //if (_sceneNodes != null && _sceneNodes.Count > 0)
                    //{
                    //	foreach (Node n in _sceneNodes)
                    //	{
                    //		n.transform.localScale = n.defaultScaling;
                    //		Debugger.advancedDebug("scaleing Scene" + n.transform.name, _advancedDebug);
                    //	}
                    //}
                    Debugger.advancedDebug("attach Nodes", _advancedDebug);
                    if (_nonScalingNodes != null && _nonScalingNodes.Count > 0)
                    {
                        foreach (Node n in _nonScalingNodes)
                        {
                            n.transform.parent = n.originalParent;
                            n.transform.localScale = n.defaultScaling;
                            Debugger.advancedDebug("Reattaching Node" + n.transform.name, _advancedDebug);
                        }
                    }
                    Debugger.advancedDebug("scale lights", _advancedDebug);
                    if (_sceneLights != null && _sceneLights.Count > 0)
                    {
                        foreach (Light l in _sceneLights)
                        {
                            if (l != null)
                            {
                                if (l.type == LightType.Spot)
                                {
                                    l.range /= _scalingFactor;
                                    Debugger.advancedDebug("scaling light", _advancedDebug);
                                }
                            }
                        }
                    }

                    if (HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().hideHangars)
                    {
                        Debugger.advancedDebug("hide Hangars", _advancedDebug);
                        if (_hangarNodes != null && _hangarNodes.Count > 0)
                        {
                            foreach (Node n in _hangarNodes)
                            {
                                List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
                                n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
                                foreach (SkinnedMeshRenderer r in skinRenderers)
                                {
                                    r.enabled = true;
                                }
                                List<MeshRenderer> renderers = new List<MeshRenderer>();
                                n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
                                foreach (MeshRenderer r in renderers)
                                {
                                    r.enabled = true;
                                }
                            }
                        }
                        Debugger.advancedDebug("hide Hangars complete", _advancedDebug);
                    }

                    Debugger.advancedDebug("update Button", _advancedDebug);
#if false
                    if (_toolbarButton != null && _extendIcon != null)
					{
						_toolbarButton.SetTexture(_extendIcon);
					}
#endif
                    if (toolbarControl != null)
                    {
                        toolbarControl.SetTexture(Constants.extentIconFileName + "_38", Constants.extentIconFileName + "_24");
                    }
                    Debugger.advancedDebug("shrink scene complete", _advancedDebug);
                }
                else
                {
                    Debugger.advancedDebug("extend scene", _advancedDebug);

                    EditorBounds.Instance.constructionBounds = new Bounds(EditorBounds.Instance.constructionBounds.center, (_originalConstructionBoundExtends * _scalingFactor));
                    EditorBounds.Instance.cameraOffsetBounds = new Bounds(EditorBounds.Instance.cameraOffsetBounds.center, (_originalCameraOffsetBoundExtends * _scalingFactor));
                    EditorBounds.Instance.cameraMaxDistance *= _scalingFactor;
                    Debugger.advancedDebug("Bounds scaled", _advancedDebug);


                    sceneCamera.farClipPlane *= _scalingFactor * 2;
                    for (int i = 0; i < sceneCamera.layerCullDistances.Length; i++)
                    {
                        sceneCamera.layerCullDistances[i] *= _scalingFactor * 2;
                    }
                    foreach (VABCamera c in _vabCameras)
                    {
                        c.maxHeight *= _scalingFactor;
                        c.maxDistance *= _scalingFactor;
                    }
                    Debugger.advancedDebug("vabCameras scaled", _advancedDebug);
                    foreach (SPHCamera c in _sphCameras)
                    {
                        c.maxHeight *= _scalingFactor;
                        c.maxDistance *= _scalingFactor;
                        c.maxDisplaceX *= _scalingFactor;
                        c.maxDisplaceZ *= _scalingFactor;
                    }
                    Debugger.advancedDebug("sphCameras scaled", _advancedDebug);


                    RenderSettings.fogStartDistance *= _scalingFactor;
                    RenderSettings.fogEndDistance *= _scalingFactor;

                    if (HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().hideHangars)
                    {
                        Debugger.advancedDebug("hide Hangars", _advancedDebug);
                        if (_hangarNodes != null && _hangarNodes.Count > 0)
                        {
                            foreach (Node n in _hangarNodes)
                            {
                                List<SkinnedMeshRenderer> skinRenderers = new List<SkinnedMeshRenderer>();
                                n.transform.GetComponentsInChildren<SkinnedMeshRenderer>(skinRenderers);
                                foreach (SkinnedMeshRenderer r in skinRenderers)
                                {
                                    r.enabled = false;
                                }
                                List<MeshRenderer> renderers = new List<MeshRenderer>();
                                n.transform.GetComponentsInChildren<MeshRenderer>(renderers);
                                foreach (MeshRenderer r in renderers)
                                {
                                    r.enabled = false;
                                }
                            }
                        }
                        Debugger.advancedDebug("hide Hangars complete", _advancedDebug);
                    }
                    Debugger.advancedDebug("Detach Nodes", _advancedDebug);
                    if (_nonScalingNodes != null && _nonScalingNodes.Count > 0)
                    {
                        foreach (Node n in _nonScalingNodes)
                        {
                            n.transform.parent = _tempParent;
                            n.transform.localScale = n.defaultScaling;
                            Debugger.advancedDebug("Dettaching Node - " + n.transform.name, _advancedDebug);
                        }
                    }
                    Debugger.advancedDebug("scale Hangar", _advancedDebug);
                    if (_hangarNodes != null && _hangarNodes.Count > 0)
                    {
                        foreach (Node n in _hangarNodes)
                        {
                            n.transform.localScale = n.defaultScaling * _scalingFactor;
                            Debugger.advancedDebug("scaleing HangarNode - " + n.transform.name, _advancedDebug);
                        }
                    }
                    //Debugger.advancedDebug("scale Scene", _advancedDebug);
                    //if (_sceneNodes != null && _sceneNodes.Count > 0)
                    //{
                    //	foreach (Node n in _sceneNodes)
                    //	{
                    //		n.transform.localScale = n.defaultScaling * _scalingFactor;
                    //		Debugger.advancedDebug("scaling SceneNode - "+n.transform.name, _advancedDebug);
                    //	}
                    //}

                    Debugger.advancedDebug("scale lights", _advancedDebug);
                    if (_sceneLights != null && _sceneLights.Count > 0)
                    {
                        foreach (Light l in _sceneLights)
                        {
                            if (l != null)
                            {
                                if (l.type == LightType.Spot)
                                {
                                    l.range *= _scalingFactor;
                                    Debugger.advancedDebug("scaling light", _advancedDebug);
                                }
                            }
                        }
                    }

                    Debugger.advancedDebug("update Button", _advancedDebug);
                    if (toolbarControl != null)
                    {
                        toolbarControl.SetTexture(Constants.shrinkIconFileName + "_38", Constants.shrinkIconFileName + "_24");
                    }
                    Debugger.advancedDebug("extend scene complete", _advancedDebug);
                }
                _sceneScaled = !_sceneScaled;
            }
            Debugger.advancedDebug("Attempting work area scaling complete", _advancedDebug);
            Debugger.advancedDebug("Editor camera set to Min = " + EditorBounds.Instance.cameraMinDistance + " Max = " + EditorBounds.Instance.cameraMaxDistance + " Start = " + EditorBounds.Instance.cameraStartDistance, _advancedDebug);
            Debugger.advancedDebug("EditorBounds.Instance.constructionBounds.center = " + EditorBounds.Instance.constructionBounds.center + " EditorBounds.Instance.constructionBounds.extents = (" + EditorBounds.Instance.constructionBounds.extents.x + " , " + EditorBounds.Instance.constructionBounds.extents.y + " , " + EditorBounds.Instance.constructionBounds.extents.z + ")", _advancedDebug);
            Debugger.advancedDebug("EditorBounds.Instance.cameraOffsetBounds.center = " + EditorBounds.Instance.cameraOffsetBounds.center + " EditorBounds.Instance.cameraOffsetBounds.extents = (" + EditorBounds.Instance.cameraOffsetBounds.extents.x + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.y + " , " + EditorBounds.Instance.cameraOffsetBounds.extents.z + ")", _advancedDebug);
        }


        /// <summary>
        /// method to fetch the cameras in the scene and assigns the zoom limits to it
        /// </summary>
        private void fetchCameras()
        {
            _vabCameras = ((VABCamera[])Resources.FindObjectsOfTypeAll(typeof(VABCamera))).ToList();
            _sphCameras = ((SPHCamera[])Resources.FindObjectsOfTypeAll(typeof(SPHCamera))).ToList();
        }


        /// <summary>
        /// method to return a list of every single child transforms from a wanted transform
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static List<Transform> getTransformChildsList(Transform parent)
        {
            if (parent != null)
            {
                List<Transform> newList = new List<Transform>();
                foreach (Transform t in parent)
                {
                    newList.Add(t);
                    foreach (Transform tx in getTransformChildsList(t))
                    {
                        newList.Add(tx);
                    }
                }
                return newList;
            }
            return null;
        }


        /// <summary>
        /// debug output of the root transforms
        /// </summary>
        private static void listNodes()
        {
            Debugger.advancedDebug("listNodes started", true);
            List<Transform> rootNodes = new List<Transform>();
            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
            {
                Transform newTransform = t.root;
                while (newTransform.parent != null)
                {
                    newTransform = newTransform.parent;
                }
                if (!rootNodes.Contains(newTransform))
                {
                    rootNodes.Add(newTransform);
                }
            }
            foreach (Transform t in rootNodes)
            {
                foreach (Transform ct in t)
                {
                    Debugger.advancedDebug(t.name + " --- " + ct.name, true);
                }
            }
            Debugger.advancedDebug("listNodes finished", true);
        }


        /// <summary>
        /// debug output for the root transforms and their childs
        /// </summary>
        private static void listNodesAdvanced()
        {
            Debugger.advancedDebug("listNodesAdvanced started", true);
            List<Transform> rootNodes = new List<Transform>();
            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
            {
                Transform newTransform = t.root;
                while (newTransform.parent != null)
                {
                    newTransform = newTransform.parent;
                }
                if (!rootNodes.Contains(newTransform))
                {
                    rootNodes.Add(newTransform);
                }
            }
            Debugger.advancedDebug("listNodesAdvanced finished", true);
        }


        /// <summary>
        /// collects all Lights in the scene.
        /// </summary>
        private void fetchLights()
        {
            _sceneLights = ((Light[])FindObjectsOfType(typeof(Light))).ToList();
            foreach (Light l in _sceneLights)
            {
                Debugger.advancedDebug("Light = " + l.name + " - Type = " + l.type + " - Intensity = " + l.intensity, _advancedDebug);
            }
        }


        /// <summary>
        /// method that will read the transforms from the scene
        /// </summary>
        private void fetchSceneNodes()
        {
            Debugger.advancedDebug("fetchSceneNodes Nodes found = " + UnityEngine.Object.FindObjectsOfType<Transform>().Length, _advancedDebug);
            List<Transform> rootNodes = new List<Transform>();
            _hangarNodes.Clear();
            _sceneNodes.Clear();
            _nonScalingNodes.Clear();
            GameObject temp = new GameObject();
            _tempParent = temp.transform;
            _tempParent.position = Vector3.zero;
            _tempParent.localScale = Vector3.one;
            _tempParent.name = Constants.defaultTempParentName;

            foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>())
            {
                Transform newTransform = t.root;
                while (newTransform.parent != null)
                {
                    newTransform = newTransform.parent;
                }
                if (!rootNodes.Contains(newTransform))
                {
                    rootNodes.Add(newTransform);
                }
            }
            Debugger.advancedDebug("root nodes collected", _advancedDebug);

            foreach (Transform t in rootNodes)
            {
                foreach (string s in Constants.baseHangarNames)
                {
                    if (string.Equals(t.name.ToLower(), s))
                    {
                        if (!doesListContain(_hangarNodes, t))
                        {
                            Node newNode = new Node();
                            newNode.transform = t;
                            newNode.originalParent = t.parent;
                            newNode.defaultScaling = t.localScale;
                            _hangarNodes.Add(newNode);
                            Debugger.advancedDebug("found new hangar node: " + t.name, _advancedDebug);
                            break;
                        }
                    }
                }
                foreach (string s in Constants.baseSceneNames)
                {
                    if (string.Equals(t.name.ToLower(), s))
                    {
                        if (!doesListContain(_sceneNodes, t))
                        {
                            Node newNode = new Node();
                            newNode.transform = t;
                            newNode.originalParent = t.parent;
                            newNode.defaultScaling = t.localScale;
                            _sceneNodes.Add(newNode);
                            Debugger.advancedDebug("found new scene node: " + t.name, _advancedDebug);
                            break;
                        }
                    }
                }
            }
            if (_hangarNodes.Count < 1 || _sceneNodes.Count < 1)
            {
                Debugger.advancedDebug("no scalable nodes found", _advancedDebug);
                return;
            }
            Debugger.advancedDebug("base scaling nodes collected", _advancedDebug);

            foreach (Node n in _hangarNodes)
            {
                foreach (Transform t in getTransformChildsList(n.transform))
                {
                    foreach (string s in Constants.nonScalingNodeNames)
                    {
                        if (string.Equals(t.name.ToLower(), s))
                        {
                            if (!doesListContain(_nonScalingNodes, t))
                            {
                                Node newNode = new Node();
                                newNode.transform = t;
                                newNode.originalParent = t.parent;
                                newNode.defaultScaling = t.localScale;
                                _nonScalingNodes.Add(newNode);
                                Debugger.advancedDebug("found new hangar node for not scaling: " + t.name + " | position = " + t.localPosition.x + " | " + t.localPosition.y + " | " + t.localPosition.z, _advancedDebug);
                                break;
                            }
                        }
                    }
                }
            }
            foreach (Node n in _sceneNodes)
            {
                foreach (Transform t in getTransformChildsList(n.transform))
                {
                    foreach (string s in Constants.nonScalingNodeNames)
                    {
                        if (string.Equals(t.name.ToLower(), s))
                        {
                            if (!doesListContain(_nonScalingNodes, t))
                            {
                                Node newNode = new Node();
                                newNode.transform = t;
                                newNode.originalParent = t.parent;
                                newNode.defaultScaling = t.localScale;
                                _nonScalingNodes.Add(newNode);
                                Debugger.advancedDebug("found new scene node for not scaling: " + t.name + " | position = " + t.localPosition.x + " | " + t.localPosition.y + " | " + t.localPosition.z, _advancedDebug);
                                break;
                            }
                        }
                    }
                }
            }
            Debugger.advancedDebug("nonscaling nodes collected", _advancedDebug);
        }


        /// <summary>
        /// loads the settings from the settings file
        /// </summary>
        private static void getSettings()
        {
            _scalingFactor = HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().scalingFactor;
            _advancedDebug = HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().advancedDebug;
            Debugger.advancedDebug("Assigned scalingFactor: " + _scalingFactor, true);
        }


        /// <summary>
        /// reas single lines from the config file and returns them
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private static string readSetting(StreamReader stream)
        {
            string newLine = string.Empty;
            try
            {
                while (newLine == string.Empty && !stream.EndOfStream)
                {
                    newLine = stream.ReadLine();
                    newLine = newLine.Trim(' ');
                    if (newLine.Length > 1)
                    {
                        if (newLine.Substring(0, 2) == "//")
                        {
                            newLine = string.Empty;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debugger.advancedDebug("stream reader error: " + e.ToString(), true);
            }
            return newLine;
        }


        /// <summary>
        /// method to check for pressed key
        /// </summary>
        /// <returns></returns>
        private static bool rescaleKeyPressed()
        {
            bool gotKeyPress = false;
            try
            {
                gotKeyPress = (_lastKeyPressed == HighLogic.CurrentGame.Parameters.CustomParams<HangerExtender>().newHotKey);
                _lastKeyPressed = KeyCode.None;

            }
            catch
            {
                Debugger.advancedDebug("Invalid keycode. Resetting to numpad *", true);
                gotKeyPress = false;
            }
            return gotKeyPress;
        }
    }
}