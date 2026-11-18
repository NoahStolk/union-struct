using Microsoft.CodeAnalysis;
using UnionStruct.Internals.Utils;

namespace UnionStruct.Internals.Model;

/// <summary>
/// An equatable stand-in for <see cref="Diagnostic"/>, which (through its <see cref="Location"/>) holds on to syntax
/// trees and would defeat the incremental pipeline's caching.
/// </summary>
internal sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> MessageArgs)
{
	public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs)
	{
		return new DiagnosticInfo(descriptor, LocationInfo.From(location), new EquatableArray<string>([.. messageArgs]));
	}

	public Diagnostic ToDiagnostic()
	{
		return Diagnostic.Create(Descriptor, Location?.ToLocation(), MessageArgs.Cast<object>().ToArray());
	}
}
