﻿using Ninject;
using Panacea.Core;
using Panacea.Implementations;
using Panacea.Windows;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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


        }
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

            var logger = new EventViewerLogger("Panacea");
            logger.Info(this, "Hi!");
            var identification = new TerminalIdentifier(serializer);
            var putik = await identification.GetIdentifierAsync();
            kernel.Bind<ILogger>().ToConstant(logger);

            var cache = new SqLiteNetworkCache(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cache"), "cache.db");
            var httpClient = new HttpClient(
                new Uri("http://dev.i3panacea.com:1362"), 
                //new Uri("http://hs.internal.ci:1338"),
                0,
                cache,
                serializer,
                logger);
            var userService = new UserService(httpClient, logger);
            httpClient.AddMiddleware(new UserAuthenticationMiddleware(userService));
            httpClient.AddMiddleware(new TerminalIdentificationMiddleware(putik));
            var loader = new PluginLoader(kernel, logger);
            var webSocket = new WebSocketCommunicator(putik);
            webSocket.Connect();
            await userService.LoginFromFileAsync();
            kernel.Bind<PanaceaServices>().ToConstant(new PanaceaServices(httpClient, userService, loader, logger, webSocket, serializer));
            var dir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));


            if (Debugger.IsAttached)
            {
                new DevConsole(logger).Show();
            }
            logger.Info(this, "Hi!");
            //Console.WriteLine("-------------");
            var watch = new Stopwatch();
            watch.Start();
            await loader.LoadPlugins(pluginsPath, null);
            watch.Stop();
            logger.Info(this, watch.ElapsedMilliseconds.ToString());            
            splashScreen.Close();
        }
    }
}
