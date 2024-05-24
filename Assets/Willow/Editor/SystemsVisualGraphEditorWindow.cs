using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Linq;
using Sirenix.Utilities;
using UnityEngine;
using System;

namespace Willow.Bloom
{
    public class SystemsVisualGraphEditorWindow : OdinEditorWindow
    {
        [System.Serializable]
        public class QueryView
        {
            [System.NonSerialized] public List<string> all = new();
            [System.NonSerialized] public List<string> any = new();
            [System.NonSerialized] public List<string> absent = new();
            [System.NonSerialized] public List<string> disabled = new();
            [System.NonSerialized] public List<string> none = new();

            [Multiline, ReadOnly, HideLabel] public string description;

            private string GetQueryDesc()
            {
                string desc = "";

                if (all.Count > 0)
                    desc += $"All: {string.Join(", ", all)}\n";

                if (any.Count > 0)
                    desc += $"Any: {string.Join(", ", any)}\n";

                if (absent.Count > 0)
                    desc += $"Absent: {string.Join(", ", absent)}\n";

                if (disabled.Count > 0)
                    desc += $"Disabled: {string.Join(", ", disabled)}\n";

                if (none.Count > 0)
                    desc += $"None: {string.Join(", ", none)}\n";

                return desc;
            }

            public void GenerateStringDesc() => description = GetQueryDesc();
        }

        [System.Serializable]
        public class SystemView
        {
            public string name;
            public bool isEnabled;
            public List<QueryView> queryViews;
        }

        [MenuItem("Willow/Open Bloom Graph")]
        static void Open()
        {
            GetWindow<SystemsVisualGraphEditorWindow>("Bloom").Show();
        }

        string prevWorldName;

        [ValueDropdown("GetAllWorlds")]
        public string currentWorld;

        [ValueDropdown("GetAllPossibleNamespaces")]
        public string namespaceFilter = "All";

        [InlineEditor]
        public List<SystemView> currentSystems = new();

        private bool HasCurrentWorldChanged
            => string.IsNullOrEmpty(prevWorldName) && prevWorldName != currentWorld;

        private IEnumerable GetAllWorlds()
        {
            foreach (var world in World.All)
                yield return new ValueDropdownItem(world.Name, world.Name);
        }

        private static IEnumerable<string> GetAllPossibleNamespaces()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(ComponentSystemBase)))
                .Select(type => type.Namespace)
                .Distinct()
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Append("All");
        }

        private void OnValidate()
        {
            if (HasCurrentWorldChanged)
                InitializeWorld();
        }

        [Button(SdfIconType.ArrowClockwise, DirtyOnClick = false), PropertyOrder(-1)]
        private void InitializeWorld()
        {
            currentSystems.Clear();
            prevWorldName = currentWorld;

            foreach (var world in World.All)
            {
                if (world.Name != currentWorld) continue;

                foreach (var system in world.Systems)
                {
                    if(namespaceFilter != "All" && !system.ToString().Contains(namespaceFilter)) continue;

                    var entityQueries = system.EntityQueries;

                    List<QueryView> queryViews = new();

                    foreach (var entityQuery in entityQueries)
                    {
                        var entityQueryDesc = entityQuery.GetEntityQueryDesc();

                        var queryView = new QueryView()
                        {
                            all = entityQueryDesc.All.Select(ct => ct.ToString()).ToList(),
                            any = entityQueryDesc.Any.Select(ct => ct.ToString()).ToList(),
                            absent = entityQueryDesc.Absent.Select(ct => ct.ToString()).ToList(),
                            disabled = entityQueryDesc.Disabled.Select(ct => ct.ToString()).ToList(),
                            none = entityQueryDesc.None.Select(ct => ct.ToString()).ToList()
                        };
                        queryView.GenerateStringDesc();
                        queryViews.Add(queryView);
                    }

                    currentSystems.Add(new SystemView()
                    {
                        name = system.ToString(),
                        isEnabled = system.Enabled,
                        queryViews = queryViews
                    });

                }
            }
        }
    }
}