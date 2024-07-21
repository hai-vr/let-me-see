using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Management;
#if LETMESEE_OPENXR_EXISTS
using UnityEngine.XR.OpenXR;
#endif

namespace Resilience.LetMeSee
{
    [InitializeOnLoad]
    public class LetMeSeeCore
    {
        public static readonly LetMeSeeCore Instance = new LetMeSeeCore();
        internal const string Prefix = "Resilience.LetMeSee";
        private const float SmallAmount = 0.000001f;
        
        private Vector3 _initialLocalPos = Vector3.zero;
        private Quaternion _initialLocalRot = Quaternion.identity;
        private Vector3 _initialLocalScale = Vector3.one;
        private bool _resetView;
        
        private Vector3 RecenterCapturedPos
        {
            get => SessionState.GetVector3(Key(nameof(RecenterCapturedPos)), Vector3.zero);
            set => SessionState.SetVector3(Key(nameof(RecenterCapturedPos)), value);
        }
        private Vector3 RecenterCapturedRotEuler
        {
            get => SessionState.GetVector3(Key(nameof(RecenterCapturedRotEuler)), Vector3.zero);
            set => SessionState.SetVector3(Key(nameof(RecenterCapturedRotEuler)), value);
        }
        public bool LockSceneView
        {
            get => SessionState.GetBool(Key(nameof(LockSceneView)), false);
            set => SessionState.SetBool(Key(nameof(LockSceneView)), value);
        }
        public bool FreezeCamera
        {
            get => SessionState.GetBool(Key(nameof(FreezeCamera)), false);
            set => SessionState.SetBool(Key(nameof(FreezeCamera)), value);
        }
        
        private Vector3 _lastValidPoseDataPos;
        private Quaternion _lastValidPoseDataRot = Quaternion.identity;
        
        private Vector3 _scenePos;
        private Quaternion _sceneRot;
        private SceneView _sceneViewToUse;
        private Camera _camRef;

        public bool Enabled
        {
            get => SessionState.GetBool(Key(nameof(Enabled)), false);
            set => SessionState.SetBool(Key(nameof(Enabled)), value);
        }

        private string Key(object prop)
        {
            return $"{Prefix}.{typeof(LetMeSeeCore)}.{prop}";
        }

        static LetMeSeeCore()
        {
            TryUpdateInitXR();

            if (Instance.Enabled)
            {
                LetMeSeeHooks.RegisterEditModeHook();
            }
        }

        private static void TryUpdateInitXR()
        {
            if (LetMeSeeUserSettings.DefaultXRSettingOverride < 1)
            {
                if (XRGeneralSettings.Instance != null)
                {
                    // When XR Management Plugin is installed for the first time, it will set itself to run on start.
                    // We want to avoid this happening on users' projects.
                    XRGeneralSettings.Instance.InitManagerOnStart = false;
                    LetMeSeeUserSettings.DefaultXRSettingOverride = 1;
                }
            }
        }

        internal void DoPlayModeStateChanged(PlayModeStateChange change)
        {
            if (!Enabled) return;
            if (change == PlayModeStateChange.ExitingEditMode)
            {
                RestoreCamera();
            }

            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                TryUpdateInitXR();
            }

            if (change == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += TryHardRestart;
            }
        }

        private void TryHardRestart()
        {
            if (!Enabled) return;
            DoHardRestart();
        }

        public void DoRecenter()
        {
            _resetView = true;
        }

        internal void DoUpdate()
        {
            RunHMDUpdateHack();
            
            if (_resetView)
            {
                _resetView = false;
                
                var pos = _lastValidPoseDataPos;
                var rot = _lastValidPoseDataRot;
                
                RecenterCapturedPos = -pos;
                RecenterCapturedRotEuler = new Vector3(0, -rot.eulerAngles.y, 0);
            }

            if (!LockSceneView || _sceneViewToUse == null)
            {
                _sceneViewToUse = SceneView.lastActiveSceneView;
            }

            if (!FreezeCamera && _sceneViewToUse != null)
            {
                var sceneCamera = _sceneViewToUse.camera;
                var sceneTransform = sceneCamera.transform;
            
                var scenePos = sceneTransform.position;
                var sceneRot = sceneTransform.rotation;
                var sceneRotEuler = sceneRot.eulerAngles;
                _scenePos = scenePos;
                _sceneRot = Quaternion.Euler(0, sceneRotEuler.y, 0);
            }

            if (!IsUnityEditorWindowFocused())
            {
                // If the window is not focused, the Game Tab will not redraw. Force repaint it.
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            }
        }

        private static bool IsUnityEditorWindowFocused()
        {
            return UnityEditorInternal.InternalEditorUtility.isApplicationActive;
        }

        private static void RunHMDUpdateHack()
        {
            // This is a hack.
            // We want to get the Game tab to update. This hack moves the camera by a tiny bit,
            // so that the Unity Editor will trigger the Application.onBeforeRender hook.
            var hmd = SLOW_GetVRCameraOrNull();
            if (hmd == null) return;
            hmd.transform.position += GetSmallVector();
        }

        internal void DoBeforeRender()
        {
            var poseFlags = PoseDataSource.GetDataFromSource(TrackedPoseDriver.TrackedPose.Center, out var currentPose);
            if (poseFlags != PoseDataFlags.NoData)
            {
                var camera = SLOW_GetVRCameraOrNull();

                _lastValidPoseDataPos = currentPose.position;
                _lastValidPoseDataRot = currentPose.rotation;

                var recenterCapturedRot = Quaternion.Euler(RecenterCapturedRotEuler);

                var rescale = LetMeSeeUserSettings.Rescale;
                var accountedScale = _initialLocalScale * rescale;
                var accountedScaleScalar = accountedScale.y;
                var recenterPos = recenterCapturedRot * (_lastValidPoseDataPos + RecenterCapturedPos);
                var recenterRot = recenterCapturedRot * _lastValidPoseDataRot;
                
                var cameraMode = LetMeSeeUserSettings.CameraMode;
                if (cameraMode == LetMeSeeCameraMode.SceneView)
                {
                    camera.transform.position = _sceneRot * recenterPos * accountedScaleScalar + _scenePos;
                    camera.transform.rotation = _sceneRot * recenterRot;
                }
                else if (cameraMode == LetMeSeeCameraMode.LocalSpace)
                {
                    camera.transform.localPosition = recenterPos * rescale + LetMeSeeUserSettings.MoveUp * Vector3.up * LetMeSeeUserSettings.UserHeight * LetMeSeeUserSettings.Rescale;
                    camera.transform.localRotation = recenterRot;
                }
                else if (cameraMode == LetMeSeeCameraMode.LocalPosition)
                {
                    camera.transform.localPosition = recenterPos * rescale + LetMeSeeUserSettings.MoveUp * Vector3.up * LetMeSeeUserSettings.UserHeight * LetMeSeeUserSettings.Rescale;
                    camera.transform.rotation = recenterRot;
                }

                camera.transform.localScale = accountedScale;
            }
        }

        internal void DoHardStart()
        {
            if (Enabled) return;

            if (LetMeSeeUserSettings.ForceUseOpenXR)
            {
#if LETMESEE_OPENXR_EXISTS
                var loaders = new List<XRLoader> { ScriptableObject.CreateInstance<OpenXRLoader>() };
                XRGeneralSettings.Instance.Manager.TrySetLoaders(loaders);
#endif
            }
            XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
            XRGeneralSettings.Instance.Manager.StartSubsystems();
            LetMeSeeHooks.RegisterEditModeHook();

            SaveCamera();

            Enabled = true;
        }

        internal void DoHardStop()
        {
            if (!Enabled) return;
            
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            LetMeSeeHooks.UnregisterEditModeHook();
            
            RestoreCamera();
            
            Enabled = false;
        }

        public void DoHardRestart()
        {
            DoHardStop();
            DoHardStart();
        }

        internal void DoSoftRestart()
        {
            if (!Enabled) return;
            
            XRGeneralSettings.Instance.Manager.StopSubsystems();
            XRGeneralSettings.Instance.Manager.StartSubsystems();
        }

        private static Camera SLOW_GetVRCameraOrNull()
        {
            var main = Camera.main;
            if (main != null) return main;
            
            var hmd = Object.FindObjectOfType<Camera>();
            if (hmd.targetTexture != null) return null;
            
            return hmd;
        }

        private void SaveCamera()
        {
            var cam = SLOW_GetVRCameraOrNull();
            if (cam != null)
            {
                _camRef = cam;
                _initialLocalPos = cam.transform.localPosition;
                _initialLocalRot = cam.transform.localRotation;
                _initialLocalScale = cam.transform.localScale;
            }
        }

        private void RestoreCamera()
        {
            var cam = SLOW_GetVRCameraOrNull();
            if (cam != null && _camRef == cam)
            {
                cam.transform.localPosition = _initialLocalPos;
                cam.transform.localRotation = _initialLocalRot;
                cam.transform.localScale = _initialLocalScale;
            }
        }

        private static Vector3 GetSmallVector()
        {
            var smallButNotTooSmallAmount = Random.Range(SmallAmount / 10f, SmallAmount);
            var randomDirection = Random.Range(0, 1) > 0.5f ? 1 : -1;
            return Vector3.left * smallButNotTooSmallAmount * randomDirection;
        }

        public Vector3 LastValidPoseDataPos()
        {
            return _lastValidPoseDataPos;
        }
    }

    public enum LetMeSeeCameraMode
    {
        SceneView, LocalSpace, LocalPosition
    }
}