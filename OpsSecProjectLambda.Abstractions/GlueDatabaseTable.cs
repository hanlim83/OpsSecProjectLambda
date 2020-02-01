
namespace OpsSecProjectLambda.Abstractions
{
    public class GlueDatabaseTable
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int LinkedDatabaseID { get; set; }
        public virtual GlueDatabase LinkedDatabase { get; set; }
        public int LinkedGlueConsolidatedInputEntityID { get; set; }
        public virtual GlueConsolidatedEntity LinkedGlueConsolidatedInputEntity { get; set; }
    }
}
