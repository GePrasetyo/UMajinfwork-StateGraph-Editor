using UnityEngine;
using System;

namespace Majinfwork.StateGraph {
    public abstract class StateNodeAsset : ScriptableObject {
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;

        // The Runner will subscribe to this to know when a transition is requested
        public Action<StateTransition> onTransitionTriggered;

        public abstract void Begin();
        public abstract void Tick();
        public abstract void End();

        public virtual void ResetNode() { }

        protected void TriggerExit(StateTransition transition) {
            onTransitionTriggered?.Invoke(transition);
        }
    }
}