using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;

namespace Willow.Bloom
{
    public class SystemsVisualGraphEditorWindow : OdinEditorWindow
    {

        private static readonly string VISUAL_TREE_ASSET_PATH = "Assets/Willow/Bloom/Editor/SystemsVisualGraph.uxml";

        // #################################################################################

        [Serializable]
        public class QueryView
        {
            [NonSerialized] public List<string> all = new();
            [NonSerialized] public List<string> any = new();
            [NonSerialized] public List<string> absent = new();
            [NonSerialized] public List<string> disabled = new();
            [NonSerialized] public List<string> none = new();

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

        [Serializable]
        public class SystemView
        {
            public string name;
            public bool isEnabled;

            [HideLabel]
            [ListDrawerSettings(ShowFoldout = false, IsReadOnly = true)]
            public List<QueryView> queryViews;
        }

        // #################################################################################

        [MenuItem("Willow/Open Bloom Graph")]
        private static void Open()
        {
            GetWindow<SystemsVisualGraphEditorWindow>("Bloom").Show();
        }

        // #################################################################################

        private static IEnumerable GetAllWorlds()
        {
            foreach (var world in World.All)
                yield return new ValueDropdownItem(world.Name, world.Name);
        }

        private static IEnumerable<T> GetAllInstanceInNamespace<T>(string @namespace)
        {
            var instances = AssemblyHelper.GetInstances<T>();
            if (string.IsNullOrEmpty(@namespace) || @namespace == "All")
                return instances;
            return instances.Where(x => IsInNamespace<T>(@namespace));
        }

        private static bool IsInNamespace<T>(string @namespace = null) =>
            IsInNamespace(typeof(T), @namespace);

        private static bool IsInNamespace(Type t, string @namespace = null)
        {
            if (@namespace == "All") return true;
            return t.Namespace.Contains(@namespace);
        }

        // #################################################################################

        [ValueDropdown("GetAllWorlds")]
        [TabGroup("Systems", false, 1, Icon = SdfIconType.GearWide)]
        public string currentWorld;

        [ValueDropdown("GetAllPossibleNamespaces")]
        [TabGroup("Systems")]
        public string namespaceFilter = "All";

        //[TabGroup("Systems")]
        //public List<SystemView> currentSystems = new();

        [TabGroup("Systems")]
        public List<string> systems = new();

        [TabGroup("Node Editor", false, 0, Icon = SdfIconType.NodePlus)]
        public Node selectedNode;

        // #################################################################################

        private bool isInitialised;

        private string prevWorldName;

        private World selectedWorld;

        private SystemsVisualGraphView systemsVisualGraphView;
        private TreeView inspectorView;

        // #################################################################################

        private bool HasCurrentWorldChanged
            => string.IsNullOrEmpty(prevWorldName) && prevWorldName != currentWorld;

        protected internal World SelectedWorld => selectedWorld;

        // #################################################################################

        [PropertyOrder(-1)]
        [TabGroup("Systems")]
        [Button(SdfIconType.ArrowClockwise, DirtyOnClick = false, Name = "Reload")]
        private void InitializeWorld()
        {
            //currentSystems.Clear();
            systems.Clear();
            prevWorldName = currentWorld;

            foreach (var world in World.All)
            {
                if (world.Name != currentWorld) continue;

                selectedWorld = world;

                var unmanagedSystemHandles = world.Unmanaged.GetAllSystems(Allocator.Temp);

                // Scan through all unmanaged systems
                foreach (var systemHandle in unmanagedSystemHandles)
                {
                    var systemState = world.Unmanaged.ResolveSystemStateRef(systemHandle);
                    var systemName = systemState.DebugName.ToString();

                    if (namespaceFilter != "All" && !systemName.Contains(namespaceFilter)) continue;

                    systems.Add(systemName);
                }

                // Scan through all managed Systems
                foreach (var system in world.Systems)
                {
                    var systemName = system.ToString();

                    if (namespaceFilter != "All" && !systemName.Contains(namespaceFilter)) continue;

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

                    systems.Add(systemName);

                    //currentSystems.Add(new SystemView()
                    //{
                    //    name = systemName,
                    //    isEnabled = system.Enabled,
                    //    queryViews = queryViews
                    //});
                }
            }
        }

        protected void OnWorldSelected(World world)
        {
            if (world == null || !world.IsCreated)
                return;

            //TODO?
        }

        private IEnumerable<string> GetAllPossibleNamespaces()
        {
            //return AppDomain.CurrentDomain.GetAssemblies()
            //.SelectMany(assembly => assembly.GetTypes())
            //.Where(type => type.IsSubclassOf(typeof(SystemBase)) || type.IsSubclassOf(typeof(ISystem)))
            //.Select(type => type.Namespace)
            //.Distinct()
            //.Where(ns => !string.IsNullOrEmpty(ns))
            //.Prepend("All");

            return AssemblyHelper.GetInstances<SystemBase>()
                .Select(x => x.GetType().Namespace)
                .Concat(AssemblyHelper.GetInstances<ISystem>().Select(x => x.GetType().Namespace))
                .Distinct()
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Prepend("All");
        }

        private IEnumerable<SystemBase> GetAllManagedSystemsInNamespace()
        {
            return GetAllInstanceInNamespace<SystemBase>(namespaceFilter);
        }

        private IEnumerable<ISystem> GetAllUnmanagedSystemsInNamespace()
        {
            return GetAllInstanceInNamespace<ISystem>(namespaceFilter);
        }

        private IEnumerable<IComponentData> GetAllComponentsInNamespace()
        {
            return GetAllInstanceInNamespace<IComponentData>(namespaceFilter);
        }

        private IEnumerable<IComponentData> GetAllManagedComponentsInNamespace()
        {
            return GetAllComponentsInNamespace().Where(c => c.GetType().IsClass);
        }

        private IEnumerable<IComponentData> GetAllUnmanagedComponentsInNamespace()
        {
            return GetAllComponentsInNamespace().Where(c => !c.GetType().IsClass);
        }

        // #################################################################################

        private void OnValidate()
        {
            if (HasCurrentWorldChanged)
                InitializeWorld();
        }

        protected override void Initialize()
        {
            isInitialised = false;
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();

            if (isInitialised) return;

            isInitialised = true;

            var template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(VISUAL_TREE_ASSET_PATH).CloneTree();
            template.style.flexGrow = 1;
            rootVisualElement.Add(template);

            systemsVisualGraphView = rootVisualElement.Q<SystemsVisualGraphView>();
            inspectorView = rootVisualElement.Q<TreeView>("InspectorView");

            rootVisualElement.Q<Button>("FitAll").clicked += () => systemsVisualGraphView.FrameAll();

            var replaceContainer = rootVisualElement.Q<IMGUIContainer>("OdinInsert");
            var odinImGUIContainer = rootVisualElement.Q<IMGUIContainer>("Odin ImGUIContainer");

            rootVisualElement.Remove(odinImGUIContainer);
            replaceContainer.parent.Insert(0, odinImGUIContainer);
            replaceContainer.RemoveFromHierarchy();

            systemsVisualGraphView.OnNodeSelected = OnNodeSelectionChanged;

            OnSelectionChange();
        }

        private void OnSelectionChange()
        {
            SystemsVisualAsset asset = Selection.activeObject as SystemsVisualAsset;
            if (asset)
            {
                systemsVisualGraphView.PopulateView(asset);
            }
        }

        void OnNodeSelectionChanged(SystemsVisualGraphNodeView nodeView)
        {
            selectedNode = nodeView.node;
        }
    }
}