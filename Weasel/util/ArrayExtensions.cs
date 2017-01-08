/// <summary>
/// Extension methods for arrays
/// </summary>
public static class ArrayExtensions
{
    /// <summary>
    /// Get the last element of the array
    /// </summary>
    public static T Last<T>(this T[] array)
    {
        return array[array.Length - 1];
    }
}
