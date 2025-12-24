using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class WaitState : StateNodeAsset {
        public StateTransition Exit;

        public float waitTime;
        private float timer;

        public override void Begin() {
            timer = waitTime;
        }

        public override void Tick() { 
            timer -= Time.deltaTime;

            if (timer < 0) {
                TriggerExit(Exit);
            }
        }

        public override void End() { }
    }
}