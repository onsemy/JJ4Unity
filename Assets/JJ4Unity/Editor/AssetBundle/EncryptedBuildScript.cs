using System.IO;
using System.Security.Cryptography;
using JJ4Unity.Runtime.AssetBundle;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using Debug = JJ4Unity.Runtime.Extension.Debug;

namespace JJ4Unity.Editor.AssetBundle
{
    [CreateAssetMenu(fileName = "EncryptedBuildScript", menuName = "Addressables/Encrypted Build Script")]
    public class EncryptedBuildScript : BuildScriptPackedMode
    {
        public override string Name => "Encrypted Build Script";
        private JJ4UnitySettings _settings;

        protected override TResult DoBuild<TResult>(
            AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext
        )
        {
            _settings = LoadSettings();
            if (null == _settings)
            {
                return default;
            }
            
            // TODO(JJO): base.DoBuild하기 전에 aaContent를 수정해야 함.
            //
            
            var result = base.DoBuild<TResult>(builderInput, aaContext);

            if (null == result.Error
                && result is AddressablesPlayerBuildResult buildResult
            )
            {
                EncryptBuiltBundles(buildResult);

                ModifyContentCatalog(buildResult);
            }

            return result;
        }

        private JJ4UnitySettings LoadSettings()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(JJ4UnitySettings)}");
            if (guids.Length == 0)
            {
                var message =
                    $"{nameof(JJ4UnitySettings)} not found.\n\nTry to `[JJ4Unity]->[Create JJ4Unity Settings in Assets]`";
                if (false == Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Error", message, "OK");
                }
                Debug.LogError(message);
                return null;
            }
            
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            var settings = AssetDatabase.LoadAssetAtPath<JJ4UnitySettings>(path);
            if (null == settings)
            {
                var message =
                    $"{nameof(JJ4UnitySettings)} not found.\n\nTry to `[JJ4Unity]->[Create JJ4Unity Settings in Assets]`";
                if (false == Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Error", message, "OK");
                }
                Debug.LogError(message);
                return null;
            }

            return settings;
        }

        private void EncryptBuiltBundles(AddressablesPlayerBuildResult result)
        {
            var buildResults = result.AssetBundleBuildResults;

            var key = System.Text.Encoding.UTF8.GetBytes(_settings.AESKey);
            var iv = System.Text.Encoding.UTF8.GetBytes(_settings.AESIV);

            foreach (var buildResult in buildResults)
            {
                var bundleFile = buildResult.FilePath;
                var tempFile = $"{bundleFile}.tmp";

                Encrypt(bundleFile, tempFile, key, iv);

                File.Delete(bundleFile);
                File.Move(tempFile, bundleFile);

                Debug.Log($"Encrypted Bundle: {bundleFile}");
            }
        }

        private void Encrypt(string bundleFile, string tempFile, byte[] key, byte[] iv)
        {
            using var inputStream = new FileStream(bundleFile, FileMode.Open, FileAccess.Read);
            using var outputStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write);
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write);
            inputStream.CopyTo(cryptoStream);
        }

        private void ModifyContentCatalog(AddressablesPlayerBuildResult buildResult)
        {
            var originDirectoryPath = Path.GetDirectoryName(buildResult.OutputPath);
            if (string.IsNullOrEmpty(originDirectoryPath))
            {
                return;
            }

            ModifyContentCatalogFromJson(originDirectoryPath);

            ModifyContentCatalogFromBinary(originDirectoryPath);
        }

        private static bool ModifyContentCatalogFromJson(string originDirectoryPath)
        {
            var originCatalogPath = Path.Combine(originDirectoryPath, "catalog.json");
            if (false == File.Exists(originCatalogPath))
            {
                return false;
            }

            var targetCatalogPath = $"{originCatalogPath}.tmp";

            using (var reader = new StreamReader(originCatalogPath))
            {
                var catalogJson = reader.ReadToEnd();
                var updatedJson = catalogJson.Replace(
                    typeof(AssetBundleProvider).FullName!,
                    typeof(EncryptedAssetBundleProvider).FullName);

                using var writer = new StreamWriter(targetCatalogPath, false);
                writer.Write(updatedJson);
            }

            File.Delete(originCatalogPath);
            File.Move(targetCatalogPath, originCatalogPath);

            Debug.Log($"Content Catalog updated with EncryptedAssetBundleProvider: {originCatalogPath}");

            return true;
        }
        
        private static bool ModifyContentCatalogFromBinary(string originDirectoryPath)
        {
            var originCatalogPath = Path.Combine(originDirectoryPath, "catalog.bin");
            if (false == File.Exists(originCatalogPath))
            {
                originCatalogPath = Path.Combine(originDirectoryPath, "catalog.bundle");
            }

            if (false == File.Exists(originCatalogPath))
            {
                return false;
            }

            var targetCatalogPath = $"{originCatalogPath}.tmp";
            
            using (var readFileStream = new FileStream(originCatalogPath, FileMode.Open, FileAccess.Read))
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                var catalog = (ContentCatalogData)formatter.Deserialize(readFileStream);
                for (int i = catalog.ResourceProviderData.Count; i >= 0; i--)
                {
                    if (catalog.ResourceProviderData[i].Id == typeof(AssetBundleProvider).FullName)
                    {
                        var resource = ObjectInitializationData.CreateSerializedInitializationData(typeof(EncryptedAssetBundleProvider));
                        catalog.ResourceProviderData.RemoveAt(i);
                        catalog.ResourceProviderData.Add(resource);
                    }
                }

                for (int i = catalog.ProviderIds.Length; i >= 0; i--)
                {
                    if (catalog.ProviderIds[i] == typeof(AssetBundleProvider).FullName)
                    {
                        catalog.ProviderIds[i] = typeof(EncryptedAssetBundleProvider).FullName;
                    }
                }

                using var writeFileStream = new FileStream(targetCatalogPath, FileMode.Create, FileAccess.Write);
                var writeFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                writeFormatter.Serialize(writeFileStream, catalog);
            }

            File.Delete(originCatalogPath);
            File.Move(targetCatalogPath, originCatalogPath);

            Debug.Log($"Content Catalog updated with EncryptedAssetBundleProvider: {originCatalogPath}");

            return true;
        }
    }
}