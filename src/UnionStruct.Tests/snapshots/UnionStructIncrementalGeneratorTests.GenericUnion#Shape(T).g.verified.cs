﻿//HintName: Shape(T).g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

internal partial record struct Shape<T>
{
	public const global::System.Int32 CircleIndex = 0;
	public const global::System.Int32 RectangleIndex = 1;

	public readonly global::System.Int32 CaseIndex;

	public T? CircleData = default!;

	public RectangleCase RectangleData = default!;

	private Shape(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public bool IsCircle => CaseIndex == CircleIndex;
	public bool IsRectangle => CaseIndex == RectangleIndex;

	public static partial Shape<T> Circle(
		T? @radius
	)
	{
		Shape<T> ___factoryReturnValue = new(CircleIndex);
		___factoryReturnValue.CircleData = @radius;
		return ___factoryReturnValue;
	}

	public static partial Shape<T> Rectangle(
		T? @width,
		T? @height
	)
	{
		Shape<T> ___factoryReturnValue = new(RectangleIndex);
		___factoryReturnValue.RectangleData.Width = @width;
		___factoryReturnValue.RectangleData.Height = @height;
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<T?> @circle,
		global::System.Action<T?, T?> @rectangle
	)
	{
		switch (CaseIndex)
		{
			case CircleIndex: @circle.Invoke(CircleData); break;
			case RectangleIndex: @rectangle.Invoke(RectangleData.Width, RectangleData.Height); break;
			default: throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}.");
		}
	}

	public TMatchOut Match<TMatchOut>(
		global::System.Func<T?, TMatchOut> @circle,
		global::System.Func<T?, T?, TMatchOut> @rectangle
	)
	{
		return CaseIndex switch
		{
			CircleIndex => @circle.Invoke(CircleData),
			RectangleIndex => @rectangle.Invoke(RectangleData.Width, RectangleData.Height),
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public override global::System.String ToString()
	{
		return CaseIndex switch
		{
			CircleIndex => $"Circle {{ Radius = {CircleData} }}",
			RectangleIndex => $"Rectangle {{ Width = {RectangleData.Width}, Height = {RectangleData.Height} }}",
			_ => throw new global::System.Diagnostics.UnreachableException($"Invalid case index: {CaseIndex}."),
		};
	}

	public struct RectangleCase
	{
		public T? Width;

		public T? Height;

	}

}
