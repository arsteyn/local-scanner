using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using BM.DTO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Scanner;

namespace Bet18
{
    public class Bet18Scanner : ScannerBase, IDisposable
    {
        readonly object _lock = new object();

        public override string Name => "Bet18";

        public sealed override string Host => "https://www.18bet.com/";

        private readonly ChromeDriver _driver;

        public Bet18Scanner()
        {
            var chromeOptions = new ChromeOptions();
            var chromeDriverService = ChromeDriverService.CreateDefaultService();

            //Create a new proxy object
            var proxy = new Proxy();
            proxy.Kind = ProxyKind.Manual;
            proxy.IsAutoDetect = false;
            //Set the http proxy value, host and port.
            proxy.HttpProxy = proxy.SslProxy = "196.18.167.228:8000";
            //Set the proxy to the Chrome options
            chromeOptions.Proxy = proxy;

            chromeOptions.AddArgument("ignore-certificate-errors");

            //TODO: при каждом перезапуске создается фоновый процесс!!!!!!
            //chromeOptions.AddArguments(new List<string>
            //{
            //        "--silent-launch",
            //        "--no-startup-window",
            //        "no-sandbox",
            //        "headless"
            //});

            _driver = new ChromeDriver(chromeDriverService, chromeOptions, TimeSpan.FromSeconds(180));

            _driver.Navigate().GoToUrl(Host + "en/sport/live");

            new Thread(AdditionalOutcomeButtonClicker).Start();

        }

        private void AdditionalOutcomeButtonClicker()
        {
            while (true)
            {
                try
                {
                    var leagues = _driver.FindElementsByClassName("league-container");

                    foreach (var league in leagues)
                    {
                        var inner = league.FindElement(By.CssSelector("div.league-content.collapse"));

                        if (inner != null && !inner.GetAttribute("class").Contains("show"))
                        {
                            ScrollElementToBecomeVisible(_driver, league);
                            league.Click();
                        }


                        //var events = league.FindElements(By.CssSelector("div.event.event-container.event-container"));
                        //if (events == null) continue;

                        //foreach (var @event in events)
                        //{
                        //    var marketCounter = @event.FindElement(By.CssSelector("div.market-counter"));
                        //    var detailWrapper = @event.FindElement(By.CssSelector("div.event-all-markets-wrapper"));

                        //    if (marketCounter == null || detailWrapper == null || detailWrapper.GetAttribute("class").Contains("visible")) continue;

                        //    ScrollElementToBecomeVisible(_driver, marketCounter);
                        //    marketCounter.Click();
                        //}
                    }


                }
                catch (Exception exception) { }
            }
        }

        public static void ScrollElementToBecomeVisible(IWebDriver driver, IWebElement element)
        {
            IJavaScriptExecutor jsExec = (IJavaScriptExecutor)driver;
            jsExec.ExecuteScript("arguments[0].scrollIntoView(true);", element);
        }

        ~Bet18Scanner()
        {
            Dispose(false);
        }

        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            try
            {
                var pageSource = _driver.PageSource;

                var htmlParser = new HtmlParser();

                var document = htmlParser.Parse(pageSource);

                var sportContainers = document.QuerySelectorAll("div.game-mode-container.mode-live>.game-mode-content>.sport-container");

                foreach (var sportContainer in sportContainers)
                {
                    var converter = new Bet18LineConverter();

                    var l = converter.Convert(sportContainer, Name);

                    lock (_lock) lines.AddRange(l);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count(c => c != null), new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                ActualLines = lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info($"ERROR Bet18 {e.Message} {e.StackTrace}");
            }


        }

        private void ReleaseUnmanagedResources() { }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();

            _driver?.Close();
            _driver?.Quit();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
