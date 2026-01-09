using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Majinfwork.StateGraph {
    [CreateAssetMenu(fileName = "NewStateGraph", menuName = "MFramework/State Machine/Graph Asset")]
    public class StateGraphAsset : ScriptableObject {
        public string entryNodeGuid;
        public List<StateNodeAsset> allStates = new List<StateNodeAsset>();
        public bool SharedAsset;

#if HAS_MAJINFWORK_CROSSREF
        // String constants for reflection-based cross-scene reference support
        private const string CrossSceneExtensionTypeName = "Majinfwork.CrossRef.CrossSceneCloneExtension";
        private const string CrossSceneAssemblyName = "Majinfwork";
        private const string ResolveCrossSceneMethodName = "ResolveCrossSceneReferences";

        // Cached reflection
        private static MethodInfo _resolveCrossSceneMethod;
        private static bool _crossSceneMethodSearched;

        private static void TryResolveCrossSceneReferences(Dictionary<StateNodeAsset, StateNodeAsset> cloneMap) {
            if (!_crossSceneMethodSearched) {
                _crossSceneMethodSearched = true;
                var extensionType = Type.GetType($"{CrossSceneExtensionTypeName}, {CrossSceneAssemblyName}");

                if (extensionType != null) {
                    // Find the generic method ResolveCrossSceneReferences<T>(Dictionary<T,T>)
                    var methods = extensionType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (var method in methods) {
                        if (method.Name == ResolveCrossSceneMethodName && method.IsGenericMethod) {
                            var parameters = method.GetParameters();
                            if (parameters.Length == 1 && parameters[0].ParameterType.IsGenericType &&
                                parameters[0].ParameterType.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
                                _resolveCrossSceneMethod = method.MakeGenericMethod(typeof(StateNodeAsset));
                                break;
                            }
                        }
                    }
                }
            }

            _resolveCrossSceneMethod?.Invoke(null, new object[] { cloneMap });
        }
#endif

        public StateGraphAsset Clone() {
            StateGraphAsset newGraph = Instantiate(this);
            newGraph.allStates = new List<StateNodeAsset>();

            var cloneMap = new Dictionary<StateNodeAsset, StateNodeAsset>();

            foreach (var original in allStates) {
                var clone = Instantiate(original);
                newGraph.allStates.Add(clone);
                cloneMap.Add(original, clone);
            }

            // Remap transitions (cached reflection)
            cloneMap.RemapTransitions();

#if HAS_MAJINFWORK_CROSSREF
            // Resolve cross-scene references if Majinfwork is present
            TryResolveCrossSceneReferences(cloneMap);
#endif

            return newGraph;
        }

        /// <summary>
        /// Exposes the clone map for external extensions to process.
        /// Use when you need to apply additional cloning logic (e.g., CrossSceneReferences).
        /// </summary>
        public StateGraphAsset CloneWithMap(out Dictionary<StateNodeAsset, StateNodeAsset> cloneMap) {
            StateGraphAsset newGraph = Instantiate(this);
            newGraph.allStates = new List<StateNodeAsset>();

            cloneMap = new Dictionary<StateNodeAsset, StateNodeAsset>();

            foreach (var original in allStates) {
                var clone = Instantiate(original);
                newGraph.allStates.Add(clone);
                cloneMap.Add(original, clone);
            }

            cloneMap.RemapTransitions();

#if HAS_MAJINFWORK_CROSSREF
            TryResolveCrossSceneReferences(cloneMap);
#endif

            return newGraph;
        }
    }
}