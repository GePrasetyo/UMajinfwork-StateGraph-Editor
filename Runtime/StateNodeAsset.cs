using UnityEngine;
using System;

namespace Majinfwork.StateGraph {
    public abstract class StateNodeAsset : ScriptableObject {
        [HideInInspector] public string guid;
        [HideInInspector] public Vector2 position;

        /// <summary>Index into a StateContext's data slots. Assigned by the graph.</summary>
        public int RuntimeIndex { get; internal set; } = -1;

        /// <summary>Fallback for exits triggered outside a lifecycle call.</summary>
        public Action<StateTransition> onTransitionTriggered;

        // Shared across agents: keep mutable data in ctx, not in fields.

        public virtual void Begin(StateContext ctx) => Begin();
        public virtual void Tick(StateContext ctx) => Tick();
        public virtual void End(StateContext ctx) => End();

        // Single-instance: fields are fine, but the graph must be cloned per agent.

        public virtual void Begin() { }
        public virtual void Tick() { }
        public virtual void End() { }

        public virtual void ResetNode() { }

        /// <summary>Call synchronously inside Begin/Tick/End. After an await, use ctx.Exit.</summary>
        protected void TriggerExit(StateTransition transition) {
            var ctx = StateContext.Current;
            if (ctx != null) {
                ctx.Exit(transition);
                return;
            }

            onTransitionTriggered?.Invoke(transition);
        }
    }
}
