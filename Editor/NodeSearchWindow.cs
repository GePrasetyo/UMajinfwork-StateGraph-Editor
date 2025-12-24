using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Majinfwork.StateGraph {
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider {
        private StateGraphView graphView;
        private EditorWindow window;

        public void Init(StateGraphView graphView, EditorWindow window) {
            this.graphView = graphView;
            this.window = window;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) {
            var tree = new List<SearchTreeEntry> {
            new SearchTreeGroupEntry(new GUIContent("Create State")),
        };

            // Automatically find all scripts that inherit from StateNodeAsset
            var types = TypeCache.GetTypesDerivedFrom<StateNodeAsset>();
            foreach (var type in types) {
                tree.Add(new SearchTreeEntry(new GUIContent(type.Name)) { level = 1, userData = type });
            }

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context) {
            var mousePosition = window.rootVisualElement.ChangeCoordinatesTo(
                window.rootVisualElement.parent, context.screenMousePosition - window.position.position);
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(mousePosition);

            graphView.CreateNode((Type)entry.userData, graphMousePosition);
            return true;
        }
    }
}