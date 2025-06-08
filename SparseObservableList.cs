using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace rawinator
{
    public class SparseObservableList<T> : INotifyCollectionChanged, INotifyPropertyChanged
    {
        private readonly Dictionary<int, T> sparseItems = [];
        private readonly ObservableCollection<T> orderedItems = [];

        public event NotifyCollectionChangedEventHandler? CollectionChanged {
            add => orderedItems.CollectionChanged += value;
            remove => orderedItems.CollectionChanged -= value;
        }

        private event PropertyChangedEventHandler? propertyChanged;
        public event PropertyChangedEventHandler? PropertyChanged {
            add => propertyChanged += value;
            remove => propertyChanged -= value;
        }

        public int Count => orderedItems.Count;

        public ObservableCollection<T> Ordered => orderedItems;

        public T? this[int index] {
            get => sparseItems.TryGetValue(index, out var item) ? item : default;
            set {
                sparseItems[index] = value!;

                if (index >= orderedItems.Count)
                {
                    while (orderedItems.Count <= index)
                        orderedItems.Add(default!);
                }

                orderedItems[index] = value!;
                OnPropertyChanged(nameof(Count));
            }
        }

        public void Add(T item)
        {
            int index = orderedItems.Count;
            sparseItems[index] = item;
            orderedItems.Add(item);
            OnPropertyChanged(nameof(Count));
        }

        public void Clear()
        {
            sparseItems.Clear();
            orderedItems.Clear();
            OnPropertyChanged(nameof(Count));
        }

        public bool Remove(T item)
        {
            int index = orderedItems.IndexOf(item);
            if (index >= 0)
            {
                orderedItems.RemoveAt(index);
                sparseItems.Remove(index);
                ReindexSparse();
                OnPropertyChanged(nameof(Count));
                return true;
            }
            return false;
        }

        private void ReindexSparse()
        {
            sparseItems.Clear();
            for (int i = 0; i < orderedItems.Count; i++)
            {
                sparseItems[i] = orderedItems[i];
            }
        }

        private void OnPropertyChanged(string name)
        {
            propertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}