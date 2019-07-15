using Mono.Options;
using Panacea.Core;
using Panacea.Implementations;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Panacea
{
    class Program
    {
        static bool _showHelp;
        static OptionSet _options;
        public static NameValueCollection StartupArgs { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            StartupArgs = new NameValueCollection();
            _options = new OptionSet()
            {
                {
                    "n", "Skip update.",
                    v => StartupArgs["disable-updates"] = "1"
                },
                {
                    "x|exclude=", "Exclude specified plugins separated by comma. Case sensitive.",
                    v => StartupArgs["exclude"] = v
                },
                {
                    "i|include=", "Include specified plugins separated by comma. Case sensitive.",
                    v => StartupArgs["include"] = v
                },
                {
                    "?|h|help", "This",
                    v => _showHelp = v != null
                }
            };
            if (args.Contains("noupdate=1"))
            {
                StartupArgs["noupdate"] = "1";
            }
            _options.Parse(args);
            if (_showHelp)
            {
                ShowHelp();
                return;
            }
            else
            {
                var app = new App();

                app.Run();
            }
        }

        private static void ShowHelp()
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS))
                AllocConsole();

            _options.WriteOptionDescriptions(Console.Out);
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pluginsPath = Path.Combine(basePath, "Plugins");
            if (!Directory.Exists(pluginsPath))
            {
                pluginsPath = Path.Combine(new DirectoryInfo(basePath).Parent.Parent.Parent.Parent.Parent.Parent.Parent.FullName, "Modules");
            }


            var dict = new PluginLoader().GetInjectableVariables(pluginsPath).GetAwaiter().GetResult();
            Console.WriteLine(Environment.NewLine);
            foreach (var p in dict)
            {
                var attr = p.GetCustomAttribute<PanaceaInjectAttribute>();
                var original = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{attr.Alias} <{p.PropertyType.Name}>");
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"\t{attr.Description}");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\t{attr.Example}");
                Console.ForegroundColor = original;
                Console.WriteLine();
            }
            FreeConsole();
        }

        private const int ATTACH_PARENT_PROCESS = -1;

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
