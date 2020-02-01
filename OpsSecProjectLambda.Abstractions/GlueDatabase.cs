using System.Collections.Generic;

namespace OpsSecProjectLambda.Abstractions
{
    public class GlueDatabase
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public virtual ICollection<GlueDatabaseTable> Tables { get; set; }
    }
}
