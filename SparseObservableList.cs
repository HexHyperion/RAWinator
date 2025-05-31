using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace rawinator
{
    // This is a custom dictionary-based implementation of an observable list that can store
    // items at non-sequential indexes, which is needed mainly for the library view,
    // where images are imported asynchronously, but need to be displayed in the import order.
    public class SparseObservableList<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private Dictionary<int, T> items = [];
        private int maxIndex = -1;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public T this[int index] {
            get => items.TryGetValue(index, out T? value) ? value! : default!;
            set {
                items[index] = value;
                maxIndex = Math.Max(maxIndex, index);

                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, index));

                OnPropertyChanged(nameof(Count));
            }
        }

        public int Count => maxIndex + 1;

        public bool IsReadOnly => false;

        public void Add(T item) => this[++maxIndex] = item;

        public void Clear()
        {
            items.Clear();
            maxIndex = -1;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged(nameof(Count));
        }

        public bool Contains(T item) => items.ContainsValue(item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (int i = 0; i <= maxIndex; i++)
                array[arrayIndex + i] = this[i];
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i <= maxIndex; i++)
            {
                yield return this[i];
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int IndexOf(T item)
        {
            foreach (var pair in items)
            {
                if (EqualityComparer<T>.Default.Equals(pair.Value, item))
                {
                    return pair.Key;
                }
            }
            return -1;
        }

        public void Insert(int index, T item) => this[index] = item;

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (items.Remove(index))
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, default(T), index));
                OnPropertyChanged(nameof(Count));
            }
        }

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}