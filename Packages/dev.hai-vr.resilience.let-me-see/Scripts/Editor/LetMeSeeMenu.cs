using UnityEditor;
using UnityEngine;

namespace Resilience.LetMeSee
{
    public class LetMeSeeMenu
    {
#if USE_RESILIENCE_SDK
        private const string StartLabel = "Resilience/LetMeSee - Start";
        private const string StopLabel = "Resilience/LetMeSee - Stop";
        private const string RecenterViewLabel = "Resilience/LetMeSee - Recenter View";
        private const string RestartLabel = "Resilience/LetMeSee/Restart";
        private const string SoftRestartLabel = "Resilience/LetMeSee/Soft Restart";
        private const string SettingsLabel = "Resilience/LetMeSee/Edit Settings";
#else
        private const string StartLabel = "Tools/LetMeSee - Start";
        private const string StopLabel = "Tools/LetMeSee - Stop";
        private const string RecenterViewLabel = "Tools/LetMeSee - Recenter View";
        private const string RestartLabel = "Tools/LetMeSee/Restart";
        private const string SoftRestartLabel = "Tools/LetMeSee/Soft Restart";
        private const string SettingsLabel = "Tools/LetMeSee/Edit Settings";
#endif
        
        [MenuItem(StartLabel)] public static void HardStart() => LetMeSeeCore.Instance.DoHardStart();
        [MenuItem(StartLabel, true)] public static bool HardStart_Check() => !LetMeSeeCore.Instance.Enabled;
        [MenuItem(StopLabel)] public static void HardStop() => LetMeSeeCore.Instance.DoHardStop();
        [MenuItem(StopLabel, true)] public static bool HardStop_Check() => LetMeSeeCore.Instance.Enabled;
        [MenuItem(RecenterViewLabel)] public static void RecenterView() => LetMeSeeCore.Instance.DoRecenter();
        [MenuItem(RecenterViewLabel, true)] public static bool RecenterView_Check() => LetMeSeeCore.Instance.Enabled;
        [MenuItem(RestartLabel)] public static void HardRestart() => LetMeSeeCore.Instance.DoHardRestart();
        [MenuItem(RestartLabel, true)] public static bool HardRestart_Check() => LetMeSeeCore.Instance.Enabled;
        [MenuItem(SoftRestartLabel)] public static void SoftRestart() => LetMeSeeCore.Instance.DoSoftRestart();
        [MenuItem(SoftRestartLabel, true)] public static bool SoftRestart_Check() => LetMeSeeCore.Instance.Enabled;
        [MenuItem(SettingsLabel)] public static void Settings()
        {
            Obtain().Show();
        }

        private static LetMeSeeEditorWindow Obtain()
        {
            var editor = EditorWindow.GetWindow<LetMeSeeEditorWindow>(false, null, false);
            editor.titleContent = new GUIContent("LetMeSee");
            return editor;
        }
    }
}