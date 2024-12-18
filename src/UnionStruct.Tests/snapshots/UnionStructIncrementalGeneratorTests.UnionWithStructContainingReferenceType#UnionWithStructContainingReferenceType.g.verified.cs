﻿//HintName: UnionWithStructContainingReferenceType.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

internal partial struct UnionWithStructContainingReferenceType : global::System.IEquatable<UnionWithStructContainingReferenceType>
{
	public const global::System.Int32 IntIndex = 0;
	public const global::System.Int32 TextIndex = 1;

	public readonly global::System.Int32 CaseIndex;

	public int IntData;

	public TextCase TextData;

	private UnionWithStructContainingReferenceType(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public bool IsInt => CaseIndex == IntIndex;
	public bool IsText => CaseIndex == TextIndex;

	public static partial UnionWithStructContainingReferenceType Int(
		int @value
	)
	{
		UnionWithStructContainingReferenceType ___factoryReturnValue = new(IntIndex);
		___factoryReturnValue.IntData = @value;
		return ___factoryReturnValue;
	}

	public static partial UnionWithStructContainingReferenceType Text(
		char @a,
		string @b
	)
	{
		UnionWithStructContainingReferenceType ___factoryReturnValue = new(TextIndex);
		___factoryReturnValue.TextData = new(@a, @b);
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<int> @int,
		global::System.Action<char, string> @text
	)
	{
		switch (CaseIndex)
		{
			case IntIndex: @int.Invoke(IntData); break;
			case TextIndex: @text.Invoke(TextData.A, TextData.B); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<int, TMatchOut> @int,
		global::System.Func<char, string, TMatchOut> @text
	)
	{
		return CaseIndex switch
		{
			IntIndex => @int.Invoke(IntData),
			TextIndex => @text.Invoke(TextData.A, TextData.B),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			IntIndex => $"Int {{ Value = {IntData} }}",
			TextIndex => $"Text {{ A = {TextData.A}, B = {TextData.B} }}",
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public static bool operator !=(UnionWithStructContainingReferenceType left, UnionWithStructContainingReferenceType right)
	{
		return !(left == right);
	}

	public static bool operator ==(UnionWithStructContainingReferenceType left, UnionWithStructContainingReferenceType right)
	{
		return left.Equals(right);
	}

	public override global::System.Int32 GetHashCode()
	{
		return CaseIndex switch
		{
			IntIndex => unchecked ( IntIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<int>.Default.GetHashCode(IntData) ),
			TextIndex => unchecked ( TextIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<char>.Default.GetHashCode(TextData.A) * -1521134295 + global::System.Collections.Generic.EqualityComparer<string>.Default.GetHashCode(TextData.B) ),
			_ => 2,
		};
	}

	public override global::System.Boolean Equals(global::System.Object? obj)
	{
		return obj is UnionWithStructContainingReferenceType && Equals((UnionWithStructContainingReferenceType)obj);
	}

	public global::System.Boolean Equals(UnionWithStructContainingReferenceType other)
	{
		return CaseIndex == other.CaseIndex && CaseIndex switch
		{
			IntIndex => global::System.Collections.Generic.EqualityComparer<int>.Default.Equals(IntData, other.IntData),
			TextIndex => global::System.Collections.Generic.EqualityComparer<char>.Default.Equals(TextData.A, other.TextData.A) && global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(TextData.B, other.TextData.B),
			_ => true,
		};
	}

	public struct TextCase
	{
		public char A;

		public string B;

		public TextCase(
			char @a,
			string @b
		)
		{
			A = @a;
			B = @b;
		}
	}

}
