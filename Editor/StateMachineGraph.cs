using System;
using Unity.GraphToolkit.Editor;
using UnityEditor;

namespace Majinfwork.StateGraph {
    /// <summary>A state graph authored in Graph Toolkit. StateMachineGraphImporter bakes it into a StateGraphAsset.</summary>
    [Serializable]
    [Graph(Extension, GraphOptions.Default)]
    public class StateMachineGraph : Graph {
        public const string Extension = "mstategraph";

        [MenuItem("Assets/Create/MFramework/State Machine/State Graph")]
        private static void Create() => GraphDatabase.PromptInProjectBrowserToCreateNewAsset<StateMachineGraph>("New State Graph");

        public override void OnGraphChanged(GraphLogger logger) {
            int entries = 0;
            foreach (INode node in GetNodes()) {
                if (node is not StateGraphEntryNode entry) continue;

                entries++;
                if (!entry.GetOutputPortByName(StateGraphEntryNode.StartPort).IsConnected) {
                    logger.LogWarning("Entry isn't connected to a state.", entry);
                }
            }

            if (entries == 0) logger.LogError("The graph needs an Entry node.");
            else if (entries > 1) logger.LogError("The graph has more than one Entry node.");
        }
    }
}
