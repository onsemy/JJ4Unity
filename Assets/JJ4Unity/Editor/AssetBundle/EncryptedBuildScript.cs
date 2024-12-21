using System.IO;
using System.Security.Cryptography;
using JJ4Unity.Runtime.AssetBundle;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace JJ4Unity.Editor.AssetBundle
{
    [CreateAssetMenu(fileName = "EncryptedBuildScript", menuName = "Addressables/Encrypted Build Script")]
    public class EncryptedBuildScript : BuildScriptPackedMode
    {
        public override string Name => "Encrypted Build Script";

        protected override TResult DoBuild<TResult>(
            AddressablesDataBuilderInput builderInput,
            AddressableAssetsBuildContext aaContext
        )
        {
            var result = base.DoBuild<TResult>(builderInput, aaContext);

            if (null == result.Error && result is AddressablesPlayerBuildResult buildResult)
            {
                EncryptBuiltBundles(buildResult);

                var originDirectoryPath = Path.GetDirectoryName(buildResult.OutputPath);
                var targetDirectoryPath = Path.GetDirectoryName(buildResult.AssetBundleBuildResults[0].FilePath);
                if (false == string.IsNullOrEmpty(originDirectoryPath) &&
                    false == string.IsNullOrEmpty(targetDirectoryPath)
                )
                {
                    var originCatalogPath = Path.Combine(originDirectoryPath, "catalog.json");
                    var targetCatalogPath = Path.Combine(targetDirectoryPath, "catalog.json.tmp");

                    ModifyContentCatalog(originCatalogPath, targetCatalogPath);
                    
                    File.Delete(originCatalogPath);
                    File.Move(targetCatalogPath, originCatalogPath);
                }
            }

            return result;
        }

        private void EncryptBuiltBundles(AddressablesPlayerBuildResult result)
        {
            var buildResults = result.AssetBundleBuildResults;

            var key = System.Text.Encoding.UTF8.GetBytes(JJ4UnityEditorConfig.AESKey);
            var iv = System.Text.Encoding.UTF8.GetBytes(JJ4UnityEditorConfig.AESIV);

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

        private void ModifyContentCatalog(string originPath, string targetPath)
        {
            if (false == File.Exists(originPath))
            {
                Debug.LogError($"Catalog file does not exist: {originPath}");
                return;
            }

            using var reader = new StreamReader(originPath);
            var catalogJson = reader.ReadToEnd();
            var updatedJson = catalogJson.Replace(
                typeof(AssetBundleProvider).FullName!,
                typeof(EncryptedAssetBundleProvider).FullName);

            using var writer = new StreamWriter(targetPath, false);
            writer.Write(updatedJson);

            Debug.Log($"Content Catalog updated with EncryptedAssetBundleProvider: {targetPath}");
        }
    }
}