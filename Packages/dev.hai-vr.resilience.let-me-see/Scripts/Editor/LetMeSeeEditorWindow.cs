using System;
using System.Globalization;
using UnityEditor;
#if !LETMESEE_OPENXR_EXISTS
using UnityEditor.PackageManager;
#endif
using UnityEngine;
using UnityEngine.XR.Management;

namespace Resilience.LetMeSee
{
    public class LetMeSeeEditorWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        private static T ColoredBackground<T>(bool isActive, Color bgColor, Func<T> inside)
        {
            var col = GUI.color;
            try
            {
                if (isActive) GUI.color = bgColor;
                return inside();
            }
            finally
            {
                GUI.color = col;
            }
        }
        
        private void OnGUI()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(position.height - EditorGUIUtility.singleLineHeight));
            
            var isEnabled = LetMeSeeCore.Instance.Enabled;
            if (ColoredBackground(isEnabled, Color.red, () => GUILayout.Button("Run")))
            {
                if (isEnabled) LetMeSeeCore.Instance.DoHardStop();
                else LetMeSeeCore.Instance.DoHardStart();
            }
            if (ColoredBackground(LetMeSeeCore.Instance.FreezeCamera, new Color(0.77f, 1f, 0f, 0.75f), () => GUILayout.Button("Freeze Camera")))
            {
                LetMeSeeCore.Instance.FreezeCamera = !LetMeSeeCore.Instance.FreezeCamera;
            }
            EditorGUILayout.Separator();
            
            if (GUILayout.Button("Recenter view", GUILayout.Height(50)))
            {
                LetMeSeeCore.Instance.DoRecenter();
            }
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("IPD and Scale", EditorStyles.boldLabel);
            var isHeightEditable = LetMeSeeUserSettings.IsHeightEditable;
            var newHeightEditable = EditorGUILayout.Toggle("Edit your height", isHeightEditable);
            if (newHeightEditable != isHeightEditable)
            {
                LetMeSeeUserSettings.IsHeightEditable = newHeightEditable;
            }

            if (newHeightEditable)
            {
                EditorGUI.BeginDisabledGroup(!LetMeSeeUserSettings.IsHeightEditable);
                var heightInMeters = LetMeSeeUserSettings.UserHeight;
                var newHeight = EditorGUILayout.FloatField("Your height (m)", heightInMeters);
                if (newHeight != heightInMeters)
                {
                    LetMeSeeUserSettings.UserHeight = newHeight;
                }

                var heightInFeet = LetMeSeeUserSettings.UserHeight / 0.3048f;
                var newHeightInFeet = EditorGUILayout.FloatField("Your height (ft)", heightInFeet);
                if (newHeightInFeet != heightInFeet)
                {
                    LetMeSeeUserSettings.UserHeight = newHeightInFeet * 0.3048f;
                }
                EditorGUI.EndDisabledGroup();
            }

            {
                var heightInMeters = LetMeSeeUserSettings.UserHeight;
                var heightInFeet = LetMeSeeUserSettings.UserHeight / 0.3048f;
                var rescale0 = LetMeSeeUserSettings.Rescale;
                var rescaledHeight = EditorGUILayout.FloatField("Rescaled height (m)", heightInMeters * rescale0);
                if (rescaledHeight != heightInMeters * rescale0 && rescaledHeight >= 0)
                {
                    LetMeSeeUserSettings.Rescale = rescaledHeight / heightInMeters;
                }

                var rescale1 = LetMeSeeUserSettings.Rescale;
                var rescaledHeightInFeet = EditorGUILayout.FloatField("Rescaled height (ft)", heightInFeet * rescale1);
                if (rescaledHeightInFeet != heightInFeet * rescale1 && rescaledHeightInFeet >= 0)
                {
                    LetMeSeeUserSettings.Rescale = rescaledHeightInFeet / heightInFeet;
                }
            }

            if (LetMeSeeUserSettings.Rescale <= 2f)
            {
                ShowRescaler(0f, 2f);
            }
            else
            {
                var rescale2 = LetMeSeeUserSettings.Rescale;
                var newRescale = EditorGUILayout.FloatField("Rescale", rescale2);
                if (newRescale != rescale2 && newRescale >= 0)
                {
                    LetMeSeeUserSettings.Rescale = rescale2;
                }
            }
            if (GUILayout.Button("Reset scale"))
            {
                LetMeSeeUserSettings.Rescale = 1;
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Alignment", EditorStyles.boldLabel);
            var newCameraMode = (LetMeSeeCameraMode)EditorGUILayout.EnumPopup(new GUIContent("Camera Mode"), LetMeSeeUserSettings.CameraMode);
            if (newCameraMode != LetMeSeeUserSettings.CameraMode)
            {
                LetMeSeeUserSettings.CameraMode = newCameraMode;
            }
            if (ColoredBackground(LetMeSeeCore.Instance.LockSceneView, new Color(0f, 1f, 1f, 0.75f), () => GUILayout.Button("Do not switch Scene tabs")))
            {
                LetMeSeeCore.Instance.LockSceneView = !LetMeSeeCore.Instance.LockSceneView;
            }

            if (newCameraMode != LetMeSeeCameraMode.SceneView)
            {
                var newMoveUp = EditorGUILayout.Slider("Move camera up", LetMeSeeUserSettings.MoveUp, 0, 1);
                if (newMoveUp != LetMeSeeUserSettings.MoveUp)
                {
                    LetMeSeeUserSettings.MoveUp = newMoveUp;
                }
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            var useOpenXR = EditorGUILayout.Toggle("Force use OpenXR", LetMeSeeUserSettings.ForceUseOpenXR);
            if (useOpenXR != LetMeSeeUserSettings.ForceUseOpenXR)
            {
                LetMeSeeUserSettings.ForceUseOpenXR = useOpenXR;
            }

#if !LETMESEE_OPENXR_EXISTS
            if (LetMeSeeUserSettings.ForceUseOpenXR)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox("OpenXR is missing from the project.\nInstall OpenXR?", MessageType.Warning);
                if (GUILayout.Button("Install OpenXR", GUILayout.Width(200), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
                {
                    Client.Add("com.unity.xr.openxr");
                }
                EditorGUILayout.EndHorizontal();
            }
#endif

            if (XRGeneralSettings.Instance != null)
            {
                var xrOnStartup = EditorGUILayout.Toggle("(DEBUG) Initialize XR on Startup", XRGeneralSettings.Instance.InitManagerOnStart);
                if (xrOnStartup != XRGeneralSettings.Instance.InitManagerOnStart)
                {
                    XRGeneralSettings.Instance.InitManagerOnStart = xrOnStartup;
                }
            }

            var lastValidPos = LetMeSeeCore.Instance.LastValidPoseDataPos();
            EditorGUILayout.LabelField(string.Format(CultureInfo.InvariantCulture, "Last valid pose data: {0:0.00000}, {1:0.00000}, {2:0.00000}", lastValidPos.x, lastValidPos.y, lastValidPos.z));
            
            EditorGUILayout.EndScrollView();
        }

        private static void ShowRescaler(float min, float max)
        {
            var rescale = LetMeSeeUserSettings.Rescale;
            var newRescale = EditorGUILayout.Slider(GUIContent.none, rescale, min, max);
            if (newRescale != rescale)
            {
                LetMeSeeUserSettings.Rescale = newRescale;
            }
        }
    }
}