using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace S888
{
    public class FavbetModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "S888";

        public void Init()
        {
            Scanner = new S888Scanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
