using UnityEditor;

namespace Resilience.LetMeSee
{
    public class LetMeSeeUserSettings
    {
        public static float UserHeight
        {
            get => EditorPrefs.GetFloat(Key(nameof(UserHeight)), 1.7f);
            set => EditorPrefs.SetFloat(Key(nameof(UserHeight)), value);
        }
        
        public static bool IsHeightEditable
        {
            get => EditorPrefs.GetBool(Key(nameof(IsHeightEditable)), true);
            set => EditorPrefs.SetBool(Key(nameof(IsHeightEditable)), value);
        }
        
        public static LetMeSeeCameraMode CameraMode
        {
            get => (LetMeSeeCameraMode)EditorPrefs.GetInt(Key(nameof(CameraMode)), (int)LetMeSeeCameraMode.SceneView);
            set => EditorPrefs.SetInt(Key(nameof(CameraMode)), (int)value);
        }

        public static float Rescale
        {
            get => EditorPrefs.GetFloat(Key(nameof(Rescale)), 1f);
            set => EditorPrefs.SetFloat(Key(nameof(Rescale)), value);
        }

        public static float MoveUp
        {
            get => EditorPrefs.GetFloat(Key(nameof(MoveUp)), 0f);
            set => EditorPrefs.SetFloat(Key(nameof(MoveUp)), value);
        }
        
        public static bool ForceUseOpenXR
        {
            get => EditorPrefs.GetBool(Key(nameof(ForceUseOpenXR)), true);
            set => EditorPrefs.SetBool(Key(nameof(ForceUseOpenXR)), value);
        }
        
        public static int DefaultXRSettingOverride
        {
            get => EditorPrefs.GetInt(Key(nameof(DefaultXRSettingOverride)), 0);
            set => EditorPrefs.SetInt(Key(nameof(DefaultXRSettingOverride)), value);
        }

        private static string Key(object prop)
        {
            return $"{LetMeSeeCore.Prefix}.{typeof(LetMeSeeUserSettings)}.{prop}";
        }
    }
}