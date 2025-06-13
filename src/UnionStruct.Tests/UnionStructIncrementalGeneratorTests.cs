using UnionStruct.Tests.Utils;

namespace UnionStruct.Tests;

public sealed class UnionStructIncrementalGeneratorTests
{
	[Fact]
	public async Task EmptySingleMulti()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion Empty();
				[UnionCase] public static partial TestUnion PositionCase(System.Numerics.Vector3 position);
				[UnionCase] public static partial TestUnion MultiCase(System.Numerics.Vector3 position, System.Numerics.Vector3 velocity);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task SingleCases()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion AngleCase(float angle);
				[UnionCase] public static partial TestUnion PositionCase(System.Numerics.Vector3 position);
				[UnionCase] public static partial TestUnion RotationCase(System.Numerics.Quaternion rotation);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task EmptyUnion()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion;
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task MultipleEmptyCases()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion Empty1();
				[UnionCase] public static partial TestUnion Empty2();
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task PublicUnion()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			public partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion Empty();
				[UnionCase] public static partial TestUnion CaseA(int A);
				[UnionCase] public static partial TestUnion CaseB(long B);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithoutAccessibility()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion Empty();
				[UnionCase] public static partial TestUnion CaseA(int A);
				[UnionCase] public static partial TestUnion CaseB(long B);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task CasesWithSameName()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion Int(int value);
				[UnionCase] public static partial TestUnion Long(long value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task GenericUnion()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct Shape<T>
				where T : System.Numerics.INumber<T>
			{
				[UnionCase] public static partial Shape<T> Circle(T radius);
				[UnionCase] public static partial Shape<T> Rectangle(T width, T height);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task GenericUnionWithStructTypeConstraint()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct Shape<T>
				where T : struct, System.Numerics.INumber<T>
			{
				[UnionCase] public static partial Shape<T> Circle(T radius);
				[UnionCase] public static partial Shape<T> Rectangle(T width, T height);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithGenericData()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase] public static partial TestUnion Int(System.Nullable<int> value);
				[UnionCase] public static partial TestUnion Long(System.Nullable<long> value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task GenericUnionWithGenericData()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion<T>
			{
				[UnionCase] public static partial TestUnion<T> Int(System.Nullable<int> value);
				[UnionCase] public static partial TestUnion<T> Long(System.Nullable<long> value);
				[UnionCase] public static partial TestUnion<T> TCase(T value);
				[UnionCase] public static partial TestUnion<T> UCase(System.Nullable<TestGeneric<byte, short>> value);

				internal record struct TestGeneric<T1, T2>(T1 A, T2 B);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task GenericUnionWithNullableOfT()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion<T> where T : struct
			{
				[UnionCase] public static partial TestUnion<T> Empty();
				[UnionCase] public static partial TestUnion<T> Nullable(System.Nullable<T> value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task ComplexUnion()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct ComplexUnion<T1, T2>
			{
				[UnionCase] public static partial ComplexUnion<T1, T2> Int(int? value);
				[UnionCase] public static partial ComplexUnion<T1, T2> Long(long? value);
				[UnionCase] public static partial ComplexUnion<T1, T2> TCase(T1? value);
				[UnionCase] public static partial ComplexUnion<T1, T2> UCase(TestGeneric<byte, short>? value, T1 a, T2 b);
				[UnionCase] public static partial ComplexUnion<T1, T2> UCaseNested(TestGenericNested<byte, short>? value, T1 a, T2 b);

				internal record struct TestGenericNested<T3, T4>(T3 A, T4 B);
			}

			internal record struct TestGeneric<T3, T4>(T3 A, T4 B);
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task NestedUnions()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct RootUnion
			{
				[UnionCase] public static partial RootUnion Empty();
				[UnionCase] public static partial RootUnion NestedCase(NestedUnion value);
			}

			[Union]
			internal partial struct NestedUnion
			{
				[UnionCase] public static partial NestedUnion Empty();
				[UnionCase] public static partial NestedUnion Node(int value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithReferenceType()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct UnionWithReferenceType
			{
				[UnionCase] public static partial UnionWithReferenceType Int(int value);
				[UnionCase] public static partial UnionWithReferenceType String(string value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithNullableReferenceType()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct UnionWithNullableReferenceType
			{
				[UnionCase] public static partial UnionWithNullableReferenceType Int(int value);
				[UnionCase] public static partial UnionWithNullableReferenceType String(string? nullableString);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithStructContainingReferenceType()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct UnionWithStructContainingReferenceType
			{
				[UnionCase] public static partial UnionWithStructContainingReferenceType Int(int value);
				[UnionCase] public static partial UnionWithStructContainingReferenceType Text(char a, string b);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithStructContainingNullableReferenceType()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct UnionWithStructContainingNullableReferenceType
			{
				[UnionCase] public static partial UnionWithStructContainingNullableReferenceType Int(int value);
				[UnionCase] public static partial UnionWithStructContainingNullableReferenceType Text(char a, string? b);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithOptionalParameters()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct UnionWithOptionalParameters
			{
				[UnionCase] public static partial UnionWithOptionalParameters Int(int value = 16);
				[UnionCase] public static partial UnionWithOptionalParameters Text(string b = "default");
			}
			""";

		await TestHelper.Verify(code);
	}

	[Fact]
	public async Task UnionWithDisplayNames()
	{
		const string code =
			"""
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct UnionWithDisplayNames
			{
				[UnionCase] public static partial UnionWithDisplayNames None();

				[UnionCase(DisplayName = "Unsigned 8-bit")] public static partial UnionWithDisplayNames Unsigned8(byte value);
				[UnionCase(DisplayName = "Unsigned 16-bit")] public static partial UnionWithDisplayNames Unsigned16(ushort value);
				[UnionCase(DisplayName = "Unsigned 32-bit")] public static partial UnionWithDisplayNames Unsigned32(uint value);
				[UnionCase(DisplayName = "Unsigned 64-bit")] public static partial UnionWithDisplayNames Unsigned64(ulong value);

				[UnionCase(DisplayName = "Signed 8-bit")] public static partial UnionWithDisplayNames Signed8(sbyte value);
				[UnionCase(DisplayName = "Signed 16-bit")] public static partial UnionWithDisplayNames Signed16(short value);
				[UnionCase(DisplayName = "Signed 32-bit")] public static partial UnionWithDisplayNames Signed32(int value);
				[UnionCase(DisplayName = "Signed 64-bit")] public static partial UnionWithDisplayNames Signed64(long value);
			}
			""";

		await TestHelper.Verify(code);
	}
}
