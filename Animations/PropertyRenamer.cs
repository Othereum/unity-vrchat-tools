using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OthereumTools
{
    public class PropertyRenamer : ScriptableWizard
    {
        const string Title = "Rename Animation Properties";

        [MenuItem(OthereumTools.PREFIX + Title, priority = AnimUtil.PRIORITY)]
        public static PropertyRenamer Open()
        {
            return DisplayWizard<PropertyRenamer>(Title, nameof(Rename));
        }

        public void Rename()
        {
            foreach (var clip in clips) {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                    foreach (var property in properties) {
                        if (binding.path != property.originalPath) {
                            continue;
                        }

                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        Undo.RecordObject(clip, Title);

                        if (!string.IsNullOrEmpty(property.newPath)) {
                            var newBinding = binding;
                            newBinding.path = property.newPath;
                            AnimationUtility.SetEditorCurve(clip, newBinding, curve);
                        }
                        if (!property.copy) {
                            AnimationUtility.SetEditorCurve(clip, binding, null);
                        }
                    }
                }
            }
        }

        [System.Serializable]
        public struct Property
        {
            public string originalPath;
            public string newPath;
            public bool copy;
        }


        public List<AnimationClip> clips;
        public List<Property> properties;

        void OnEnable()
        {
            clips = AnimUtil.GetSelectedClips();
            OnValidate();
        }

        void OnValidate()
        {
            isValid = clips.Count > 0;
        }

        void OnWizardCreate()
        {
            Rename();
        }

        protected override bool DrawWizardGUI()
        {
            if (clips.Count == 0) {
                EditorGUILayout.HelpBox(AnimUtil.ClipSelectionHelpMessage, MessageType.Info);
            }
            return base.DrawWizardGUI();
        }
    }
}

