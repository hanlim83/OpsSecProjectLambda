using System.Collections.Generic;

namespace OpsSecProjectLambda.Abstractions
{
    public enum LogInputCategory
    {
        SSH, ApacheWebServer, SquidProxy, WindowsEventLogs
    }
    public class LogInput
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public LogInputCategory LogInputCategory { get; set; }
        public string ConfigurationJSON { get; set; }
        public bool InitialIngest { get; set; }
        public int LinkedUserID { get; set; }
        public int LinkedS3BucketID { get; set; }
        public virtual S3Bucket LinkedS3Bucket { get; set; }
        public virtual GlueConsolidatedEntity LinkedGlueEntity { get; set; }
        public virtual KinesisConsolidatedEntity LinkedKinesisConsolidatedEntity { get; set; }
        public virtual ICollection<SagemakerConsolidatedEntity> LinkedSagemakerEntities { get; set; }
    }
}
