using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OthereumTools
{
    public class ArmatureJoiner : ScriptableWizard
    {
        const string Title = "Join Armatures";

        [MenuItem(OthereumTools.PREFIX + Title)]
        public static ArmatureJoiner Open()
        {
            return DisplayWizard<ArmatureJoiner>(Title, nameof(Join));
        }


        public Transform parent;
        public Transform child;
        public string suffix;
        public bool exactHierarchy;
        public bool keepWorldTransform;
        bool hasEverSearchedDynamicBones;

        [Space]
        [Tooltip("Optional: Adds nested bones to the exclusions.")]
        public List<MonoBehaviour> dynamicBones;

        Dictionary<string, Transform> parentTransforms;
        Dictionary<Transform, HashSet<SerializedProperty>> exclusionsByDynamicBone;

        void OnValidate()
        {
            isValid = parent != null && child != null;

            if (dynamicBones.Any()) {
                return;
            }

            if (parent == null) {
                hasEverSearchedDynamicBones = false;
            }
            else if (!hasEverSearchedDynamicBones) {
                foreach (var behaviour in parent.parent.GetComponentsInChildren<MonoBehaviour>()) {
                    if (behaviour.GetType().Name == "DynamicBone") {
                        dynamicBones.Add(behaviour);
                    }
                }
                hasEverSearchedDynamicBones = true;
            }
        }

        void OnWizardCreate()
        {
            if (!exactHierarchy) {
                parentTransforms = new Dictionary<string, Transform>();
                CacheBaseTransforms(parent);
            }
            CacheDynamicBones();

            foreach (Transform a in child) {
                Join(a, "");
            }
        }

        void Join(Transform current, string basepath)
        {
            var curpath = basepath + current.name;

            Transform newbase;
            if (exactHierarchy) {
                newbase = parent.Find(curpath);
            } else {
                parentTransforms.TryGetValue(current.name, out newbase);
            }

            if (newbase != null) {
                Undo.SetTransformParent(current, newbase, Title);
                Undo.RecordObject(current.gameObject, Title);
                current.name += " " + suffix;
                if (!keepWorldTransform) {
                    current.localPosition = Vector3.zero;
                    current.localRotation = Quaternion.identity;
                    current.localScale = Vector3.one;
                }
                exclusionsByDynamicBone.TryGetValue(newbase, out var properties);
                if (properties != null) {
                    foreach (var exclusions in properties) {
                        var excluded = exclusions.GetArrayElementAtIndex(exclusions.arraySize++);
                        excluded.objectReferenceValue = current;
                        exclusions.serializedObject.ApplyModifiedProperties();
                    }
                }
            }

            foreach (var a in current.Cast<Transform>().ToArray()) {
                Join(a, curpath + "/");
            }
        }

        void CacheBaseTransforms(Transform transform)
        {
            foreach (Transform tr in transform) {
                try {
                    parentTransforms.Add(tr.name, tr);
                } catch (System.ArgumentException) {
                    Debug.LogWarning("A transform with the same name exists: " + tr);
                }
                CacheBaseTransforms(tr);
            }
        }

        void CacheDynamicBones()
        {
            exclusionsByDynamicBone = new Dictionary<Transform, HashSet<SerializedProperty>>();

            foreach (var db in dynamicBones) {
                try {
                    if (db == null) {
                        continue;
                    }

                    var so = new SerializedObject(db);
                    var exclusions = so.FindProperty("m_Exclusions");

                    var root = (Transform)so.FindProperty("m_Root").objectReferenceValue;
                    AddExclusionsProperty(root, exclusions);

                    var roots = so.FindProperty("m_Roots");
                    for (int i = 0; i < roots.arraySize; i++) {
                        var r = (Transform)roots.GetArrayElementAtIndex(i).objectReferenceValue;
                        AddExclusionsProperty(r, exclusions);
                    }

                    Undo.RecordObject(db, Title);
                }
                catch {
                    Debug.LogWarning("It's not a dynamic bone: " + db);
                }
            }
        }

        void AddExclusionsProperty(Transform root, SerializedProperty exclusions)
        {
            if (root == null) {
                return;
            }

            exclusionsByDynamicBone.TryGetValue(root, out var properties);
            if (properties == null) {
                properties = new HashSet<SerializedProperty>();
                exclusionsByDynamicBone[root] = properties;
            }
            properties.Add(exclusions);

            foreach (Transform tr in root) {
                AddExclusionsProperty(tr, exclusions);
            }
        }
    }
}
