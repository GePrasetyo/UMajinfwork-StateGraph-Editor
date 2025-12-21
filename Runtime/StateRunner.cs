using UnityEngine;
using System.Linq;

namespace Majinfwork.StateGraph {
    public sealed class StateRunner : MonoBehaviour {
        [SerializeField] private StateGraphAsset graphTemplate;
        private StateGraphAsset _runtimeGraph;
        private StateNodeAsset _activeState;

        void Awake() {
            if (graphTemplate != null) {
                _runtimeGraph = graphTemplate.SharedAsset ? graphTemplate : graphTemplate.Clone();
            }
        }

        void Start() {
            if (_runtimeGraph == null) return;

            var entry = _runtimeGraph.allStates.FirstOrDefault(s => s.guid == _runtimeGraph.entryNodeGuid);
            if (entry != null) TransitionTo(entry);
        }

        void Update() => _activeState?.Tick(this);

        public void TransitionTo(StateNodeAsset next) {
            if (_activeState != null) {
                _activeState.onTransitionTriggered -= OnStateRequestedTransition;
                _activeState.End(this);
            }

            _activeState = next;

            if (_activeState != null) {
                _activeState.onTransitionTriggered += OnStateRequestedTransition;
                _activeState.Begin(this);
            }
        }

        private void OnStateRequestedTransition(StateTransition transition) {
            if (transition.targetState != null) TransitionTo(transition.targetState);
        }
    }
}