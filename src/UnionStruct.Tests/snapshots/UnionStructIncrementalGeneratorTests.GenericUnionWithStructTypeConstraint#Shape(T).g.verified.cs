﻿//HintName: Shape(T).g.cs
// <auto-generated>
// This code was generated by UnionStruct.
// </auto-generated>

#nullable enable

namespace Tests;

internal partial struct Shape<T> : global::System.IEquatable<Shape<T>>
{
	public const global::System.Int32 CircleIndex = 0;
	public const global::System.Int32 RectangleIndex = 1;

	public readonly global::System.Int32 CaseIndex;

	public T CircleData;

	public RectangleCase RectangleData;

	private Shape(global::System.Int32 caseIndex)
	{
		CaseIndex = caseIndex;
	}

	public readonly bool IsCircle => CaseIndex == CircleIndex;
	public readonly bool IsRectangle => CaseIndex == RectangleIndex;

	public static partial Shape<T> Circle(
		T @radius
	)
	{
		Shape<T> ___factoryReturnValue = new(CircleIndex);
		___factoryReturnValue.CircleData = @radius;
		return ___factoryReturnValue;
	}

	public static partial Shape<T> Rectangle(
		T @width,
		T @height
	)
	{
		Shape<T> ___factoryReturnValue = new(RectangleIndex);
		___factoryReturnValue.RectangleData = new(@width, @height);
		return ___factoryReturnValue;
	}

	public void Switch(
		global::System.Action<T> @circle,
		global::System.Action<T, T> @rectangle
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
		global::System.Func<T, TMatchOut> @circle,
		global::System.Func<T, T, TMatchOut> @rectangle
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

	public static bool operator !=(Shape<T> left, Shape<T> right)
	{
		return !(left == right);
	}

	public static bool operator ==(Shape<T> left, Shape<T> right)
	{
		return left.Equals(right);
	}

	public override global::System.Int32 GetHashCode()
	{
		return CaseIndex switch
		{
			CircleIndex => unchecked ( CircleIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<T>.Default.GetHashCode(CircleData) ),
			RectangleIndex => unchecked ( RectangleIndex * -1521134295 + global::System.Collections.Generic.EqualityComparer<T>.Default.GetHashCode(RectangleData.Width) * -1521134295 + global::System.Collections.Generic.EqualityComparer<T>.Default.GetHashCode(RectangleData.Height) ),
			_ => 2,
		};
	}

	public override global::System.Boolean Equals(global::System.Object? obj)
	{
		return obj is Shape<T> && Equals((Shape<T>)obj);
	}

	public global::System.Boolean Equals(Shape<T> other)
	{
		return CaseIndex == other.CaseIndex && CaseIndex switch
		{
			CircleIndex => global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(CircleData, other.CircleData),
			RectangleIndex => global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(RectangleData.Width, other.RectangleData.Width) && global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(RectangleData.Height, other.RectangleData.Height),
			_ => true,
		};
	}

	public struct RectangleCase
	{
		public T Width;

		public T Height;

		public RectangleCase(
			T @width,
			T @height
		)
		{
			Width = @width;
			Height = @height;
		}
	}

}
