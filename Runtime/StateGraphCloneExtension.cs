using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Majinfwork.StateGraph {
    public static class StateGraphCloneExtension {
        // Cache: Type -> StateTransition fields
        private static readonly Dictionary<Type, FieldInfo[]> _transitionCache = new();

        private static FieldInfo[] GetTransitionFields(Type type) {
            if (_transitionCache.TryGetValue(type, out var cached))
                return cached;

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var transitionFields = Array.FindAll(fields, f => f.FieldType == typeof(StateTransition));

            _transitionCache[type] = transitionFields;
            return transitionFields;
        }

        /// <summary>
        /// Remaps StateTransition.targetState from original assets to their clones.
        /// </summary>
        public static void RemapTransitions(this StateNodeAsset clone, Dictionary<StateNodeAsset, StateNodeAsset> cloneMap) {
            if (clone == null) return;

            var fields = GetTransitionFields(clone.GetType());
            foreach (var f in fields) {
                var trans = f.GetValue(clone) as StateTransition;
                if (trans?.targetState != null && cloneMap.TryGetValue(trans.targetState, out var localClone)) {
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
