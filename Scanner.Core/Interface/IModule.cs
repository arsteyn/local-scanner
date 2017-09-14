using BM.DTO;

namespace Scanner.Interface
{
    public interface IModule
    {
        string Name { get; }
        void Init();
        LineDTO[] GetLines();
    }
}
