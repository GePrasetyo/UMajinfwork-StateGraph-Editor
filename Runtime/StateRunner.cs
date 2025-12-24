using UnityEngine;
using System.Linq;

namespace Majinfwork.StateGraph {
    public sealed class StateRunner : MonoBehaviour {
        [SerializeField] private StateGraphAsset graphTemplate;
        private StateGraphAsset runtimeGraph;
        private StateNodeAsset activeState;
        private bool newStateBegin;

        public StateNodeAsset CurrentState => activeState;

        void Start() {
            if (graphTemplate != null) {
                SetRuntimeGraph(graphTemplate.SharedAsset ? graphTemplate : graphTemplate.Clone());
            }
        }

        void Update() {
            if (runtimeGraph == null) return;

            if (newStateBegin) {
                newStateBegin = false;
                activeState?.Begin();
            }
            else {
                activeState?.Tick();
            }
        } 

        public void TransitionTo(StateNodeAsset next) {
            if (activeState != null) {
                activeState.onTransitionTriggered -= OnStateRequestedTransition;
                activeState.End();
            }

            if (next != null) {
                next.onTransitionTriggered += OnStateRequestedTransition;
            }

            activeState = next;
            newStateBegin = true;
        }

        private void OnStateRequestedTransition(StateTransition transition) {
            if (transition.targetState != null) TransitionTo(transition.targetState);
        }

        public void SetGraph(StateGraphAsset asset) {
            graphTemplate = asset;
        }

        public void SetRuntimeGraph(StateGraphAsset asset) {
            runtimeGraph = asset;

            var entry = runtimeGraph.allStates.FirstOrDefault(s => s.guid == runtimeGraph.entryNodeGuid);
            if (entry != null) {
                TransitionTo(entry);
            }
        }
    }
}