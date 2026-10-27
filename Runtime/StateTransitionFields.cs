using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Majinfwork.StateGraph {
    /// <summary>A state type's serialized StateTransition fields: public or [SerializeField], inherited ones included.</summary>
    public static class StateTransitionFields {
        private static readonly Dictionary<Type, FieldInfo[]> cache = new();

        public static FieldInfo[] Of(Type type) {
            if (cache.TryGetValue(type, out var fields)) return fields;

            var found = new List<FieldInfo>();
            for (var t = type; t != null && t != typeof(StateNodeAsset); t = t.BaseType) {
                var declared = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in declared) {
                    if (field.FieldType != typeof(StateTransition) || field.IsNotSerialized) continue;
                    if (field.IsPublic || field.IsDefined(typeof(SerializeField), false)) found.Add(field);
                }
            }

            fields = found.ToArray();
            cache[type] = fields;
            return fields;
        }

        public static FieldInfo Find(Type type, string name) {
            foreach (var field in Of(type)) {
                if (field.Name == name) return field;
            }

            return null;
        }

        /// <summary>Whether <paramref name="transition"/> is one of <paramref name="state"/>'s exits.</summary>
        public static bool Owns(StateNodeAsset state, StateTransition transition) {
            foreach (var field in Of(state.GetType())) {
                if (ReferenceEquals(field.GetValue(state), transition)) return true;
            }

            return false;
        }
    }
}
