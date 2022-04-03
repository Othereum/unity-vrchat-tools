using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OthereumTools
{
    public class PropertyRenamer : ScriptableWizard
    {
        const string Title = "Rename Properties";

        [MenuItem(OthereumTools.PREFIX + Title, priority = AnimUtil.PRIORITY)]
        public static PropertyRenamer Open()
        {
            return DisplayWizard<PropertyRenamer>(Title, nameof(Rename));
        }

        public static void Rename(IEnumerable<AnimationClip> clips, IEnumerable<PathRename> paths)
        {
            foreach (var clip in clips) {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                    foreach (var path in paths) {
                        if (binding.path == path.from) {
                            var newBinding = binding;
                            newBinding.path = path.to;
                            var curve = AnimationUtility.GetEditorCurve(clip, binding);
                            Undo.RecordObject(clip, Title);
                            AnimationUtility.SetEditorCurve(clip, newBinding, curve);
                            if (!path.copy) {
                                AnimationUtility.SetEditorCurve(clip, binding, null);
                            }
                        }
                    }
                }
            }
        }

        [System.Serializable]
        public struct PathRename
        {
            public string from;
            public string to;
            public bool copy;
        }


        public List<AnimationClip> clips;
        public List<PathRename> paths;

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
            Rename(clips, paths);
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

