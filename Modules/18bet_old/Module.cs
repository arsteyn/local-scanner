using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Bet18
{
    public class Bet18Module : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Bet18";

        public void Init()
        {
            Scanner = new Bet18Scanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
