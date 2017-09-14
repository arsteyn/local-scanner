using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Leonbets
{
    public class LeonBetsModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Leonbets";

        public void Init()
        {
            Scanner = new LeonBetsScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
