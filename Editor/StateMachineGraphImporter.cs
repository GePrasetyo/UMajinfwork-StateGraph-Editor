using System.Collections.Generic;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Majinfwork.StateGraph {
    /// <summary>
    /// Bakes a StateMachineGraph into the StateGraphAsset that StateRunner runs: one state sub-asset per state node,
    /// each exit's owner and target from the wires (portals included), the entry and sharing from the Entry node.
    /// </summary>
    [ScriptedImporter(1, StateMachineGraph.Extension)]
    public sealed class StateMachineGraphImporter : ScriptedImporter {
        public override void OnImportAsset(AssetImportContext ctx) {
            var asset = ScriptableObject.CreateInstance<StateGraphAsset>();
            ctx.AddObjectToAsset("graph", asset);
            ctx.SetMainObject(asset);

            var graph = GraphDatabase.LoadGraphForImporter<StateMachineGraph>(ctx.assetPath);
            if (graph == null) return;

            var states = new Dictionary<Hash128, StateNodeAsset>();
            foreach (INode node in graph.GetNodes()) {
                if (node is not IStateNode stateNode) continue;

                StateNodeAsset state = stateNode.BakeState();
                state.guid = node.ID.ToString();
                state.position = node.Position;
                states.Add(node.ID, state);
                asset.allStates.Add(state);
                ctx.AddObjectToAsset(state.guid, state);

                // A state's exits are its ports: bake again when its script changes.
                ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(state)));
            }

            foreach (INode node in graph.GetNodes()) {
                if (states.TryGetValue(node.ID, out StateNodeAsset state)) Connect(node, state, asset.allStates.IndexOf(state), states);

                if (node is StateGraphEntryNode entry) {
                    INode first = entry.GetOutputPortByName(StateGraphEntryNode.StartPort).FirstConnectedPort?.GetNode();
                    if (first != null && states.TryGetValue(first.ID, out StateNodeAsset start)) asset.entryNodeGuid = start.guid;
                    entry.GetNodeOptionByName(StateGraphEntryNode.SharedOption)?.TryGetValue(out asset.SharedAsset);
                }
            }
        }

        // Each exit port is named after its StateTransition field, or its exit list's element ("places[2]").
        private static void Connect(INode node, StateNodeAsset state, int owner, Dictionary<Hash128, StateNodeAsset> states) {
            using var serialized = new SerializedObject(state);
            foreach (IPort port in node.GetOutputPorts()) {
                SerializedProperty transition = ExitProperty.Find(serialized, port.Name);
                if (transition == null) continue;

                StateNodeAsset next = null;
                INode target = port.FirstConnectedPort?.GetNode();
                if (target != null) states.TryGetValue(target.ID, out next);

                transition.FindPropertyRelative(nameof(StateTransition.owner)).intValue = owner;
                transition.FindPropertyRelative(nameof(StateTransition.targetState)).objectReferenceValue = next;
                transition.FindPropertyRelative(nameof(StateTransition.targetNodeGuid)).stringValue = next != null ? next.guid : string.Empty;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
