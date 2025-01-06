using System.IO;
using System.Security.Cryptography;
using JJ4Unity.Runtime.AssetBundle;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
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

                var originDirectoryPath = Path.GetDirectoryName(buildResult.OutputPath);
                if (false == string.IsNullOrEmpty(originDirectoryPath))
                {
                    ModifyContentCatalog(originDirectoryPath, aaContext);
                }
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
            string originDirectoryPath,
            AddressableAssetsBuildContext aaContext
        )
        {
#if ENABLE_JSON_CATALOG
            // NOTE(JJO): catalog.json
            ModifyContentCatalogFromJson(originDirectoryPath, aaContext);
#else
            // NOTE(JJO): catalog.bin
            ModifyContentCatalogFromBinary(originDirectoryPath, aaContext);
#endif
        }

#if ENABLE_JSON_CATALOG
        private void ModifyContentCatalogFromJson(
            string originDirectoryPath,
            AddressableAssetsBuildContext aaContext
        )
        {
            var originCatalogPath = Path.Combine(originDirectoryPath, "catalog.json");
            if (false == File.Exists(originCatalogPath))
            {
                return;
            }

            var targetCatalogPath = $"{originCatalogPath}.tmp";

            var cd = CreateContentCatalogData(aaContext);
            var updatedJson = JsonUtility.ToJson(cd);

            using (var writer = new StreamWriter(targetCatalogPath, false))
            {
                writer.Write(updatedJson);
            }

            File.Delete(originCatalogPath);
            File.Move(targetCatalogPath, originCatalogPath);

            Debug.Log($"Content Catalog updated with EncryptedAssetBundleProvider: {originCatalogPath}");
        }
#else
        private void ModifyContentCatalogFromBinary(
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

            var cd = CreateContentCatalogData(aaContext);

            var bytes = cd.SerializeToByteArray();
            var contentHash = HashingMethods.Calculate(bytes);

            if (aaContext.Settings.BuildRemoteCatalog
                || ProjectConfigData.GenerateBuildLayout)
            {
                cd.LocalHash = contentHash.ToString();
            }

            using (var writeFileStream = new FileStream(targetCatalogPath, FileMode.Create, FileAccess.Write))
            {
                writeFileStream.Write(bytes, 0, bytes.Length);
            }

            File.Delete(originCatalogPath);
            File.Move(targetCatalogPath, originCatalogPath);

            Debug.Log($"Content Catalog updated with EncryptedAssetBundleProvider: {originCatalogPath}");
        }
#endif

        /// <summary>
        /// NOTE(JJO): aaContext를 이용하여 생성된 ContentCatalogData 재구성
        /// </summary>
        /// <param name="aaContext"></param>
        /// <returns></returns>
        private ContentCatalogData CreateContentCatalogData(AddressableAssetsBuildContext aaContext)
        {
            for (int i = aaContext.locations.Count - 1; i >= 0; i--)
            {
                var location = aaContext.locations[i];
                if (location.Provider != typeof(AssetBundleProvider).FullName)
                {
                    continue;
                }

                // NOTE(JJO): EncryptedAssetBundleProvider와 DecryptedBundleResource로 재설정
                var newLocation = new ContentCatalogDataEntry(
                    typeof(DecryptedBundleResource),
                    location.InternalId,
                    typeof(EncryptedAssetBundleProvider).FullName,
                    location.Keys,
                    location.Dependencies,
                    location.Data
                );

                aaContext.locations[i] = newLocation;
            }

            // NOTE(JJO): 재설정한 locations를 새로 생성하는 카탈로그에 설정
            var contentCatalogData = new ContentCatalogData(
                aaContext.locations,
                ResourceManagerRuntimeData.kCatalogAddress
            );

            var schema = aaContext
                .Settings
                .DefaultGroup
                .GetSchema<BundledAssetGroupSchema>();

            // NOTE(JJO): AddressableAssetSettings에 설정한 AssetBundleProvider를 EncryptedAssetBundleProvider로 변경
            var assetBundleProviderType =
                typeof(AssetBundleProvider) == schema.AssetBundleProviderType.Value
                    ? typeof(EncryptedAssetBundleProvider)
                    : schema.AssetBundleProviderType.Value;

            contentCatalogData.ResourceProviderData.Add(
                ObjectInitializationData.CreateSerializedInitializationData(
                    assetBundleProviderType
                )
            );

            // NOTE(JJO): 빌드에서 이미 쓰여진 ProviderType들을 검사하여 EncryptedAssetBundleProvider로 변경
            foreach (var type in aaContext.providerTypes)
            {
                var providerType =
                    typeof(AssetBundleProvider) == type
                        ? typeof(EncryptedAssetBundleProvider)
                        : type;

                contentCatalogData.ResourceProviderData.Add(
                    ObjectInitializationData.CreateSerializedInitializationData(
                        providerType
                    )
                );
            }

            contentCatalogData.InstanceProviderData =
                ObjectInitializationData.CreateSerializedInitializationData(
                    instanceProviderType.Value
                );
            contentCatalogData.SceneProviderData =
                ObjectInitializationData.CreateSerializedInitializationData(
                    sceneProviderType.Value
                );

            return contentCatalogData;
        }
    }
}