using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OpsSecProjectLambda.Abstractions;

namespace OpsSecProjectLambda.EF
{
    public class LogContextOperations : ILogContextOperations
    {
        public LogContext Context { get; }

        public LogContextOperations(LogContext context)
        {
            Context = context;
        }

        public bool IfInputExist(string Name)
        {
            List<LogInput> inputs = Context.LogInputs.ToList();
            foreach(LogInput input in inputs)
            {
                if (input.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public bool UpdateInputIngestionStatus(string Name)
        {
            List<LogInput> inputs = Context.LogInputs.ToList();
            LogInput operatedInput = null;
            foreach (LogInput input in inputs)
            {
                if (input.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    operatedInput = input;
                    break;
                }
            }
            if (operatedInput == null)
            return false;
            else
            {
                operatedInput.InitialIngest = true;
                Context.LogInputs.Update(operatedInput);
                try
                {
                    Context.SaveChanges();
                    return true;
                }
                catch (DbUpdateException)
                {
                    return false;
                }
            }
        }

        public LogInput GetLogInputByName(string Name)
        {
            List<LogInput> inputs = Context.LogInputs.ToList();
            foreach (LogInput input in inputs)
            {
                if (input.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase))
                    return input;
            }
            return null;
        }

        public LogInput GetLogInputByID(int ID)
        {
            return Context.LogInputs.Find(ID);
        }

        public bool InputIngestionStatus(string Name)
        {
            List<LogInput> inputs = Context.LogInputs.ToList();
            foreach (LogInput input in inputs)
            {
                if (input.Name.Equals(Name, System.StringComparison.InvariantCultureIgnoreCase))
                    return input.InitialIngest;
            }
            return false;
        }

        public GlueConsolidatedEntity GetGlueConsolidatedEntity(int ID)
        {
            LogInput result = Context.LogInputs.Find(ID);
            return result.LinkedGlueEntity;
        }

        public bool UpdateGlueConsolidatedEntity(GlueConsolidatedEntity input)
        {
            Context.GlueConsolidatedEntities.Update(input);
            try
            {
                Context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public GlueDatabaseTable GetGlueDatabaseTable(int ID)
        {
            LogInput result1 = Context.LogInputs.Find(ID);
            GlueConsolidatedEntity result2 = result1.LinkedGlueEntity;
            if (result2 == null)
                return null;
            else
            {
                return result2.LinkedTable;
            }
        }

        public bool UpdateGlueDatabaseTable(GlueDatabaseTable input)
        {
            Context.GlueDatabaseTables.Update(input);
            try
            {
                Context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public bool AddGlueConsolidatedEntity(GlueConsolidatedEntity input)
        {
            Context.GlueConsolidatedEntities.Add(input);
            try
            {
                Context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public string GetInputS3BucketName(int ID)
        {
            LogInput result = Context.LogInputs.Find(ID);
            if (result.LinkedS3Bucket == null)
                return null;
            else
                return result.LinkedS3Bucket.Name;
        }

        public GlueDatabase GetGlueDatabase()
        {
            return Context.GlueDatabases.Find(1);
        }

        public List<GlueConsolidatedEntity> GetGlueConsolidatedEntities()
        {
            return Context.GlueConsolidatedEntities.ToList();
        }

        public bool AddGlueDatabaseTable(GlueDatabaseTable input)
        {
            Context.GlueDatabaseTables.Add(input);
            try
            {
                Context.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }
    }
}