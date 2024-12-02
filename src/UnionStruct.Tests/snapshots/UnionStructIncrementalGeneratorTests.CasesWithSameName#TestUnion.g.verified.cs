﻿//HintName: TestUnion.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
internal partial record struct TestUnion
{
	public const global::System.Int32 IntIndex = 0;
	public const global::System.Int32 LongIndex = 1;

	[global::System.Runtime.InteropServices.FieldOffset(0)]
	public readonly global::System.Int32 CaseIndex;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public System.Int32 IntData;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public System.Int64 LongData;

	private TestUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public bool IsInt => CaseIndex == IntIndex;
	public bool IsLong => CaseIndex == LongIndex;

	public static partial TestUnion Int(
		System.Int32 @value
	)
	{
		TestUnion ___factoryReturnValue = new(IntIndex);
		___factoryReturnValue.IntData = @value;
		return ___factoryReturnValue;
	}

	public static partial TestUnion Long(
		System.Int64 @value
	)
	{
		TestUnion ___factoryReturnValue = new(LongIndex);
		___factoryReturnValue.LongData = @value;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<System.Int32> @int,
		global::System.Action<System.Int64> @long
	)
	{
		switch (CaseIndex)
		{
			case IntIndex: @int.Invoke(IntData); break;
			case LongIndex: @long.Invoke(LongData); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public T Match<T>(
		global::System.Func<System.Int32, T> @int,
		global::System.Func<System.Int64, T> @long
	)
	{
		return CaseIndex switch
		{
			IntIndex => @int.Invoke(IntData),
			LongIndex => @long.Invoke(LongData),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.")
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			IntIndex => IntData.ToString(),
			LongIndex => LongData.ToString(),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.")
		};
	}

}
