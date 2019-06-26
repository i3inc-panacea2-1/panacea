using Panacea.Implementations;
using System;
using System.Collections.Generic;
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
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Any(a => a.ToLower() == "help")|| true)
            {
                var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var pluginsPath = Path.Combine(basePath, "Plugins");
                if (!Directory.Exists(pluginsPath))
                {
                    pluginsPath = Path.Combine(new DirectoryInfo(basePath).Parent.Parent.Parent.Parent.Parent.Parent.Parent.FullName, "Modules");
                }

                if (!AttachConsole(ATTACH_PARENT_PROCESS))
                    AllocConsole();

                var dict = new PluginLoader().GetInjectableVariables(pluginsPath).GetAwaiter().GetResult();
                Console.WriteLine(Environment.NewLine);
                foreach (var p in dict)
                {
                    var original = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(p.Alias);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"\t{p.Description}");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\t{p.Example}");
                    Console.ForegroundColor = original;
                    Console.WriteLine();
                }
                FreeConsole();

                return;
            }
            else
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
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
