using System.Runtime.CompilerServices;

namespace UnionStruct.Tests.Integration.Unions;

/// <summary>
/// <c>Idle</c> has no fields, so <c>ToString</c> prints only its name. <c>CancellationToken</c> comes from a reference
/// assembly, where all of its fields are private; it must still print its payload.
/// </summary>
[Union]
internal partial struct Signal
{
	public partial Signal(Idle value);

	public partial Signal(CancellationToken value);

	public readonly struct Idle;
}
