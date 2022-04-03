using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace OthereumTools
{
    public class ExpressionCleaner : ScriptableWizard
    {
        const string Title = "Clean Up Expression Clip";

        [MenuItem(OthereumTools.PREFIX + Title, priority = AnimUtil.PRIORITY)]
        public static ExpressionCleaner Open()
        {
            return DisplayWizard<ExpressionCleaner>(Title, "Clean Up");
        }

        [System.Flags]
        public enum Flags
        {
            ShareCommonProperties = 1 << 0,
            RemoveUnusedCurves = 1 << 1,
            AddKeyframeIfEmpty =  1 << 2,
            ForceFirstKeyTimeZero = 1 << 3,
            KeepOnlyFirstKeyframe = 1 << 4,
            AtLeastTwoKeyframes = 1 << 5,
        }

        public static void CleanUp(IEnumerable<AnimationClip> clips, Flags flags)
        {
            var commonProps = new HashSet<(string, string)>();
            if ((flags & Flags.ShareCommonProperties) != 0) {
                foreach (var clip in clips) {
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip)) {
                        if (binding.type != typeof(SkinnedMeshRenderer)) {
                            // only blendshapes are supported for now
                            continue;
                        }
                        if ((flags & Flags.RemoveUnusedCurves) != 0) {
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
                    if ((flags & Flags.ShareCommonProperties) != 0) {
                        isCurveBeingUsed = commonProps.Contains((binding.path, binding.propertyName));
                    } else {
                        isCurveBeingUsed = IsCurveBeingUsed(clip, binding);
                    }

                    if (isCurveBeingUsed) {
                        ValidateCurve(clip, binding, flags);
                    }
                    else {
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                    }
                    missingProps.Remove((binding.path, binding.propertyName));
                }
                if ((flags & Flags.ShareCommonProperties) != 0) {
                    foreach (var property in missingProps) {
                        var binding = new EditorCurveBinding {
                            path = property.Item1,
                            propertyName = property.Item2,
                            type = typeof(SkinnedMeshRenderer)
                        };
                        ValidateCurve(clip, binding, flags);
                    }
                }
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = false;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
        }

        static void ValidateCurve(AnimationClip clip, EditorCurveBinding binding, Flags flags)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null) {
                curve = new AnimationCurve();
            }

            var keys = curve.keys;
            if (keys.Length == 0) {
                if ((flags & Flags.AddKeyframeIfEmpty) != 0) {
                    keys = new Keyframe[1];
                } else {
                    return;
                }
            }
            if ((flags & Flags.ForceFirstKeyTimeZero) != 0) {
                keys[0].time = 0f;
            }
            if ((flags & Flags.KeepOnlyFirstKeyframe) != 0) {
                System.Array.Resize(ref keys, 1);
            }
            if ((flags & Flags.AtLeastTwoKeyframes) != 0 && keys.Length < 2) {
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
            Flags flags = 0;
            if (shareCommonProperties) {
                flags |= Flags.ShareCommonProperties;
            }
            if (removeUnusedCurves) {
                flags |= Flags.RemoveUnusedCurves;
            }
            if (addKeyframeIfEmpty) {
                flags |= Flags.AddKeyframeIfEmpty;
            }
            if (forceFirstKeyTimeZero) {
                flags |= Flags.ForceFirstKeyTimeZero;
            }
            if (keepOnlyFirstKeyframe) {
                flags |= Flags.KeepOnlyFirstKeyframe;
            }
            if (atLeastTwoKeyframes) {
                flags |= Flags.AtLeastTwoKeyframes;
            }
            CleanUp(clips, flags);
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
