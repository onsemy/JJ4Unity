using JJ4Unity.Runtime.Attribute;
using JJ4Unity.Runtime.Extension;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField, AssignPath]
    private GameObject _gameObject;
    [AssignPath(true)]
    public Transform _transform;
    [SerializeField, AssignPath("GameObject/Directional Light")]
    private Light _light;

    private void Awake()
    {
        this.AssignPaths();
    }

    // Start is called before the first frame update
    private void Start()
    {
        JJ4Unity.Runtime.Extension.Debug.Log($"_gameObject is null? {_gameObject == null}");
        JJ4Unity.Runtime.Extension.Debug.Log($"_transform is null? {_transform == null}");
        JJ4Unity.Runtime.Extension.Debug.Log($"_light is null? {_light == null}");
    }
}
