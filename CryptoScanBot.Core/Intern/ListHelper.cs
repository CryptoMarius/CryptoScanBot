using System.Collections.Generic;

namespace CryptoScanBot.Core.Intern;

public static class ListHelper
{
    //SortedList<int, MyValueClass> slist = new SortedList<int, MyValueClass>(new DuplicateKeyComparer<int>());

    public class DuplicateKeyComparer<TKey> : IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey? x, TKey? y)
        {
            int result = x!.CompareTo(y!);

            // Handle equality as being greater.
            // Note: this will break Remove(key) or IndexOfKey(key) since the comparer never returns 0 to signal key equality
            if (result == 0)
                return 1; 
            else          
                return result;
        }
    }

    //int offset = data.SymbolInterval.CandleList.BinarySearchIndexOf(123);

    public static int BinarySearchIndexOf<T>(this IList<T>? list, T value, IComparer<T>? comparer = null)
    {
        // This assumes that the list in question is already sorted,
        // according to the same rules that the comparer will use.
        ArgumentNullException.ThrowIfNull(list, nameof(list));

        int lower = 0;
        int upper = list.Count - 1;
        comparer ??= Comparer<T>.Default;

        while (lower <= upper)
        {
            int middle = lower + (upper - lower) / 2;
            int comparisonResult = comparer.Compare(value, list[middle]);
            if (comparisonResult == 0)
                return middle;
            else if (comparisonResult < 0)
                upper = middle - 1;
            else
                lower = middle + 1;
        }

        return lower;
    }


}
