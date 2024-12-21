using UnityEngine;
using Debug = JJ4Unity.Runtime.Extension.Debug;

public class Some : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
        Debug.Log($"Start - {nameof(Some)}");
    }
}
