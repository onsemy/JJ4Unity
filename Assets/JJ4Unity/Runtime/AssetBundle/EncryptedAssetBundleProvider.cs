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

        public EncryptedAssetBundleProvider(string key, string iv)
        {
            _key = key;
            _iv = iv;
            Debug.Log($"Key: {_key}, IV: {_iv}");
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            var internalId = provideHandle.Location.InternalId;
            Debug.Log($"Loading encrypted asset bundle: {internalId}");

            if (internalId.StartsWith("jar:file://"))
            {
                ProvideFromJarFile(provideHandle, internalId);
            }
            else
            {
                ProvideFromFile(provideHandle, internalId);
            }
        }

        private void ProvideFromFile(ProvideHandle provideHandle, string internalId)
        {
            var data = File.ReadAllBytes(internalId);
            DecryptBundle(provideHandle, data);
        }

        private void ProvideFromJarFile(ProvideHandle provideHandle, string internalId)
        {
            var request = UnityWebRequest.Get(internalId);
            
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
                    var data = request.downloadHandler.data;
                    DecryptBundle(provideHandle, data);
                }
                finally
                {
                    request.Dispose();
                }
            };
        }

        private void DecryptBundle(ProvideHandle provideHandle, byte[] data)
        {
            try
            {
                var decryptedData = DecryptDataToByteArray(data);
                var bundle = UnityEngine.AssetBundle.LoadFromMemory(decryptedData);
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
        }

        private MemoryStream DecryptDataToMemoryStream(Stream encryptedStream)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            var decryptedStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(decryptedStream);
                decryptedStream.Seek(0, SeekOrigin.Begin);
            }

            return decryptedStream;
        }

        private byte[] DecryptDataToByteArray(byte[] encryptedData)
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
            return decryptedStream.ToArray();
        }
    }
}