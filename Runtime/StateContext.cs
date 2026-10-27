using System;
using System.Threading;

namespace Majinfwork.StateGraph {
    /// <summary>Per-runner state, so one graph asset can run on many agents.</summary>
    public sealed class StateContext {

        /// <summary>Valid only inside a lifecycle call. Set by StateRunner.</summary>
        public static StateContext Current { get; internal set; }

        public StateRunner Runner { get; }

        private object[] slots;
        private StateTransition requested;
        private CancellationTokenSource exitSource;

        /// <summary>Cancelled when the running state ends: async work it started stops with it.</summary>
        public CancellationToken ExitToken => exitSource?.Token ?? CancellationToken.None;

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
            if (transition == null) return;

            // Async work of a state that has already ended must not move the runner on from the state it is in now.
            var active = Runner != null ? Runner.CurrentState : null;
            if (active != null && !StateTransitionFields.Owns(active, transition)) return;

            requested = transition;
        }

        internal void BeginState() {
            exitSource?.Dispose();
            exitSource = new CancellationTokenSource();
        }

        internal void EndState() {
            if (exitSource == null) return;

            exitSource.Cancel();
            exitSource.Dispose();
            exitSource = null;
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
            EndState();
        }
    }
}
