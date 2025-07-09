namespace MoE.Commercial.Data
{
    public class Db2Settings
    {
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string Schema { get; set; }
        public CommFrameworkSettings CommFramework { get; set; }
    }

    public class CommFrameworkSettings
    {
        public string RequestUri { get; set; }
    }
}
