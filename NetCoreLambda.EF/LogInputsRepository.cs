using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetCoreLambda.Abstractions;

namespace NetCoreLambda.EF
{
    public class LogInputsRepository : ILogInputsRepository
    {
        public LogContext Context { get; }

        public LogInputsRepository(LogContext context)
        {
            Context = context;
        }

        public bool IfInputExist(string Name)
        {
            LogInput result = Context.LogInputs.Where(L => L.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (result == null)
                return false;
            else
                return true;
        }

        public bool UpdateInputIngestionStatus(string Name)
        {
            LogInput operatedInput = Context.LogInputs.Where(L => L.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            operatedInput.InitialIngest = true;
            Context.LogInputs.Update(operatedInput);
            try
            {
                Context.SaveChanges();
                return true;
            } catch (DbUpdateException)
            {
                return false;
            }
        }

        public Task<LogInput> GetLogInput(string Name)
        {
            return Context.LogInputs.Where(L => L.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefaultAsync();
        }

        public bool InputIngestionStatus(string Name)
        {
            LogInput result = Context.LogInputs.Where(L => L.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            return result.InitialIngest;
        }
    }
}
