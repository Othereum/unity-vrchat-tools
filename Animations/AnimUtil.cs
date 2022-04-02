using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OthereumTools.Animations
{
    public static class AnimUtil
    {
        public static string ClipSelectionHelpMessage =>
            Application.systemLanguage == SystemLanguage.Korean
                ? ClipSelectionHelpMessage_KO
                : ClipSelectionHelpMessage_EN;

        public static string ClipSelectionHelpMessage_EN =>
            "When you open it with layer(state machine), states or clip assets selected," +
            " all included clips are automatically listed.";

        public static string ClipSelectionHelpMessage_KO =>
            "레이어(상태 머신), 상태 또는 클립 애셋을 선택한 상태에서 열면 포함된 모든 클립이 자동으로 나열됩니다.";

        public static List<AnimationClip> GetSelectedClips()
        {
            var clips = new List<AnimationClip>();
            foreach (var machine in Selection.GetFiltered<AnimatorStateMachine>(SelectionMode.Editable)) {
                foreach (var child in machine.states) {
                    GetClips(clips, child.state.motion);
                }
            }
            foreach (var state in Selection.GetFiltered<AnimatorState>(SelectionMode.Editable)) {
                GetClips(clips, state.motion);
            }
            foreach (var asset in Selection.GetFiltered<AnimationClip>(SelectionMode.Assets)) {
                clips.Add(asset);
            }
            return clips;
        }

        public static void GetClips(ICollection<AnimationClip> clips, Motion motion)
        {
            if (motion is AnimationClip) {
                clips.Add((AnimationClip)motion);
            }
            else if (motion is BlendTree) {
                var blendTree = (BlendTree)motion;
                foreach (var child in blendTree.children) {
                    GetClips(clips, child.motion);
                }
            }
        }
    }
}
