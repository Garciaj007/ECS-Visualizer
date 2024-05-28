using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Willow
{
    public static class AssemblyHelper
    {
        public static List<MethodInfo> GetMethods(BindingFlags bindingFlags)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(s => s.GetTypes());
            var typesList = types.ToList();

            return typesList.SelectMany(t => t.GetMethods(bindingFlags)).ToList();
        }

        public static List<MethodInfo> GetMethods(BindingFlags bindingFlags, Type type)
        {
            var methods = GetMethods(bindingFlags);

            return methods.Where(m =>
            {
                return m.GetCustomAttributes(type, false).Length > 0;
            }).ToList();
        }

        public static List<Type> GetClassesWithAttribute<Attribute>()
        {
            var type = typeof(Attribute);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.SelectMany(s => s.GetTypes());
            var typesList = types.ToList();

            return typesList.Where(t => t.GetCustomAttributes(type, true).Length > 0).ToList();
        }

        public static List<T> GetInstances<T>()
        {
            List<T> providers = new List<T>();

            System.Type type = typeof(T);
            IEnumerable<System.Type> types = System.AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where((p) =>
            {
                if (p.IsAbstract || p.IsGenericType)
                {
                    return false;
                }

                return p.GetInterfaces().Contains(type);
            });

            foreach (System.Type t in types)
            {
                T inst = (T)System.Activator.CreateInstance(t);
                providers.Add(inst);
            }

            return providers;
        }
    }
}
