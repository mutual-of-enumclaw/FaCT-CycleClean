using System.ComponentModel;

namespace MoE.Commercial.Data.Extensions
{
	static class ConvertFromStringExtension
	{
		public static T Convert<T>(this string input)
		{
			try
			{
				var converter = TypeDescriptor.GetConverter(typeof(T));
				if (converter != null)
				{
					// Cast ConvertFromString(string text) : object to (T)
					return (T)converter.ConvertFromString(input);
				}
				return default(T);
			}
			catch (NotSupportedException)
			{
				return default(T);
			}
		}
	}
}
