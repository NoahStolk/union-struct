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
			internal partial record struct TestUnion
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
			internal partial record struct TestUnion
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
			internal partial record struct TestUnion;
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
			internal partial record struct TestUnion
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
			public partial record struct TestUnion
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
			partial record struct TestUnion
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
			internal partial record struct TestUnion
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
			internal partial record struct Shape<T>
				where T : INumber<T>
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
			internal partial record struct Shape<T>
				where T : struct, INumber<T>
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
			internal partial record struct TestUnion
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
			internal partial record struct TestUnion<T>
			{
				[UnionCase] public static partial TestUnion Int(System.Nullable<int> value);
				[UnionCase] public static partial TestUnion Long(System.Nullable<long> value);
				[UnionCase] public static partial TestUnion TCase(System.Nullable<T> value);
				[UnionCase] public static partial TestUnion UCase(System.Nullable<TestGeneric<byte, short>> value);

				internal record struct TestGeneric<T1, T2>(T1 A, T2 B);
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
			internal partial record struct ComplexUnion<T1, T2>
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
}
