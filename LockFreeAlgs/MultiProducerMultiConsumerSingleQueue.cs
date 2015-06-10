using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.cultivatedvariety.lockfree
{
    /// <summary>
    /// Queue-like implementation whose goal is consistent throughput and latency
    /// that scales well as the number of cores increases
    /// 
    /// On intitial enqueue/dequeue producers/consumers are allocated an inital slot in the queue and then
    /// loop through the queue until they find an open slot to work with. A thread's queue slot is stored in
    /// a thread local variable so that it does not have to be recalculated.
    /// 
    /// Enqueueing/dequeueing is achieved through CAS operations. Not all slots in the queue are used to reduce
    /// the chances of false sharing between producer and consumer
    /// 
    /// Parameters should be tuned depending on the machine configurations
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MultiProducerMultiConsumerSingleQueue<T> where T : class
    {
        private readonly int _slotSpacing;
        private readonly int _capacity;
        private readonly T[] _buffer;
        private int _threadCount;
        
        /// <summary>
        /// Create a new queue
        /// </summary>
        /// <param name="capacity">Queue capacity</param>
        /// <param name="slotSpacing">Use every Xth slot e.g. 4 = use every 4th slot. 8 = use every 8th slot</param>
        public MultiProducerMultiConsumerSingleQueue(int capacity, int slotSpacing)
        {
            _slotSpacing = slotSpacing;
            _capacity = FindNextPowerOf2(capacity);
            _buffer = new T[_capacity];
            for (int i = 0; i < _buffer.Length; i++)
                _buffer[i] = null;
            _threadCount = 0;
        }

        /// <summary>
        /// Try enqueue <see cref="val"/>
        /// </summary>
        /// <param name="val">Value to enqueue</param>
        /// <returns>True if enqueued, false otherwise</returns>
        public bool TryEnqueue(T val)
        {
            T swappedVal = val;
            int nextSlot = GetNextSlot();
            int firstNextSlot = nextSlot;

            do
            {
                //try to enqueue until a value can be CAS'd into a slot. A value has been successfully
                //CASd when the return value from CAS op is NULL, indicating that the slot was previously empty
                if (_buffer[nextSlot] == null)
                    swappedVal = Interlocked.CompareExchange(ref _buffer[nextSlot], val, null);

                nextSlot += _slotSpacing;
                if (nextSlot >= _capacity)
                    nextSlot = 0;
                if (nextSlot == firstNextSlot)
                    //looped all the way around with no free slots
                    break;
            } while (swappedVal != null); 
            SetNextSlot(nextSlot);
            return swappedVal == null;
        }

        public T TryDequeue()
        {
            int nextSlot = GetNextSlot();
            int firstNextSlot = nextSlot;

            T dequeued = null;
            do
            {
                //try to dequeue until a value is CAS'd from a slot. A value is CAS'd from a slot
                //when it can successfully be switched for NULL and the value is returned
                T tryDequeue = _buffer[nextSlot];
                if (tryDequeue != null)
                    dequeued = Interlocked.CompareExchange(ref _buffer[nextSlot], null, tryDequeue);
                nextSlot += _slotSpacing;
                if (nextSlot >= _capacity)
                    nextSlot = 0;
                if (nextSlot == firstNextSlot)
                    //looped all the way around without finding anything
                    break;
            } while (dequeued == null);
            SetNextSlot(nextSlot);
            return dequeued;
        }

        private int GetNextSlotInitialValue()
        {
            //from testing it appears that the better this method distributes the
            //inital slots, the fast the queue goes. possibly worth optimising
            int threadCount = Interlocked.Increment(ref _threadCount);
            int initialVal = _threadCount%2 == 0
                ? _capacity/threadCount
                : (_capacity - _capacity/threadCount);
            while (initialVal % _slotSpacing != 0)
                initialVal++;
            if (initialVal >= _capacity)
                initialVal = 0;
            return initialVal;
        }

        /// <summary>
        /// The next slot to try enqueueing/dequeueing from is stored in a thread local variable
        /// </summary>
        /// <returns></returns>
        private int GetNextSlot()
        {
            int nextSlot;
            object nextSlotObj = CallContext.LogicalGetData("NextSlot");
            if (nextSlotObj == null)
            {
                nextSlot = GetNextSlotInitialValue();
            }
            else
            {
                nextSlot = (int) nextSlotObj;
            }
            return nextSlot;
        }

        private void SetNextSlot(int nextSlot)
        {
            CallContext.LogicalSetData("NextSlot", nextSlot);
        }

        private static int FindNextPowerOf2(int x)
        {
            x--;
            x |= x >> 1; // handle  2 bit numbers
            x |= x >> 2; // handle  4 bit numbers
            x |= x >> 4; // handle  8 bit numbers
            x |= x >> 8; // handle 16 bit numbers
            x |= x >> 16; // handle 32 bit numbers
            x++;

            return x;
        }
    }
}
