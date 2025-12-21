using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Collections.Generic;

namespace Majinfwork.StateGraph {
    public class StateNodeVisual : Node {
        public StateNodeAsset data;
        public Port inputPort;
        public Dictionary<string, Port> transitionPorts = new Dictionary<string, Port>();

        public StateNodeVisual(StateNodeAsset nodeData) {
            data = nodeData;
            viewDataKey = nodeData.guid;
            title = nodeData.name;
            SetPosition(new Rect(data.position, new Vector2(200, 150)));

            // 1. Enter Port
            inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            inputPort.portName = "Enter";
            inputContainer.Add(inputPort);

            // 2. Exit Ports & Fields
            SerializedObject so = new SerializedObject(data);
            SerializedProperty prop = so.GetIterator();
            prop.NextVisible(true);

            while (prop.NextVisible(false)) {
                if (prop.type == nameof(StateTransition)) {
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    port.portName = prop.displayName;
                    port.viewDataKey = prop.name; // Link UI Port to C# Field Name
                    outputContainer.Add(port);
                    transitionPorts.Add(prop.name, port);
                }
                else {
                    var field = new PropertyField(prop);
                    field.Bind(so);
                    extensionContainer.Add(field);
                }
            }

            RefreshExpandedState();
            RefreshPorts();
            SetPosition(new Rect(nodeData.position, new Vector2(200, 150)));
        }

        public void MarkAsEntry(bool isEntry) {
            style.backgroundColor = isEntry ? new Color(0.15f, 0.35f, 0.15f) : new Color(0.22f, 0.22f, 0.22f);
        }
    }
}