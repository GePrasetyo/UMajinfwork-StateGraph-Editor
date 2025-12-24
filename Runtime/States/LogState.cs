using UnityEngine;

namespace Majinfwork.StateGraph {
    public sealed class LogState : StateNodeAsset {
        public StateTransition Exit;

        public string message = "Hello World";
        public Color logColor = Color.white;
        

        public override void Begin() {
            Debug.Log(message);
            TriggerExit(Exit);
        }

        public override void Tick() { }
        public override void End() { }
    }
}