using BM.DTO;

namespace Scanner.Interface
{
    public interface IScanner
    {
        void StartScan();
        LineDTO[] ActualLines { get; set; }
        string Name { get; }
        string Host { get; }
    }
}