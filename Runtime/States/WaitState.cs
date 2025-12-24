using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class WaitState : StateNodeAsset {
        public StateTransition Exit;

        public float waitTime;
        private float timer;

        public override void Begin(StateRunner owner) {
            timer = waitTime;
        }

        public override void Tick(StateRunner owner) { 
            timer -= Time.deltaTime;

            if (timer < 0) {
                Trigger(Exit);
            }
        }

        public override void End(StateRunner owner) { }
    }
}