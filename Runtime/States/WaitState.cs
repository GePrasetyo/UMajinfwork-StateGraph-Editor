using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class WaitState : StateNodeAsset {
        public StateTransition Exit;

        public float waitTime;

        private sealed class Data {
            public float timer;
        }

        public override void Begin(StateContext ctx) {
            ctx.GetData<Data>(this).timer = waitTime;
        }

        public override void Tick(StateContext ctx) {
            var data = ctx.GetData<Data>(this);
            data.timer -= Time.deltaTime;

            if (data.timer < 0) TriggerExit(Exit);
        }

    }
}
