using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Parimatch
{
    public class ParimatchModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Parimatch";

        public void Init()
        {
            Scanner = new ParimatchScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
