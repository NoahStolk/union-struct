﻿//HintName: TestUnion.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
internal partial struct TestUnion : global::System.IEquatable<TestUnion>
{
	public const global::System.Int32 Empty1Index = 0;
	public const global::System.Int32 Empty2Index = 1;

	[global::System.Runtime.InteropServices.FieldOffset(0)]
	public readonly global::System.Int32 CaseIndex;

	private TestUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public readonly bool IsEmpty1 => CaseIndex == Empty1Index;
	public readonly bool IsEmpty2 => CaseIndex == Empty2Index;

	public static partial TestUnion Empty1(
	)
	{
		TestUnion ___factoryReturnValue = new(Empty1Index);
		return ___factoryReturnValue;
	}

	public static partial TestUnion Empty2(
	)
	{
		TestUnion ___factoryReturnValue = new(Empty2Index);
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action @empty1,
		global::System.Action @empty2
	)
	{
		switch (CaseIndex)
		{
			case Empty1Index: @empty1.Invoke(); break;
			case Empty2Index: @empty2.Invoke(); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<TMatchOut> @empty1,
		global::System.Func<TMatchOut> @empty2
	)
	{
		return CaseIndex switch
		{
			Empty1Index => @empty1.Invoke(),
			Empty2Index => @empty2.Invoke(),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			Empty1Index => "Empty1",
			Empty2Index => "Empty2",
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public static bool operator !=(TestUnion left, TestUnion right)
	{
		return !(left == right);
	}

	public static bool operator ==(TestUnion left, TestUnion right)
	{
		return left.Equals(right);
	}

	public override global::System.Int32 GetHashCode()
	{
		return CaseIndex switch
		{
			Empty1Index => unchecked ( Empty1Index ),
			Empty2Index => unchecked ( Empty2Index ),
			_ => 2,
		};
	}

	public override global::System.Boolean Equals(global::System.Object? obj)
	{
		return obj is TestUnion && Equals((TestUnion)obj);
	}

	public global::System.Boolean Equals(TestUnion other)
	{
		return CaseIndex == other.CaseIndex && CaseIndex switch
		{
			Empty1Index => true,
			Empty2Index => true,
			_ => true,
		};
	}

}
