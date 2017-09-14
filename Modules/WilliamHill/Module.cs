using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace WilliamHill
{
    public class WilliamHillModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "WilliamHill";

        public void Init()
        {
            Scanner = new WilliamHillScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
