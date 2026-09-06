using UnityEngine;
using System.Linq;

namespace Majinfwork.StateGraph {
    public sealed class StateRunner : MonoBehaviour {
        [SerializeField] private StateGraphAsset graphTemplate;
        private StateGraphAsset runtimeGraph;
        private StateNodeAsset activeState;
        // True while activeState is queued to Begin() but has not begun yet.
        private bool beginPending;

        public StateNodeAsset CurrentState => activeState;

        void Start() {
            if (graphTemplate != null) {
                SetRuntimeGraph(graphTemplate.SharedAsset ? graphTemplate : graphTemplate.Clone());
            }
        }

        void Update() {
            if (runtimeGraph == null) return;

            if (beginPending) {
                beginPending = false;
                activeState?.Begin();
            }
            else {
                activeState?.Tick();
            }
        }

        public void TransitionTo(StateNodeAsset next) {
            if (activeState != null) {
                activeState.onTransitionTriggered -= OnStateRequestedTransition;

                // Only end a state that actually began. A runner initialised
                // externally and then again by Start() (GameInstance.Construct
                // does exactly this) would otherwise call End() on an entry
                // state whose Begin() never ran.
                if (!beginPending) activeState.End();
            }

            if (next != null) {
                next.onTransitionTriggered += OnStateRequestedTransition;
            }

            activeState = next;
            beginPending = true;
        }

        private void OnStateRequestedTransition(StateTransition transition) {
            if (transition.targetState != null) TransitionTo(transition.targetState);
        }

        public void SetGraph(StateGraphAsset asset) {
            graphTemplate = asset;
        }

        public void SetRuntimeGraph(StateGraphAsset asset) {
            runtimeGraph = asset;
            if (runtimeGraph == null) return;

            var entry = runtimeGraph.allStates.FirstOrDefault(s => s.guid == runtimeGraph.entryNodeGuid);
            if (entry != null) {
                TransitionTo(entry);
            }
        }
    }
}