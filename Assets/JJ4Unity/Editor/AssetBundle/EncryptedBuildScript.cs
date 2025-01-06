using System.IO;
using System.Linq;
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

            var result = base.DoBuild<TResult>(builderInput, aaContext);

            if (null == result.Error
                && result is AddressablesPlayerBuildResult buildResult
               )
            {
                EncryptBuiltBundles(buildResult);

                ModifyContentCatalog(buildResult, aaContext);
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

        private void ModifyContentCatalog(
            AddressablesPlayerBuildResult buildResult,
            AddressableAssetsBuildContext aaContext
        )
        {
            var originDirectoryPath = Path.GetDirectoryName(buildResult.OutputPath);
            if (string.IsNullOrEmpty(originDirectoryPath))
            {
                return;
            }

#if ENABLE_JSON_CATALOG
            // NOTE(JJO): catalog.json
            ModifyContentCatalogFromJson(originDirectoryPath);
#else
            // NOTE(JJO): catalog.bin
            ModifyContentCatalogFromBinary(originDirectoryPath, aaContext);
#endif
        }

        private static void ModifyContentCatalogFromJson(string originDirectoryPath)
        {
            var originCatalogPath = Path.Combine(originDirectoryPath, "catalog.json");
            if (false == File.Exists(originCatalogPath))
            {
                return;
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
        }

        private static void ModifyContentCatalogFromBinary(
            string originDirectoryPath,
            AddressableAssetsBuildContext aaContext
        )
        {
            var originCatalogPath = Path.Combine(originDirectoryPath, "catalog.bin");
            if (false == File.Exists(originCatalogPath))
            {
                return;
            }

            var targetCatalogPath = $"{originCatalogPath}.tmp";

            using (var readFileStream = new FileStream(originCatalogPath, FileMode.Open, FileAccess.Read))
            {
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                var catalog = (ContentCatalogData)formatter.Deserialize(readFileStream);
                // ModifyContentCatalogImpl(catalog);
                for (int i = catalog.ResourceProviderData.Count - 1; i >= 0; i--)
                {
                    if (catalog.ResourceProviderData[i].Id == typeof(AssetBundleProvider).FullName)
                    {
                        var resource = ObjectInitializationData.CreateSerializedInitializationData(typeof(EncryptedAssetBundleProvider));
                        catalog.ResourceProviderData.RemoveAt(i);
                        catalog.ResourceProviderData.Add(resource);
                    }
                }
                
                for (int i = aaContext.locations.Count - 1; i >= 0; i--)
                {
                    var location = aaContext.locations[i];
                    if (location.Provider != typeof(AssetBundleProvider).FullName)
                    {
                        continue;
                    }

                    var newLocation = new ContentCatalogDataEntry(
                        location.ResourceType,
                        location.InternalId,
                        typeof(EncryptedAssetBundleProvider).FullName,
                        location.Keys,
                        location.Dependencies,
                        location.Data
                    );

                    aaContext.locations[i] = newLocation;
                }
                
                // TODO(JJO): ContentCatalog에 SetData
                catalog.SetData(aaContext.locations.OrderBy(f => f.InternalId).ToList());
                // for (int i = catalog.ProviderIds.Length - 1; i >= 0; i--)
                // {
                //     if (catalog.ProviderIds[i] == typeof(AssetBundleProvider).FullName)
                //     {
                //         catalog.ProviderIds[i] = typeof(EncryptedAssetBundleProvider).FullName;
                //     }
                // }

                var bytes = catalog.SerializeToByteArray();
                using var writeFileStream = new FileStream(targetCatalogPath, FileMode.Create, FileAccess.Write);
                writeFileStream.Write(bytes, 0, bytes.Length);
                // var writeFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                // writeFormatter.Serialize(writeFileStream, catalog);
            }

            File.Delete(originCatalogPath);
            File.Move(targetCatalogPath, originCatalogPath);

            Debug.Log($"Content Catalog updated with EncryptedAssetBundleProvider: {originCatalogPath}");
        }

        // private static bool ModifyContentCatalogFromBundle(string originDirectoryPath)
        // {
        //     var originCatalogPath = Path.Combine(originDirectoryPath, "catalog.bundle");
        //     if (false == File.Exists(originCatalogPath))
        //     {
        //         Debug.LogWarning($"Failed to modify catalog - file not exist: {originCatalogPath}");
        //         return false;
        //     }
        //
        //     var targetCatalogPath = $"{originCatalogPath}.tmp";
        //
        //     var bundle = UnityEngine.AssetBundle.LoadFromFile(originCatalogPath);
        //     if (null == bundle)
        //     {
        //         Debug.LogError($"Failed to load bundle: {originCatalogPath}");
        //         return false;
        //     }
        //
        //     var names = bundle.GetAllAssetNames();
        //     if (names.Length == 0)
        //     {
        //         Debug.LogError($"Failed to load bundle - no assets: {originCatalogPath}");
        //         bundle.Unload(false);
        //         return false;
        //     }
        //     
        //     var catalogData = bundle.LoadAsset<TextAsset>(names[0]);
        //     bundle.Unload(false);
        //     if (null == catalogData)
        //     {
        //         Debug.LogError($"Failed to load catalog data: {originCatalogPath}");
        //         return false;
        //     }
        //     
        //     var catalog = JsonUtility.FromJson<ContentCatalogData>(catalogData.text);
        //     var modifiedJson = ModifyContentCatalogImpl(catalog);
        //     var tempPath = "Assets/catalog.json";
        //     
        //     File.WriteAllText(tempPath, modifiedJson);
        //     AssetDatabase.ImportAsset(tempPath);
        //     AssetDatabase.Refresh();
        //
        //     var buildMap = new AssetBundleBuild[]
        //     {
        //         new()
        //         {
        //             assetBundleName = "catalog.bundle",
        //             assetNames = new[] { tempPath }
        //         }
        //     };
        //
        //     if (Directory.Exists("BuildBundles"))
        //     {
        //         Directory.Delete("BuildBundles", true);
        //     }
        //     
        //     Directory.CreateDirectory("BuildBundles");
        //     
        //     var manifest = BuildPipeline.BuildAssetBundles("BuildBundles", buildMap, BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
        //     if (null != manifest)
        //     {
        //         AssetDatabase.DeleteAsset(tempPath);
        //         AssetDatabase.Refresh();
        //     }
        //     
        //     File.Delete(originCatalogPath);
        //     File.Move("BuildBundles/catalog.bundle", originCatalogPath);
        //     // File.Copy("BuildBundles/catalog.bundle", originCatalogPath, true);
        //     
        //     Directory.Delete("BuildBundles", true);
        //
        //     Debug.Log($"Content Catalog updated with EncryptedAssetBundleProvider: {originCatalogPath}");
        //
        //     return true;
        // }

        private static string ModifyContentCatalogImpl(ContentCatalogData catalog)
        {
            for (int i = catalog.ResourceProviderData.Count - 1; i >= 0; i--)
            {
                if (catalog.ResourceProviderData[i].Id == typeof(AssetBundleProvider).FullName)
                {
                    var resource =
                        ObjectInitializationData.CreateSerializedInitializationData(
                            typeof(EncryptedAssetBundleProvider));
                    catalog.ResourceProviderData.RemoveAt(i);
                    catalog.ResourceProviderData.Insert(i, resource);
                }
            }

#if ENABLE_JSON_CATALOG
            for (int i = catalog.ProviderIds.Length - 1; i >= 0; i--)
            {
                if (catalog.ProviderIds[i] == typeof(AssetBundleProvider).FullName)
                {
                    catalog.ProviderIds[i] = typeof(EncryptedAssetBundleProvider).FullName;
                }
            }
#else
#endif

            var modifiedJson = JsonUtility.ToJson(catalog);
            return modifiedJson;
        }
    }
}