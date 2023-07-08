#pragma warning disable AV1008 // Class should not be static
#pragma warning disable AV1553 // Do not use optional parameters with default value null for strings, collections or tasks

namespace JsonApiDotNetCore.ExtendedQuery;

internal static class ObjectExtensions
{
    public static IEnumerable<T> AsEnumerable<T>(this T element)
    {
        yield return element;
    }

    public static T[] AsArray<T>(this T element)
    {
        return new[]
        {
            element
        };
    }

    public static List<T> AsList<T>(this T element)
    {
        return new List<T>
        {
            element
        };
    }

    public static HashSet<T> AsHashSet<T>(this T element)
    {
        return new HashSet<T>
        {
            element
        };
    }
}
