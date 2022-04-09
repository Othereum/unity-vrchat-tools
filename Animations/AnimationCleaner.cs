using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OthereumTools
{
    public class AnimationCleaner : ScriptableWizard
    {
        const string Title = "Clean Up Animation Clip";

        [MenuItem(OthereumTools.PREFIX + Title, priority = AnimUtil.PRIORITY)]
        public static AnimationCleaner Open()
        {
            return DisplayWizard<AnimationCleaner>(Title, "Clean Up");
        }

        public void CleanUp()
        {
            var commonProps = new HashSet<(string, string)>();
            if (shareCommonProperties) {
                foreach (var clip in clips) {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                        if (binding.type != typeof(SkinnedMeshRenderer)) {
                            // only blendshapes are supported for now
                            continue;
                        }
                        if (removeUnusedCurves) {
                            if (!IsCurveBeingUsed(clip, binding)) {
                                continue;
                            }
                        }
                        commonProps.Add((binding.path, binding.propertyName));
                    }
                }
            }

            foreach (var clip in clips) {
                Undo.RecordObject(clip, Title);
                var missingProps = new HashSet<(string, string)>(commonProps);
                foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                    if (binding.type != typeof(SkinnedMeshRenderer)) {
                        // only blendshapes are supported for now
                        continue;
                    }

                    bool isCurveBeingUsed;
                    if (shareCommonProperties) {
                        isCurveBeingUsed = commonProps.Contains((binding.path, binding.propertyName));
                    } else {
                        isCurveBeingUsed = IsCurveBeingUsed(clip, binding);
                    }

                    if (isCurveBeingUsed) {
                        ValidateCurve(clip, binding);
                    }
                    else {
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                    }
                    missingProps.Remove((binding.path, binding.propertyName));
                }
                if (shareCommonProperties) {
                    foreach (var property in missingProps) {
                        var binding = new EditorCurveBinding {
                            path = property.Item1,
                            propertyName = property.Item2,
                            type = typeof(SkinnedMeshRenderer)
                        };
                        ValidateCurve(clip, binding);
                    }
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
        }

        void ValidateCurve(AnimationClip clip, EditorCurveBinding binding)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) {
                curve = new AnimationCurve();
            }

            var keys = curve.keys;
            if (keys.Length == 0) {
                if (addKeyframeIfEmpty) {
                    keys = new Keyframe[1];
                } else {
                    return;
                }
            }
            if (forceFirstKeyTimeZero) {
                keys[0].time = 0f;
            }
            if (keepOnlyFirstKeyframe) {
                System.Array.Resize(ref keys, 1);
            }
            if (atLeastTwoKeyframes && keys.Length < 2) {
                System.Array.Resize(ref keys, 2);
                keys[1].value = keys[0].value;
                keys[1].time = keys[0].time + 1f / 30f;
            }
            curve.keys = keys;
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        static bool IsCurveBeingUsed(AnimationClip clip, EditorCurveBinding binding)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve.keys == null || curve.keys.Length == 0) {
                return false;
            }
            if (Mathf.Approximately(curve.keys[0].value, 0f)) {
                return false;
            }
            return true;
        }


        public List<AnimationClip> clips;
        public bool shareCommonProperties = false;
        public bool removeUnusedCurves = true;
        public bool addKeyframeIfEmpty = false;
        public bool forceFirstKeyTimeZero = true;
        public bool keepOnlyFirstKeyframe = true;
        public bool atLeastTwoKeyframes = false;

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
            CleanUp();
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
