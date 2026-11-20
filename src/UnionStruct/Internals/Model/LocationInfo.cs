using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace UnionStruct.Internals.Model;

internal sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
{
	public static LocationInfo? From(Location? location)
	{
		if (location?.SourceTree is null)
			return null;

		return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
	}

	public Location ToLocation()
	{
		return Location.Create(FilePath, TextSpan, LineSpan);
	}
}
