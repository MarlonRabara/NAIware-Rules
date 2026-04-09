namespace NAIware.Core.Collections;

/// <summary>
/// A class that represents an extended array list with custom search and sort capabilities.
/// </summary>
public class ArrayList : System.Collections.ArrayList
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayList"/> class.
    /// </summary>
    public ArrayList() : base() { }

    #region Static Methods

    /// <summary>
    /// Gets a boolean that indicates whether an object is contained within the array list.
    /// </summary>
    /// <param name="arraylist">The array list to search.</param>
    /// <param name="obj">The object to search for.</param>
    /// <returns><c>true</c> if contained; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// Uses a comparer that will check internal content for equality if object references are not equal.
    /// </remarks>
    public static bool Contains(System.Collections.ArrayList arraylist, object obj) =>
        Contains(arraylist, obj, new Comparer(obj.GetType()));

    /// <summary>
    /// Gets a boolean that indicates whether an object is contained within the array list.
    /// </summary>
    /// <param name="arraylist">The array list to search.</param>
    /// <param name="obj">The object to search for.</param>
    /// <param name="comparer">The comparer algorithm to use in the matching.</param>
    /// <returns><c>true</c> if contained; otherwise <c>false</c>.</returns>
    public static bool Contains(System.Collections.ArrayList arraylist, object obj, System.Collections.IComparer comparer) =>
        IndexOf(arraylist, obj, comparer) != -1;

    /// <summary>
    /// Gets the index of the object in the specified array list.
    /// </summary>
    /// <param name="arraylist">The array list to obtain an index from.</param>
    /// <param name="obj">The object to obtain an index for.</param>
    /// <returns>The index of the object in the array list or -1 if not found.</returns>
    /// <remarks>
    /// Uses a comparer that will check internal content for equality if object references are not equal.
    /// </remarks>
    public static int IndexOf(System.Collections.ArrayList arraylist, object obj) =>
        IndexOf(arraylist, obj, new Comparer(obj.GetType()));

    /// <summary>
    /// Gets the index of the object in the specified array list.
    /// </summary>
    /// <param name="arraylist">The array list to obtain an index from.</param>
    /// <param name="obj">The object to obtain an index for.</param>
    /// <param name="comparer">The comparer algorithm to use in the matching.</param>
    /// <returns>The index of the object in the array list or -1 if not found.</returns>
    public static int IndexOf(System.Collections.ArrayList arraylist, object obj, System.Collections.IComparer comparer)
    {
        ArgumentNullException.ThrowIfNull(arraylist);
        ArgumentNullException.ThrowIfNull(comparer);

        if (arraylist.Count == 0) return -1;

        if (arraylist.Count == 1)
        {
            return comparer.Compare(arraylist[0], obj) == 0 ? 0 : -1;
        }

        var randgen = new Random();
        int endsearch = randgen.Next(0, arraylist.Count - 1);
        int arraymax = arraylist.Count - 1;
        int index = endsearch == arraymax ? 0 : endsearch + 1;
        int searchterminator = index;

        do
        {
            if (comparer.Compare(arraylist[index], obj) == 0)
                return index;
            else
                index = index == arraymax ? 0 : index + 1;
        } while (index != searchterminator);

        return -1;
    }

    /// <summary>
    /// Sorts the specified array list by the specified sort algorithm.
    /// </summary>
    /// <param name="anyList">Any list to sort.</param>
    /// <param name="containedType">The type that is contained within.</param>
    /// <param name="propertyNames">The names of the properties to sort by.</param>
    public static void Sort(System.Collections.ArrayList anyList, Type containedType, string[] propertyNames)
    {
        anyList.Sort(new SortableComparer(containedType, propertyNames));
    }

    #endregion Static Methods
}
