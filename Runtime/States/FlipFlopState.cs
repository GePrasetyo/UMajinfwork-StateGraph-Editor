namespace Majinfwork.StateGraph {
    public sealed class FlipFlopState : StateNodeAsset {
        public StateTransition ExitA;
        public StateTransition ExitB;

        private sealed class Data {
            public int index;
        }

        public override void Begin(StateContext ctx) {
            var data = ctx.GetData<Data>(this);
            data.index++;

            TriggerExit(data.index % 2 == 0 ? ExitB : ExitA);
        }
    }
}
