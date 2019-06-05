using System;
using System.Threading;
using BM.DTO;
using Newtonsoft.Json;
using NLog;
using Scanner.Interface;

namespace Bet18
{
    public class Bet18Module : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Bet18";

        public void Init()
        {
            try
            {
                Scanner = new Bet18Scanner();
            

                new Thread(Scanner.ConvertSocketData).Start();
            }
            catch (Exception e)
            {
                LogManager.GetCurrentClassLogger().Info($"ERROR Bet18 Exception {JsonConvert.SerializeObject(e)}");
            }
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
