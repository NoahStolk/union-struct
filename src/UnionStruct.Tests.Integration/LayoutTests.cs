using System.Runtime.CompilerServices;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

public sealed class LayoutTests
{
	[Test]
	public async Task UnmanagedNonGenericUnionOverlapsPayloads()
	{
		// 4-byte CaseIndex, then the three payloads share the next 4 bytes.
		await Assert.That(Unsafe.SizeOf<CompressedIndex>()).IsEqualTo(8);
	}

	[Test]
	public async Task GenericUnionStoresPayloadsSequentially()
	{
		// CaseIndex + Circle (1 int) + Rectangle (2 ints).
		await Assert.That(Unsafe.SizeOf<Shape<int>>()).IsEqualTo(16);
	}

	[Test]
	public async Task OverlappedPayloadsShareMemory()
	{
		CompressedIndex index = 0x01020304u;

		// Deliberately memory-unsafe: the inactive cases' fields alias the active one.
		await Assert.That(index.ByteData).IsEqualTo((byte)0x04);
		await Assert.That(index.UInt16Data).IsEqualTo((ushort)0x0304);
	}
}
