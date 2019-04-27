using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Pinnacle
{
    public class PinnacleModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Pinnacle";

        public void Init()
        {
            Scanner = new PinnacleScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
