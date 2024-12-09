using System.IO;
using System.Security.Cryptography;
using System.Text;
using JJ4Unity.Runtime.Extension;
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
        }

        public override void Provide(ProvideHandle provideHandle)
        {
            var internalId = provideHandle.Location.InternalId;
            Debug.Log($"Loading encrypted asset bundle: {internalId}");

            try
            {
                using var encryptedStream = new FileStream(internalId, FileMode.Open, FileAccess.Read);
                using var decryptedStream = new MemoryStream();
                
                DecryptStream(encryptedStream, decryptedStream);
                decryptedStream.Seek(0, SeekOrigin.Begin);

                var bundle = UnityEngine.AssetBundle.LoadFromStream(decryptedStream);
                provideHandle.Complete(bundle, true, null);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load encrypted AssetBundle: {e.Message}");
                provideHandle.Complete<UnityEngine.AssetBundle>(null, false, e);
            }
        }

        protected virtual void DecryptStream(Stream encryptedStream, Stream decryptedStream)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(_key);
            aes.IV = Encoding.UTF8.GetBytes(_iv);
            aes.Padding = PaddingMode.PKCS7;
            aes.Mode = CipherMode.CBC;
            
            using var cryptoStream = new CryptoStream(encryptedStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            cryptoStream.CopyTo(decryptedStream);
        }
    }
}