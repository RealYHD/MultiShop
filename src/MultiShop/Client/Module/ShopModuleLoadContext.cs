using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace MultiShop.Client.Module
{
    public class ShopModuleLoadContext : AssemblyLoadContext
    {
        private IDictionary<string, byte[]> rawAssemblies;
        private Dictionary<string, int> useCounter;
        public IReadOnlyDictionary<string, int> UseCounter {get => useCounter;}
        public ShopModuleLoadContext(IEnumerable<KeyValuePair<string, byte[]>> assemblies)
        {
            this.rawAssemblies = new Dictionary<string, byte[]>(assemblies);
            useCounter = new Dictionary<string, int>();
            foreach (string name in rawAssemblies.Keys)
            {
                useCounter[name] = 0;
            }
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            if (!rawAssemblies.ContainsKey(assemblyName.FullName)) return null;

            useCounter[assemblyName.FullName] += 1;

            using (MemoryStream stream = new MemoryStream(rawAssemblies[assemblyName.FullName]))
            {
                return LoadFromStream(stream);
            }
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            throw new NotImplementedException($"Cannot load unmanaged dll \"{unmanagedDllName}\".");
        }
    }
}