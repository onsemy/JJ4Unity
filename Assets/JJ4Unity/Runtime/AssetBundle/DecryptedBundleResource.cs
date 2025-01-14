using UnityEngine.ResourceManagement.ResourceProviders;

namespace JJ4Unity.Runtime.AssetBundle
{
    public class DecryptedBundleResource : IAssetBundleResource
    {
        private UnityEngine.AssetBundle _assetBundle;
        private System.IO.Stream _decryptedStream;

        public DecryptedBundleResource(UnityEngine.AssetBundle assetBundle, System.IO.Stream decryptedStream = null)
        {
            _assetBundle = assetBundle;
            _decryptedStream = decryptedStream;
        }
        
        public UnityEngine.AssetBundle GetAssetBundle()
        {
            return _assetBundle;
        }

        public void Unload()
        {
            if (null == _assetBundle)
            {
                return;
            }
            
            _assetBundle.Unload(true);
            _assetBundle = null;
            
            _decryptedStream?.Dispose();
            _decryptedStream = null;
        }
    }
}
