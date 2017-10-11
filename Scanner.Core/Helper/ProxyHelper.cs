using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web.Hosting;

namespace Scanner.Helper
{
    public static class ProxyHelper
    {
        public static List<WebProxy> GetHostList()
        {
            var path = HostingEnvironment.ApplicationPhysicalPath + "Proxy/ip.txt";

            var list = File.ReadAllText(path);

            var hostList = list.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            var result = new List<WebProxy>();


            foreach (var t in hostList)
            {
                var pr = new WebProxy(t.Split(':')[0], int.Parse(t.Split(':')[1]));

                var networkCredential = new NetworkCredential
                {
                    UserName = t.Split(':')[2],
                    Password = t.Split(':')[3]
                };

                pr.Credentials = networkCredential;
                result.Add(pr);
            }

            return result;
        }
 
    }
}