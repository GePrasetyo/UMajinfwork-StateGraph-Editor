using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class FlipFlopState : StateNodeAsset {
        public StateTransition ExitA;
        public StateTransition ExitB;

        public int index;

        public override void Begin(StateRunner owner) {
            index++;

            if (index % 2 == 0) {
                Trigger(ExitB);
            }
            else {
                Trigger(ExitA);
            }
        }

        public override void Tick(StateRunner owner) { }
        public override void End(StateRunner owner) { }
    }
}