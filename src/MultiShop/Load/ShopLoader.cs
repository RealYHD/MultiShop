using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MultiShop.ShopFramework;

namespace MultiShop.Load
{
    public class ShopLoader
    {
        public IEnumerable<IShop> LoadShops(string shop) {
            return InstantiateShops(LoadAssembly(shop));
        }

        public IReadOnlyDictionary<string, IEnumerable<IShop>> LoadAllShops(IEnumerable<string> directories) {
            Dictionary<string, IEnumerable<IShop>> res = new Dictionary<string, IEnumerable<IShop>>();
            foreach (string dir in directories)
            {
                res.Add(dir, LoadShops(dir));
            }
            return res;
        }

        private IEnumerable<IShop> InstantiateShops(Assembly assembly) {
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IShop).IsAssignableFrom(type)) {
                    IShop shop = Activator.CreateInstance(type) as IShop;
                    if (shop != null) {
                        yield return shop;
                    }
                }
            }
        }

        private Assembly LoadAssembly(string path) {
            LoaderContext context = new LoaderContext(path);
            return context.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
        }
    }
}