using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Majinfwork.StateGraph {
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider {
        private StateGraphView _graphView;
        private EditorWindow _window;

        public void Init(StateGraphView graphView, EditorWindow window) {
            _graphView = graphView;
            _window = window;
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
            var mousePosition = _window.rootVisualElement.ChangeCoordinatesTo(
                _window.rootVisualElement.parent, context.screenMousePosition - _window.position.position);
            var graphMousePosition = _graphView.contentViewContainer.WorldToLocal(mousePosition);

            _graphView.CreateNode((Type)entry.userData, graphMousePosition);
            return true;
        }
    }
}