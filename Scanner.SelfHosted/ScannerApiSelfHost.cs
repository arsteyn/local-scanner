using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using Microsoft.Owin.Hosting;
using Scanner.Helper;
using Scanner.Interface;

namespace Scanner.SelfHosted
{
    class ScannerApiSelfHost
    {
        public static List<IModule> BookmakerScanners; 

        static void Main(string[] args)
        {
            string baseAddress = "http://127.0.0.1:9000/";

            ServicePointManager.DefaultConnectionLimit = 10000;
            ServicePointManager.Expect100Continue = false;

            var assembly = Assembly.GetExecutingAssembly();
            BookmakerScanners = ModuleHelper.LoadModules(assembly, path: Path.GetDirectoryName(assembly.Location));

            //запускаем все сканеры
            foreach (var module in BookmakerScanners) module.Init();

            // Start OWIN host 
            WebApp.Start<Startup>(baseAddress);

            Console.ReadLine();
        }

       
    }
}
