using UnityEditor;
using UnityEditor.Callbacks;

namespace Majinfwork.StateGraph {
    public class StateGraphEditor : EditorWindow {
        private StateGraphView _graphView;

        [OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line) {
            if (EditorUtility.EntityIdToObject(instanceID) is StateGraphAsset asset) {
                var window = GetWindow<StateGraphEditor>("State Machine");
                window._graphView.Populate(asset);
                return true;
            }
            return false;
        }

        private void OnEnable() {
            _graphView = new StateGraphView(this);
            rootVisualElement.Add(_graphView);
        }
    }
}