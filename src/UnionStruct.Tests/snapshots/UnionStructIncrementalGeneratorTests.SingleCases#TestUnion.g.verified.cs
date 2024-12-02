﻿//HintName: TestUnion.g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]
internal partial record struct TestUnion
{
	public const global::System.Int32 AngleCaseIndex = 0;
	public const global::System.Int32 PositionCaseIndex = 1;
	public const global::System.Int32 RotationCaseIndex = 2;

	[global::System.Runtime.InteropServices.FieldOffset(0)]
	public readonly global::System.Int32 CaseIndex;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public System.Single AngleCaseData;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public System.Numerics.Vector3 PositionCaseData;

	[global::System.Runtime.InteropServices.FieldOffset(4)]
	public System.Numerics.Quaternion RotationCaseData;

	private TestUnion(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public bool IsAngleCase => CaseIndex == AngleCaseIndex;
	public bool IsPositionCase => CaseIndex == PositionCaseIndex;
	public bool IsRotationCase => CaseIndex == RotationCaseIndex;

	public static partial TestUnion AngleCase(
		System.Single @angle
	)
	{
		TestUnion ___factoryReturnValue = new(AngleCaseIndex);
		___factoryReturnValue.AngleCaseData = @angle;
		return ___factoryReturnValue;
	}

	public static partial TestUnion PositionCase(
		System.Numerics.Vector3 @position
	)
	{
		TestUnion ___factoryReturnValue = new(PositionCaseIndex);
		___factoryReturnValue.PositionCaseData = @position;
		return ___factoryReturnValue;
	}

	public static partial TestUnion RotationCase(
		System.Numerics.Quaternion @rotation
	)
	{
		TestUnion ___factoryReturnValue = new(RotationCaseIndex);
		___factoryReturnValue.RotationCaseData = @rotation;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<System.Single> @angleCase,
		global::System.Action<System.Numerics.Vector3> @positionCase,
		global::System.Action<System.Numerics.Quaternion> @rotationCase
	)
	{
		switch (CaseIndex)
		{
			case AngleCaseIndex: @angleCase.Invoke(AngleCaseData); break;
			case PositionCaseIndex: @positionCase.Invoke(PositionCaseData); break;
			case RotationCaseIndex: @rotationCase.Invoke(RotationCaseData); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<System.Single, TMatchOut> @angleCase,
		global::System.Func<System.Numerics.Vector3, TMatchOut> @positionCase,
		global::System.Func<System.Numerics.Quaternion, TMatchOut> @rotationCase
	)
	{
		return CaseIndex switch
		{
			AngleCaseIndex => @angleCase.Invoke(AngleCaseData),
			PositionCaseIndex => @positionCase.Invoke(PositionCaseData),
			RotationCaseIndex => @rotationCase.Invoke(RotationCaseData),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			AngleCaseIndex => AngleCaseData.ToString() ?? string.Empty,
			PositionCaseIndex => PositionCaseData.ToString() ?? string.Empty,
			RotationCaseIndex => RotationCaseData.ToString() ?? string.Empty,
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

}
