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
            if (ColoredBackground(isEnabled, Color.red, () => GUILayout.Button(LetMeSeeLocalizationPhrase.RunLabel)))
            {
                if (isEnabled) LetMeSeeCore.Instance.DoHardStop();
                else LetMeSeeCore.Instance.DoHardStart();
            }
            if (ColoredBackground(LetMeSeeCore.Instance.FreezeCamera, new Color(0.77f, 1f, 0f, 0.75f), () => GUILayout.Button(LetMeSeeLocalizationPhrase.FreezeCameraLabel)))
            {
                LetMeSeeCore.Instance.FreezeCamera = !LetMeSeeCore.Instance.FreezeCamera;
            }
            EditorGUILayout.Separator();
            
            if (GUILayout.Button(LetMeSeeLocalizationPhrase.RecenterViewLabel, GUILayout.Height(50)))
            {
                LetMeSeeCore.Instance.DoRecenter();
            }
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField(LetMeSeeLocalizationPhrase.IpdAndScaleLabel, EditorStyles.boldLabel);
            var isHeightEditable = LetMeSeeUserSettings.IsHeightEditable;
            var newHeightEditable = EditorGUILayout.Toggle(LetMeSeeLocalizationPhrase.EditYourHeightLabel, isHeightEditable);
            if (newHeightEditable != isHeightEditable)
            {
                LetMeSeeUserSettings.IsHeightEditable = newHeightEditable;
            }

            if (newHeightEditable)
            {
                EditorGUI.BeginDisabledGroup(!LetMeSeeUserSettings.IsHeightEditable);
                var heightInMeters = LetMeSeeUserSettings.UserHeight;
                var newHeight = EditorGUILayout.FloatField(LetMeSeeLocalizationPhrase.YourHeightMetresLabel, heightInMeters);
                if (newHeight != heightInMeters)
                {
                    LetMeSeeUserSettings.UserHeight = newHeight;
                }

                var heightInFeet = LetMeSeeUserSettings.UserHeight / 0.3048f;
                var newHeightInFeet = EditorGUILayout.FloatField(LetMeSeeLocalizationPhrase.YourHeightFeetLabel, heightInFeet);
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
                var rescaledHeight = EditorGUILayout.FloatField(LetMeSeeLocalizationPhrase.RescaledHeightMetresLabel, heightInMeters * rescale0);
                if (rescaledHeight != heightInMeters * rescale0 && rescaledHeight >= 0)
                {
                    LetMeSeeUserSettings.Rescale = rescaledHeight / heightInMeters;
                }

                var rescale1 = LetMeSeeUserSettings.Rescale;
                var rescaledHeightInFeet = EditorGUILayout.FloatField(LetMeSeeLocalizationPhrase.RescaledHeightFeetLabel, heightInFeet * rescale1);
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
                var newRescale = EditorGUILayout.FloatField(LetMeSeeLocalizationPhrase.RescaleLabel, rescale2);
                if (newRescale != rescale2 && newRescale >= 0)
                {
                    LetMeSeeUserSettings.Rescale = rescale2;
                }
            }
            if (GUILayout.Button(LetMeSeeLocalizationPhrase.ResetScaleLabel))
            {
                LetMeSeeUserSettings.Rescale = 1;
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(LetMeSeeLocalizationPhrase.AlignmentLabel, EditorStyles.boldLabel);
            var newCameraMode = (LetMeSeeCameraMode)EditorGUILayout.EnumPopup(new GUIContent(LetMeSeeLocalizationPhrase.CameraModeLabel), LetMeSeeUserSettings.CameraMode);
            if (newCameraMode != LetMeSeeUserSettings.CameraMode)
            {
                LetMeSeeUserSettings.CameraMode = newCameraMode;
            }
            if (ColoredBackground(LetMeSeeCore.Instance.LockSceneView, new Color(0f, 1f, 1f, 0.75f), () => GUILayout.Button(LetMeSeeLocalizationPhrase.DoNotSwitchSceneTabsLabel)))
            {
                LetMeSeeCore.Instance.LockSceneView = !LetMeSeeCore.Instance.LockSceneView;
            }

            var showCursor = EditorGUILayout.Toggle(LetMeSeeLocalizationPhrase.ShowCursorLabel, LetMeSeeUserSettings.ShowCursor);
            if (showCursor != LetMeSeeUserSettings.ShowCursor)
            {
                LetMeSeeUserSettings.ShowCursor = showCursor;
            }
            
            
            var cursorColor = LetMeSeeUserSettings.CursorColor;
            var newColor = EditorGUILayout.ColorField(new GUIContent(LetMeSeeLocalizationPhrase.CursorColorLabel), cursorColor);
            if (newColor != cursorColor)
            {
                LetMeSeeUserSettings.CursorColor = newColor;
                LetMeSeeCore.Instance.ForceUpdateCursorColor();
            }

            if (newCameraMode != LetMeSeeCameraMode.SceneView)
            {
                var newMoveUp = EditorGUILayout.Slider(LetMeSeeLocalizationPhrase.MoveCameraUpLabel, LetMeSeeUserSettings.MoveUp, 0, 1);
                if (newMoveUp != LetMeSeeUserSettings.MoveUp)
                {
                    LetMeSeeUserSettings.MoveUp = newMoveUp;
                }
            }
            
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField(LetMeSeeLocalizationPhrase.AdvancedLabel, EditorStyles.boldLabel);
            var useOpenXR = EditorGUILayout.Toggle(LetMeSeeLocalizationPhrase.ForceUseOpenXRLabel, LetMeSeeUserSettings.ForceUseOpenXR);
            if (useOpenXR != LetMeSeeUserSettings.ForceUseOpenXR)
            {
                LetMeSeeUserSettings.ForceUseOpenXR = useOpenXR;
            }

#if !LETMESEE_OPENXR_EXISTS
            if (LetMeSeeUserSettings.ForceUseOpenXR)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(LetMeSeeLocalizationPhrase.MsgOpenXRMissing, MessageType.Warning);
                EditorGUI.BeginDisabledGroup(_waitOpenXR);
                if (GUILayout.Button(_waitOpenXR ? LetMeSeeLocalizationPhrase.PleaseWaitLabel : LetMeSeeLocalizationPhrase.InstallOpenXRLabel, GUILayout.Width(200), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2)))
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
                var xrOnStartup = EditorGUILayout.Toggle(LetMeSeeLocalizationPhrase.InitializeXROnStartupLabel, XRGeneralSettings.Instance.InitManagerOnStart);
                if (xrOnStartup != XRGeneralSettings.Instance.InitManagerOnStart)
                {
                    XRGeneralSettings.Instance.InitManagerOnStart = xrOnStartup;
                }
            }

            var lastValidPos = LetMeSeeCore.Instance.LastValidPoseDataPos();
            EditorGUILayout.LabelField(string.Format(CultureInfo.InvariantCulture, LetMeSeeLocalizationPhrase.LastValidPoseDataLabel, lastValidPos.x, lastValidPos.y, lastValidPos.z));

            EditorGUILayout.Separator();
            LetMeSeeLocalization.DisplayLanguageSelector();
            
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