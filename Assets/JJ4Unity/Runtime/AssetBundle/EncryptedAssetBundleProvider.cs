using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using JJ4Unity.Runtime.Extension;
using JJ4Unity.Runtime.Utility;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace JJ4Unity.Runtime.AssetBundle
{
    public class EncryptedAssetBundleProvider : AssetBundleProvider
    {
        private readonly string _key; // 16바이트
        private readonly string _iv; // 16바이트
        
        private StreamDownloadHandler _downloadHandler;

        public EncryptedAssetBundleProvider(string key, string iv)
        {
            _key = key;
            _iv = iv;
            Debug.Log($"Key: {_key}, IV: {_iv}");
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            // Debug.Log($"Try to provide AssetBundle - provideHandle.Location is null? {provideHandle}, key: {_key}, iv: {_iv}");
            var internalId = provideHandle.Location.InternalId;
            Debug.Log($"Loading encrypted asset bundle: {internalId}");

            if (internalId.StartsWith("jar:file://"))
            {
                ProvideFromJarFile(provideHandle, internalId);
            }
            else
            {
                ProvideFromFileStream(provideHandle, internalId);
            }
        }

        private void ProvideFromFileStream(ProvideHandle provideHandle, string internalId)
        {
            using var fileStream = new FileStream(internalId, FileMode.Open, FileAccess.Read);
            DecryptBundle(provideHandle, fileStream);
        }

        private void ProvideFromJarFile(ProvideHandle provideHandle, string internalId)
        {
            _downloadHandler = new StreamDownloadHandler();
            
            var request = new UnityWebRequest(
                internalId,
                UnityWebRequest.kHttpVerbGET,
                _downloadHandler,
                null
            );
            
            request.SendWebRequest().completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load encrypted AssetBundle: {request.error}");
                    provideHandle.Complete<DecryptedBundleResource>(null, false,
                        new Exception($"Failed to load encrypted asset bundle: {internalId}"));
                    return;
                }

                try
                {
                    var stream = _downloadHandler.GetStream();
                    DecryptBundle(provideHandle, stream);
                }
                finally
                {
                    request.Dispose();
                    
                    _downloadHandler?.Dispose();
                    _downloadHandler = null;
                }
            };
        }

        private void DecryptBundle(ProvideHandle provideHandle, Stream stream)
        {
            MemoryStream decryptedStream = null;
            
            try
            {
                decryptedStream = DecryptDataToMemoryStream(stream);

                var bundle = UnityEngine.AssetBundle.LoadFromStream(decryptedStream);
                var assetBundleResource = new DecryptedBundleResource(bundle);
                provideHandle.Complete(assetBundleResource, true, null);
            }
            catch (CryptographicException e)
            {
                provideHandle.Complete<DecryptedBundleResource>(null, false, e);
            }
            catch (Exception e)
            {
                provideHandle.Complete<DecryptedBundleResource>(null, false, e);
            }
            finally
            {
                decryptedStream?.Dispose();
            }
        }

        private MemoryStream DecryptDataToMemoryStream(Stream encryptedStream)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using var cryptoStream =
                new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);

            var decryptedStream = new MemoryStream();
            cryptoStream.CopyTo(decryptedStream);
            decryptedStream.Seek(0, SeekOrigin.Begin);

            return decryptedStream;
        }

        private MemoryStream DecryptDataToMemoryStream(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            using var encryptedStream = new MemoryStream(encryptedData);
            using var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            var decryptedStream = new MemoryStream();
            cryptoStream.CopyTo(decryptedStream);
            decryptedStream.Seek(0, SeekOrigin.Begin);
            return decryptedStream;
        }
    }
}