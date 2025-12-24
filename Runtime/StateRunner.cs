using UnityEngine;
using System.Linq;

namespace Majinfwork.StateGraph {
    public sealed class StateRunner : MonoBehaviour {
        [SerializeField] private StateGraphAsset graphTemplate;
        private StateGraphAsset runtimeGraph;
        private StateNodeAsset activeState;
        private bool newStateBegin;

        public StateNodeAsset CurrentState => activeState;

        void Awake() {
            if (graphTemplate != null) {
                runtimeGraph = graphTemplate.SharedAsset ? graphTemplate : graphTemplate.Clone();
            }
        }

        void Start() {
            if (runtimeGraph == null) return;

            var entry = runtimeGraph.allStates.FirstOrDefault(s => s.guid == runtimeGraph.entryNodeGuid);
            if (entry != null) {
                TransitionTo(entry);
            }
        }

        void Update() {
            if (newStateBegin) {
                newStateBegin = false;
                activeState?.Begin(this);
            }
            else {
                activeState?.Tick(this);
            }
        } 

        public void TransitionTo(StateNodeAsset next) {
            if (activeState != null) {
                activeState.onTransitionTriggered -= OnStateRequestedTransition;
                activeState.End(this);
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
    }
}