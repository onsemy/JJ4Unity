using UnityEngine.ResourceManagement.ResourceProviders;

namespace JJ4Unity.Runtime.AssetBundle
{
    public class CustomAssetBundleResource : IAssetBundleResource
    {
        private UnityEngine.AssetBundle _assetBundle;

        public CustomAssetBundleResource(UnityEngine.AssetBundle assetBundle)
        {
            _assetBundle = assetBundle;
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
        }
    }
}
