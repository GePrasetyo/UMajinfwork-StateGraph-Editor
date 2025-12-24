using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Majinfwork.StateGraph {
    public class StateGraphEditor : EditorWindow {
        private StateGraphView graphView;
        private StateGraphAsset currentAsset; 

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line) {
            if (EditorUtility.EntityIdToObject(instanceID) is StateGraphAsset asset) {
                var window = GetWindow<StateGraphEditor>("State Machine");
                window.minSize = new Vector2(800, 600);
                if (window.position.width < 200 || window.position.height < 200) {
                    window.position = new Rect(100, 100, 1024, 768);
                }

                window.LoadAsset(asset);
                return true;
            }
            return false;
        }

        private void OnEnable() {
            if (graphView == null) {
                graphView = new StateGraphView(this);
                rootVisualElement.Add(graphView);
            }

            // Re-populate if we were already looking at an asset
            if (currentAsset != null) {
                LoadAsset(currentAsset);
            }

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable() {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void Update() {
            UpdateWindowTitle();

            // Check if the user is selecting a GameObject with a StateRunner
            GameObject selected = Selection.activeGameObject;
            if (selected != null) {
                var runner = selected.GetComponent<StateRunner>();

                if(runner == null) {
                    return;
                }

                FieldInfo field;
                if (Application.isPlaying) {
                    field = typeof(StateRunner).GetField("runtimeGraph", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                else {
                    field = typeof(StateRunner).GetField("graphTemplate", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                var graph = field.GetValue(runner) as StateGraphAsset;
                
                if(currentAsset != graph) {
                    LoadAsset(graph);
                }

                if (Application.isPlaying) {
                    if (runner != null && runner.CurrentState != null) {
                        graphView.HighlightActiveState(runner.CurrentState.guid);
                    }
                }
            }
        }

        private void LoadAsset(StateGraphAsset asset) {
            currentAsset = asset;
            if (graphView == null) return;

            graphView.Populate(asset);
            graphView.ReFrameView();
            UpdateWindowTitle();
        }

        private void OnSelectionChange() {
            if (Selection.activeGameObject == null) return;

            var runner = Selection.activeGameObject.GetComponent<StateRunner>();
            if (runner != null) {
                // Use reflection to get the private 'runtimeGraph' field
                var field = typeof(StateRunner).GetField("runtimeGraph",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field != null) {
                    var graph = field.GetValue(runner) as StateGraphAsset;
                    if (graph != null && graph != currentAsset) {
                        LoadAsset(graph);
                    }
                }
            }
        }

        private void UpdateWindowTitle() {
            if (currentAsset == null) {
                titleContent.text = "State Graph";
                return;
            }

            string assetName = currentAsset.name;

            // 1. Check if the asset is a file on disk (Persistent)
            bool isPersistent = EditorUtility.IsPersistent(currentAsset);
            bool isDirty = isPersistent && EditorUtility.IsDirty(currentAsset);

            string prefix = isPersistent ? "" : "[Running] ";
            string dirtyMarker = isDirty ? "*" : "";

            string newTitle = $"{prefix}{assetName}{dirtyMarker}";

            if (titleContent.text != newTitle) {
                titleContent.text = newTitle;
            }
        }

        private void OnUndoRedo() {
            if (currentAsset != null) {
                LoadAsset(currentAsset);
            }
        }
    }
}