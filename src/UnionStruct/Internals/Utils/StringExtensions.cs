using System.Globalization;

namespace UnionStruct.Internals.Utils;

internal static class StringExtensions
{
	public static string FirstCharToLowerCase(this string str)
	{
		if (string.IsNullOrEmpty(str))
			return string.Empty;

		if (char.IsUpper(str[0]))
			return str.Length == 1 ? char.ToLower(str[0], CultureInfo.InvariantCulture).ToString() : char.ToLower(str[0], CultureInfo.InvariantCulture) + str.Substring(1);

		return str;
	}

	public static string FirstCharToUpperCase(this string str)
	{
		if (string.IsNullOrEmpty(str))
			return string.Empty;

		if (char.IsLower(str[0]))
			return str.Length == 1 ? char.ToUpper(str[0], CultureInfo.InvariantCulture).ToString() : char.ToUpper(str[0], CultureInfo.InvariantCulture) + str.Substring(1);

		return str;
	}
}
