using UnityEditor;
using UnityEngine;

namespace OthereumTools
{
    public class BoundsEncapsulator : ScriptableWizard
    {
        const string Title = "Encapsulate Skinned Mesh Bounds";

        [MenuItem(OthereumTools.PREFIX + Title)]
        public static BoundsEncapsulator Open()
        {
            return DisplayWizard<BoundsEncapsulator>(Title, "Encapsulate");
        }

        public SkinnedMeshRenderer[] meshes = new SkinnedMeshRenderer[0];

        public void Encapsulate()
        {
            var bounds = new Bounds();
            foreach (var mesh in meshes) {
                bounds.Encapsulate(mesh.rootBone.TransformPoint(mesh.localBounds.min));
                bounds.Encapsulate(mesh.rootBone.TransformPoint(mesh.localBounds.max));
            }
            foreach (var mesh in meshes) {
                var localBounds = new Bounds();
                localBounds.Encapsulate(mesh.rootBone.InverseTransformPoint(bounds.min));
                localBounds.Encapsulate(mesh.rootBone.InverseTransformPoint(bounds.max));
                Undo.RecordObject(mesh, Title);
                mesh.localBounds = localBounds;
            }
        }

        void OnEnable()
        {
            OnValidate();
        }

        void OnValidate()
        {
            isValid = meshes.Length > 0;
        }

        void OnWizardCreate()
        {
            Encapsulate();
        }
    }
}
