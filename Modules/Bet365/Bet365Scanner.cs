using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom.Events;
using AngleSharp.Parser.Html;
using Bet365.Extensions;
using BM.DTO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Scanner;

namespace Bet365
{
    public class Bet365Scanner : ScannerBase, IDisposable
    {
        readonly object _lock = new object();

        public override string Name => "Bet365";

        //Bet365 блокирует IP при открытии большого количсетва вкладок
        public sealed override string Host => "https://www.448365365.com/";

        private readonly ChromeDriver _driver;

        public Bet365Scanner()
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

            _driver.Navigate().GoToUrl(Host + "en");
            _driver.Navigate().GoToUrl(Host + "#/IP/");

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(_eventViewButton));


            events = new Dictionary<string, int>();
            //show menu
            _driver.FindElement(_eventViewButton).Click();
            //hide menu
            //_driver.FindElement(_mainMarketsButton).Click();

            var eventslist = _driver.FindElementsByClassName("ipo-TeamStack");

            //foreach (var element in eventslist)
            //{
            //    var t = element.FindElements(By.ClassName("ipo-TeamStack_TeamWrapper")).First().Text;
            //    events.Add(t,0);
            //}

            //foreach (var element in events.OrderBy(d=>Guid.NewGuid()))
            //{
            //    _driver.ExecuteScript("window.open();");

            //    _driver.SwitchTo().Window(_driver.WindowHandles.Last());

            //    events[element.Key] = _driver.WindowHandles.Count;

            //    _driver.Navigate().GoToUrl(Host + "en");
            //    _driver.Navigate().GoToUrl(Host + "#/IP/");

            //    //wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(_eventViewButton));
            //}

            //foreach (var element in events)
            //{
            //    _driver.SwitchTo().Window(_driver.WindowHandles[element.Value]);

            //    _driver.Navigate().GoToUrl(Host + "en");
            //    _driver.Navigate().GoToUrl(Host + "#/IP/");


                
            //    //wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(_eventViewButton));
            //}



        }

        ~Bet365Scanner()
        {
            Dispose(false);
        }

        //private readonly By _mainMarketsButton = By.CssSelector(".ipo-ClassificationHeader_MarketsButton.ipo-InPlayClassificationMarketSelector.ipo-ClassificationHeader_MarketsButton-selected");
        private readonly By _eventViewButton = By.XPath("//*[text()='Overview']");
        //private readonly By _marketTypesListItem = By.CssSelector(".ipo-InPlayClassificationMarketSelectorDropDown_DropDownItem.ipo-MarketSelectorDropDownItem.wl-DropDownItem");


        Dictionary<string, int> events;


        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            //try
            //{
            //    var marketTypes = _driver.FindElements(_marketTypesListItem);

            //    for (var index = 0; index < marketTypes.Count; index++)
            //    {
            //        var marketType = marketTypes[index];
            //        //_driver.FindElement(_mainMarketsButton).Click();

            //        marketType.Click();

            //        var pageSource = _driver.PageSource;

            //        var htmlParser = new HtmlParser();

            //        var document = htmlParser.Parse(pageSource);

            //        var leagueContainer = document.QuerySelector("div.ipo-OverViewDetail_Container.ipo-Classification");

            //        var converter = new Bet365LineConverter();

            //        var l = converter.Convert(leagueContainer, Name, (MarketType)index);

            //        lock (_lock) lines.AddRange(l);
            //    }

            //    ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count(c => c != null), new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

            //    ActualLines = lines.ToArray();
            //}
            //catch (Exception e)
            //{
            //    Log.Info($"ERROR {Name} {e.Message} {e.StackTrace}");
            //}


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
