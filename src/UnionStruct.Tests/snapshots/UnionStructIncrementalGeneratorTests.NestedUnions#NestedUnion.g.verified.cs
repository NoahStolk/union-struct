﻿//HintName: NestedUnion.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
internal partial struct NestedUnion : global::System.IEquatable<NestedUnion>
{
	public const global::System.Int32 CaseCount = 2;

	public const global::System.Int32 EmptyIndex = 0;
	public const global::System.Int32 NodeIndex = 1;

	[global::System.Runtime.InteropServices.FieldOffset(0)]
	public readonly global::System.Int32 CaseIndex;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public int NodeData;

	private NestedUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public readonly bool IsEmpty => CaseIndex == EmptyIndex;
	public readonly bool IsNode => CaseIndex == NodeIndex;

	public static global::System.ReadOnlySpan<global::System.Byte> NullTerminatedMemberNames => "Empty\0Node\0"u8;

	public static partial NestedUnion Empty(
	)
	{
		NestedUnion ___factoryReturnValue = new(EmptyIndex);
		return ___factoryReturnValue;
	}

	public static partial NestedUnion Node(
		int @value
	)
	{
		NestedUnion ___factoryReturnValue = new(NodeIndex);
		___factoryReturnValue.NodeData = @value;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action @empty,
		global::System.Action<int> @node
	)
	{
		switch (CaseIndex)
		{
			case EmptyIndex: @empty.Invoke(); break;
			case NodeIndex: @node.Invoke(NodeData); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<TMatchOut> @empty,
		global::System.Func<int, TMatchOut> @node
	)
	{
		return CaseIndex switch
		{
			EmptyIndex => @empty.Invoke(),
			NodeIndex => @node.Invoke(NodeData),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override readonly global::System.String ToString()
	{
		return CaseIndex switch
		{
			EmptyIndex => "Empty",
			NodeIndex => $"Node {{ Value = {NodeData} }}",
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public static global::System.String GetTypeString(global::System.Int32 caseIndex)
	{
		return caseIndex switch
		{
			EmptyIndex => "Empty",
			NodeIndex => "Node",
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {caseIndex}."),
		};
	}

	public readonly global::System.String GetTypeString()
	{
		return GetTypeString(CaseIndex);
	}

	public static global::System.ReadOnlySpan<global::System.Byte> GetTypeAsUtf8Span(global::System.Int32 caseIndex)
	{
		return caseIndex switch
		{
			EmptyIndex => "Empty"u8,
			NodeIndex => "Node"u8,
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {caseIndex}."),
		};
	}

	public readonly global::System.ReadOnlySpan<global::System.Byte> GetTypeAsUtf8Span()
	{
		return GetTypeAsUtf8Span(CaseIndex);
	}

	public static bool operator !=(NestedUnion left, NestedUnion right)
	{
		return !(left == right);
	}

	public static bool operator ==(NestedUnion left, NestedUnion right)
	{
		return left.Equals(right);
	}

	public override readonly global::System.Int32 GetHashCode()
	{
		return CaseIndex switch
		{
			EmptyIndex => unchecked ( EmptyIndex ),
			NodeIndex => unchecked ( NodeIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<int>.Default.GetHashCode(NodeData) ),
			_ => 2,
		};
	}

	public override readonly global::System.Boolean Equals(global::System.Object? obj)
	{
		return obj is NestedUnion && Equals((NestedUnion)obj);
	}

	public readonly global::System.Boolean Equals(NestedUnion other)
	{
		return CaseIndex == other.CaseIndex && CaseIndex switch
		{
			EmptyIndex => true,
			NodeIndex => global::System.Collections.Generic.EqualityComparer<int>.Default.Equals(NodeData, other.NodeData),
			_ => true,
		};
	}

}
