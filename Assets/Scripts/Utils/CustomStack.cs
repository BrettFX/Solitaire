using System.Collections.Generic;

namespace Solitaire
{
    public class CustomStack<T>
    {
        private readonly List<T> items = new List<T>();
        public int Count
        {
            get
            {
                return items.Count;
            }
        }

        public void Push(T item)
        {
            items.Add(item);
        }

        public T Pop()
        {
            if (items.Count > 0)
            {
                T temp = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                return temp;
            }
            else
                return default;
        }

        public T Peek()
        {
            return items.Count > 0 ? items[items.Count - 1] : default;
        }

        public void Clear()
        {
            items.Clear();
        }

        public void Remove(int itemAtPosition)
        {
            if (items.Count > 0 && itemAtPosition < items.Count)
                items.RemoveAt(itemAtPosition);
        }

        public void RemoveOldest()
        {
            if (items.Count > 0)
                items.RemoveAt(0);
        }
    }
}

