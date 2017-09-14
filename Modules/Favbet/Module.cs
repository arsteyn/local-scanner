using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Favbet
{
    public class FavbetModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Favbet";

        public void Init()
        {
            Scanner = new FavBetScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
