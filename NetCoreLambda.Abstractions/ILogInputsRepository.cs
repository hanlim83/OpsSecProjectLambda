using System.Threading.Tasks;

namespace NetCoreLambda.Abstractions
{
    public interface ILogInputsRepository
    {
        bool IfInputExist(string Name);
        bool InputIngestionStatus(string Name);
        bool UpdateInputIngestionStatus(string Name);

        Task<LogInput> GetLogInput(string Name);
    }
}
