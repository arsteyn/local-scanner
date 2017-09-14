using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Bwin
{
    public class BwinModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Bwin";

        public void Init()
        {
            Scanner = new BwinScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
