// File: ArrayExtensions.cs
using System;
using System.Linq;

public static class ArrayExtensions
{
    public static T[] AppendIfMissing<T>(this T[] array, T item)
    {
        if (array == null)
            return new[] { item };

        if (!array.Contains(item))
            return array.Concat(new[] { item }).ToArray();

        return array;
    }
}
