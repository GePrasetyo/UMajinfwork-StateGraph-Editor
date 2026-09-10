using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Majinfwork.StateGraph;
using UnityEngine;
using UnityEngine.TestTools;

namespace MStateGraph.Tests {

    public class ProbeState : StateNodeAsset {
        public StateTransition Exit;

        [NonSerialized] public List<string> calls = new List<string>();
        [NonSerialized] public bool exitOnBegin;

        public override void Begin() {
            calls.Add(nameof(Begin));
            if (exitOnBegin) TriggerExit(Exit);
        }

        public override void Tick() => calls.Add(nameof(Tick));
        public override void End() => calls.Add(nameof(End));
    }


    public class SharedProbeState : StateNodeAsset {
        public StateTransition Exit;

        [NonSerialized] public string exitForRunner;

        private sealed class Data {
            public int begins;
            public int ends;
        }

        public override void Begin(StateContext ctx) => ctx.GetData<Data>(this).begins++;

        public override void Tick(StateContext ctx) {
            if (!string.IsNullOrEmpty(exitForRunner) && ctx.Runner != null && ctx.Runner.name == exitForRunner) {
                TriggerExit(Exit);
            }
        }

        public override void End(StateContext ctx) => ctx.GetData<Data>(this).ends++;

        public int Begins(StateContext ctx) => ctx.HasData(this) ? ctx.GetData<Data>(this).begins : 0;
        public int Ends(StateContext ctx) => ctx.HasData(this) ? ctx.GetData<Data>(this).ends : 0;
    }

    public class StateGraphTests {

        private static ProbeState NewState(string name) {
            var state = ScriptableObject.CreateInstance<ProbeState>();
            state.name = name;
            state.guid = Guid.NewGuid().ToString();
            return state;
        }

        private static StateGraphAsset BuildGraph(bool shared, params ProbeState[] states) {
            var graph = ScriptableObject.CreateInstance<StateGraphAsset>();
            graph.SharedAsset = shared;
            graph.allStates.AddRange(states);
            graph.entryNodeGuid = states[0].guid;
            return graph;
        }

        private static void Link(ProbeState from, ProbeState to) {
            from.Exit = new StateTransition { targetNodeGuid = to.guid, targetState = to };
        }

        private static bool RequirePlayMode() {
            if (Application.isPlaying) return true;
            Assert.Ignore("PlayMode only - MonoBehaviour Start/Update do not run in EditMode.");
            return false;
        }

        [Test]
        public void StateGraphAsset_Create_Returns_Valid_Instance() {
            var graph = ScriptableObject.CreateInstance<StateGraphAsset>();

            Assert.IsNotNull(graph);

            UnityEngine.Object.DestroyImmediate(graph);
        }

        [Test]
        public void StateTransition_Default_Has_Null_Target() {
            var transition = new StateTransition();

            Assert.IsNull(transition.targetState);
        }

        [UnityTest]
        public IEnumerator Runner_DoesNotEndAStateThatNeverBegan() {
            if (!RequirePlayMode()) yield break;

            var entry = NewState("Entry");
            var graph = BuildGraph(shared: true, entry);
            var go = new GameObject("runner");
            var runner = go.AddComponent<StateRunner>();

            runner.SetGraph(graph);
            runner.SetRuntimeGraph(graph);

            yield return null; // Start() + first Update()

            CollectionAssert.DoesNotContain(entry.calls, nameof(ProbeState.End));
            CollectionAssert.AreEqual(new[] { nameof(ProbeState.Begin) }, entry.calls);

            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Runner_BeginsThenTicks() {
            if (!RequirePlayMode()) yield break;

            var entry = NewState("Entry");
            var graph = BuildGraph(shared: true, entry);
            var go = new GameObject("runner");
            var runner = go.AddComponent<StateRunner>();
            runner.SetGraph(graph);

            yield return null; // Start() + Begin()
            yield return null; // Tick()

            CollectionAssert.AreEqual(
                new[] { nameof(ProbeState.Begin), nameof(ProbeState.Tick) }, entry.calls);

            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Runner_TransitionEndsSourceAndBeginsTarget() {
            if (!RequirePlayMode()) yield break;

            var first = NewState("First");
            var second = NewState("Second");
            Link(first, second);
            first.exitOnBegin = true;

            var graph = BuildGraph(shared: true, first, second);
            var go = new GameObject("runner");
            var runner = go.AddComponent<StateRunner>();
            runner.SetGraph(graph);

            yield return null; // Start(), First.Begin() -> transition
            yield return null; // Second.Begin()

            CollectionAssert.AreEqual(
                new[] { nameof(ProbeState.Begin), nameof(ProbeState.End) }, first.calls);
            CollectionAssert.AreEqual(
                new[] { nameof(ProbeState.Begin) }, second.calls);
            Assert.AreSame(second, runner.CurrentState);

            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Runner_EndsPreviousStateExactlyOnce() {
            if (!RequirePlayMode()) yield break;

            var first = NewState("First");
            var second = NewState("Second");
            Link(first, second);
            first.exitOnBegin = true;

            var graph = BuildGraph(shared: true, first, second);
            var go = new GameObject("runner");
            var runner = go.AddComponent<StateRunner>();
            runner.SetGraph(graph);
            runner.SetRuntimeGraph(graph); // double init, as GameInstance does

            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(1, first.calls.FindAll(c => c == nameof(ProbeState.End)).Count);

            UnityEngine.Object.Destroy(go);
        }

        [Test]
        public void SetRuntimeGraph_WithNull_DoesNotThrow() {
            var go = new GameObject("runner");
            var runner = go.AddComponent<StateRunner>();

            Assert.DoesNotThrow(() => runner.SetRuntimeGraph(null));

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Clone_RemapsTransitionsOntoClonedNodes() {
            var first = NewState("First");
            var second = NewState("Second");
            Link(first, second);

            var graph = BuildGraph(shared: false, first, second);
            var clone = graph.Clone();

            var clonedFirst = (ProbeState)clone.allStates[0];
            var clonedSecond = (ProbeState)clone.allStates[1];

            Assert.AreNotSame(first, clonedFirst);
            Assert.AreSame(clonedSecond, clonedFirst.Exit.targetState,
                "Cloned transition still points at the original asset.");
            Assert.AreSame(second, first.Exit.targetState,
                "Cloning must not rewrite the original graph.");
        }

        [Test]
        public void Clone_PreservesEntryNodeResolution() {
            var first = NewState("First");
            var second = NewState("Second");
            var graph = BuildGraph(shared: false, first, second);

            var clone = graph.Clone();

            Assert.AreEqual(graph.entryNodeGuid, clone.entryNodeGuid);
            Assert.AreEqual(clone.entryNodeGuid, clone.allStates[0].guid,
                "Entry guid must still resolve to a node inside the clone.");
        }

        private static SharedProbeState NewShared(string name) {
            var state = ScriptableObject.CreateInstance<SharedProbeState>();
            state.name = name;
            state.guid = Guid.NewGuid().ToString();
            return state;
        }

        private static StateGraphAsset BuildSharedGraph(params SharedProbeState[] states) {
            var graph = ScriptableObject.CreateInstance<StateGraphAsset>();
            graph.SharedAsset = true;
            graph.allStates.AddRange(states);
            graph.entryNodeGuid = states[0].guid;
            return graph;
        }

        [UnityTest]
        public IEnumerator SharedGraph_OneAgentTransitioning_DoesNotMoveTheOther() {
            if (!RequirePlayMode()) yield break;

            var chase = NewShared("Chase");
            var attack = NewShared("Attack");
            chase.Exit = new StateTransition { targetNodeGuid = attack.guid, targetState = attack };
            chase.exitForRunner = "agent-1";

            var graph = BuildSharedGraph(chase, attack);

            var go1 = new GameObject("agent-1");
            var go2 = new GameObject("agent-2");
            var r1 = go1.AddComponent<StateRunner>();
            var r2 = go2.AddComponent<StateRunner>();
            r1.SetGraph(graph);
            r2.SetGraph(graph);

            yield return null; // Start + Begin
            yield return null; // Tick -> agent-1 exits
            yield return null; // agent-1 begins Attack

            Assert.AreSame(attack, r1.CurrentState, "agent-1 should have moved to Attack.");
            Assert.AreSame(chase, r2.CurrentState, "agent-2 must not be dragged along by agent-1.");

            UnityEngine.Object.Destroy(go1);
            UnityEngine.Object.Destroy(go2);
        }

        [UnityTest]
        public IEnumerator SharedGraph_PerAgentDataIsIsolated() {
            if (!RequirePlayMode()) yield break;

            var chase = NewShared("Chase");
            var attack = NewShared("Attack");
            chase.Exit = new StateTransition { targetNodeGuid = attack.guid, targetState = attack };
            chase.exitForRunner = "agent-1";

            var graph = BuildSharedGraph(chase, attack);

            var go1 = new GameObject("agent-1");
            var go2 = new GameObject("agent-2");
            var r1 = go1.AddComponent<StateRunner>();
            var r2 = go2.AddComponent<StateRunner>();
            r1.SetGraph(graph);
            r2.SetGraph(graph);

            yield return null;
            yield return null;
            yield return null;

            Assert.AreEqual(1, chase.Begins(r1.Context), "agent-1 entered Chase once.");
            Assert.AreEqual(1, chase.Begins(r2.Context), "agent-2 entered Chase once.");
            Assert.AreEqual(1, chase.Ends(r1.Context), "agent-1 left Chase.");
            Assert.AreEqual(0, chase.Ends(r2.Context), "agent-2 never left Chase.");
            Assert.AreEqual(1, attack.Begins(r1.Context), "agent-1 entered Attack.");
            Assert.AreEqual(0, attack.Begins(r2.Context), "agent-2 never entered Attack.");

            UnityEngine.Object.Destroy(go1);
            UnityEngine.Object.Destroy(go2);
        }

        [UnityTest]
        public IEnumerator SharedGraph_RunsWithoutCloningTheAsset() {
            if (!RequirePlayMode()) yield break;

            var only = NewShared("Idle");
            var graph = BuildSharedGraph(only);

            var go = new GameObject("agent");
            var runner = go.AddComponent<StateRunner>();
            runner.SetGraph(graph);

            yield return null;

            Assert.AreSame(only, runner.CurrentState,
                "A shared graph must execute the original asset, not a clone.");
            Assert.IsNotNull(runner.Context);

            UnityEngine.Object.Destroy(go);
        }

        [Test]
        public void StateContext_DataIsLazyAndPerNode() {
            var a = NewShared("A");
            var b = NewShared("B");
            var graph = BuildSharedGraph(a, b);
            graph.PrepareRuntimeIndices();

            var ctx = new StateContext(null, graph.allStates.Count);

            Assert.IsFalse(ctx.HasData(a), "Data should not exist before a state is entered.");
            Assert.AreEqual(0, a.Begins(ctx));

            a.Begin(ctx);

            Assert.IsTrue(ctx.HasData(a));
            Assert.IsFalse(ctx.HasData(b), "Entering A must not allocate data for B.");
            Assert.AreEqual(1, a.Begins(ctx));

            ctx.Reset();
            Assert.IsFalse(ctx.HasData(a), "Reset should drop per-agent data for pooling.");
        }

        [Test]
        public void PrepareRuntimeIndices_AssignsListPositions() {
            var a = NewShared("A");
            var b = NewShared("B");
            var graph = BuildSharedGraph(a, b);

            graph.PrepareRuntimeIndices();

            Assert.AreEqual(0, a.RuntimeIndex);
            Assert.AreEqual(1, b.RuntimeIndex);
        }

    }
}
