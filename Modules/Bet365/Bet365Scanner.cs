using System;
using System.Collections.Generic;
using System.Linq;
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

        public sealed override string Host => "https://www.348365365.com/";

        //https://www.448365365.com/#/HO/
        //https://www.348365365.com
        //https://www.838365.com/en/

        private readonly ChromeDriver _driver;

        public Bet365Scanner()
        {
            var chromeOptions = new ChromeOptions();
            var chromeDriverService = ChromeDriverService.CreateDefaultService();

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
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(_mainMarketsButton));

            //show menu
            _driver.FindElement(_mainMarketsButton).Click();
            //hide menu
            _driver.FindElement(_mainMarketsButton).Click();

        }

        ~Bet365Scanner()
        {
            Dispose(false);
        }

        private readonly By _mainMarketsButton = By.CssSelector(".ipo-ClassificationHeader_MarketsButton.ipo-InPlayClassificationMarketSelector.ipo-ClassificationHeader_MarketsButton-selected");
        private readonly By _marketTypesListItem = By.CssSelector(".ipo-InPlayClassificationMarketSelectorDropDown_DropDownItem.ipo-MarketSelectorDropDownItem.wl-DropDownItem");


        protected override void UpdateLiveLines()
        {
            var lines = new List<LineDTO>();

            try
            {
                var marketTypes = _driver.FindElements(_marketTypesListItem);

                for (var index = 0; index < marketTypes.Count; index++)
                {
                    var marketType = marketTypes[index];
                    _driver.FindElement(_mainMarketsButton).Click();

                    marketType.Click();

                    var pageSource = _driver.PageSource;

                    var htmlParser = new HtmlParser();

                    var document = htmlParser.Parse(pageSource);

                    var leagueContainer = document.QuerySelector("div.ipo-OverViewDetail_Container.ipo-Classification");

                    var converter = new Bet365LineConverter();

                    var l = converter.Convert(leagueContainer, Name, (MarketType)index);

                    lock (_lock) lines.AddRange(l);
                }

                LastUpdatedDiff = DateTime.Now - LastUpdated;

                ConsoleExt.ConsoleWrite(Name, ProxyList.Count, lines.Count(c => c != null), new DateTime(LastUpdatedDiff.Ticks).ToString("mm:ss"));

                ActualLines = lines.ToArray();
            }
            catch (Exception e)
            {
                Log.Info($"ERROR {Name} {e.Message} {e.StackTrace}");
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
