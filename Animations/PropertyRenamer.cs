using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OthereumTools.Animations
{
    public class PropertyRenamer : ScriptableWizard
    {
        const string Title = "Rename Properties";

        [MenuItem("Animation/" + Title)]
        public static PropertyRenamer Open()
        {
            return DisplayWizard<PropertyRenamer>(Title, "Rename");
        }

        public static void Rename(IEnumerable<AnimationClip> clips, string path, string newPath)
        {
            foreach (var clip in clips) {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                    if (binding.path == path) {
                        var newBinding = binding;
                        newBinding.path = newPath;
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        Undo.RecordObject(clip, Title);
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                        AnimationUtility.SetEditorCurve(clip, newBinding, curve);
                    }
                }
            }
        }


        public List<AnimationClip> clips;
        public string newPath;

        string[] paths;
        int selectedPath;

        void OnEnable()
        {
            clips = AnimUtil.GetSelectedClips();
            OnValidate();
        }

        void OnValidate()
        {
            isValid = clips.Count > 0;

            var pathSet = new HashSet<string>();
            foreach (var clip in clips) {
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                    pathSet.Add(binding.path);
                }
            }
            paths = pathSet.ToArray();
        }

        void OnWizardCreate()
        {
            Rename(clips, paths.FirstOrDefault(), newPath);
        }

        protected override bool DrawWizardGUI()
        {
            if (clips.Count == 0) {
                EditorGUILayout.HelpBox(AnimUtil.ClipSelectionHelpMessage, MessageType.Info);
            }
            bool changed = base.DrawWizardGUI();
            EditorGUI.BeginChangeCheck();
            selectedPath = EditorGUILayout.Popup("Path", selectedPath, paths);
            if (EditorGUI.EndChangeCheck()) {
                changed = true;
            }
            return changed;
        }
    }
}

