using System.Collections.Generic;

namespace Majinfwork.StateGraph {
    public static class StateGraphCloneExtension {
        /// <summary>
        /// Remaps StateTransition.targetState from original assets to their clones, exit lists included.
        /// </summary>
        public static void RemapTransitions(this StateNodeAsset clone, Dictionary<StateNodeAsset, StateNodeAsset> cloneMap) {
            if (clone == null) return;

            foreach (var trans in StateTransitionFields.Transitions(clone)) {
                if (trans.targetState != null && cloneMap.TryGetValue(trans.targetState, out var localClone)) {
                    trans.targetState = localClone;
                }
            }
        }

        /// <summary>
        /// Remaps StateTransition.targetState for all cloned nodes.
        /// </summary>
        public static void RemapTransitions(this Dictionary<StateNodeAsset, StateNodeAsset> cloneMap) {
            foreach (var (_, clone) in cloneMap) {
                clone.RemapTransitions(cloneMap);
            }
        }
    }
}
