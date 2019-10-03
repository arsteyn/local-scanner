using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using BM;
using BM.DTO;
using NLog;
using NLog.Fluent;
using Scanner.Helper;
using Scanner.Interface;

namespace Scanner
{
    public abstract class ScannerBase : IScanner
    {
        protected List<WebProxy> ProxyList;

        public virtual void StartScan()
        {
            ProxyList = ProxyHelper.GetHostList();

            CheckDict();

            WriteLiveProxy();

            while (true)
            {
                if (!ProxyList.Any())
                {
                    Log.Info($"ERROR {Name} no proxy");

                    Thread.Sleep(10000);

                    ProxyList = ProxyHelper.GetHostList();

                    CheckDict();

                    WriteLiveProxy();

                    continue;
                }

                UpdateLiveLines();

                LastUpdated = DateTime.Now;
            }
        }

        public virtual void ConvertSocketData()
        {
            while (true)
            {
                UpdateLiveLines();

                LastUpdated = DateTime.Now;
            }
        }


        protected DateTime LastUpdated { get; set; }

        protected TimeSpan LastUpdatedDiff => DateTime.Now - LastUpdated;

        private LineDTO[] _actualLines;

        public virtual LineDTO[] ActualLines
        {
            get => LastUpdated.AddSeconds(30) <= DateTime.Now ? new LineDTO[] { } : _actualLines;
            set => _actualLines = value;
        }

        public virtual Logger Log => LogManager.GetCurrentClassLogger();


        public abstract string Name { get; }

        public abstract string Host { get;}

        protected virtual string Domain => new Uri(Host).Host;

        protected abstract void UpdateLiveLines();

        protected virtual void WriteLiveProxy()
        {

            var path = HostingEnvironment.ApplicationPhysicalPath + $"CheckedProxy/{Name}.txt";

            using (var outputFile = new StreamWriter(path, false))
            {
                foreach (var proxy in ProxyList)
                {
                    outputFile.WriteLine($"{proxy.Address.Host}");
                }


            }
        }

        protected virtual void CheckDict()
        {
            var hostsToDelete = new List<WebProxy>();

            Parallel.ForEach(ProxyList, (host, state) =>
            {
                try
                {
                    using (var webClient = new Extensions.WebClientEx(host))
                    {
                        webClient.DownloadString(Host);
                    }
                }
                catch (Exception e)
                {
                    hostsToDelete.Add(host);
                }
            });

            foreach (var host in hostsToDelete) ProxyList.Remove(host);
        }
    }


}
