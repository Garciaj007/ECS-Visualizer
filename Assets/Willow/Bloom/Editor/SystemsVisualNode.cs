using Sirenix.OdinInspector;
using System;
using UnityEngine;

namespace Willow.Bloom
{
    [Serializable]
    public abstract class Node
    {
        [ReadOnly][LabelWidth(100)] public string guid;
        [ReadOnly][LabelWidth(100)] public Vector2 position;

        [HideLabel]
        [Title("Type", horizontalLine: false)]
        [InlineButton("@this.isManaged = true", Label = "Managed", Icon = SdfIconType.CheckSquare, ShowIf = "@!this.isManaged")]
        [InlineButton("@this.isManaged = false", Label = "Managed", Icon = SdfIconType.CheckSquareFill, ShowIf = "@this.isManaged")]
        public Type type;

        [Multiline(10)]
        [HideLabel]
        [Title("Description", horizontalLine: false)]
        [PropertySpace(SpaceBefore = -10)]
        [PropertyOrder(int.MaxValue)]
        public string description;

        [HideInInspector]
        public bool isManaged;
    }

    [Serializable]
    public class SystemNode : Node { }

    [Serializable]
    public class ComponentNode : Node { }

    [Serializable]
    public class Connection : IEquatable<Connection>
    {
        [ReadOnly] public string aGuid;
        [ReadOnly] public string bGuid;

        public Connection(string aGuid, string bGuid)
        {
            this.aGuid = aGuid;
            this.bGuid = bGuid;
        }

        public bool Equals(Connection other)
        {
            return aGuid == other.aGuid && bGuid == other.bGuid;
        }
    }
}
