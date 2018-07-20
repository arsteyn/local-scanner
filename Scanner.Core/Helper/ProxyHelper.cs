using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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


        public static void UpdateLeonEvents(string line)
        {
            var path = HostingEnvironment.ApplicationPhysicalPath + "OddsTypes.txt";

            var contents = File.ReadAllLines(path);

            if (contents.Any(l=> l == line)) return;

            using (var w = File.AppendText(path))
            {
                w.WriteLine(line);
            }

            var contents2 = File.ReadAllLines(path);

            var orderedScores = contents2.OrderBy(x => x);

            File.WriteAllLines(path, orderedScores);
        }




        public static string GetDomain(string host)
        {
            var domain = host.RegexStringValue("(?:http[s]?[:][/]+)(?:www.*?[.])?(?<value>.*?)(?:[/]|$)");
            return domain;
        }

        public static CookieCollection GetAllCookies(this CookieContainer container)
        {
            var allCookies = new CookieCollection();
            var domainTableField = container.GetType().GetRuntimeFields().FirstOrDefault(x => x.Name == "m_domainTable");
            var domains = (IDictionary)domainTableField.GetValue(container);

            foreach (var val in domains.Values)
            {
                var type = val.GetType().GetRuntimeFields().First(x => x.Name == "m_list");
                var values = (IDictionary)type.GetValue(val);
                foreach (CookieCollection cookies in values.Values)
                {
                    allCookies.Add(cookies);
                }
            }
            return allCookies;
        }

    }
}