using System;

namespace Majinfwork.StateGraph {
    /// <summary>An exit of a state. The State Graph importer fills it from the graph's wires.</summary>
    [Serializable]
    public class StateTransition {
        public string targetNodeGuid;
        public StateNodeAsset targetState;

        /// <summary>Index in the graph's states of the state this exit belongs to.</summary>
        public int owner = -1;
    }
}
