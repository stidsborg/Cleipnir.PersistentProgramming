using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ExecutionEngine.DataStructures
{
    public class CArray<T> : IEnumerable<T>
    {
        private readonly T[] _elements;
        private int _atIndex;

        public CArray() => _elements = new T[100_000];
        public CArray(int bucketSize) => _elements = new T[bucketSize];

        public CArray(IEnumerable<T> initialElements)
        {
            _elements = initialElements.ToArray();
            _atIndex = _elements.Length;
        } 

        public T this[int index] => _elements[index];

        public void Add(T element)
        {
            _elements[_atIndex] = element;
            _atIndex++;
        }

        public void Add(CArray<T> cArray)
        {
            if (cArray.Empty) return;

            var source = cArray._elements;
            var sourceCount = cArray._atIndex;
            var destination = _elements;

            Array.Copy(source, 0, destination, _atIndex, sourceCount);
            _atIndex += sourceCount;
        }

        public void Add(T[] arr)
        {
            if (arr.Length == 0) return;

            Array.Copy(arr, 0, _elements, _atIndex, arr.Length);
            _atIndex += arr.Length;
        }

        public bool Empty => _atIndex == 0;
        public int Count => _atIndex;

        public void MoveTo(CArray<T> destination)
        {
            if (Empty) return;

            destination.Add(this);
            Clear();
        }

        public void Clear()
        {
            if (_atIndex == 0) return;

            Array.Clear(_elements, 0, _atIndex);
            _atIndex = 0;
        }

        public IEnumerable<T> GetAll()
        {
            for (var i = 0; i < _atIndex; i++)
                yield return _elements[i];
        }

        public IEnumerator<T> GetEnumerator() => GetAll().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
