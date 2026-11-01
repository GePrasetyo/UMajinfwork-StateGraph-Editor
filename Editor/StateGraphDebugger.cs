using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using Unity.GraphToolkit.Editor.GraphVisualization;
using UnityEditor;
using UnityEngine;

namespace Majinfwork.StateGraph {
    /// <summary>
    /// In Play mode, marks the running states in the State Graph windows on screen: the selected runner's state when a
    /// runner is selected, every runner's otherwise. Baked states carry their node's id, which is how they're found.
    /// </summary>
    [InitializeOnLoad]
    internal static class StateGraphDebugger {
        private const double Interval = 0.1;
        private const float Full = 100f;

        private static readonly HashSet<Hash128> running = new();
        private static readonly HashSet<Hash128> requested = new();
        private static readonly Dictionary<Hash128, HashSet<Hash128>> marked = new();
        private static double nextUpdate;

        static StateGraphDebugger() {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += change => {
                if (change != PlayModeStateChange.ExitingPlayMode) return;

                // Graph Toolkit drops every visualization context when Play mode ends.
                requested.Clear();
                marked.Clear();
            };
        }

        private static void Update() {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextUpdate) return;
            nextUpdate = EditorApplication.timeSinceStartup + Interval;

            CollectRunning();
            foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>()) {
                if (window is not IGraphWindow { Graph: StateMachineGraph graph }) continue;

                // A window behind another tab has no node views to mark; it's marked afresh when it shows again.
                if (window.rootVisualElement.panel == null) marked.Remove(graph.ID);
                else Mark(graph);
            }
        }

        private static void CollectRunning() {
            running.Clear();
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<StateRunner>() : null;
            if (selected != null) {
                Add(selected);
                return;
            }

            foreach (StateRunner runner in Object.FindObjectsByType<StateRunner>(FindObjectsInactive.Exclude)) Add(runner);

            static void Add(StateRunner runner) {
                StateNodeAsset state = runner.CurrentState;
                if (state != null) running.Add(Hash128.Parse(state.guid));
            }
        }

        private static void Mark(StateMachineGraph graph) {
            Context context = Registry.GetActiveContext(graph.ID);
            if (context == null) {
                // The graph's window attaches to the context once it exists.
                if (requested.Add(graph.ID)) Registry.CreateVisualizationContext(graph.ID);
                return;
            }

            if (!context.IsGraphLoaded) return;

            if (!marked.TryGetValue(graph.ID, out HashSet<Hash128> nodes)) {
                context.ClearAllVisualization();
                marked[graph.ID] = nodes = new HashSet<Hash128>();
            }

            foreach (INode node in graph.GetNodes()) {
                if (node is not IStateNode) continue;

                bool isRunning = running.Contains(node.ID);
                if (isRunning == nodes.Contains(node.ID)) continue;

                NodeReference reference = context.GetNodeReference(node.ID);
                if (isRunning) {
                    reference.FillAmount = Full;
                    context.Motion.Play(reference, 1f);
                    nodes.Add(node.ID);
                }
                else {
                    context.Motion.Stop(reference);
                    reference.ClearCustomization();
                    nodes.Remove(node.ID);
                }
            }
        }
    }
}
