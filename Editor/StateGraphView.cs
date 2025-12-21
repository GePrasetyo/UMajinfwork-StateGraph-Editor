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
        private StateGraphAsset _asset;
        private NodeSearchWindow _searchWindow;

        public StateGraphView(EditorWindow window) {
            style.flexGrow = 1;
            Insert(0, new GridBackground());
            
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("packages://com.majingari.stategraph/Editor/MajinNodeStyle.uss");

            if (styleSheet == null) {
                // This searches for the file by name anywhere in the project/package
                string[] guids = AssetDatabase.FindAssets("MajinNodeStyle t:StyleSheet");
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

            _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            _searchWindow.Init(this, window);
            nodeCreationRequest = ctx => SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);

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
            _asset.entryNodeGuid = node.data.guid;
            RefreshNodeVisuals();
        }

        public void Populate(StateGraphAsset asset) {
            _asset = asset;
            _asset.allStates.RemoveAll(s => s == null);
            graphElements.ForEach(RemoveElement);

            Dictionary<string, StateNodeVisual> visualNodes = new Dictionary<string, StateNodeVisual>();

            // 1. Create all Visual Nodes
            foreach (var data in _asset.allStates) {
                if (data == null) continue;
                var visualNode = new StateNodeVisual(data);
                visualNodes.Add(data.guid, visualNode);
                AddElement(visualNode);
            }

            // 2. Create Edges by reflecting over StateTransition fields
            foreach (var data in _asset.allStates) {
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
            FrameAll();
        }

        private void RefreshNodeVisuals() {
            graphElements.ForEach(e => {
                if (e is StateNodeVisual v) v.MarkAsEntry(v.data.guid == _asset.entryNodeGuid);
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
                        if (_asset != null) {
                            EditorUtility.SetDirty(_asset);
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
                        if (_asset != null) EditorUtility.SetDirty(_asset); // Dirty main asset
                    }
                }
            }

            // 3. Handle Deletion
            if (change.elementsToRemove != null) {
                foreach (var element in change.elementsToRemove) {
                    if (element is StateNodeVisual visual) {
                        StateNodeAsset dataToRemove = visual.data;

                        // 1. Clean up references in other nodes
                        foreach (var state in _asset.allStates) {
                            if (state == null || state == dataToRemove) continue;
                            ClearReferencesTo(state, dataToRemove.guid);
                        }

                        // 2. Remove from the list
                        _asset.allStates.Remove(dataToRemove);

                        // 3. EXPLICITLY remove from the Asset Container
                        // This is the step that stops them from appearing under the Graph Asset
                        AssetDatabase.RemoveObjectFromAsset(dataToRemove);

                        // 4. Destroy and register Undo
                        Undo.DestroyObjectImmediate(dataToRemove);
                    }
                }

                // 5. CRITICAL: Force Unity to rebuild the asset hierarchy view
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();

                // This line forces the Project window to refresh the sub-asset list
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(_asset));
            }

            // Force a save to disk so it persists across sessions
            AssetDatabase.SaveAssets();
            return change;
        }

        public void CreateNode(Type type, Vector2 pos) {
            var newNode = ScriptableObject.CreateInstance(type) as StateNodeAsset;
            newNode.name = type.Name;
            newNode.guid = Guid.NewGuid().ToString();
            newNode.position = pos;

            AssetDatabase.AddObjectToAsset(newNode, _asset);
            _asset.allStates.Add(newNode);

            if (string.IsNullOrEmpty(_asset.entryNodeGuid)) _asset.entryNodeGuid = newNode.guid;

            Populate(_asset); // Refresh the whole graph to ensure ports align
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
                    AssetDatabase.AddObjectToAsset(newData, _asset);
                    _asset.allStates.Add(newData);
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

        public void PurgeOrphanedNodes() {
            if (_asset == null) return;

            // Get the path to the main asset
            string path = AssetDatabase.GetAssetPath(_asset);

            // Load ALL sub-assets of this file
            UnityEngine.Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);

            bool changed = false;
            foreach (var sub in subAssets) {
                // If the sub-asset is a Node but NOT in our allStates list, it's an orphan
                if (sub is StateNodeAsset node && !_asset.allStates.Contains(node)) {
                    AssetDatabase.RemoveObjectFromAsset(node);
                    UnityEngine.Object.DestroyImmediate(node, true);
                    changed = true;
                }
            }

            if (changed) {
                EditorUtility.SetDirty(_asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(path);
                Debug.Log("Purged orphaned state nodes from asset.");
            }
        }
    }
}