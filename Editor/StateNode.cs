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

    /// <summary>How a state's serialized exits look to the editor.</summary>
    internal static class ExitProperty {
        /// <summary>A single exit: one port.</summary>
        public static bool IsExit(SerializedProperty property) => !property.isArray && property.type == nameof(StateTransition);

        /// <summary>A StateTransition[] or List&lt;StateTransition&gt;: one port per element, its size set on the node.</summary>
        public static bool IsExitList(SerializedProperty property) =>
            property.isArray && property.propertyType == SerializedPropertyType.Generic && property.arrayElementType == nameof(StateTransition);

        /// <summary>The port of an exit list's element: "places[2]" (Graph Toolkit reserves '.' in port ids).</summary>
        public static string ElementPort(string listName, int index) => $"{listName}[{index}]";

        /// <summary>The exit a port stands for: a single exit by its field name, a list element by its element port name.</summary>
        public static SerializedProperty Find(SerializedObject state, string portName) {
            int open = portName.LastIndexOf('[');
            if (open <= 0 || !portName.EndsWith("]") || !int.TryParse(portName.Substring(open + 1, portName.Length - open - 2), out int index)) {
                return state.FindProperty(portName);
            }

            SerializedProperty list = state.FindProperty(portName.Substring(0, open));
            return list != null && IsExitList(list) && index >= 0 && index < list.arraySize ? list.GetArrayElementAtIndex(index) : null;
        }
    }

    /// <summary>
    /// The node for one state type: an Enter port, an exit port per StateTransition field and per element of a
    /// StateTransition list (its size is set on the node), and the state's other fields drawn on the node. Graph Toolkit
    /// lists one entry per node class, so each state type gets a one-line subclass:
    /// <c>[Serializable] public class WaitStateNode : StateNode&lt;WaitState&gt; { }</c>.
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

            // The node's own values: an exit list has as many ports as the node gives it elements.
            TState probe = ScriptableObject.CreateInstance<TState>();
            try {
                if (GetNodeOptionByName(StateNodeIds.FieldsOption)?.TryGetValue(out StateFields<TState> fields) == true) {
                    fields?.WriteTo(probe);
                }

                using var state = new SerializedObject(probe);
                SerializedProperty property = state.GetIterator();
                property.NextVisible(true);
                while (property.NextVisible(false)) {
                    if (ExitProperty.IsExit(property)) {
                        AddExitPort(context, property.name, property.displayName);
                    }
                    else if (ExitProperty.IsExitList(property)) {
                        for (int i = 0; i < property.arraySize; i++) {
                            AddExitPort(context, ExitProperty.ElementPort(property.name, i), $"{property.displayName} {i + 1}");
                        }
                    }
                }
            }
            finally {
                Object.DestroyImmediate(probe);
            }
        }

        private static void AddExitPort(IPortDefinitionContext context, string name, string displayName) {
            context.AddOutputPort(name)
                .WithDisplayName(displayName)
                .WithCapacity(PortCapacity.Single)
                .Build();
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
