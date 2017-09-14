using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace BetFair
{
    public class BetfairModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Betfair";

        public void Init()
        {
            Scanner = new BetfairScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
