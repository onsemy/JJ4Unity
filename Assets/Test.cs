using JJ4Unity.Runtime.Attribute;
using JJ4Unity.Runtime.Extension;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField, AssignPath]
    private GameObject _childGameObject;
    [AssignPath(true)]
    public Transform _childTransform;
    [SerializeField, AssignPath("ChildGameObject/Directional Light")]
    private Light _light;
    [SerializeField, ReadOnly]
    private int _readOnlyIntValue = 10;
    [ReadOnly]
    public string _publicStringValue = "PublicStringValue";

    private void Awake()
    {
        this.AssignPaths();
    }

    // Start is called before the first frame update
    private void Start()
    {
        JJ4Unity.Runtime.Extension.Debug.Log($"_childGameObject is null? {_childGameObject == null}");
        JJ4Unity.Runtime.Extension.Debug.Log($"_childTransform is null? {_childTransform == null}");
        JJ4Unity.Runtime.Extension.Debug.Log($"_light is null? {_light == null}");
    }
}
