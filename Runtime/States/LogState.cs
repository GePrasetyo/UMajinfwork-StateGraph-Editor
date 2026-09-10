using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class LogState : StateNodeAsset {
        public StateTransition Exit;

        public string message = "Hello World";
        public Color logColor = Color.white;

        public override void Begin(StateContext ctx) {
            Debug.Log(message);
            ctx.Exit(Exit);
        }
    }
}
