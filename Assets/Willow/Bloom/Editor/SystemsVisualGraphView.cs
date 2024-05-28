using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace Willow.Bloom
{
    public class SystemsVisualGraphView : GraphView
    {
        private static readonly string VISUAL_TREE_UCSS_ASSET_PATH = "Assets/Willow/Bloom/Editor/SystemsVisualGraph.uss";

        public new class UxmlFactory : UxmlFactory<SystemsVisualGraphView, GraphView.UxmlTraits> { }

        public Action<SystemsVisualGraphNodeView> OnNodeSelected;

        private SystemsVisualAsset asset;

        public SystemsVisualGraphView()
        {
            Insert(0, new GridBackground());

            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(VISUAL_TREE_UCSS_ASSET_PATH);
            styleSheets.Add(styleSheet);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent e)
        {
            var types = TypeCache.GetTypesDerivedFrom<Node>();
            foreach (var type in types)
            {
                e.menu.AppendAction(type.Name, (a) => CreateNode(type));
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
            {
                var node = endPort.node as SystemsVisualGraphNodeView;

                return endPort.direction != startPort.direction && endPort.node != startPort.node;
            }).ToList();
        }

        internal void PopulateView(SystemsVisualAsset asset /*TODO: Pass System Data*/)
        {
            this.asset = asset;

            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements);
            graphViewChanged += OnGraphViewChanged;

            // TODO: We want to populate the graph ourselves
            foreach (var node in asset.nodes)
            {
                CreateNodeView(node);
            }

            foreach (var connection in asset.connections)
            {
                CreateConnection(connection);
            }
        }

        private SystemsVisualGraphNodeView FindNodeView(string guid)
        {
            return GetNodeByGuid(guid) as SystemsVisualGraphNodeView;
        }

        private void CreateNode(Type type)
        {
            if (asset == null) return;

            Node node = asset.CreateNode(type);
            CreateNodeView(node);
        }

        private void CreateConnection(Connection connection)
        {
            var a = FindNodeView(connection.aGuid);
            var b = FindNodeView(connection.bGuid);

            if (a == null || b == null) return;

            Edge edge = a.input.ConnectTo(b.output);

            AddElement(edge);
        }

        private void CreateNodeView(Node node)
        {
            SystemsVisualGraphNodeView nodeView = new(node);
            nodeView.OnNodeSelected = OnNodeSelected;
            AddElement(nodeView);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.elementsToRemove != null)
            {
                foreach (var elem in graphViewChange.elementsToRemove)
                {
                    SystemsVisualGraphNodeView nodeView = elem as SystemsVisualGraphNodeView;
                    if (nodeView != null)
                        asset.DeleteNode(nodeView.node);

                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        SystemsVisualGraphNodeView aView = edge.input.node as SystemsVisualGraphNodeView;
                        SystemsVisualGraphNodeView bView = edge.output.node as SystemsVisualGraphNodeView;
                        asset.RemoveConnection(aView.node, bView.node);
                    }
                }
            }

            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    SystemsVisualGraphNodeView aView = edge.input.node as SystemsVisualGraphNodeView;
                    SystemsVisualGraphNodeView bView = edge.output.node as SystemsVisualGraphNodeView;
                    asset.AddConnection(aView.node, bView.node);
                }
            }

            return graphViewChange;
        }
    }
}
