using System;
using System.Collections.Generic;

namespace FrontierDispatch.Core
{
    public sealed class MinPriorityQueue<T>
    {
        private readonly List<Entry> _heap = new List<Entry>();
        
        public int Count => _heap.Count;
        
        public void Enqueue(T item, int priority)  
        {  
            Entry entry = new Entry(item, priority);  
  
            _heap.Add(entry);  
    
            int entryIndex = _heap.Count - 1;  
      
            SiftUp(entryIndex);  
        }
        
        public T PeekNext()
        {
            return _heap.Count == 0 ? throw new InvalidOperationException("The dispatch queue is empty.") : _heap[0].Item;

        }  
        
        public T Dequeue()  
        {  
            if (_heap.Count == 0)  
            {  
                throw new InvalidOperationException("The dispatch queue is empty.");  
            }  
  
            if (_heap.Count == 1)  
            {  
                var entry = _heap[0];  
                _heap.RemoveAt(0);  
                return entry.Item;  
            }  
              
            Entry dequeuedEntry = _heap[0];  
            
            int lastEntryIndex = _heap.Count - 1;
              
            Swap(0, lastEntryIndex);  
              
            _heap.RemoveAt(lastEntryIndex);  
              
            SiftDown(0);  
              
            return dequeuedEntry.Item;  
  
        }  

        
        private void SiftUp(int index)  
        {  
            int parentIndex = ParentIndex(index);  
      
            while (_heap[parentIndex].Priority > _heap[index].Priority)  
            {  
                Swap(parentIndex, index);  
          
                index = parentIndex;  
                parentIndex = ParentIndex(index);  
            }  
        }
        
        private void SiftDown(int index)  
        {  
            while (true)  
            {  
                int leftIndex = LeftChildIndex(index);  
                int rightIndex = RightChildIndex(index);  
                int lowestChildIndex = index;  
                  
                if (IsInHeap(rightIndex))  
                {  
                    lowestChildIndex = _heap[leftIndex].Priority < _heap[rightIndex].Priority ? leftIndex : rightIndex;  
                }  
  
                if (IsInHeap(leftIndex) && !IsInHeap(rightIndex))  
                {  
                    lowestChildIndex = leftIndex;  
                }  
  
                if (!IsInHeap(leftIndex))  
                {  
                    return;  
                }  
                  
                if (_heap[lowestChildIndex].Priority >= _heap[index].Priority)  
                {  
                    return;  
                }  
  
                Swap(lowestChildIndex, index);  
                index = lowestChildIndex;  
            }  
        }  
        
        private static int ParentIndex(int index)  
        {  
            return (index - 1) / 2;  
        }  
  
        private static int LeftChildIndex(int index)  
        {  
            return 2 * index + 1;  
        }  
  
        private static int RightChildIndex(int index)  
        {  
            return 2 * index + 2;  
        }  
  
        private void Swap(int a, int b)  
        {  
            Entry temp = _heap[a];  
            _heap[a] = _heap[b];  
            _heap[b] = temp;  
        }
          
        private bool IsInHeap(int index)  
        {  
            return index <= _heap.Count - 1;  
        }  

        
        
         
        private readonly struct Entry
        {
            public Entry(T item, int priority)
            {
                Item = item;
                Priority = priority;
            }

            public T Item { get; }
            public int Priority { get; }
        }
    }
}
