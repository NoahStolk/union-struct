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
				[UnionCase] public static partial TestUnion AngleCase(float Angle);
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

	// TODO:
	/*
- Generic union struct
- Nested union struct (cases as nested types)
- Recursive union struct (where another union struct is a case of another)
- All of the above

- Equality
- Match
- Switch
- ToString
- Cases using
  - Nullable<T>
  - Enums
  - ValueTuples
  - Properties instead of fields
	*/
}
