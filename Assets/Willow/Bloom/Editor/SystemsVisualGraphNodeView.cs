using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Willow.Bloom
{
    public class SystemsVisualGraphNodeView : UnityEditor.Experimental.GraphView.Node
    {
        public Action<SystemsVisualGraphNodeView> OnNodeSelected;
        public Node node;
        public Port input;
        public Port output;

        Color componentBG;
        Color componentFG;
        Color systemBG;
        Color systemFG;

        public SystemsVisualGraphNodeView(Node node)
        {
            this.node = node;
            title = node.GetType().Name.Replace("Node", "");
            viewDataKey = node.guid;

            capabilities |= Capabilities.Resizable;

            style.left = node.position.x;
            style.top = node.position.y;

            ColorUtility.TryParseHtmlString("#001524AA", out componentBG);
            ColorUtility.TryParseHtmlString("#15616D", out componentFG);
            ColorUtility.TryParseHtmlString("#FFECD1AA", out systemBG);
            ColorUtility.TryParseHtmlString("#FF7D00", out systemFG);

            style.color = node is SystemNode ? systemFG : componentFG;

            CreateInputPorts();
            CreateOutputPorts();
        }

        private void CreateInputPorts()
        {
            input = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));

            if (input != null)
            {
                input.portName = "";
                input.portColor = node is SystemNode ? systemFG : componentFG;
                inputContainer.Add(input);
            }
        }

        private void CreateOutputPorts()
        {
            output = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));

            if (output != null)
            {
                output.portName = "";
                output.portColor = node is SystemNode ? systemFG : componentFG;
                outputContainer.Add(output);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            node.position = newPos.position;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            OnNodeSelected?.Invoke(this);
        }
    }

}