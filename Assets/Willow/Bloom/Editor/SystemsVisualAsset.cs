using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Willow.Bloom
{
    [CreateAssetMenu]
    public class SystemsVisualAsset : ScriptableObject
    {
        public new string name;
        [SerializeReference] public List<Node> nodes = new();
        public List<Connection> connections = new();

        public Node CreateNode(System.Type nodeType)
        {
            var node = System.Activator.CreateInstance(nodeType) as Node;
            node.guid = GUID.Generate().ToString();
            nodes.Add(node);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);

            return node;
        }

        public void DeleteNode(Node node)
        {
            nodes.Remove(node);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void AddConnection(Node a, Node b)
        {
            var connection = new Connection(a.guid, b.guid);
            if (!connections.Contains(connection))
                connections.Add(connection);
        }

        public void RemoveConnection(Node a, Node b)
        {
            var connection = new Connection(a.guid, b.guid);
            connections.Remove(connection);
        }
    }
}