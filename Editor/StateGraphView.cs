using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Majinfwork.StateGraph {
    public class StateGraphView : GraphView {
        private StateGraphAsset asset;
        private NodeSearchWindow searchWindow;

        private string lastActiveGuid;

        public StateGraphView(EditorWindow window) {
            style.flexGrow = 1;
            Insert(0, new GridBackground());
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.majingari.stategraph/Editor/MNodeStyle.uss");

            if (styleSheet == null) {
                // This searches for the file by name anywhere in the project/package
                string[] guids = AssetDatabase.FindAssets("MNodeStyle t:StyleSheet");
                if (guids.Length > 0) {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
            }

            if (styleSheet != null) {
                styleSheets.Add(styleSheet);
            }

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            searchWindow.Init(this, window);
            nodeCreationRequest = ctx => SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), searchWindow);

            serializeGraphElements = MySerializeMethod;
            canPasteSerializedData = MyCanPasteMethod;
            unserializeAndPaste = MyPasteMethod;

            graphViewChanged = OnGraphChanged;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);
            if (evt.target is StateNodeVisual node) {
                evt.menu.AppendAction("Set as Entry Node", (a) => SetEntryNode(node));
            }

            if (evt.target is StateGraphView || evt.target is GridBackground) {
                evt.menu.AppendAction("Purge Trash Node", (a) => PurgeOrphanedNodes());
            }
        }

        private void SetEntryNode(StateNodeVisual node) {
            asset.entryNodeGuid = node.data.guid;
            RefreshNodeVisuals();
        }

        public void Populate(StateGraphAsset asset) {
            this.asset = asset;
            this.asset.allStates.RemoveAll(s => s == null);
            graphElements.ForEach(RemoveElement);

            Dictionary<string, StateNodeVisual> visualNodes = new Dictionary<string, StateNodeVisual>();

            // 1. Create all Visual Nodes
            foreach (var data in this.asset.allStates) {
                if (data == null) continue;
                var visualNode = new StateNodeVisual(data);
                visualNodes.Add(data.guid, visualNode);
                AddElement(visualNode);
            }

            // 2. Create Edges by reflecting over StateTransition fields
            foreach (var data in this.asset.allStates) {
                var sourceVisual = visualNodes[data.guid];

                // Look for fields of type StateTransition
                var fields = data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields) {
                    if (field.FieldType == typeof(StateTransition)) {
                        var transition = field.GetValue(data) as StateTransition;
                        if (transition != null && !string.IsNullOrEmpty(transition.targetNodeGuid)) {
                            if (visualNodes.TryGetValue(transition.targetNodeGuid, out var targetVisual)) {
                                // Find the port that matches this field name
                                var port = sourceVisual.transitionPorts[field.Name];
                                var edge = port.ConnectTo(targetVisual.inputPort);
                                AddElement(edge);
                            }
                        }
                    }
                }
            }

            RefreshNodeVisuals();
        }

        private void RefreshNodeVisuals() {
            graphElements.ForEach(e => {
                if (e is StateNodeVisual v) v.MarkAsEntry(v.data.guid == asset.entryNodeGuid);
            });
        }

        private GraphViewChange OnGraphChanged(GraphViewChange change) {
            // 1. Handle Node Movement
            if (change.movedElements != null) {
                foreach (var element in change.movedElements) {
                    if (element is StateNodeVisual node) {
                        // Update the ScriptableObject data with the new position
                        node.data.position = node.GetPosition().position;

                        // CRITICAL: Mark the node and the main asset as dirty
                        EditorUtility.SetDirty(node.data);
                        if (asset != null) {
                            EditorUtility.SetDirty(asset);
                        }
                    }
                }
            }

            // 2. Handle Edge Creation
            if (change.edgesToCreate != null) {
                foreach (var edge in change.edgesToCreate) {
                    var source = edge.output.node as StateNodeVisual;
                    var target = edge.input.node as StateNodeVisual;
                    string fieldName = edge.output.viewDataKey;

                    var field = source.data.GetType().GetField(fieldName);
                    if (field != null) {
                        var trans = field.GetValue(source.data) as StateTransition ?? new StateTransition();
                        trans.targetNodeGuid = target.data.guid;
                        trans.targetState = target.data;

                        field.SetValue(source.data, trans);
                        EditorUtility.SetDirty(source.data);
                        if (asset != null) EditorUtility.SetDirty(asset); // Dirty main asset
                    }
                }
            }

            // 3. Handle Deletion
            if (change.elementsToRemove != null) {
                Undo.RecordObject(asset, "Delete Nodes");

                foreach (var element in change.elementsToRemove) {
                    if (element is StateNodeVisual visual) {
                        StateNodeAsset dataToRemove = visual.data;

                        // 1. Clean up references in other nodes
                        foreach (var state in asset.allStates) {
                            if (state == null || state == dataToRemove) continue;
                            Undo.RecordObject(state, "Clean References");
                            ClearReferencesTo(state, dataToRemove.guid);
                        }

                        // 2. Remove from the list
                        asset.allStates.Remove(dataToRemove);
                        Undo.DestroyObjectImmediate(dataToRemove);
                    }
                }

                //Set dirty changes
                EditorUtility.SetDirty(asset);
            }

            return change;
        }

        public void CreateNode(Type type, Vector2 pos) {
            var newNode = ScriptableObject.CreateInstance(type) as StateNodeAsset;
            newNode.name = type.Name;
            newNode.guid = Guid.NewGuid().ToString();
            newNode.position = pos;

            AssetDatabase.AddObjectToAsset(newNode, asset);
            asset.allStates.Add(newNode);

            if (string.IsNullOrEmpty(asset.entryNodeGuid)) asset.entryNodeGuid = newNode.guid;

            Populate(asset);
        }

        public void ReFrameView() {
            schedule.Execute(() => {
                FrameAll();
            }).ExecuteLater(50);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            return ports.ToList().Where(p => p.direction != startPort.direction && p.node != startPort.node).ToList();
        }

        // 1. Pack the GUIDs of selected nodes into a string
        private string MySerializeMethod(IEnumerable<GraphElement> elements) {
            var nodes = elements.OfType<StateNodeVisual>().Select(v => v.data.guid);
            if (!nodes.Any()) return string.Empty;
            return string.Join(",", nodes);
        }

        // 2. Check if the clipboard contains GUIDs
        private bool MyCanPasteMethod(string data) {
            return !string.IsNullOrEmpty(data);
        }

        // 3. Create NEW ScriptableObjects for the pasted nodes
        private void MyPasteMethod(string operationName, string data) {
            string[] guids = data.Split(',');

            foreach (var guid in guids) {
                // Find the original visual node to get its data
                var original = graphElements.OfType<StateNodeVisual>()
                    .FirstOrDefault(v => v.data.guid == guid);

                if (original != null) {
                    // Create a NEW instance of the ScriptableObject
                    var newData = UnityEngine.Object.Instantiate(original.data);
                    newData.name = original.data.name;
                    newData.guid = Guid.NewGuid().ToString(); // New ID is critical
                    newData.position = original.data.position + new Vector2(40, 40);

                    // Clear transitions in the duplicate
                    ClearTransitionData(newData);

                    // Add to the asset container
                    AssetDatabase.AddObjectToAsset(newData, asset);
                    asset.allStates.Add(newData);
                    Undo.RegisterCreatedObjectUndo(newData, "Duplicate Node");

                    // Create and add the new visual node
                    var newVisual = new StateNodeVisual(newData);
                    AddElement(newVisual);
                    newVisual.Select(this, true);
                }
            }
        }

        private void ClearTransitionData(StateNodeAsset nodeData) {
            var fields = nodeData.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields) {
                if (field.FieldType == typeof(StateTransition)) {
                    field.SetValue(nodeData, new StateTransition());
                }
            }
        }

        private void ClearReferencesTo(StateNodeAsset state, string targetGuid) {
            bool changed = false;
            var fields = state.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields) {
                if (field.FieldType == typeof(StateTransition)) {
                    var trans = field.GetValue(state) as StateTransition;
                    if (trans != null && trans.targetNodeGuid == targetGuid) {
                        // Null out the references
                        trans.targetNodeGuid = string.Empty;
                        trans.targetState = null;
                        changed = true;
                    }
                }
            }

            if (changed) {
                EditorUtility.SetDirty(state);
            }
        }

        // Remove all unused Nodes
        public void PurgeOrphanedNodes() {
            if (asset == null) return;

            // Load ALL sub-assets of this file
            string path = AssetDatabase.GetAssetPath(asset);
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);

            bool changed = false;
            foreach (var sub in subAssets) {
                // If the sub-asset is a Node but NOT in our allStates list, it's an orphan
                if (sub is StateNodeAsset node && !asset.allStates.Contains(node)) {
                    AssetDatabase.RemoveObjectFromAsset(node);
                    UnityEngine.Object.DestroyImmediate(node, true);
                    changed = true;
                }
            }

            if (changed) {
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path);
            }
        }

        // Highlight Active State To Debug
        public void HighlightActiveState(string activeGuid) {
            if (lastActiveGuid == activeGuid) return;
            lastActiveGuid = activeGuid;

            graphElements.ForEach(element => {
                if (element is StateNodeVisual visual) {
                    if (visual.data.guid == activeGuid) {
                        visual.AddToClassList("node-running");
                    }
                    else {
                        visual.RemoveFromClassList("node-running");
                    }
                }
            });
        }
    }
}