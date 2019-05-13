using Ninject;
using Panacea.Core;
using Panacea.Modularity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    class PluginLoader : IPluginLoader
    {
        public PluginLoader(IKernel kernel)
        {
            _kernel = kernel;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            foreach (var path in _assemblyLookUpPaths)
            {
                var files = Directory.GetFiles(path, args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll",  SearchOption.AllDirectories);
                if (files.Any())
                {
                    return Assembly.LoadFile(files.First());
                }
            }
            return null;
        }

        Dictionary<string, IPlugin> _loadedPlugins = new Dictionary<string, IPlugin>();
        private readonly IKernel _kernel;
        List<string> _assemblyLookUpPaths = new List<string>();

        public IReadOnlyDictionary<string, IPlugin> LoadedPlugins => new ReadOnlyDictionary<string, IPlugin>(_loadedPlugins.ToDictionary(k => k.Key, v => v.Value));

        public event EventHandler<IPlugin> PluginLoaded;
        public event EventHandler<IPlugin> PluginUnloaded;

        public T GetPlugin<T>() where T : IPlugin
        {
            var items = GetPlugins<T>();
            if (!items.Any())
            {
                throw new Exception($"No plugins of type '{typeof(T).Name}' found.");
            }
            if (items.Count() > 1)
            {
                throw new MultiplePluginsLoadedException<T>();
            }
            return items.First();
        }

        public string GetPluginDirectory(string pluginName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetPlugins<T>() where T : IPlugin
        {
            return _loadedPlugins.Where(p => p.Value is T).Select(p=>p.Value).Cast<T>();
        }

        public async Task LoadPlugins(string basePath, List<string> names)
        {
            _assemblyLookUpPaths.Clear();
            var files = Directory.GetFiles(basePath, "Panacea.Modules.*.dll", SearchOption.AllDirectories);
            var currentLoaded = new List<IPlugin>();
            foreach (var file in files.GroupBy(f => Path.GetFileName(f).Split('.')[2]).Select(g => g.First()))
            {
                var name = Path.GetFileName(file).Split('.')[2];
                if (names == null || names.Contains(name))
                {
                    _assemblyLookUpPaths.Add(Path.GetDirectoryName(file));
                    var ass = Assembly.LoadFrom(file);
                    var pluginType = ass.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
                    if (pluginType == null) return;
                    var inst = _kernel.Get(pluginType) as IPlugin;
                    currentLoaded.Add(inst);
                    _loadedPlugins.Add(name, inst);

                }
            }
            foreach (var inst in currentLoaded)
            {
                await inst.BeginInit();
            }
            foreach (var inst in currentLoaded)
            {
                await inst.EndInit();
            }
           
        }
    }
}
