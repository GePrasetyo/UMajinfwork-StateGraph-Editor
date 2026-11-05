using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Majinfwork.StateGraph {
    /// <summary>
    /// A state's field values, held by its node as one option. Kept as Unity's editor JSON of the state,
    /// so the state class stays the only place its fields are declared.
    /// </summary>
    [Serializable]
    public abstract class StateFields {
        [SerializeField] internal string json;

        public abstract StateNodeAsset CreateState();

        public void WriteTo(StateNodeAsset state) {
            if (!string.IsNullOrEmpty(json)) EditorJsonUtility.FromJsonOverwrite(json, state);
        }
    }

    [Serializable]
    public sealed class StateFields<TState> : StateFields where TState : StateNodeAsset {
        public override StateNodeAsset CreateState() => ScriptableObject.CreateInstance<TState>();

        /// <summary>
        /// Fields holding a state's current values, for tools that build graphs. Its exits' targets, id and position are
        /// left out: the graph's wires and nodes own those. An exit list keeps its size, which sets its ports.
        /// </summary>
        public static StateFields<TState> Of(TState state) {
            TState values = Object.Instantiate(state);
            try {
                values.guid = string.Empty;
                values.position = default;
                using var serialized = new SerializedObject(values);
                SerializedProperty property = serialized.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false)) {
                    if (ExitProperty.IsExit(property)) {
                        property.boxedValue = new StateTransition();
                    }
                    else if (ExitProperty.IsExitList(property)) {
                        for (int i = 0; i < property.arraySize; i++) {
                            property.GetArrayElementAtIndex(i).boxedValue = new StateTransition();
                        }
                    }
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                return new StateFields<TState> { json = EditorJsonUtility.ToJson(values) };
            }
            finally {
                Object.DestroyImmediate(values);
            }
        }
    }
}
