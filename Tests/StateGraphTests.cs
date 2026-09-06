using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Majinfwork.StateGraph;
using UnityEngine;
using UnityEngine.TestTools;

namespace MStateGraph.Tests {

    /// <summary>Records the lifecycle calls the runner makes, in order.</summary>
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

        // Regression: GameInstance.Construct initialises the runner, then Unity
        // calls Start() which initialises it again. The entry state must not be
        // ended before its Begin() has ever run.
        [UnityTest]
        public IEnumerator Runner_DoesNotEndAStateThatNeverBegan() {
            if (!RequirePlayMode()) yield break;

            var entry = NewState("Entry");
            var graph = BuildGraph(shared: true, entry);
            var go = new GameObject("runner");
            var runner = go.AddComponent<StateRunner>();

            // Mimic GameInstance.Construct: initialise before Start() runs.
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
    }
}
