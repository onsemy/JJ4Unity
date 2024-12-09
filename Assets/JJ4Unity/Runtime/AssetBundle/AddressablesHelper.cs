using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace JJ4Unity.Runtime.AssetBundle
{
    public class AddressablesHelper
    {
        public static void SetEncryptedProvider(EncryptedAssetBundleProvider provider)
        {
            var providers = Addressables.ResourceManager.ResourceProviders;
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                if (providers[i].GetType() == typeof(AssetBundleProvider)
                    || providers[i].GetType() == typeof(EncryptedAssetBundleProvider))
                {
                    providers.RemoveAt(i);
                }
            }

            providers.Add(provider);
        }
    }
}