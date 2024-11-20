using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace JJ4Unity.Runtime.Extension
{
    using Attribute;

    public static class AssignPath
    {
        public static void AssignPaths(this MonoBehaviour behaviour, bool isLoadFromEditor = false)
        {
            var behaviourType = behaviour.GetType();
            var componentType = typeof(Component);
            var pathType = typeof(AssignPathAttribute);

            var members = behaviourType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m =>
                    (m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property)
                    && m.GetCustomAttributes(pathType, true).Length == 1
                );

            var memberInfos = members.ToList();

            // NOTE(JJO): AssignPath로 지정된 Component 할당 시도
            AssignComponents(behaviour, isLoadFromEditor, componentType, memberInfos);

            // NOTE(JJO): List<T> 할당 시도
            AssignGenericLists(behaviour, isLoadFromEditor, componentType, memberInfos);

            // NOTE(JJO): GameObject 할당 시도
            AssignGameObjects(behaviour, isLoadFromEditor, memberInfos);

#if UNITY_EDITOR
            if (false == Application.isPlaying)
            {
                UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(behaviour);
                UnityEditor.EditorUtility.SetDirty(behaviour);
            }
#endif
        }

        private static void AssignGameObjects(
            MonoBehaviour behaviour,
            bool isLoadFromEditor,
            List<MemberInfo> memberInfos
        )
        {
            var gameObjectMembers = memberInfos.Where(m =>
            {
                var memberType = m.GetMemberType();
                if (null == memberType)
                {
                    return false;
                }

                return memberType == typeof(GameObject);
            });

            foreach (var item in gameObjectMembers)
            {
                if (false == TryGetChild(behaviour, isLoadFromEditor, item, out var child))
                {
                    continue;
                }

                var memberComponent = child.gameObject;
                if (null == memberComponent)
                {
                    Debug.LogError($"can't find component {child.name} GameObject");
                    continue;
                }

                item.SetValue(behaviour, memberComponent);
            }
        }

        private static void AssignGenericLists(
            MonoBehaviour behaviour,
            bool isLoadFromEditor,
            Type componentType,
            List<MemberInfo> memberInfos
        )
        {
            var genericMembers = memberInfos.Where(m =>
            {
                var memberType = m.GetMemberType();
                if (null == memberType)
                {
                    return false;
                }

                return memberType.IsGenericType;
            });

            foreach (var item in genericMembers)
            {
                if (false == TryGetAssignPathAttribute(isLoadFromEditor, item, out var attribute))
                {
                    continue;
                }

                var genericType = item.GetMemberType().GenericTypeArguments[0];
                // var searchType = genericType.IsSubclassOf(componentType) ? genericType : typeof(Transform);
                Type searchType;
                if (genericType.IsSubclassOf(componentType))
                {
                    searchType = genericType;
                }
                else if (genericType == typeof(GameObject))
                {
                    searchType = typeof(Transform);
                }
                else
                {
                    // NOTE(JJO): Component 또는 Transform 이 아니라면 예외 처리
                    continue;
                }

                var objectName = attribute.IsSelf ? item.Name : attribute.path;

                var listType = typeof(List<>).MakeGenericType(genericType);
                var list = (IList)System.Activator.CreateInstance(listType);

                var childList = behaviour.transform.GetComponentsInChildren(searchType, true);
                foreach (var c in childList)
                {
                    if (c.name.StartsWith(objectName))
                    {
                        list.Add(c);
                    }
                    // NOTE(JJO): 앞에 `_`를 붙여서 다시 찾아본다.
                    else if (objectName.Length > 2
                        || c.name.StartsWith($"{char.ToUpper(objectName[1])}{objectName.Substring(2)}"))
                    {
                        list.Add(c);
                    }
                }

                item.SetValue(behaviour, list);
            }
        }

        private static void AssignComponents(
            MonoBehaviour behaviour,
            bool isLoadFromEditor,
            Type componentType,
            IReadOnlyList<MemberInfo> memberInfos
        )
        {
            var componentMembers = memberInfos.Where(m =>
            {
                var memberType = m.GetMemberType();
                if (null == memberType)
                {
                    return false;
                }

                return memberType.IsSubclassOf(componentType);
            });

            foreach (var item in componentMembers)
            {
                if (false == TryGetChild(behaviour, isLoadFromEditor, item, out var child))
                {
                    continue;
                }

                Type memberType = item.GetMemberType();
                if (false == child.TryGetComponent(memberType, out var memberComponent))
                {
                    memberComponent = child.gameObject.AddComponent(memberType);
                }

                if (null == memberComponent)
                {
                    Debug.LogError($"can't find component {child.name} {memberType}");
                    continue;
                }

                item.SetValue(behaviour, memberComponent);
            }
        }

        private static bool TryGetChild(MonoBehaviour behaviour, bool isLoadFromEditor, MemberInfo item, out Transform child)
        {
            if (false == TryGetAssignPathAttribute(isLoadFromEditor, item, out var attribute))
            {
                child = null;
                return false;
            }

            var path = attribute.IsSelf ? item.Name : attribute.path;
            return TryFindChild(attribute, behaviour, path, out child);
        }

        private static bool TryFindChildTransform(
            IEnumerable<Transform> childList,
            string path,
            out Transform child
        )
        {
            child = null;
            foreach (var t in childList)
            {
                if (t.name.Equals(path))
                {
                    child = t;
                    return true;
                }
            }

            return false;
        }

        private static bool TryFindChild(
            AssignPathAttribute attribute,
            MonoBehaviour behaviour,
            string path,
            out Transform child
        )
        {
            child = behaviour.transform.Find(path);
            if (null != child)
            {
                return true;
            }

            // NOTE(JJO): 이름/경로로 못찾는 경우
            // NOTE(JJO): path를 특정한 경우라면 더 찾아도 소용이 없어서 못찾은 것으로 결론.
            if (false == attribute.IsSelf)
            {
                return false;
            }

            // NOTE(JJO): 모든 자식들로부터 찾아본다.
            var childList = behaviour.transform.GetComponentsInChildren<Transform>(true);
            if (false == TryFindChildTransform(childList, path, out child)
                && (path.Length <= 2 || false == path.StartsWith('_')))
            {
                Debug.LogError($"can't find child in {path}");
                return false;
            }

            // NOTE(JJO): 앞에 `_`를 붙여서 다시 찾아본다.
            path = $"{char.ToUpper(path[1])}{path.Substring(2)}";

            child = behaviour.transform.Find(path);
            if (child != null)
            {
                return true;
            }

            if (false == TryFindChildTransform(childList, path, out child))
            {
                Debug.LogError($"can't find child in {path}");
                return false;
            }

            return true;
        }

        private static bool TryGetAssignPathAttribute(
            bool isLoadFromEditor,
            MemberInfo item,
            out AssignPathAttribute result
        )
        {
            var customAttributes = item.GetCustomAttributes(typeof(AssignPathAttribute), true);
            if (null == customAttributes
                || 0 == customAttributes.Length)
            {
                result = null;
                return false;
            }

            if (customAttributes[0] is not AssignPathAttribute attribute
                || isLoadFromEditor != attribute.IsLoadFromEditor)
            {
                result = null;
                return false;
            }

            result = attribute;
            return true;
        }
    }
}
