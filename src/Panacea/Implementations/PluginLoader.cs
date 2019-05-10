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
        }

        Dictionary<string, IPlugin> _loadedPlugins;
        private readonly IKernel _kernel;

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
            return _loadedPlugins.Where(p => p.Value is T).Cast<T>();
        }

        public async Task LoadPlugins(string basePath, List<string> names)
        {
            var files = Directory.GetFiles(basePath, "Panacea.Modules.*.dll", SearchOption.AllDirectories);
            foreach (var file in files.GroupBy(f => Path.GetFileName(f).Split('.')[2]).Select(g => g.First()))
            {
                var name = Path.GetFileName(file).Split('.')[2];
                if (names.Contains(name))
                {
                    var ass = Assembly.LoadFrom(file);
                    var pluginType = ass.GetTypes().FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
                    if (pluginType == null) return;
                    var inst = _kernel.Get(pluginType) as IPlugin;
                    await inst.BeginInit();
                    await inst.EndInit();
                }
            }
        }
    }
}
