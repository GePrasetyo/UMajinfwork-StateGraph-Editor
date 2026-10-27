using UnityEngine;
using System.Linq;

namespace Majinfwork.StateGraph {
    public sealed class StateRunner : MonoBehaviour {
        [SerializeField] private StateGraphAsset graphTemplate;
        private StateGraphAsset runtimeGraph;
        private StateContext context;
        private StateNodeAsset activeState;
        private bool beginPending; // queued to Begin(), not begun yet

        public StateNodeAsset CurrentState => activeState;

        public StateContext Context => context;

        void Start() {
            if (graphTemplate != null) {
                SetRuntimeGraph(graphTemplate.SharedAsset ? graphTemplate : graphTemplate.Clone());
            }
        }

        void Update() {
            if (runtimeGraph == null || activeState == null) return;

            if (beginPending) {
                beginPending = false;
                Run(activeState, begin: true);
            }
            else {
                Run(activeState, begin: false);
            }
        }

        void OnDestroy() {
            EndActiveState();
            activeState = null;
        }

        private void Run(StateNodeAsset state, bool begin) {
            var previous = StateContext.Current;
            StateContext.Current = context;

            try {
                if (begin) {
                    context.BeginState();
                    state.Begin(context);
                }
                else {
                    state.Tick(context);
                }
            }
            finally {
                StateContext.Current = previous;
            }

            var requested = context?.ConsumeRequest();
            if (requested?.targetState != null) TransitionTo(requested.targetState);
        }

        public void TransitionTo(StateNodeAsset next) {
            EndActiveState();

            if (next != null) {
                next.onTransitionTriggered += OnStateRequestedTransition;
            }

            activeState = next;
            beginPending = true;
        }

        private void EndActiveState() {
            if (activeState == null) return;

            activeState.onTransitionTriggered -= OnStateRequestedTransition;

            // A state queued but not begun has nothing to end.
            if (beginPending) return;

            context.EndState();
            var previous = StateContext.Current;
            StateContext.Current = context;

            try { activeState.End(context); }
            finally { StateContext.Current = previous; }

            context.ClearRequest();
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

            runtimeGraph.PrepareRuntimeIndices();

            if (context == null) context = new StateContext(this, runtimeGraph.allStates.Count);
            else context.Resize(runtimeGraph.allStates.Count);

            var entry = runtimeGraph.allStates.FirstOrDefault(s => s.guid == runtimeGraph.entryNodeGuid);
            if (entry != null) {
                TransitionTo(entry);
            }
        }
    }
}
