﻿//HintName: TestUnion(T).g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

internal partial record struct TestUnion<T>
{
	public const global::System.Int32 IntIndex = 0;
	public const global::System.Int32 LongIndex = 1;
	public const global::System.Int32 TCaseIndex = 2;
	public const global::System.Int32 UCaseIndex = 3;

	public readonly global::System.Int32 CaseIndex;

	public int? IntData = default!;

	public long? LongData = default!;

	public T? TCaseData = default!;

	public Tests.TestUnion<T>.TestGeneric<byte, short>? UCaseData = default!;

	private TestUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public bool IsInt => CaseIndex == IntIndex;
	public bool IsLong => CaseIndex == LongIndex;
	public bool IsTCase => CaseIndex == TCaseIndex;
	public bool IsUCase => CaseIndex == UCaseIndex;

	public static partial TestUnion<T> Int(
		int? @value
	)
	{
		TestUnion<T> ___factoryReturnValue = new(IntIndex);
		___factoryReturnValue.IntData = @value;
		return ___factoryReturnValue;
	}

	public static partial TestUnion<T> Long(
		long? @value
	)
	{
		TestUnion<T> ___factoryReturnValue = new(LongIndex);
		___factoryReturnValue.LongData = @value;
		return ___factoryReturnValue;
	}

	public static partial TestUnion<T> TCase(
		T? @value
	)
	{
		TestUnion<T> ___factoryReturnValue = new(TCaseIndex);
		___factoryReturnValue.TCaseData = @value;
		return ___factoryReturnValue;
	}

	public static partial TestUnion<T> UCase(
		Tests.TestUnion<T>.TestGeneric<byte, short>? @value
	)
	{
		TestUnion<T> ___factoryReturnValue = new(UCaseIndex);
		___factoryReturnValue.UCaseData = @value;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<int?> @int,
		global::System.Action<long?> @long,
		global::System.Action<T?> @tCase,
		global::System.Action<Tests.TestUnion<T>.TestGeneric<byte, short>?> @uCase
	)
	{
		switch (CaseIndex)
		{
			case IntIndex: @int.Invoke(IntData); break;
			case LongIndex: @long.Invoke(LongData); break;
			case TCaseIndex: @tCase.Invoke(TCaseData); break;
			case UCaseIndex: @uCase.Invoke(UCaseData); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<int?, TMatchOut> @int,
		global::System.Func<long?, TMatchOut> @long,
		global::System.Func<T?, TMatchOut> @tCase,
		global::System.Func<Tests.TestUnion<T>.TestGeneric<byte, short>?, TMatchOut> @uCase
	)
	{
		return CaseIndex switch
		{
			IntIndex => @int.Invoke(IntData),
			LongIndex => @long.Invoke(LongData),
			TCaseIndex => @tCase.Invoke(TCaseData),
			UCaseIndex => @uCase.Invoke(UCaseData),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			IntIndex => $"Int {{ Value = {IntData} }}",
			LongIndex => $"Long {{ Value = {LongData} }}",
			TCaseIndex => $"TCase {{ Value = {TCaseData} }}",
			UCaseIndex => $"UCase {{ Value = {UCaseData} }}",
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

}
