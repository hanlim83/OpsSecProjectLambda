using System.Collections.Generic;

namespace OpsSecProjectLambda.Abstractions
{
    public interface ILogContextOperations
    {
        bool IfInputExist(string Name);
        bool InputIngestionStatus(string Name);
        bool UpdateInputIngestionStatus(string Name);
        LogInput GetLogInputByName(string Name);
        LogInput GetLogInputByID(int ID);
        GlueConsolidatedEntity GetGlueConsolidatedEntity(int ID);
        bool AddGlueConsolidatedEntity(GlueConsolidatedEntity input);
        bool UpdateGlueConsolidatedEntity(GlueConsolidatedEntity input);
        GlueDatabaseTable GetGlueDatabaseTable(int ID);
        bool UpdateGlueDatabaseTable(GlueDatabaseTable input);
        string GetInputS3BucketName(int ID);
        GlueDatabase GetGlueDatabase();
        bool AddGlueDatabaseTable(GlueDatabaseTable input);
        List<GlueConsolidatedEntity> GetGlueConsolidatedEntities();
    }
}
