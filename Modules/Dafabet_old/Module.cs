using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Dafabet
{
    public class FavbetModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Dafabet";

        public void Init()
        {
            Scanner = new DafabetScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
