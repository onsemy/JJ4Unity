using JJ4Unity.Runtime.Extension;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace JJ4Unity.Runtime.AssetBundle
{
    public class AddressablesHelper
    {
        // public static void SetEncryptedProvider(EncryptedAssetBundleProvider provider)
        // {
        //     var providers = Addressables.ResourceManager.ResourceProviders;
        //     for (int i = providers.Count - 1; i >= 0; i--)
        //     {
        //         if (providers[i] is AssetBundleProvider)
        //         {
        //             Debug.Log($"Remove {providers[i].ProviderId}");
        //             providers.RemoveAt(i);
        //         }
        //     }
        //
        //     providers.Add(provider);
        // }
    }
}