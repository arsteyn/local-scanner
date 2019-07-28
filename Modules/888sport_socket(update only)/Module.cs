using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace S888
{
    public class S888Module : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "S888";

        public void Init()
        {
            Scanner = new S888Scanner();
            new Thread(Scanner.ConvertSocketData).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
