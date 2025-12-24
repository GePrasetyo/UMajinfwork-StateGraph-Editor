using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class FlipFlopState : StateNodeAsset {
        public StateTransition ExitA;
        public StateTransition ExitB;

        public int index;

        public override void Begin() {
            index++;

            if (index % 2 == 0) {
                TriggerExit(ExitB);
            }
            else {
                TriggerExit(ExitA);
            }
        }

        public override void Tick() { }
        public override void End() { }
    }
}