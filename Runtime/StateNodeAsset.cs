using UnityEngine;
using System;

namespace Majinfwork.StateGraph {
    public abstract class StateNodeAsset : ScriptableObject {
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;

        // The Runner will subscribe to this to know when a transition is requested
        public Action<StateTransition> onTransitionTriggered;

        public abstract void Begin(StateRunner owner);
        public abstract void Tick(StateRunner owner);
        public abstract void End(StateRunner owner);

        protected void Trigger(StateTransition transition) {
            onTransitionTriggered?.Invoke(transition);
        }
    }
}