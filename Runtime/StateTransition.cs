using System;

namespace Majinfwork.StateGraph {
    [Serializable]
    public class StateTransition {
        public string targetNodeGuid; // Keep for the Graph View logic
        public StateNodeAsset targetState; // The Editor will fill this in
    }
}