using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Majinfwork.StateGraph {
    public static class StateNodeIds {
        public const string Enter = "Enter";
        public const string NameOption = "Name";
        public const string FieldsOption = "Fields";
    }

    /// <summary>A node the importer turns into a state.</summary>
    public interface IStateNode {
        StateNodeAsset BakeState();
    }

    /// <summary>
    /// The node for one state type: an Enter port, an exit port per StateTransition field, and the state's other
    /// fields drawn on the node. Graph Toolkit lists one entry per node class, so each state type gets a one-line
    /// subclass: <c>[Serializable] public class WaitStateNode : StateNode&lt;WaitState&gt; { }</c>.
    /// </summary>
    [Serializable]
    [UseWithGraph(typeof(StateMachineGraph))]
    public abstract class StateNode<TState> : Node, IStateNode where TState : StateNodeAsset {
        public override void OnEnable() => ShowName();

        protected override void OnDefineOptions(IOptionDefinitionContext context) {
            context.AddOption<string>(StateNodeIds.NameOption)
                .WithTooltip("Names the state in the graph and in the baked asset. Empty uses the state's type.")
                .Delayed()
                .ShowInInspectorOnly();
            context.AddOption<StateFields<TState>>(StateNodeIds.FieldsOption)
                .WithDisplayName(string.Empty)
                .WithDefaultValue(new StateFields<TState>());
        }

        protected override void OnDefinePorts(IPortDefinitionContext context) {
            // The node is defined again whenever an option changes, the name included.
            ShowName();

            context.AddInputPort(StateNodeIds.Enter).WithCapacity(PortCapacity.Multi).Build();

            TState probe = ScriptableObject.CreateInstance<TState>();
            try {
                using var state = new SerializedObject(probe);
                SerializedProperty property = state.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false)) {
                    if (property.type != nameof(StateTransition)) continue;

                    context.AddOutputPort(property.name)
                        .WithDisplayName(property.displayName)
                        .WithCapacity(PortCapacity.Single)
                        .Build();
                }
            }
            finally {
                Object.DestroyImmediate(probe);
            }
        }

        public StateNodeAsset BakeState() {
            TState state = ScriptableObject.CreateInstance<TState>();
            if (GetNodeOptionByName(StateNodeIds.FieldsOption)?.TryGetValue(out StateFields<TState> fields) == true) {
                fields?.WriteTo(state);
            }

            state.name = StateName(out _);
            return state;
        }

        private void ShowName() {
            Title = StateName(out bool named);
            Subtitle = named ? TypeName : string.Empty;
        }

        private string StateName(out bool named) {
            string name = null;
            GetNodeOptionByName(StateNodeIds.NameOption)?.TryGetValue(out name);
            named = !string.IsNullOrWhiteSpace(name);
            return named ? name.Trim() : TypeName;
        }

        private static string TypeName => ObjectNames.NicifyVariableName(typeof(TState).Name);
    }
}
