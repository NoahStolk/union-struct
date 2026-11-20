using System.Collections;
using System.Collections.Immutable;

namespace UnionStruct.Internals.Utils;

/// <summary>
/// An immutable array with value equality, so that models containing collections still compare equal across generator
/// runs and the incremental pipeline can cache them.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
internal readonly struct EquatableArray<T>(ImmutableArray<T> array) : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
	where T : IEquatable<T>
{
	private readonly ImmutableArray<T> _array = array;

	public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

	public int Count => _array.IsDefault ? 0 : _array.Length;

	public T this[int index] => _array[index];

	public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
	{
		return !left.Equals(right);
	}

	public bool Equals(EquatableArray<T> other)
	{
		if (Count != other.Count)
			return false;

		for (int i = 0; i < Count; i++)
		{
			if (!_array[i].Equals(other._array[i]))
				return false;
		}

		return true;
	}

	public override bool Equals(object? obj)
	{
		return obj is EquatableArray<T> other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hash = 17;
			for (int i = 0; i < Count; i++)
				hash = (hash * 31) + _array[i].GetHashCode();

			return hash;
		}
	}

	public IEnumerator<T> GetEnumerator()
	{
		return ((IEnumerable<T>)(_array.IsDefault ? ImmutableArray<T>.Empty : _array)).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
