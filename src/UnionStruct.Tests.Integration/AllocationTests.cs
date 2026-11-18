using System.Numerics;
using System.Runtime.CompilerServices;
using UnionStruct.Tests.Integration.Unions;

namespace UnionStruct.Tests.Integration;

/// <summary>
/// Regression test for the library's core promise. A canonical (object-backed) union boxes every value-type case; the
/// generated unions must construct and match without allocating, which only holds as long as pattern matching keeps
/// going through the generated <c>TryGetValue</c> overloads instead of <c>Value</c>.
/// </summary>
public sealed class AllocationTests
{
	private const int _iterations = 100_000;

	[Test]
	public async Task ConstructAndMatchDoesNotAllocate()
	{
		// Measured in synchronous helpers: the async test method's own state machine may allocate.
		await Assert.That(MeasureOverlapped()).IsEqualTo(0);
		await Assert.That(MeasureGeneric<float>()).IsEqualTo(0);
		await Assert.That(MeasureManaged()).IsEqualTo(0);
	}

	private static long MeasureOverlapped()
	{
		_ = MatchOverlapped(1000); // Warm up the whole loop method.
		long before = GC.GetAllocatedBytesForCurrentThread();
		_ = MatchOverlapped(_iterations);
		return GC.GetAllocatedBytesForCurrentThread() - before;
	}

	private static long MeasureGeneric<T>()
		where T : INumber<T>
	{
		_ = MatchGeneric<T>(1000);
		long before = GC.GetAllocatedBytesForCurrentThread();
		_ = MatchGeneric<T>(_iterations);
		return GC.GetAllocatedBytesForCurrentThread() - before;
	}

	private static long MeasureManaged()
	{
		_ = MatchManaged(1000);
		long before = GC.GetAllocatedBytesForCurrentThread();
		_ = MatchManaged(_iterations);
		return GC.GetAllocatedBytesForCurrentThread() - before;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static long MatchOverlapped(int n)
	{
		long acc = 0;
		for (int i = 0; i < n; i++)
		{
			CompressedIndex index = (i % 3) switch
			{
				0 => (byte)i,
				1 => (ushort)i,
				_ => (uint)i,
			};

			acc += index switch
			{
				byte b => b,
				ushort s => s,
				uint u => u,
			};
		}

		return acc;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static T MatchGeneric<T>(int n)
		where T : INumber<T>
	{
		T acc = T.Zero;
		for (int i = 0; i < n; i++)
		{
			Shape<T> shape = (i & 1) == 0 ? new Circle<T>(T.One) : new Rectangle<T>(T.One, T.One);
			acc += shape switch
			{
				Circle<T> c => c.Radius,
				Rectangle<T> r => r.Width * r.Height,
			};
		}

		return acc;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int MatchManaged(int n)
	{
		const string text = "text";
		int acc = 0;
		for (int i = 0; i < n; i++)
		{
			UnionWithReferenceType union = (i & 1) == 0 ? i : text;
#pragma warning disable CA1508 // The dataflow analysis does not know that a union's type patterns test the active case.
			acc += union switch
			{
				int value => value,
				string s => s.Length,
			};
#pragma warning restore CA1508
		}

		return acc;
	}
}
