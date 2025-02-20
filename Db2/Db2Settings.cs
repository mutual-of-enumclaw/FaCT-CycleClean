namespace MoE.Commercial.Data.Db2
{
	/// <summary>
	/// Used to configure Db2DataProvider
	/// </summary>
	public class Db2Settings
	{
		public string Name { get; set; }

		public string ConnectionString { get; set; }

		public string Schema { get; set; }
	}
}