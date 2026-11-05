using Majinfwork.StateGraph;

namespace MStateGraph.Tests {
    /// <summary>A state with a list of exits, for the importer tests.</summary>
    public class HubState : StateNodeAsset {
        public StateTransition[] places = System.Array.Empty<StateTransition>();
    }
}
