using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Majinfwork.StateGraph {
    [CreateAssetMenu(fileName = "NewStateGraph", menuName = "State Machine/Graph Asset")]
    public class StateGraphAsset : ScriptableObject {
        public string entryNodeGuid;
        public List<StateNodeAsset> allStates = new List<StateNodeAsset>();
        public bool SharedAsset;

        // Creates a unique copy of the entire graph for a specific Bot
        public StateGraphAsset Clone() {
            StateGraphAsset newGraph = Instantiate(this);
            newGraph.allStates = new List<StateNodeAsset>();

            // 1. Map to track Original -> Clone
            var cloneMap = new Dictionary<StateNodeAsset, StateNodeAsset>();

            foreach (var original in allStates) {
                var clone = Instantiate(original);
                newGraph.allStates.Add(clone);
                cloneMap.Add(original, clone);
            }

            // 2. Re-point transitions to the clones instead of the assets
            foreach (var node in newGraph.allStates) {
                var fields = node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var f in fields) {
                    if (f.FieldType == typeof(StateTransition)) {
                        var trans = f.GetValue(node) as StateTransition;
                        // If the transition points to an original asset, swap it for our local clone
                        if (trans != null && trans.targetState != null) {
                            if (cloneMap.TryGetValue(trans.targetState, out var localClone))
                                trans.targetState = localClone;
                        }
                    }
                }
            }
            return newGraph;
        }
    }
}