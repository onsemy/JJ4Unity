using UnityEditor;

namespace JJ4Unity.Editor
{
    public static class JJ4UnityEditorConfig
    {
        public static bool IsConnectToVSCode { get; private set; }
        
        public static string AESKey { get; set; }
        public static string AESIV { get; set; }

        public static void Initialize()
        {
            IsConnectToVSCode = EditorPrefs.GetBool(nameof(IsConnectToVSCode), true);
            AESKey = EditorPrefs.GetString(nameof(AESKey), null);
            AESIV = EditorPrefs.GetString(nameof(AESIV), null);
        }
        
        [MenuItem("JJ4Unity/Toggle Connect To VSCode")]
        public static void ToggleConnectToVSCode()
        {
            if (false == EditorUtility.DisplayDialog("Warning", "Are you sure you want to toggle this option?\n\nPressing OK will recompile the script to apply the toggled value. Unsaved information will be lost.", "OK", "Cancel"))
            {
                return;
            }
            
            IsConnectToVSCode = !IsConnectToVSCode;
            EditorPrefs.SetBool(nameof(IsConnectToVSCode), IsConnectToVSCode);
            EditorUtility.RequestScriptReload();
        }
        
        [MenuItem("JJ4Unity/Toggle Connect To VSCode", true)]
        public static bool ToggleConnectToVSCodeValidate()
        {
            Menu.SetChecked("JJ4Unity/Toggle Connect To VSCode", IsConnectToVSCode);
            return true;
        }
    }
}
