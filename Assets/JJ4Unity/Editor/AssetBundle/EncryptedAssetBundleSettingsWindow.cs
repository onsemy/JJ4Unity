using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace JJ4Unity.Editor.AssetBundle
{
    public class EncryptedAssetBundleSettingsWindow : EditorWindow
    {
        private void OnGUI()
        {
            JJ4UnityEditorConfig.AESKey = EditorGUILayout.TextField("AES Key:", JJ4UnityEditorConfig.AESKey);
            JJ4UnityEditorConfig.AESIV = EditorGUILayout.TextField("AES IV:", JJ4UnityEditorConfig.AESIV);

            if (GUILayout.Button("Random Key/IV"))
            {
                var keyBytes = new byte[12];
                var ivBytes = new byte[12];
                
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(keyBytes);
                rng.GetBytes(ivBytes);

                JJ4UnityEditorConfig.AESKey = Convert.ToBase64String(keyBytes);
                JJ4UnityEditorConfig.AESIV = Convert.ToBase64String(ivBytes);
            }
            
            if (GUILayout.Button("Save Settings"))
            {
                EditorPrefs.SetString("AESKey", JJ4UnityEditorConfig.AESKey);
                EditorPrefs.SetString("AESIV", JJ4UnityEditorConfig.AESIV);
            }
        }
    }
}
