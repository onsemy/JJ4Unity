using System.IO;
using System.Security.Cryptography;
using JJ4Unity.Runtime.Extension;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace JJ4Unity.Editor.AssetBundle
{
    public class BuildAssetBundleHelper
    {
        [MenuItem("JJ4Unity/Build AssetBundle")]
        public static void BuildAssetBundle()
        {
            Debug.Log("Starting Addressables build...");
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("Addressables build completed.");
            
            var assetBundleDirectory = "Library/com.unity.addressables/aa/Android";
            var encryptedBundleDirectory = "Build/EncryptedBundles";

            if (false == Directory.Exists(assetBundleDirectory))
            {
                Debug.LogError($"Failed to BuildAssetBundle: directory is not exists: {assetBundleDirectory}");
                return;
            }
            
            var bundleFiles = Directory.GetFiles(assetBundleDirectory, "*", SearchOption.AllDirectories);
            foreach (var bundleFile in bundleFiles)
            {
                if (bundleFile.EndsWith(".manifest"))
                {
                    continue;
                }
                
                var fileName = Path.GetFileName(bundleFile);
                var encryptedPath = Path.Combine(encryptedBundleDirectory, fileName);
                
                EncryptedAssetBundleAES(bundleFile, encryptedPath);
                Debug.Log($"Encrypted: {fileName} -> {encryptedPath}");
            }
            
            Debug.Log("Build AssetBundle completed.");
        }

        private static void EncryptedAssetBundleAES(string inputPath, string outputPath)
        {
            var key = System.Text.Encoding.UTF8.GetBytes(JJ4UnityEditorConfig.AESKey);
            var iv = System.Text.Encoding.UTF8.GetBytes(JJ4UnityEditorConfig.AESIV);
            
            using var inputStream = new FileStream(inputPath, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            
            using var cryptoStream = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            inputStream.CopyTo(cryptoStream);
        }

        [MenuItem("JJ4Unity/Open Encrypted AssetBundle Settings")]
        public static void OpenEncryptedAssetBundleSettings()
        {
            var window = EditorWindow.GetWindow<EncryptedAssetBundleSettingsWindow>();
            window.Show();
        }
    }
}
