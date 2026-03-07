using UnityEditor;
using UnityEngine;

namespace Majinfwork.StateGraph {

    // [CreateAssetMenu(menuName = "MFramework/State Machine/Edge Settings")]
    public class StateEdgeSettings : ScriptableObject {
        [Header("Forward Connection (Bezier)")]
        [Tooltip("Minimum tangent length for forward curves")]
        public float forwardTangentMin = 30f;
        [Tooltip("Maximum tangent length for forward curves")]
        public float forwardTangentMax = 200f;
        [Tooltip("Tangent as fraction of distance (e.g. 0.33 = dist/3)")]
        [Range(0.1f, 1f)]
        public float forwardTangentRatio = 0.33f;

        [Header("Backward Connection (Orthogonal Route)")]
        [Tooltip("Straight segment length from port before first corner")]
        public float margin = 40f;
        [Tooltip("Rounded corner radius")]
        public float cornerRadius = 20f;
        [Tooltip("Points per corner arc (higher = smoother)")]
        [Range(2, 16)]
        public int cornerSegments = 8;
        [Tooltip("Minimum vertical offset when nodes are at similar Y levels")]
        public float minVerticalOffset = 80f;
        [Tooltip("Margin scales with horizontal distance (0 = fixed margin)")]
        [Range(0f, 0.5f)]
        public float marginDistanceScale = 0.15f;

        // Singleton accessor — finds or creates the asset
        private static StateEdgeSettings s_Instance;

        public static StateEdgeSettings Instance {
            get {
                if (s_Instance != null) return s_Instance;

                // Search project for existing asset
                string[] guids = AssetDatabase.FindAssets("t:StateEdgeSettings");
                if (guids.Length > 0) {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    s_Instance = AssetDatabase.LoadAssetAtPath<StateEdgeSettings>(path);
                }

                // Fallback: create runtime instance with defaults
                if (s_Instance == null) {
                    s_Instance = CreateInstance<StateEdgeSettings>();
                }

                return s_Instance;
            }
        }

        [MenuItem("Assets/Create/MFramework/State Machine/Edge Settings")]
        private static void CreateAsset() {
            var asset = CreateInstance<StateEdgeSettings>();
            string path = "Assets/StateEdgeSettings.asset";

            // If selection is a folder, create there
            var selected = Selection.activeObject;
            if (selected != null) {
                string selectedPath = AssetDatabase.GetAssetPath(selected);
                if (AssetDatabase.IsValidFolder(selectedPath)) {
                    path = selectedPath + "/StateEdgeSettings.asset";
                }
            }

            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(path));
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
        }
    }
}
