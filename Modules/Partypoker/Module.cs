using System.Threading;
using BM.DTO;
using Scanner.Interface;

namespace Partypoker
{
    public class PartypokerModule : IModule
    {
        private IScanner Scanner { get; set; }

        public string Name => "Partypoker";

        public void Init()
        {
            Scanner = new PartypokerScanner();
            new Thread(Scanner.StartScan).Start();
        }

        public LineDTO[] GetLines()
        {
            return Scanner.ActualLines;
        }
    }
}
