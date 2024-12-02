﻿//HintName: TestUnion.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
internal partial record struct TestUnion
{

	[global::System.Runtime.InteropServices.FieldOffset(0)]
	public readonly global::System.Int32 CaseIndex;

	private TestUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}


	public void Switch(
	)
	{
		switch (CaseIndex)
		{
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
	)
	{
		return CaseIndex switch
		{
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

}
