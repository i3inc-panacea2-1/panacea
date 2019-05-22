using Ninject;
using Panacea.Core;
using Panacea.Modularity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Panacea.Implementations
{
    class PluginLoader : IPluginLoader
    {
        public PluginLoader(IKernel kernel, ILogger logger)
        {
            _kernel = kernel;
            _logger = logger ?? throw new ArgumentNullException("logger");
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = args.Name.Substring(0, args.Name.IndexOf(","));
            if (name.EndsWith(".resources")) return null;
            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            var assembly = loaded.FirstOrDefault(ass => ass.FullName.Split(',').First() == name);
            if (assembly != null)
            {
                return assembly;
            }
            var locatedFiles = new List<string>();
            foreach (var path in _assemblyLookUpPaths)
            {
                var files = Directory.GetFiles(path, name + ".dll", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    locatedFiles.Add(f);
                }
            }
            if (locatedFiles.Any())
            {
                var pref = GetHigherVersionFile(locatedFiles.ToArray());
                //foreach (var ass in loaded)
                //{
                //    Console.WriteLine(ass.FullName);
                //}
                var lassembly = LoadAssembly(pref);
                _logger.Info(this, "[+] " + lassembly.FullName);
                //return AppDomain.CurrentDomain.Load(File.ReadAllBytes(pref));
                return lassembly;
            }
            return null;
        }

        Dictionary<string, IPlugin> _loadedPlugins = new Dictionary<string, IPlugin>();
        private readonly IKernel _kernel;
        private readonly ILogger _logger;
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
            return _loadedPlugins
                .Where(p => typeof(T).IsAssignableFrom(p.Value.GetType()))
                .Select(p => p.Value)
                .Cast<T>();
        }

        private Assembly LoadAssembly(string file)
        {
            var ass = Assembly.LoadFile(file);
            //if (ass.CustomAttributes.Any(ca => ca.AttributeType == typeof(ThemeInfoAttribute)))
            //{
            //    var name = ass.FullName.Split(',').First();
            //    var uri = new Uri($"pack://application:,,,/{name};component/themes/generic.xaml", UriKind.Absolute);
            //    if (!Application
            //        .Current
            //        .Resources
            //        .MergedDictionaries
            //        .Any(d => d.Source.ToString().ToLower() == uri.ToString().ToLower()))
            //    {
            //        var resourceDictionaries = ass.GetManifestResourceNames();
            //        var dictionary = new ResourceDictionary();
            //        try
            //        {
            //            dictionary.Source = uri;
            //            Application.Current.Resources.MergedDictionaries.Add(dictionary);
            //        }
            //        catch (IOException)
            //        {

            //        }
            //    }
            //}
            return ass;
        }

        public async Task LoadPlugins(string basePath, List<string> names)
        {

            _assemblyLookUpPaths.Clear();
            var files = Directory.GetFiles(basePath, "Panacea.Modules.*.dll", SearchOption.AllDirectories);
            var uniqueFiles = files.GroupBy(f => Path.GetFileName(f).Split('.')[2]).Select(g => g.First());
            foreach (var file in uniqueFiles)
            {
                if (!_assemblyLookUpPaths.Contains(file))
                    _assemblyLookUpPaths.Add(Path.GetDirectoryName(file));
            }
            var currentLoaded = new List<IPlugin>();
            foreach (var file in uniqueFiles)
            {
                var name = Path.GetFileName(file).Split('.')[2];
                if (names == null || names.Contains(name))
                {
                    try
                    {
                        _logger.Info(this, $"Loading plugin: {Path.GetFileName(file)} - {file}");
                        var ass = LoadAssembly(file);
                        var pluginType = GetTypesSafely(ass).FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t));
                        if (pluginType == null) continue;

                        var inst = _kernel.Get(pluginType) as IPlugin;
                        currentLoaded.Add(inst);
                        _loadedPlugins.Add(name, inst);
                        PluginLoaded?.Invoke(this, inst);
                    }
                    catch
                    {
                        if (Debugger.IsAttached) Debugger.Break();
                    }
                }
            }
            foreach (var inst in currentLoaded)
            {
                try
                {
                    await inst.BeginInit();
                }
                catch
                {
                    if (Debugger.IsAttached) Debugger.Break();
                }

            }
            foreach (var inst in currentLoaded)
            {
                try
                {
                    await inst.EndInit();
                }
                catch
                {
                    if (Debugger.IsAttached) Debugger.Break();
                }
            }

        }

        private string GetHigherVersionFile(string[] files)
        {
            string highest = null;
            Version highestVersion = Version.Parse("0.0.0.0");
            string highestVersionString = null;
            foreach (var file in files)
            {
                var info = FileVersionInfo.GetVersionInfo(file);
                var version = new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart);
                if (highest == null || version >= highestVersion)
                {
                    if (highestVersionString == null || string.Compare(info.FileVersion, highestVersionString) == 1)
                    {
                        highest = file;
                        highestVersion = version;
                        highestVersionString = info.FileVersion;
                    }
                }
            }
            return highest;
        }

        private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(x => x != null);
            }
        }
    }
}
