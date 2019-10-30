using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Fonbet
{
    public class FonbetModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Fonbet";

        public void Init()
        {
            Scanner = new FonbetScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
