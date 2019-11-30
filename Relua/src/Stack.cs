using System;
using System.Collections.Generic;

namespace Relua {
    public class Stack<T> {
        protected List<T> BackingList = new List<T>();

        public void Push(T elem) {
            BackingList.Add(elem);
        }

        public T Pop() {
            if (BackingList.Count == 0) throw new InvalidOperationException($"Cannot pop empty stack");
            var elem = BackingList[Count - 1];
            BackingList.RemoveAt(BackingList.Count - 1);
            return elem;
        }

        public T Peek() {
            if (BackingList.Count == 0) throw new InvalidOperationException($"Cannot peek empty stack");
            return BackingList[BackingList.Count - 1];
        }

        public int Count => BackingList.Count;

        public T this[int index] {
            get {
                return BackingList[index];
            }
            set {
                BackingList[index] = value;
            }
        }
    }
}
