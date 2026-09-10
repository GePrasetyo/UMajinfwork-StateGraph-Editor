using System;

namespace Majinfwork.StateGraph {
    /// <summary>Per-runner state, so one graph asset can run on many agents.</summary>
    public sealed class StateContext {

        /// <summary>Valid only inside a lifecycle call. Set by StateRunner.</summary>
        public static StateContext Current { get; internal set; }

        public StateRunner Runner { get; }

        private object[] slots;
        private StateTransition requested;

        public StateContext(StateRunner runner, int nodeCount) {
            Runner = runner;
            slots = new object[Math.Max(nodeCount, 0)];
        }

        internal void Resize(int nodeCount) {
            if (nodeCount <= slots.Length) return;
            Array.Resize(ref slots, nodeCount);
        }

        /// <summary>This agent's data for a state, created on first use.</summary>
        public T GetData<T>(StateNodeAsset node) where T : class, new() {
            int index = node.RuntimeIndex;
            if (index < 0) throw new InvalidOperationException(
                $"'{node.name}' has no runtime index. The graph must be prepared by a StateRunner before use.");

            if (index >= slots.Length) Resize(index + 1);
            return (T)(slots[index] ??= new T());
        }

        public bool HasData(StateNodeAsset node) {
            int index = node.RuntimeIndex;
            return index >= 0 && index < slots.Length && slots[index] != null;
        }

        public void Exit(StateTransition transition) {
            if (transition != null) requested = transition;
        }

        internal StateTransition ConsumeRequest() {
            var value = requested;
            requested = null;
            return value;
        }

        internal void ClearRequest() => requested = null;

        /// <summary>For pooling.</summary>
        public void Reset() {
            Array.Clear(slots, 0, slots.Length);
            requested = null;
        }
    }
}
