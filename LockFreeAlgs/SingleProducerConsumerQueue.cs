using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.cultivatedvariety.lockfree
{
    /// <summary>
    /// Lock free single producer/single consumer queue.
    /// 
    /// The queue makes use of head & tail values along with CAS operations to enqueue/dequeue items
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingleProducerConsumerQueue<T> where T : class
    {
        private readonly int _capacity;
        private readonly int _mask;
        private long _head;
        private long _cachedHead;
        private long _tail;
        private long _cachedTail;
        private readonly T[] _buffer;


        public SingleProducerConsumerQueue(int capacity)
        {
            _capacity = FindNextPowerOf2(capacity); //use power of two so that bit-masking can be used instead of division to find buffer slot
            _mask = _capacity - 1;
            _buffer = new T[_capacity];
            for (int i = 0; i < _buffer.Length; i++)
                _buffer[i] = null;
        }

        public bool TryEnqueue(T val)
        {
            if (val == null)
                throw new InvalidEnumArgumentException("value cannot be null");
            if (_tail - _cachedHead >= _capacity)
            {
                //refresh _cachedHead. _cachedHead a cached copy of _head used to reduce volatile reads and improve performance
                _cachedHead = Thread.VolatileRead(ref _head); //volatile read to ensure it is read from memory and is not a cached register version
                if (_tail - _cachedHead >= _capacity)
                    //no space
                    return false;
            }

            int bufferSlot = (int) (_tail & _mask); //masking faster than division in cpu
            _buffer[bufferSlot] = val;
            _tail++; //no volatile write required as CLR does not re-order writes
            
            return true;
        }

        public T TryDequeue()
        {
            if (_cachedTail <= _head)
            {
                //refresh _cachedTail. _cachedTail is a cached copy of _tail used to reduce volatile reads and improve performance
                _cachedTail = Thread.VolatileRead(ref _tail); //volatile read to ensure it is read from memory and is not a cached register version
                if (_cachedTail == _head)
                    return null;
            }

            int bufferSlot = (int) (_head & _mask);
            T val = (T) _buffer[bufferSlot];
            _buffer[bufferSlot] = null;
            _head++; //no volatile write required as CLR does not re-order writes
            return val;
        }

        private static int FindNextPowerOf2(int x)
        {
            x--;
            x |= x >> 1;  // handle  2 bit numbers
            x |= x >> 2;  // handle  4 bit numbers
            x |= x >> 4;  // handle  8 bit numbers
            x |= x >> 8;  // handle 16 bit numbers
            x |= x >> 16; // handle 32 bit numbers
            x++;

            return x;
        }
    }
}
