using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace com.cultivatedvariety.lockfree.Tests
{
    [TestFixture]
    public class SingleProducerSingleConsumerQueueTest
    {
        private const int Repetitions = 100 * 1000 * 1000;


        [SetUp]
        public void SetUp()
        {
            
        }
            
        [Test]
        public void Test_Enqueue_Dequeue()
        {
            SingleProducerConsumerQueue<Integer> queue = new SingleProducerConsumerQueue<Integer>(64*1024);
            for (int i = 0; i < Repetitions; i++)
            {
                Assert.IsTrue(queue.TryEnqueue(new Integer(i)));
                Assert.AreEqual(i, queue.TryDequeue().Val);
            }
        }

        [Test]
        public void Test_Enqueue_Dequeue_MultiThread()
        {
            SingleProducerConsumerQueue<Integer> queue = new SingleProducerConsumerQueue<Integer>(64 * 1024);
            Task t = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < Repetitions; i++)
                {
                    Integer val;
                    do
                    {
                        val = queue.TryDequeue();
                    } while (val == null);
                    
                    Assert.AreEqual(i, val.Val);
                }
            });

            for (int i = 0; i < Repetitions; i++)
            {
                bool enqueued;
                do
                {
                    enqueued = queue.TryEnqueue(new Integer(i));
                } while (!enqueued);
            }

            t.Wait();
        }

        [Test]
        public void Test_Performance()
        {
            Stopwatch stopwatch = new Stopwatch();
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            for (int i = 0; i < 5; i++)
            {
                SingleProducerConsumerQueue<Integer> queue = new SingleProducerConsumerQueue<Integer>(64 * 1024);
                Task consumer = Task.Factory.StartNew(() =>
                {
                    waitEvent.WaitOne();
                    Integer val;
                    for (int j = 0; j < Repetitions; j++)
                    {
                        do
                        {
                            val = queue.TryDequeue();
                        } while (val == null);
                    }
                });

                stopwatch.Restart();
                waitEvent.Set();

                bool enqueued;
                for (int j = 0; j < Repetitions; j++)
                {
                    do
                    {
                        enqueued = queue.TryEnqueue(new Integer(i));
                    } while (!enqueued);
                }

                consumer.Wait();
                stopwatch.Stop();
                waitEvent.Reset();
                Console.WriteLine("{0} of 5: Ops/sec: {1:N}", i + 1, Repetitions/stopwatch.Elapsed.TotalSeconds);
            }
        }
    }
}
