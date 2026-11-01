using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.StateGraph {
    /// <summary>A runnable state graph. Authored as a State Graph (.mstategraph) and baked by its importer.</summary>
    public class StateGraphAsset : ScriptableObject {
        public string entryNodeGuid;
        public List<StateNodeAsset> allStates = new List<StateNodeAsset>();
        public bool SharedAsset;

        private int preparedCount = -1;

        /// <summary>Assigns each state its data-slot index. Idempotent.</summary>
        public void PrepareRuntimeIndices() {
            if (preparedCount == allStates.Count) return;

            for (int i = 0; i < allStates.Count; i++) {
                if (allStates[i] != null) allStates[i].RuntimeIndex = i;
            }

            preparedCount = allStates.Count;
        }

        /// <summary>A copy whose states belong to it alone, for graphs whose states keep data in their fields.</summary>
        public StateGraphAsset Clone() {
            StateGraphAsset newGraph = Instantiate(this);
            newGraph.allStates = new List<StateNodeAsset>();

            var cloneMap = new Dictionary<StateNodeAsset, StateNodeAsset>();

            foreach (var original in allStates) {
                var clone = Instantiate(original);
                newGraph.allStates.Add(clone);
                cloneMap.Add(original, clone);
            }

            cloneMap.RemapTransitions();
            return newGraph;
        }
    }
}
