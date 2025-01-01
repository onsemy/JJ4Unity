using System;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace JJ4Unity.Editor
{
    [CreateAssetMenu(fileName = "JJ4UnitySettings", menuName = "JJ4Unity/Create JJ4UnitySettings")]
    public class JJ4UnitySettings : ScriptableObject
    {
        [SerializeField] private bool _isConnectToVSCode;
        public bool IsConnectToVSCode
        {
            get => _isConnectToVSCode;
            set => _isConnectToVSCode = value;
        }

        [SerializeField] private string _aesKey;
        public string AESKey
        {
            get => _aesKey;
            set => _aesKey = value;
        }
        
        [SerializeField] private string _aesIV;
        public string AESIV
        {
            get => _aesIV;
            set => _aesIV = value;
        }
    }

    [CustomEditor(typeof(JJ4UnitySettings))]
    public class JJ4UnitySettingsInspector : UnityEditor.Editor
    {
        private JJ4UnitySettings _settings;
        
        private void OnEnable()
        {
            _settings = target as JJ4UnitySettings;
            if (null == _settings)
            {
                return;
            }
            
            // TODO(JJO): 다른 설정이 필요하면 진행
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("JJ4Unity Settings", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VS Code Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _settings.IsConnectToVSCode = EditorGUILayout.Toggle("Is Connect to VS Code", _settings.IsConnectToVSCode);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Addressables Encrypt/Decrypt Settings", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _settings.AESKey = EditorGUILayout.TextField("AES Key", _settings.AESKey);
            _settings.AESIV = EditorGUILayout.TextField("AES IV", _settings.AESIV);
            if (GUILayout.Button("Random Key/IV"))
            {
                var keyBytes = new byte[12];
                var ivBytes = new byte[12];
                
                using var rng = RandomNumberGenerator.Create();
                rng.GetBytes(keyBytes);
                rng.GetBytes(ivBytes);

                _settings.AESKey = Convert.ToBase64String(keyBytes);
                _settings.AESIV = Convert.ToBase64String(ivBytes);
            }
            EditorGUI.indentLevel--;

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
