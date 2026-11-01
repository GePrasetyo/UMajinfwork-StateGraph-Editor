using System;

namespace Majinfwork.StateGraph {
    [Serializable] public class WaitStateNode : StateNode<WaitState> { }
    [Serializable] public class LogStateNode : StateNode<LogState> { }
    [Serializable] public class FlipFlopStateNode : StateNode<FlipFlopState> { }
}
