﻿//HintName: TestUnion.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
public partial struct TestUnion : global::System.IEquatable<TestUnion>
{
	public const global::System.Int32 EmptyIndex = 0;
	public const global::System.Int32 CaseAIndex = 1;
	public const global::System.Int32 CaseBIndex = 2;

	[global::System.Runtime.InteropServices.FieldOffset(0)]
	public readonly global::System.Int32 CaseIndex;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public int CaseAData;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public long CaseBData;

	private TestUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public bool IsEmpty => CaseIndex == EmptyIndex;
	public bool IsCaseA => CaseIndex == CaseAIndex;
	public bool IsCaseB => CaseIndex == CaseBIndex;

	public static partial TestUnion Empty(
	)
	{
		TestUnion ___factoryReturnValue = new(EmptyIndex);
		return ___factoryReturnValue;
	}

	public static partial TestUnion CaseA(
		int @a
	)
	{
		TestUnion ___factoryReturnValue = new(CaseAIndex);
		___factoryReturnValue.CaseAData = @a;
		return ___factoryReturnValue;
	}

	public static partial TestUnion CaseB(
		long @b
	)
	{
		TestUnion ___factoryReturnValue = new(CaseBIndex);
		___factoryReturnValue.CaseBData = @b;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action @empty,
		global::System.Action<int> @caseA,
		global::System.Action<long> @caseB
	)
	{
		switch (CaseIndex)
		{
			case EmptyIndex: @empty.Invoke(); break;
			case CaseAIndex: @caseA.Invoke(CaseAData); break;
			case CaseBIndex: @caseB.Invoke(CaseBData); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<TMatchOut> @empty,
		global::System.Func<int, TMatchOut> @caseA,
		global::System.Func<long, TMatchOut> @caseB
	)
	{
		return CaseIndex switch
		{
			EmptyIndex => @empty.Invoke(),
			CaseAIndex => @caseA.Invoke(CaseAData),
			CaseBIndex => @caseB.Invoke(CaseBData),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			EmptyIndex => "Empty",
			CaseAIndex => $"CaseA {{ A = {CaseAData} }}",
			CaseBIndex => $"CaseB {{ B = {CaseBData} }}",
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
			EmptyIndex => unchecked ( EmptyIndex ),
			CaseAIndex => unchecked ( CaseAIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<int>.Default.GetHashCode(CaseAData) ),
			CaseBIndex => unchecked ( CaseBIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<long>.Default.GetHashCode(CaseBData) ),
			_ => 3,
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
			EmptyIndex => true,
			CaseAIndex => global::System.Collections.Generic.EqualityComparer<int>.Default.Equals(CaseAData, other.CaseAData),
			CaseBIndex => global::System.Collections.Generic.EqualityComparer<long>.Default.Equals(CaseBData, other.CaseBData),
			_ => true,
		};
	}

}
