//using System.Collections;
//using System.Collections.ObjectModel;
//using System.Collections.Specialized;
//using System.ComponentModel;

//namespace CryptoScanner.Model;

//public class ObservableRangeCollection<T> : ObservableCollection<T>
//{
//    public void AddRange(IEnumerable<T> items)
//    {
//        foreach (var item in items)
//            Items.Add(item);

//        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
//    }

//    public void Replace(IEnumerable<T> items)
//    {
//        Items.Clear();
//        AddRange(items);
//    }


//    public void AddItem(T a, IComparer comparer, ListSortDirection sortDirection)
//    {
//        if (Items.Count == 0 || comparer == null)
//        {
//            Items.Add(a);
//            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, a, Items.Count - 1));
//            return;
//        }

//        // Binary search voor insert positie
//        int index = FindInsertPosition(a, comparer, sortDirection);

//        Items.Insert(index, a);
//        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, a, index));
//    }

//    private int FindInsertPosition(T item, IComparer comparer, ListSortDirection sortDirection)
//    {
//        int left = 0;
//        int right = Items.Count;

//        while (left < right)
//        {
//            int mid = (left + right) / 2;
//            int compare = comparer.Compare(Items[mid], item);

//            // Reverse als descending
//            if (sortDirection == ListSortDirection.Descending)
//                compare = -compare;

//            if (compare < 0)
//                left = mid + 1;
//            else
//                right = mid;
//        }

//        return left;
//    }


//}
