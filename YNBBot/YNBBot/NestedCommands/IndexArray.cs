using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace YNBBot.NestedCommands
{
    public class IndexArray<T> : IEnumerable<T>, ICloneable
    {
        private T[] array;

        private int baseIndex = 0;
        public int Index
        {
            get
            {
                return baseIndex;
            }
            set
            {
                if (value > array.Length || value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                baseIndex = value;
            }
        }
        public int Count { get { return array.Length - baseIndex; } }
        public int TotalCount { get { return array.Length; } }

        #region Constructors

        public IndexArray(int count)
        {
            array = new T[count];
        }

        public IndexArray(T[] existingItems)
        {
            array = new T[existingItems.Length];
            existingItems.CopyTo(array, 0);
        }

        public IndexArray(ICollection<T> existingItems)
        {
            array = new T[existingItems.Count];
            existingItems.CopyTo(array, 0);
        }

        #endregion
        #region Accessors

        public T this[int index]
        {
            get
            {
                return array[baseIndex + index];
            }
            set
            {
                array[baseIndex + index] = value;
            }
        }

        public T First { get { return array[baseIndex]; } }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = baseIndex; i < array.Length; i++)
            {
                yield return array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = baseIndex; i < array.Length; i++)
            {
                yield return array[i];
            }
        }

        #endregion
        #region Conversions

        public static implicit operator IndexArray<T>(T[] from)
        {
            return new IndexArray<T>(from);
        }

        public static explicit operator T[] (IndexArray<T> from)
        {
            T[] result = new T[from.array.Length - from.baseIndex];
            for (int i = 0; i < from.Count; i++)
            {
                result[i] = from[i];
            }
            return result;
        }

        #endregion
        #region Misc

        public bool WithinBounds(int index)
        {
            return index >= 0 && index + baseIndex < array.Length;
        }

        public object Clone()
        {
            IndexArray<T> clone = new IndexArray<T>((T[])array.Clone());
            clone.baseIndex = baseIndex;
            return clone;
        }

        #endregion
    }
}
