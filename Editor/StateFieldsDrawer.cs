using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Majinfwork.StateGraph {
    /// <summary>
    /// Draws a node's state fields on a live copy of the state, through the state's own serialization,
    /// so every property drawer and attribute on its fields works. Edits go back to the node as JSON.
    /// </summary>
    [CustomPropertyDrawer(typeof(StateFields), true)]
    public sealed class StateFieldsDrawer : PropertyDrawer {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) =>
            new StateFieldsView((StateFields)property.boxedValue, property.FindPropertyRelative(nameof(StateFields.json)));

        private sealed class StateFieldsView : VisualElement {
            private readonly StateFields fields;
            private readonly SerializedProperty json;
            private StateNodeAsset state;
            private Editor editor;

            public StateFieldsView(StateFields fields, SerializedProperty json) {
                this.fields = fields;
                this.json = json;
                RegisterCallback<AttachToPanelEvent>(_ => Show());
                RegisterCallback<DetachFromPanelEvent>(_ => Hide());

                // An element tracks one SerializedObject: the node's value here, the state copy on the inspector.
                this.TrackPropertyValue(json, Load);
            }

            private void Show() {
                state = fields.CreateState();
                state.hideFlags = HideFlags.DontSave;
                fields.WriteTo(state);

                editor = Editor.CreateEditor(state, typeof(StateFieldsEditor));
                var inspector = new InspectorElement(editor);
                inspector.TrackSerializedObjectValue(editor.serializedObject, _ => Save());
                Add(inspector);
            }

            private void Hide() {
                Clear();
                if (editor != null) Object.DestroyImmediate(editor);
                if (state != null) Object.DestroyImmediate(state);
            }

            // An edit on the copy becomes the node's value.
            private void Save() {
                string value = EditorJsonUtility.ToJson(state);
                if (json.stringValue == value) return;

                json.stringValue = value;
                json.serializedObject.ApplyModifiedProperties();
            }

            // Undo, or another view of the node, changed the value.
            private void Load(SerializedProperty changed) {
                if (state == null || string.IsNullOrEmpty(changed.stringValue) || changed.stringValue == EditorJsonUtility.ToJson(state)) return;

                EditorJsonUtility.FromJsonOverwrite(changed.stringValue, state);
                editor.serializedObject.Update();
            }
        }
    }

    /// <summary>
    /// A state's fields without its exits, which are the node's ports. An exit list shows only its size: how many
    /// ports it has.
    /// </summary>
    public sealed class StateFieldsEditor : Editor {
        public override VisualElement CreateInspectorGUI() {
            var root = new VisualElement();
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false)) {
                if (ExitProperty.IsExit(property)) continue;

                if (ExitProperty.IsExitList(property)) {
                    root.Add(new PropertyField(property.FindPropertyRelative("Array.size"), property.displayName));
                    continue;
                }

                root.Add(new PropertyField(property.Copy()));
            }

            return root;
        }
    }
}
