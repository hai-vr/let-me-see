using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            SceneView.duringSceneGui -= OnDuringSceneGui;
            SceneView.duringSceneGui += OnDuringSceneGui;
        }

        public static void UnregisterEditModeHook()
        {
            EditorApplication.update -= OnUpdate;
            Application.onBeforeRender -= OnBeforeRender;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            SceneView.duringSceneGui -= OnDuringSceneGui;
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

        private static void OnSceneSaving(Scene scene, string path)
        {
            LetMeSeeCore.Instance.DoSceneSaving();
        }

        private static void OnDuringSceneGui(SceneView obj)
        {
            LetMeSeeCore.Instance.DoDuringSceneGui(obj);
        }
    }
}