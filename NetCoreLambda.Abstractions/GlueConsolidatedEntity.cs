
namespace NetCoreLambda.Abstractions
{
    public class GlueConsolidatedEntity
    {
        public int ID { get; set; }
        public string CrawlerName { get; set; }
        public string JobName { get; set; }
        public string JobScriptLocation { get; set; }
        public int LinkedLogInputID { get; set; }
        public virtual LogInput LinkedLogInput { get; set; }
        public virtual GlueDatabaseTable LinkedTable { get; set; }
    }
}
