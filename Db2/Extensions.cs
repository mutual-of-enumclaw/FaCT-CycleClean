using System.ComponentModel;

namespace MoE.Commercial.Data.Extensions
{
    /// <summary>
    /// Provides extension methods for converting string values to other types.
    /// </summary>
    internal static class ConvertFromStringExtension
    {
        /// <summary>
        /// Converts a string value to the specified type using TypeConverter.
        /// </summary>
        /// <typeparam name="T">The target type to convert to</typeparam>
        /// <param name="input">The string value to convert</param>
        /// <returns>The converted value of type T, or default(T) if conversion fails</returns>
        /// <remarks>
        /// This method uses the TypeDescriptor.GetConverter method to find an appropriate
        /// type converter for the target type. If no converter is available or the conversion
        /// fails, the method returns the default value for type T.
        /// </remarks>
        public static T Convert<T>(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return default(T)!;

            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter?.CanConvertFrom(typeof(string)) == true)
                {
                    // Cast ConvertFromString(string text) : object to (T)
                    var result = converter.ConvertFromString(input);
                    return result is T converted ? converted : default(T)!;
                }
                return default(T)!;
            }
            catch (NotSupportedException)
            {
                return default(T)!;
            }
            catch (FormatException)
            {
                return default(T)!;
            }
            catch (OverflowException)
            {
                return default(T)!;
            }
        }
    }
}
