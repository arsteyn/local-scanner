using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Sbobet
{
    public class SbobetModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Sbobet";

        public void Init()
        {
            Scanner = new SbobetScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
