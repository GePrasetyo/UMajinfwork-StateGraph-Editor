using System;
using Unity.GraphToolkit.Editor;

namespace Majinfwork.StateGraph {
    /// <summary>Where the graph starts, and whether one asset is shared by every runner.</summary>
    [Serializable]
    public class StateGraphEntryNode : Node {
        public const string StartPort = "Start";
        public const string SharedOption = "Shared";

        public override void OnEnable() => Title = "Entry";

        protected override void OnDefineOptions(IOptionDefinitionContext context) {
            context.AddOption<bool>(SharedOption)
                .WithDisplayName("Shared Asset")
                .WithTooltip("One graph asset drives every runner. States keep per-runner data in the StateContext.");
        }

        protected override void OnDefinePorts(IPortDefinitionContext context) {
            context.AddOutputPort(StartPort).WithCapacity(PortCapacity.Single).Build();
        }
    }
}
