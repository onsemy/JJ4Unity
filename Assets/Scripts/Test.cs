using System.Collections;
using JJ4Unity.Runtime.AssetBundle;
using JJ4Unity.Runtime.Attribute;
using JJ4Unity.Runtime.Extension;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Debug = JJ4Unity.Runtime.Extension.Debug;

public class Test : MonoBehaviour
{
    [SerializeField, AssignPath] private GameObject _childGameObject;
    [AssignPath(true)] public Transform _childTransform;

    [SerializeField, AssignPath("ChildGameObject/Directional Light")]
    private Light _light;

    [SerializeField, ReadOnly] private int _readOnlyIntValue = 10;
    [ReadOnly] public string _publicStringValue = "PublicStringValue";

    private Some _some;

    private void Awake()
    {
        this.AssignPaths();
    }

    private IEnumerator Start()
    {
        Debug.Log($"Runtime Path: {Addressables.RuntimePath}");
        Addressables.ResourceManager.ResourceProviders.Add(
            new EncryptedAssetBundleProvider(
                "dWG+4/YNKFJ/G2Kl", // NOTE(JJO): AES Key, IV는 암호화하여 넣는 것을 추천
                "7dXQLo/xPort0/6f"
            )
        );

        Debug.Log($"_childGameObject is null? {_childGameObject == null}");
        Debug.Log($"_childTransform is null? {_childTransform == null}");
        Debug.Log($"_light is null? {_light == null}");

        yield return LoadAddressables();
    }

    private IEnumerator LoadAddressables()
    {
        var handle = Addressables.LoadAssetAsync<GameObject>("Assets/Bundles/SomePrefab.prefab");
        yield return handle;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            yield break;
        }

        var obj = handle.Result;
        if (false == obj.TryGetComponent(out _some))
        {
            yield break;
        }

        GameObject.Instantiate(_some.gameObject);
    }
}