using Ninject;
using Panacea.Core;
using Panacea.Implementations;
using Panacea.Windows;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Panacea
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            System.Windows.Forms.Application.ThreadException += Application_ThreadException;
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {

            LogException(e.Exception);
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
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogException(e.Exception);
        }

        void LogException(Exception ex)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
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

        ILogger _logger;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {

            var splashScreen = new Controls.SplashScreen();
            splashScreen.Show();
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pluginsPath = Path.Combine(basePath, "Plugins");
            if (Debugger.IsAttached)
            {
                pluginsPath = Path.Combine(new DirectoryInfo(basePath).Parent.Parent.Parent.Parent.Parent.Parent.Parent.FullName, "Modules");
            }
            var kernel = new StandardKernel();
            var serializer = new PanaceaSerializer();
            kernel.Bind<ISerializer>().ToConstant(serializer);

            _logger = new EventViewerLogger("Panacea");
            _logger.Info(this, "Hi!");
            var identification = new TerminalIdentifier(serializer);
            var putik = await identification.GetIdentifierAsync();
            kernel.Bind<ILogger>().ToConstant(_logger);

            var cache = new SqLiteNetworkCache(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cache"), "cache.db");
            var httpClient = new HttpClient(
                //new Uri("http://dev.i3panacea.com:1362"), 
                new Uri("http://hs.internal.ci:1338"),
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
            kernel.Bind<PanaceaServices>().ToConstant(new PanaceaServices(httpClient, userService, loader, _logger, webSocket, serializer));
            var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));


            //if (Debugger.IsAttached)
            //{
            //    new DevConsole(logger).Show();
            //}
            //Console.WriteLine("-------------");
            var watch = new Stopwatch();
            watch.Start();

            await loader.LoadPlugins(pluginsPath, null);

            watch.Stop();
            _logger.Info(this, $"Time to load plugins:  {watch.ElapsedMilliseconds.ToString()}ms");
            splashScreen.Close();
        }
    }
}
