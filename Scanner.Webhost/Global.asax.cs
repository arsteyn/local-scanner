using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Web.Http;
using Scanner.Helper;
using Scanner.Interface;

namespace Scanner.Webhost
{
    public class ScannerApi : System.Web.HttpApplication
    {
        public static List<IModule> BookmakerScanners;

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            ServicePointManager.DefaultConnectionLimit = 10000;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            BookmakerScanners = ModuleHelper.LoadModules(Assembly.GetExecutingAssembly(), path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"));

            //запускаем все сканеры
            foreach (var module in BookmakerScanners) module.Init();
        }
    }
}
