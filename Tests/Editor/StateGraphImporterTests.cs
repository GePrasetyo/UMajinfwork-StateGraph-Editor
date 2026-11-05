using Majinfwork.StateGraph;
using NUnit.Framework;
using Unity.GraphToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace MStateGraph.Tests {
    [System.Serializable] public class HubStateNode : StateNode<HubState> { }

    public class StateGraphImporterTests {
        private const string Path = "Assets/__StateGraphImporterTest." + StateMachineGraph.Extension;

        [TearDown]
        public void TearDown() => AssetDatabase.DeleteAsset(Path);

        // Entry -> Log -> Wait -> back to Log, through Graph Toolkit's API, then baked by the importer.
        [Test]
        public void Import_BakesStatesExitsOwnersAndEntry() {
            var graph = GraphDatabase.CreateGraph<StateMachineGraph>(Path);
            var entry = new StateGraphEntryNode();
            var log = new LogStateNode();
            var wait = new WaitStateNode();
            graph.AddNode(entry);
            graph.AddNode(log);
            graph.AddNode(wait);

            var values = ScriptableObject.CreateInstance<LogState>();
            values.message = "baked";
            Assert.IsTrue(log.GetNodeOptionByName(StateNodeIds.FieldsOption).TrySetValue(StateFields<LogState>.Of(values)));
            Assert.IsTrue(log.GetNodeOptionByName(StateNodeIds.NameOption).TrySetValue("Greeting"));
            Object.DestroyImmediate(values);

            Assert.IsTrue(graph.Connect(entry.GetOutputPortByName(StateGraphEntryNode.StartPort), log.GetInputPortByName(StateNodeIds.Enter)));
            Assert.IsTrue(graph.Connect(log.GetOutputPortByName(nameof(LogState.Exit)), wait.GetInputPortByName(StateNodeIds.Enter)));
            Assert.IsTrue(graph.Connect(wait.GetOutputPortByName(nameof(WaitState.Exit)), log.GetInputPortByName(StateNodeIds.Enter)));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);

            var baked = AssetDatabase.LoadAssetAtPath<StateGraphAsset>(Path);
            Assert.IsNotNull(baked);
            Assert.AreEqual(2, baked.allStates.Count);

            var bakedLog = baked.allStates.Find(s => s is LogState) as LogState;
            var bakedWait = baked.allStates.Find(s => s is WaitState) as WaitState;
            Assert.AreEqual(bakedLog.guid, baked.entryNodeGuid);
            Assert.AreEqual("baked", bakedLog.message);
            Assert.AreEqual("Greeting", bakedLog.name);

            Assert.AreSame(bakedWait, bakedLog.Exit.targetState);
            Assert.AreEqual(baked.allStates.IndexOf(bakedLog), bakedLog.Exit.owner);
            Assert.AreSame(bakedLog, bakedWait.Exit.targetState);
            Assert.AreEqual(baked.allStates.IndexOf(bakedWait), bakedWait.Exit.owner);
            Assert.IsFalse(baked.SharedAsset);
        }

        // An exit list gets one port per element, sized by the node's own values, and each element bakes its wire.
        [Test]
        public void Import_ExitListGetsAPortPerElement() {
            var graph = GraphDatabase.CreateGraph<StateMachineGraph>(Path);
            var entry = new StateGraphEntryNode();
            var hub = new HubStateNode();
            var log = new LogStateNode();
            var wait = new WaitStateNode();
            graph.AddNode(entry);
            graph.AddNode(hub);
            graph.AddNode(log);
            graph.AddNode(wait);

            var values = ScriptableObject.CreateInstance<HubState>();
            values.places = new StateTransition[2];
            Assert.IsTrue(hub.GetNodeOptionByName(StateNodeIds.FieldsOption).TrySetValue(StateFields<HubState>.Of(values)));
            Object.DestroyImmediate(values);

            IPort first = hub.GetOutputPortByName("places[0]");
            IPort second = hub.GetOutputPortByName("places[1]");
            Assert.IsNotNull(first, "The first element has no port.");
            Assert.IsNotNull(second, "The second element has no port.");
            Assert.IsNull(hub.GetOutputPortByName("places[2]"));

            Assert.IsTrue(graph.Connect(entry.GetOutputPortByName(StateGraphEntryNode.StartPort), hub.GetInputPortByName(StateNodeIds.Enter)));
            Assert.IsTrue(graph.Connect(first, log.GetInputPortByName(StateNodeIds.Enter)));
            Assert.IsTrue(graph.Connect(second, wait.GetInputPortByName(StateNodeIds.Enter)));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);

            var baked = AssetDatabase.LoadAssetAtPath<StateGraphAsset>(Path);
            var bakedHub = baked.allStates.Find(s => s is HubState) as HubState;
            Assert.AreEqual(2, bakedHub.places.Length);
            Assert.AreSame(baked.allStates.Find(s => s is LogState), bakedHub.places[0].targetState);
            Assert.AreSame(baked.allStates.Find(s => s is WaitState), bakedHub.places[1].targetState);
            Assert.AreEqual(baked.allStates.IndexOf(bakedHub), bakedHub.places[0].owner);
            Assert.AreEqual(baked.allStates.IndexOf(bakedHub), bakedHub.places[1].owner);
        }

        // An exit with no wire still knows its state, so the runner accepts it and simply stays.
        [Test]
        public void Import_UnwiredExitKeepsItsOwnerAndNoTarget() {
            var graph = GraphDatabase.CreateGraph<StateMachineGraph>(Path);
            var entry = new StateGraphEntryNode();
            var log = new LogStateNode();
            graph.AddNode(entry);
            graph.AddNode(log);
            Assert.IsTrue(graph.Connect(entry.GetOutputPortByName(StateGraphEntryNode.StartPort), log.GetInputPortByName(StateNodeIds.Enter)));
            GraphDatabase.SaveGraph(graph);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);

            var bakedLog = (LogState)AssetDatabase.LoadAssetAtPath<StateGraphAsset>(Path).allStates[0];
            Assert.AreEqual(0, bakedLog.Exit.owner);
            Assert.IsNull(bakedLog.Exit.targetState);
        }
    }
}
