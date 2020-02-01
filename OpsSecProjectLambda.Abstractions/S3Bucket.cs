
namespace OpsSecProjectLambda.Abstractions
{
    public class S3Bucket
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual LogInput LinkedLogInput { get; set; }
    }
}
