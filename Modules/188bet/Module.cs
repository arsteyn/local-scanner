using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Bet188
{
    public class Bet188Module : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Bet188";

        public void Init()
        {
            Scanner = new Bet188Scanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
