using System.Data;
using System.Data.Common;

namespace MoE.Commercial.Data
{
	public class GenericDbParameter : DbParameter
	{
		public GenericDbParameter(string name, object value)
		{
			ParameterName = name;
			Value = value;
		}

		public GenericDbParameter(string name, object value, DbType dbType)
			: this(name, value)
		{
			DbType = dbType;
		}

		public GenericDbParameter(string name, object value, DbType dbType, int size)
			: this(name, value, dbType)
		{
			Size = size;
		}

		public GenericDbParameter(
			string name,
			object value,
			DbType dbType,
			byte precision,
			byte scale
		)
			: this(name, value, dbType)
		{
			Precision = precision;
			Scale = scale;
		}

		public override DbType DbType { get; set; }

		public override ParameterDirection Direction { get; set; }

		public override bool IsNullable { get; set; }

		public override string ParameterName { get; set; }

		public override int Size { get; set; }

		public override string SourceColumn { get; set; }

		public override bool SourceColumnNullMapping { get; set; }

		public override object Value { get; set; }

		public override void ResetDbType()
		{
			throw new NotImplementedException();
		}
	}
}