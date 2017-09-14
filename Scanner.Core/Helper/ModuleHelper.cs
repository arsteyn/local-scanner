using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Scanner.Interface;

namespace Scanner.Helper
{
    public class ModuleHelper
    {
        public static List<IModule> LoadModules(Assembly assembly, string path)
        {
            Thread.Sleep(5000);

            var files = Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly);

            var moduleTypes = new List<Type>();

            foreach (var file in files)
            {
                try
                {
                    moduleTypes.AddRange(
                        Assembly.Load(AssemblyName.GetAssemblyName(file))
                            .GetTypes()
                            .Where(t => t.GetInterfaces().Contains(typeof(IModule))));
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (var inner in ex.LoaderExceptions)
                    {
#if !DEBUG
                        Log.Info(inner.Message);
#endif
                    }
                }
            }

            return moduleTypes.Select(type => (IModule)Activator.CreateInstance(type)).ToList();
        }
    }
}
