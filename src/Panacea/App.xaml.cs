using Ninject;
using Panacea.Core;
using Panacea.Implementations;
using Panacea.Models;
using SharpRaven;
using SharpRaven.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Panacea
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : SingleInstanceApp
    {
        Controls.SplashScreen _splash;
        public App() : base("Panacea")
        {
            InitializeComponent();
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            System.Windows.Forms.Application.ThreadException += Application_ThreadException;
        }



        ILogger _logger;
        private PanaceaServices _core;
        private PanaceaRegistrySettingsManager _settings;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                _splash = new Controls.SplashScreen();
                _splash.Show();
                if (Program.StartupArgs["noupdate"] != "1" && !Program.StartupArgs.AllKeys.Contains("disable-updates"))
                {
                    await StartUpdater();
                    return;
                }
                SetProgress("Getting configuration...");

                var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var pluginsPath = Path.Combine(basePath, "ibt-plugins");
                if (!Directory.Exists(pluginsPath))
                {
                    pluginsPath = Path.Combine(new DirectoryInfo(basePath).Parent.Parent.Parent.Parent.Parent.Parent.Parent.FullName, "Modules");
                }

                var kernel = new StandardKernel();
                var serializer = new PanaceaSerializer();
                kernel.Bind<ISerializer>().ToConstant(serializer);

                _logger = new EventViewerLogger("Panacea");
                kernel.Bind<ILogger>().ToConstant(_logger);
                _logger.Info(this, "Hi!");
                var identification = new TerminalIdentifier(serializer);
                var putik = await identification.GetIdentifierAsync();

                _settings = new PanaceaRegistrySettingsManager(serializer);

                var cache = new SqLiteNetworkCache(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cache"), "cache.db");
                var httpClient = new HttpClient(
                    new Uri((await _settings.GetRegistrationInfo()).HospitalServer),
                    0,
                    cache,
                    serializer,
                    _logger);
                var userService = new UserService(httpClient, _logger);
                httpClient.AddMiddleware(new UserAuthenticationMiddleware(userService));
                httpClient.AddMiddleware(new TerminalIdentificationMiddleware(putik));
                var loader = new PluginLoader(kernel, _logger);
                var webSocket = new WebSocketCommunicator(putik);
                webSocket.Connect();
                await userService.LoginFromFileAsync();
                _core = new PanaceaServices(httpClient, userService, loader, _logger, webSocket, serializer);
                kernel.Bind<PanaceaServices>().ToConstant(_core);
                var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                var pluginsToLoad = (await GetPluginsAsync()).ToList();
                var exclude = new List<string>();
                if (Program.StartupArgs["exclude"] != null)
                {
                    exclude = Program.StartupArgs["exclude"].ToLower().Split(',').ToList();
                }

                if (Program.StartupArgs["include"] != null)
                {
                    var letters = Program.StartupArgs["include"].ToLower().Split(',');
                    pluginsToLoad = pluginsToLoad.Concat(letters).ToList();
                }


                //if (Debugger.IsAttached)
                //{
                //    new DevConsole(logger).Show();
                //}
                //Console.WriteLine("-------------");
                var watch = new Stopwatch();
                watch.Start();
                SetProgress("Loading plugins...");
                var args = await MergeParametersAsync(e.Args.ToList());
                await loader.LoadPlugins(pluginsPath, pluginsToLoad, exclude, args.ToArray());

                watch.Stop();
                _logger.Info(this, $"Time to load plugins:  {watch.ElapsedMilliseconds.ToString()}ms");
                _splash.Close();
                _splash = null;
            }
            catch (Exception ex)
            {
                SetProgress(ex.Message);
                await Task.Delay(5000);
                ShutDownSafe();
            }
        }

        async Task<List<string>> MergeParametersAsync(List<string> args1)
        {
            var response = await _settings.GetRegistrationInfo();
            if (response.ManagementServerResponse.Result.TerminalType?.Pairs != null)
            {
                var lines = response.ManagementServerResponse.Result.TerminalType.Pairs.Split(new[] { "\r\n", "\r", "\n" },
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    args1.Add(line);
                }
            }
            return args1;
        }

        async Task<IEnumerable<string>> GetPluginsAsync()
        {
            var resp = await _core.HttpClient.GetObjectAsync<GetVersionsResponse>("get_versions/");
            if (resp.Success)
            {
                var plugins = resp.Result.Plugins.Select(p => p.Name);
                return plugins;
            }
            else
            {
                throw new Exception($"get_versions error: {resp.Error}");
            }
        }

        string GetArgsAsString()
        {
            var sb = new StringBuilder();
            foreach (string key in Program.StartupArgs.Keys.Cast<string>().Where(key => key != "noupdate"))
            {
                sb.Append(key);
                if (Program.StartupArgs[key] != null) sb.Append("=\"" + Program.StartupArgs[key] + "\"");
                sb.Append(" ");
            }
            return sb.ToString();
        }

        private async Task StartUpdater()
        {
            SetProgress("Updating...");
            await Task.Delay(1500);
            ShutDownSafe();
            try
            {
                Process.Start(GetPath("Updater", "IBT.Updater.exe"), GetArgsAsString());
            }
            catch
            {
                // no updater
            }
        }

        public static void ShutDownSafe()
        {
            if (Current.Dispatcher != null && !Current.Dispatcher.CheckAccess())
            {
                Current.Dispatcher.BeginInvoke(new Action(() => Current.Shutdown()));
            }
            else Current.Shutdown();
        }

        string GetPath(params string[] parts)
        {
            var arr = new string[1] {
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            };
            return Path.Combine(arr.Concat(parts).ToArray());
        }

        void SetProgress(string text)
        {
            var s = _splash;
            if (s != null)
            {
                s.Dispatcher.Invoke(() =>
                {
                    s.ProgressText = text;
                });
            }
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {

            LogException(e.Exception);
            ShdutDownSafe();
        }

        private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {

            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                LogException(ex);
            }
            else
            {
                ex = new NotSupportedException("Unhandled exception doesn't derive from System.Exception: " + e.ExceptionObject.ToString());
                LogException(ex);
            }
            ShdutDownSafe();
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogException(e.Exception);
            ShdutDownSafe();
        }

        void ShdutDownSafe()
        {
            if (Current?.Dispatcher != null)
            {
                Current.Dispatcher.Invoke(() => Shutdown());
            }
            else Shutdown();
        }

        void LogException(Exception ex)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
            SetProgress(ex.Message);
            if (ex == null) return;
            int tries = 0;
            while (tries < 3)
            {
                tries++;
                try
                {
                    var ravenClient = new RavenClient("https://bcc69eaff16a497986eae58e24af6666@sentry.io/1489869");
                    AsyncHelpers.RunSync(() => ravenClient.CaptureAsync(new SentryEvent(ex)), 7000);
                    tries = 10;
                    _logger?.Error("App", ex.Message);
                    _logger?.Error("App", ex.StackTrace);

                }
                catch
                {
                    AsyncHelpers.RunSync(async () => await Task.Delay(3000), 4000);
                }
            }
        }

        public override bool SignalExternalCommandLineArgs(IList<string> args)
        {
            foreach (var arg in args)
            {

            }
            return true;
        }

        void ParsePluginCommand()
        {

        }
    }

    class PluginCommand
    {
        public string PluginType { get; set; }


    }
}
