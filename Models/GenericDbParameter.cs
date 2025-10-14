using System.Data;
using System.Data.Common;

namespace MoE.Commercial.Data
{
    /// <summary>
    /// A generic database parameter implementation that can be used with different database providers.
    /// This class provides a unified way to handle database parameters across different data access technologies.
    /// </summary>
    public class GenericDbParameter : DbParameter
    {
        /// <summary>
        /// Initializes a new instance of the GenericDbParameter class with a name and value.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="value">The parameter value</param>
        public GenericDbParameter(string name, object value)
        {
            ParameterName = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the GenericDbParameter class with a name, value, and database type.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="value">The parameter value</param>
        /// <param name="dbType">The database type of the parameter</param>
        public GenericDbParameter(string name, object value, DbType dbType)
            : this(name, value)
        {
            DbType = dbType;
        }

        /// <summary>
        /// Initializes a new instance of the GenericDbParameter class with a name, value, database type, and size.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="value">The parameter value</param>
        /// <param name="dbType">The database type of the parameter</param>
        /// <param name="size">The size of the parameter</param>
        public GenericDbParameter(string name, object value, DbType dbType, int size)
            : this(name, value, dbType)
        {
            Size = size;
        }

        /// <summary>
        /// Initializes a new instance of the GenericDbParameter class with a name, value, database type, precision, and scale.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="value">The parameter value</param>
        /// <param name="dbType">The database type of the parameter</param>
        /// <param name="precision">The precision of the parameter</param>
        /// <param name="scale">The scale of the parameter</param>
        public GenericDbParameter(
            string name,
            object value,
            DbType dbType,
            byte precision,
            byte scale)
            : this(name, value, dbType)
        {
            Precision = precision;
            Scale = scale;
        }

        /// <summary>
        /// Gets or sets the database type of the parameter.
        /// </summary>
        public override DbType DbType { get; set; }

        /// <summary>
        /// Gets or sets the direction of the parameter (Input, Output, InputOutput, or ReturnValue).
        /// </summary>
        public override ParameterDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter accepts null values.
        /// </summary>
        public override bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        public override string ParameterName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the parameter.
        /// </summary>
        public override int Size { get; set; }

        /// <summary>
        /// Gets or sets the name of the source column.
        /// </summary>
        public override string SourceColumn { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the source column is nullable.
        /// </summary>
        public override bool SourceColumnNullMapping { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter.
        /// </summary>
        public override object? Value { get; set; }

        /// <summary>
        /// Resets the database type of the parameter to its original value.
        /// This method is not implemented for GenericDbParameter.
        /// </summary>
        /// <exception cref="NotImplementedException">This method is not implemented</exception>
        public override void ResetDbType()
        {
            throw new NotImplementedException("ResetDbType is not implemented for GenericDbParameter");
        }
    }
}