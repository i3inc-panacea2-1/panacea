using Ninject;
using Panacea.Core;
using Panacea.Implementations;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            var kernel = new StandardKernel();
            var serializer = new PanaceaSerializer();
            kernel.Bind<ISerializer>().ToConstant(serializer);

            var logger = new EventViewerLogger("Panacea");
            kernel.Bind<ILogger>().ToConstant(logger);

            var cache = new SqLiteNetworkCache(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "cache"), "cache.db");
            var httpClient = new HttpClient(
                new Uri("http://dev.i3panacea.com:1362"), 
                0,
                cache,
                serializer,
                logger);
            var userService = new UserService(httpClient, logger);
            httpClient.AddMiddleware(new UserAuthenticationMiddleware(userService));
            httpClient.AddMiddleware(new TerminalIdentificationMiddleware("7F-AF-75-70-5A-AB"));

            kernel.Bind<PanaceaServices>().ToConstant(new PanaceaServices(httpClient, null, null));
            await new PluginLoader(kernel).LoadPlugins(@"E:\Repositories\Panacea\Modules", new List<string>() { "ModernUi" });
        }
    }
}
