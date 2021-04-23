using System;
using System.Reflection;
using System.Runtime.Loader;

// from https://docs.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support#load-plugins
namespace MultiShop.Shops
{
    public class LoaderContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver resolver;
        public LoaderContext(string path)
        {
            resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string path = resolver.ResolveAssemblyToPath(assemblyName);
            return path == null ? LoadFromAssemblyPath(path) : null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string path = resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return path == null ? LoadUnmanagedDllFromPath(path) : IntPtr.Zero;
        }
    }
}