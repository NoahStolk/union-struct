using UnionStruct.Tests.Utils;

namespace UnionStruct.Tests;

public sealed class UnionStructIncrementalGeneratorTests
{
	/// <summary>
	/// The user half of the golden sample <c>research/dotnet-11-unions/Samples/S07_GoldenEmission.cs</c>: every case
	/// type is unmanaged and the union is not generic, so the payloads overlap.
	/// </summary>
	[Test]
	public async Task GoldenOverlap()
	{
		const string code =
			"""
			using System.Numerics;
			using System.Runtime.CompilerServices;
			namespace Samples;
			public readonly record struct Angle(float Value);
			public readonly record struct Position(Vector3 Value);
			public readonly record struct Rotation(Quaternion Value);
			[Union]
			public partial struct Transform
			{
				public partial Transform(Angle value);

				public partial Transform(Position value);

				[UnionStruct.UnionCase(DisplayName = "Quaternion rotation")]
				public partial Transform(Rotation value);
			}
			""";

		await TestHelper.Verify(code);
	}

	/// <summary>
	/// The user half of the golden sample <c>research/dotnet-11-unions/Samples/S10_InputSurface.cs</c>: a generic union
	/// with a managed case, so the layout is sequential.
	/// </summary>
	[Test]
	public async Task GoldenSequential()
	{
		const string code =
			"""
			using System.Numerics;
			using System.Runtime.CompilerServices;
			using UnionStruct;
			namespace Samples;
			public readonly record struct Circle<T>(T Radius);
			public readonly record struct Rectangle<T>(T Width, T Height);
			public readonly record struct Label(string Text);
			[Union]
			public partial struct Shape<T> where T : INumber<T>
			{
				public partial Shape(Circle<T> value);

				public partial Shape(Rectangle<T> value);

				[UnionCase(DisplayName = "Text label")]
				public partial Shape(Label value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task ManagedNonGenericUnion()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(int value);
				public partial TestUnion(long value);
				public partial TestUnion(string value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task NullableReferenceCase()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(int value);
				public partial TestUnion(string? value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task GenericTypeParameterCases()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct Result<TValue, TError>
				where TError : notnull
			{
				public partial Result(TValue value);
				public partial Result(TError error);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task EnumLikeReadOnlyUnion()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal readonly partial struct Medal
			{
				public readonly struct Bronze;
				public readonly struct Silver;
				public readonly struct Gold;

				public partial Medal(Bronze _);
				public partial Medal(Silver _);
				public partial Medal(Gold _);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task NestedInPartialTypes()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			internal static partial class Outer
			{
				internal partial record struct Middle<T>
				{
					[Union]
					internal partial struct Inner
					{
						public partial Inner(int value);
						public partial Inner(T value);
					}
				}
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task GlobalNamespace()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(int value);
				public partial TestUnion(float value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task NestedUnions()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			public readonly record struct Empty;
			[Union]
			internal partial struct RootUnion
			{
				public partial RootUnion(Empty value);
				public partial RootUnion(NestedUnion value);
			}

			[Union]
			internal partial struct NestedUnion
			{
				public partial NestedUnion(Empty value);
				public partial NestedUnion(int value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task ArrayAndNullableValueTypeCases()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(int[] value);
				public partial TestUnion(int? value);
				public partial TestUnion(System.Collections.Generic.List<int> value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task InterfaceAndObjectCasesGetNoImplicitConversion()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				public partial TestUnion(System.IDisposable value);
				public partial TestUnion(object value);
				public partial TestUnion(int value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task ConstructorModifiersAndParameterNamesArePreserved()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			public partial struct TestUnion
			{
				public partial TestUnion(int @class);
				internal partial TestUnion(long Int64Data);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task NamedCases()
	{
		// Name tells apart cases whose types share a name, and names tuples, which have none of their own. DisplayName
		// defaults to Name and only changes strings.
		const string code =
			"""
			using System.Runtime.CompilerServices;
			using UnionStruct;
			namespace Tests;
			public readonly record struct Circle<T>(T Radius);
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase(Name = "IntCircle")] public partial TestUnion(Circle<int> value);
				[UnionCase(Name = "FloatCircle", DisplayName = "Circle (float)")] public partial TestUnion(Circle<float> value);
				[UnionCase(Name = "Point")] public partial TestUnion((int X, int Y) value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task FieldlessCasesPrintOnlyTheirName()
	{
		// Marker, Tag and OnlyStatic have no instance fields, so ToString prints just the (escaped) name. An auto-property,
		// a field-like event and a captured primary constructor parameter all hide a field. A partial struct may get
		// fields from a generator (a nested union does), and primitives and type parameters always print their payload.
		const string code =
			"""
			using System.Runtime.CompilerServices;
			using UnionStruct;
			namespace Tests;
			public struct Marker;
			public readonly record struct Tag;
			public struct OnlyStatic { public static int Count; }
			public struct WithAutoProperty { public int X { get; set; } }
			public struct WithEvent { public event System.Action? Changed; public readonly void Raise() => Changed?.Invoke(); }
			public struct WithCapturedParameter(int x) { public readonly int X => x; }
			public partial struct PartialEmpty;
			[Union]
			internal partial struct TestUnion<T>
			{
				public partial TestUnion(Marker value);
				[UnionCase(DisplayName = "Quoted \"tag\" {}")] public partial TestUnion(Tag value);
				public partial TestUnion(OnlyStatic value);
				public partial TestUnion(WithAutoProperty value);
				public partial TestUnion(WithEvent value);
				public partial TestUnion(WithCapturedParameter value);
				public partial TestUnion(PartialEmpty value);
				public partial TestUnion(int value);
				public partial TestUnion(T value);
			}
			""";

		await TestHelper.Verify(code);
	}

	[Test]
	public async Task DisplayNamesAreEscaped()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			using UnionStruct;
			namespace Tests;
			[Union]
			internal partial struct TestUnion
			{
				[UnionCase(DisplayName = "Quoted \"name\"")] public partial TestUnion(int value);
				[UnionCase(DisplayName = @"Braces {} and \backslash")] public partial TestUnion(long value);
			}
			""";

		await TestHelper.Verify(code);
	}

	/// <summary>
	/// The generator triggers on the BCL <c>[Union]</c>, which hand-written unions carry too. Without partial
	/// constructors there is nothing to complete, so it must stay silent.
	/// </summary>
	[Test]
	public async Task HandWrittenUnionIsIgnored()
	{
		const string code =
			"""
			using System.Runtime.CompilerServices;
			namespace Tests;
			[Union]
			internal struct HandWritten : IUnion
			{
				private readonly object? _value;
				public HandWritten(int value) => _value = value;
				public HandWritten(string value) => _value = value;
				public object? Value => _value;
			}
			""";

		await TestHelper.Verify(code);
	}
}
