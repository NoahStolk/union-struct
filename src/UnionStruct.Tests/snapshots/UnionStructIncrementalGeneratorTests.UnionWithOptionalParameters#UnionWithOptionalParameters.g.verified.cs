﻿//HintName: UnionWithOptionalParameters.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

internal partial struct UnionWithOptionalParameters : global::System.IEquatable<UnionWithOptionalParameters>
{
	public const global::System.Int32 IntIndex = 0;
	public const global::System.Int32 TextIndex = 1;

	public readonly global::System.Int32 CaseIndex;

	public int IntData;

	public string TextData = null!;

	private UnionWithOptionalParameters(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public bool IsInt => CaseIndex == IntIndex;
	public bool IsText => CaseIndex == TextIndex;

	public static partial UnionWithOptionalParameters Int(
		int @value
	)
	{
		UnionWithOptionalParameters ___factoryReturnValue = new(IntIndex);
		___factoryReturnValue.IntData = @value;
		return ___factoryReturnValue;
	}

	public static partial UnionWithOptionalParameters Text(
		string @b
	)
	{
		UnionWithOptionalParameters ___factoryReturnValue = new(TextIndex);
		___factoryReturnValue.TextData = @b;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<int> @int,
		global::System.Action<string> @text
	)
	{
		switch (CaseIndex)
		{
			case IntIndex: @int.Invoke(IntData); break;
			case TextIndex: @text.Invoke(TextData); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<int, TMatchOut> @int,
		global::System.Func<string, TMatchOut> @text
	)
	{
		return CaseIndex switch
		{
			IntIndex => @int.Invoke(IntData),
			TextIndex => @text.Invoke(TextData),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			IntIndex => $"Int {{ Value = {IntData} }}",
			TextIndex => $"Text {{ B = {TextData} }}",
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public static bool operator !=(UnionWithOptionalParameters left, UnionWithOptionalParameters right)
	{
		return !(left == right);
	}

	public static bool operator ==(UnionWithOptionalParameters left, UnionWithOptionalParameters right)
	{
		return left.Equals(right);
	}

	public override global::System.Int32 GetHashCode()
	{
		return CaseIndex switch
		{
			IntIndex => unchecked ( IntIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<int>.Default.GetHashCode(IntData) ),
			TextIndex => unchecked ( TextIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<string>.Default.GetHashCode(TextData) ),
			_ => 2,
		};
	}

	public override global::System.Boolean Equals(global::System.Object? obj)
	{
		return obj is UnionWithOptionalParameters && Equals((UnionWithOptionalParameters)obj);
	}

	public global::System.Boolean Equals(UnionWithOptionalParameters other)
	{
		return CaseIndex == other.CaseIndex && CaseIndex switch
		{
			IntIndex => global::System.Collections.Generic.EqualityComparer<int>.Default.Equals(IntData, other.IntData),
			TextIndex => global::System.Collections.Generic.EqualityComparer<string>.Default.Equals(TextData, other.TextData),
			_ => true,
		};
	}

}
