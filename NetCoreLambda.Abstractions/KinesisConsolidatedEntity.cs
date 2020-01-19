
namespace NetCoreLambda.Abstractions
{
    public class KinesisConsolidatedEntity
    {
        public int ID { get; set; }
        public string PrimaryFirehoseStreamName { get; set; }
        public string SecondaryFirehoseStreamName { get; set; }
        public string AnalyticsApplicationName { get; set; }
        public bool? AnalyticsEnabled { get; set; }
        public int LinkedLogInputID { get; set; }
        public virtual LogInput LinkedLogInput { get; set; }
    }
}
