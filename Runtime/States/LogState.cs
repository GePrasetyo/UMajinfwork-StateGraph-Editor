using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class LogState : StateNodeAsset {
        public StateTransition Exit;

        public string message = "Hello World";
        public Color logColor = Color.white;
        

        public override void Begin(StateRunner owner) {
            Debug.Log(message);
            Trigger(Exit);
        }

        public override void Tick(StateRunner owner) { }
        public override void End(StateRunner owner) { }
    }
}