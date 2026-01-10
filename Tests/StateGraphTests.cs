using NUnit.Framework;
using Majinfwork.StateGraph;
using UnityEngine;

namespace MStateGraph.Tests {
    public class StateGraphTests {
        [Test]
        public void StateGraphAsset_Create_Returns_Valid_Instance() {
            var graph = ScriptableObject.CreateInstance<StateGraphAsset>();

            Assert.IsNotNull(graph);

            Object.DestroyImmediate(graph);
        }

        [Test]
        public void StateTransition_Default_Has_Null_Target() {
            var transition = new StateTransition();

            Assert.IsNull(transition.targetState);
        }
    }
}
