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
        private const string AdvancedLabel = "Advanced";
        private const string AlignmentLabel = "Alignment";
        private const string CameraModeLabel = "Camera Mode";
        private const string CursorColorLabel = "Cursor color";
        private const string DoNotSwitchSceneTabsLabel = "Do not switch Scene tabs";
        private const string EditYourHeightLabel = "Edit your height";
        private const string ForceUseOpenXRLabel = "Force use OpenXR";
        private const string FreezeCameraLabel = "Freeze Camera";
        private const string InitializeXROnStartupLabel = "(DEBUG) Initialize XR on Startup";
        private const string InstallOpenXRLabel = "Install OpenXR";
        private const string IpdAndScaleLabel = "IPD and Scale";
        private const string LastValidPoseDataLabel = "Last valid pose data: {0:0.00000}, {1:0.00000}, {2:0.00000}";
        private const string MoveCameraUpLabel = "Move camera up";
        private const string MsgOpenXRMissing = "OpenXR is missing from the project.\nInstall OpenXR?";
        private const string PleaseWaitLabel = "Please wait...";
        private const string RecenterViewLabel = "Recenter view";
        private const string RescaleLabel = "Rescale";
        private const string RescaledHeightFeetLabel = "Rescaled height (ft)";
        private const string RescaledHeightMetresLabel = "Rescaled height (m)";
        private const string ResetScaleLabel = "Reset scale";
        private const string RunLabel = "Run";
        private const string ShowCursorLabel = "Show cursor";
        private const string YourHeightFeetLabel = "Your height (ft)";
        private const string YourHeightMetresLabel = "Your height (m)";
        
        private Vector2 _scrollPos;
        private bool _waitOpenXR;

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
            if (ColoredBackground(isEnabled, Color.red, () => GUILayout.Button(RunLabel)))
            {
                if (isEnabled) LetMeSeeCore.Instance.DoHardStop();
                else LetMeSeeCore.Instance.DoHardStart();
            }
            if (ColoredBackground(LetMeSeeCore.Instance.FreezeCamera, new Color(0.77f, 1f, 0f, 0.75f), () => GUILayout.Button(FreezeCameraLabel)))
            {
                LetMeSeeCore.Instance.FreezeCamera = !LetMeSeeCore.Instance.FreezeCamera;
            }
            EditorGUILayout.Separator();
            
            if (GUILayout.Button(RecenterViewLabel, GUILayout.Height(50)))
            {
                LetMeSeeCore.Instance.DoRecenter();
            }
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField(IpdAndScaleLabel, EditorStyles.boldLabel);
            var isHeightEditable = LetMeSeeUserSettings.IsHeightEditable;
            var newHeightEditable = EditorGUILayout.Toggle(EditYourHeightLabel, isHeightEditable);
            if (newHeightEditable != isHeightEditable)
            {
                LetMeSeeUserSettings.IsHeightEditable = newHeightEditable;
            }

            if (newHeightEditable)
            {
                EditorGUI.BeginDisabledGroup(!LetMeSeeUserSettings.IsHeightEditable);
                var heightInMeters = LetMeSeeUserSettings.UserHeight;
                var newHeight = EditorGUILayout.FloatField(YourHeightMetresLabel, heightInMeters);
                if (newHeight != heightInMeters)
                {
                    LetMeSeeUserSettings.UserHeight = newHeight;
                }

                var heightInFeet = LetMeSeeUserSettings.UserHeight / 0.3048f;
                var newHeightInFeet = EditorGUILayout.FloatField(YourHeightFeetLabel, heightInFeet);
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
                var rescaledHeight = EditorGUILayout.FloatField(RescaledHeightMetresLabel, heightInMeters * rescale0);
                if (rescaledHeight != heightInMeters * rescale0 && rescaledHeight >= 0)
                {
                    LetMeSeeUserSettings.Rescale = rescaledHeight / heightInMeters;
                }

                var rescale1 = LetMeSeeUserSettings.Rescale;
                var rescaledHeightInFeet = EditorGUILayout.FloatField(RescaledHeightFeetLabel, heightInFeet * rescale1);
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
                var newRescale = EditorGUILayout.FloatField(RescaleLabel, rescale2);
                if (newRescale != rescale2 && newRescale >= 0)
                {
                    LetMeSeeUserSettings.Rescale = rescale2;
                }
            }
            if (GUILayout.Button(ResetScaleLabel))
            {
                LetMeSeeUserSettings.Rescale = 1;
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(AlignmentLabel, EditorStyles.boldLabel);
            var newCameraMode = (LetMeSeeCameraMode)EditorGUILayout.EnumPopup(new GUIContent(CameraModeLabel), LetMeSeeUserSettings.CameraMode);
            if (newCameraMode != LetMeSeeUserSettings.CameraMode)
            {
                LetMeSeeUserSettings.CameraMode = newCameraMode;
            }
            if (ColoredBackground(LetMeSeeCore.Instance.LockSceneView, new Color(0f, 1f, 1f, 0.75f), () => GUILayout.Button(DoNotSwitchSceneTabsLabel)))
            {
                LetMeSeeCore.Instance.LockSceneView = !LetMeSeeCore.Instance.LockSceneView;
            }

            var showCursor = EditorGUILayout.Toggle(ShowCursorLabel, LetMeSeeUserSettings.ShowCursor);
            if (showCursor != LetMeSeeUserSettings.ShowCursor)
            {
                LetMeSeeUserSettings.ShowCursor = showCursor;
            }
            
            
            var cursorColor = LetMeSeeUserSettings.CursorColor;
            var newColor = EditorGUILayout.ColorField(new GUIContent(CursorColorLabel), cursorColor);
            if (newColor != cursorColor)
            {
                LetMeSeeUserSettings.CursorColor = newColor;
                LetMeSeeCore.Instance.ForceUpdateCursorColor();
            }

            if (newCameraMode != LetMeSeeCameraMode.SceneView)
            {
                var newMoveUp = EditorGUILayout.Slider(MoveCameraUpLabel, LetMeSeeUserSettings.MoveUp, 0, 1);
                if (newMoveUp != LetMeSeeUserSettings.MoveUp)
                {
                    LetMeSeeUserSettings.MoveUp = newMoveUp;
                }
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(AdvancedLabel, EditorStyles.boldLabel);
            var useOpenXR = EditorGUILayout.Toggle(ForceUseOpenXRLabel, LetMeSeeUserSettings.ForceUseOpenXR);
            if (useOpenXR != LetMeSeeUserSettings.ForceUseOpenXR)
            {
                LetMeSeeUserSettings.ForceUseOpenXR = useOpenXR;
            }

#if !LETMESEE_OPENXR_EXISTS
            if (LetMeSeeUserSettings.ForceUseOpenXR)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(MsgOpenXRMissing, MessageType.Warning);
                EditorGUI.BeginDisabledGroup(_waitOpenXR);
                if (GUILayout.Button(_waitOpenXR ? PleaseWaitLabel : InstallOpenXRLabel, GUILayout.Width(200), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
                {
                    Client.Add("com.unity.xr.openxr");
                    _waitOpenXR = true;
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
#endif

            if (XRGeneralSettings.Instance != null)
            {
                var xrOnStartup = EditorGUILayout.Toggle(InitializeXROnStartupLabel, XRGeneralSettings.Instance.InitManagerOnStart);
                if (xrOnStartup != XRGeneralSettings.Instance.InitManagerOnStart)
                {
                    XRGeneralSettings.Instance.InitManagerOnStart = xrOnStartup;
                }
            }

            var lastValidPos = LetMeSeeCore.Instance.LastValidPoseDataPos();
            EditorGUILayout.LabelField(string.Format(CultureInfo.InvariantCulture, LastValidPoseDataLabel, lastValidPos.x, lastValidPos.y, lastValidPos.z));
            
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