using BM.DTO;

namespace Scanner.Interface
{
    public interface IScanner
    {
        void StartScan();
        void ConvertSocketData();
        LineDTO[] ActualLines { get; set; }
        string Name { get; }
        string Host { get; }
    }
}