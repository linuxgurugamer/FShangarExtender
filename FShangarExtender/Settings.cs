using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;



namespace FShangarExtender
{

   // [KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
    class HangerExtenderSelectHotKey : MonoBehaviour
    {
        public static HangerExtenderSelectHotKey Instance;

        public bool active = false;
        public bool completed = false;
        KeyCode _lastKeyPressed = KeyCode.None;
        public KeyCode hotkey = KeyCode.None;
        public float lastTimeTic = 0;
        private Rect settingsRect = new Rect(200, 200, 350, 150);

        void Start()
        {
            Instance = this;
        }
        public void EnableWindow(bool b = true)
        {
            if (b)
            {
                if (!active)
                {
                    _lastKeyPressed = KeyCode.None;
                    active = true;
                    completed = false;
                }
            } else
            {
                completed = false;
            }
        }
        void OnGUI()
        {
            if (!active)
                return;

            if (Time.realtimeSinceStartup - lastTimeTic > 0.25)
            {
                active = false;
                return;
            }

            // The settings are only available in the space center

            GUI.skin = HighLogic.Skin;

            settingsRect = GUILayout.Window("HotKeySettings".GetHashCode(),
                                            settingsRect,
                                            SettingsWindowFcn,
                                            "EVA Fuel Settings",
                                            GUILayout.ExpandWidth(true),
                                            GUILayout.ExpandHeight(true));

        }

        void Update()
        {
            if (Event.current.isKey)
            {
                _lastKeyPressed = Event.current.keyCode;
            }
        }

        void SettingsWindowFcn(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Enter desired hotkey: ");
            GUILayout.Label(hotkey.ToString(), GUI.skin.textField);          

            if (_lastKeyPressed != KeyCode.None)
            {
                hotkey = _lastKeyPressed;
                _lastKeyPressed = KeyCode.None;
            }
            // look at EEX

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("OK", GUILayout.Width(60)))
            {
                active = false;
                completed = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

    }

    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    public class HangerExtender : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "General Settings"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Hangar Extender"; } }
        public override string DisplaySection { get { return "Hangar Extender"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return true; } }

        //[GameParameters.CustomParameterUI("Use Blizzy toolbar, if available")]
        //public bool useBlizzy = false;



        [GameParameters.CustomParameterUI("Change Hotkey")]
        public bool changeHotkey = false;
        

        public KeyCode newHotKey = KeyCode.KeypadMultiply;

        [GameParameters.CustomIntParameterUI("Rescaling Factor", minValue = 2, maxValue = 15, stepSize = 1,
            toolTip = "Rescaling Factor for the visual buildings")]
        public int scalingFactor { get { return ScalingFactor; } set { ScalingFactor = value; } }
        private int ScalingFactor = 5;        

        [GameParameters.CustomParameterUI("Hide Hangers in scene")]
        public bool hideHangars = true;

        [GameParameters.CustomParameterUI("Buildings start at max size")]
        public bool BuildingStartMaxSize = false;

        [GameParameters.CustomParameterUI("Advanced Debugging")]
        public bool advancedDebug = false;

        [GameParameters.CustomParameterUI("Start in external VAB")]
        public bool startInExternalView = false;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "EnabledForSave") //This Field must always be enabled.
                return true;

            return true; //otherwise return true
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {
            if (changeHotkey)
            {
                // Use MapView.MapCamera to get a gameObject
                HangerExtenderSelectHotKey gui = MapView.MapCamera.gameObject.GetComponent<HangerExtenderSelectHotKey>();
                if (gui == null)
                {
                    gui = MapView.MapCamera.gameObject.AddComponent<HangerExtenderSelectHotKey>();
                }
                if (HangerExtenderSelectHotKey.Instance != null)
                {
                    if (HangerExtenderSelectHotKey.Instance.completed)
                    {
                        changeHotkey = false;
                        HangerExtenderSelectHotKey.Instance.EnableWindow(false);
                        newHotKey = HangerExtenderSelectHotKey.Instance.hotkey;
                        UnityEngine.Object.Destroy(gui);
                        gui = null;
                        HangerExtenderSelectHotKey.Instance = null;
                    }
                    else
                    {
                        HangerExtenderSelectHotKey.Instance.lastTimeTic = Time.realtimeSinceStartup;
                        if (!HangerExtenderSelectHotKey.Instance.active)
                        {
                            HangerExtenderSelectHotKey.Instance.EnableWindow();
                            HangerExtenderSelectHotKey.Instance.hotkey = newHotKey;
                        }
                    }
                }               
            }
           return true;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
