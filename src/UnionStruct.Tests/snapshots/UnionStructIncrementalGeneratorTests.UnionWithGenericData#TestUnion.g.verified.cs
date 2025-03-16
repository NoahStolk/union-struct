﻿//HintName: TestUnion.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
internal partial struct TestUnion : global::System.IEquatable<TestUnion>
{
	public const global::System.Int32 IntIndex = 0;
	public const global::System.Int32 LongIndex = 1;

	[global::System.Runtime.InteropServices.FieldOffset(0)]
	public readonly global::System.Int32 CaseIndex;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public int? IntData;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public long? LongData;

	private TestUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public readonly bool IsInt => CaseIndex == IntIndex;
	public readonly bool IsLong => CaseIndex == LongIndex;

	public static partial TestUnion Int(
		int? @value
	)
	{
		TestUnion ___factoryReturnValue = new(IntIndex);
		___factoryReturnValue.IntData = @value;
		return ___factoryReturnValue;
	}

	public static partial TestUnion Long(
		long? @value
	)
	{
		TestUnion ___factoryReturnValue = new(LongIndex);
		___factoryReturnValue.LongData = @value;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<int?> @int,
		global::System.Action<long?> @long
	)
	{
		switch (CaseIndex)
		{
			case IntIndex: @int.Invoke(IntData); break;
			case LongIndex: @long.Invoke(LongData); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<int?, TMatchOut> @int,
		global::System.Func<long?, TMatchOut> @long
	)
	{
		return CaseIndex switch
		{
			IntIndex => @int.Invoke(IntData),
			LongIndex => @long.Invoke(LongData),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			IntIndex => $"Int {{ Value = {IntData} }}",
			LongIndex => $"Long {{ Value = {LongData} }}",
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
			IntIndex => unchecked ( IntIndex * -1521134295 + (IntData.HasValue ? global::System.Collections.Generic.EqualityComparer<int?>.Default.GetHashCode(IntData.Value) : 0) ),
			LongIndex => unchecked ( LongIndex * -1521134295 + (LongData.HasValue ? global::System.Collections.Generic.EqualityComparer<long?>.Default.GetHashCode(LongData.Value) : 0) ),
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
			IntIndex => global::System.Collections.Generic.EqualityComparer<int?>.Default.Equals(IntData, other.IntData),
			LongIndex => global::System.Collections.Generic.EqualityComparer<long?>.Default.Equals(LongData, other.LongData),
			_ => true,
		};
	}

}
