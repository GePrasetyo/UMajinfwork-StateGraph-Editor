using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Majinfwork.StateGraph {
    /// <summary>
    /// A state type's serialized exits: its StateTransition fields, and its StateTransition[] or List&lt;StateTransition&gt;
    /// fields (one exit per element). Public or [SerializeField], inherited ones included.
    /// </summary>
    public static class StateTransitionFields {
        private static readonly Dictionary<Type, FieldInfo[]> cache = new();

        public static FieldInfo[] Of(Type type) {
            if (cache.TryGetValue(type, out var fields)) return fields;

            var found = new List<FieldInfo>();
            for (var t = type; t != null && t != typeof(StateNodeAsset); t = t.BaseType) {
                var declared = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in declared) {
                    if (!IsExit(field.FieldType) || field.IsNotSerialized) continue;
                    if (field.IsPublic || field.IsDefined(typeof(SerializeField), false)) found.Add(field);
                }
            }

            fields = found.ToArray();
            cache[type] = fields;
            return fields;
        }

        /// <summary>Every exit of a state: its single exits and each element of its exit lists.</summary>
        public static IEnumerable<StateTransition> Transitions(StateNodeAsset state) {
            foreach (var field in Of(state.GetType())) {
                switch (field.GetValue(state)) {
                    case StateTransition exit:
                        yield return exit;
                        break;
                    case IEnumerable<StateTransition> exits:
                        foreach (var exit in exits) {
                            if (exit != null) yield return exit;
                        }

                        break;
                }
            }
        }

        private static bool IsExit(Type type) =>
            type == typeof(StateTransition) || type == typeof(StateTransition[]) || type == typeof(List<StateTransition>);
    }
}
