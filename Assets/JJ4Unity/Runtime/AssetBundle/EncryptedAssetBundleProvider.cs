using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using JJ4Unity.Runtime.Extension;
using JJ4Unity.Runtime.Utility;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace JJ4Unity.Runtime.AssetBundle
{
    public class EncryptedAssetBundleProvider : AssetBundleProvider
    {
        #region Fields
        
        private readonly string _key; // 16바이트
        private readonly string _iv; // 16바이트
        private readonly bool _useStream;
        private DownloadHandler _downloadHandler;
        
        #endregion

        #region Constructor
        
        public EncryptedAssetBundleProvider(string key, string iv, bool useStream = false)
        {
            _key = key;
            _iv = iv;
            _useStream = useStream;
            Debug.Log($"Key: {_key}, IV: {_iv}, UseStream: {_useStream}");
        }
        
        #endregion
        
        #region Overrides

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

        public override void Release(IResourceLocation location, object asset)
        {
            if (asset is DecryptedBundleResource resource)
            {
                resource.Unload();
            }
            
            base.Release(location, asset);
        }
        
        #endregion
        
        #region Provide Decrypted Bundle

        private void ProvideFromFile(ProvideHandle provideHandle, string internalId)
        {
            var data = File.ReadAllBytes(internalId);
            DecryptBundle(provideHandle, data);
        }

        private void ProvideFromJarFile(ProvideHandle provideHandle, string internalId)
        {
            _downloadHandler = _useStream
                ? new StreamDownloadHandler()
                : new DownloadHandlerBuffer();
            
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
                    if (_useStream && _downloadHandler is StreamDownloadHandler stream)
                    {
                        DecryptBundle(provideHandle, stream.GetStream());
                    }
                    else
                    {
                        var data = request.downloadHandler.data;
                        DecryptBundle(provideHandle, data);
                    }
                }
                finally
                {
                    request.Dispose();
                }
            };
        }
        
        private void DecryptBundle(ProvideHandle provideHandle, Stream stream)
        {
            try
            {
                using var decryptedStream = DecryptToStream(stream);
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
        }

        private void DecryptBundle(ProvideHandle provideHandle, byte[] data)
        {
            try
            {
                var decryptedData = DecryptToByteArray(data);
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

        private Stream DecryptToStream(Stream encryptedStream)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;

            var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            return cryptoStream;
        }

        private byte[] DecryptToByteArray(byte[] encryptedData)
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
        
        #endregion
    }
}