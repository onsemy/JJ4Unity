using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JJ4Unity.Editor.Extension
{
#if UNITY_EDITOR
    using JJ4Unity.Runtime.Extension;
    using UnityEditor;

    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoBehaviourCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var component = target as MonoBehaviour;
            if (GUILayout.Button("Assign Variables"))
            {
                component.AssignPaths(true);
            }
        }
    }
#endif
}
