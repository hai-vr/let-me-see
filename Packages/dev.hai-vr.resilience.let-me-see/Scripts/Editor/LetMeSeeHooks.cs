using UnityEditor;
using UnityEngine;

namespace Resilience.LetMeSee
{
    public class LetMeSeeHooks
    {
        public static void RegisterEditModeHook()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            Application.onBeforeRender -= OnBeforeRender;
            Application.onBeforeRender += OnBeforeRender;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        public static void UnregisterEditModeHook()
        {
            EditorApplication.update -= OnUpdate;
            Application.onBeforeRender -= OnBeforeRender;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            LetMeSeeCore.Instance.DoPlayModeStateChanged(change);
        }

        private static void OnUpdate()
        {
            LetMeSeeCore.Instance.DoUpdate();
        }

        [BeforeRenderOrder(-30000)] // Copied from TrackedPoseDriver
        private static void OnBeforeRender()
        {
            LetMeSeeCore.Instance.DoBeforeRender();
        }
    }
}