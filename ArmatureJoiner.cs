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


        public Transform @base;
        public Transform additive;
        public string suffix;
        public bool exactHierarchy;
        public bool keepWorldTransform;
        Dictionary<string, Transform> bases;

        void OnValidate()
        {
            isValid = @base != null && additive != null;
        }

        void OnWizardCreate()
        {
            if (!exactHierarchy) {
                bases = new Dictionary<string, Transform>();
                BuildDictionary(@base);
            }
            foreach (Transform a in additive) {
                Join(a, "");
            }
        }

        void Join(Transform current, string basepath)
        {
            var curpath = basepath + current.name;

            Transform newbase;
            if (exactHierarchy) {
                newbase = @base.Find(curpath);
            } else {
                bases.TryGetValue(current.name, out newbase);
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
            }

            foreach (var a in current.Cast<Transform>().ToArray()) {
                Join(a, curpath + "/");
            }
        }

        void BuildDictionary(Transform transform)
        {
            foreach (Transform child in transform) {
                bases.Add(child.name, child);
                BuildDictionary(child);
            }
        }
    }
}
