using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace com.cultivatedvariety.lockfree.Tests
{
    [TestFixture]
    public class MultiProducerMultiConsumerSingleQueueTest
    {
        private const int Repetitions = 100 * 1000 * 1000;
        private const int NumberOfCores = 2;
            
        [SetUp]
        public void SetUp()
        {
        }

        [Test]
        public void Test_Enqueue_Dequeue()
        {
            MultiProducerMultiConsumerSingleQueue<Integer> queue = new MultiProducerMultiConsumerSingleQueue<Integer>(64*1024, 4);

            AutoResetEvent enqueuedEvent = new AutoResetEvent(false);
            AutoResetEvent dequeuedEvent = new AutoResetEvent(false);

            int repetitions = 64*1024; //enough repetitions to loop around

            Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < repetitions; i++)
                {
                    enqueuedEvent.WaitOne();
                    Integer dequeued = queue.TryDequeue();
                    Assert.IsNotNull(dequeued);
                    Assert.AreEqual(i, dequeued.Val);
                    dequeuedEvent.Set();
                }

            });

            for (int i = 0; i < repetitions; i++)
            {
                Assert.IsTrue(queue.TryEnqueue(new Integer(i)));
                enqueuedEvent.Set();
                dequeuedEvent.WaitOne();
            }
        }

        [Test]
        public void MultiProducerMultiConsumerSingleQueueTestTest_Performance()
        {
            Stopwatch stopwatch = new Stopwatch();
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            ManualResetEvent producerCompleteEvent = new ManualResetEvent(false);
            ManualResetEvent consumerCompleteEvent = new ManualResetEvent(false);
            for (int testRuns = 0; testRuns < 5; testRuns++)
            {
                MultiProducerMultiConsumerSingleQueue<Integer> queue = new MultiProducerMultiConsumerSingleQueue<Integer>(64*2048, 8);

                int numberOfProducers = NumberOfCores/2;
                int numberOfConsumers = NumberOfCores/2;
                for (int c = 0; c < numberOfConsumers; c++)
                {
                    Thread consumer = new Thread(() =>
                    {
                        int repetittions = Repetitions/ numberOfConsumers;
                        waitEvent.WaitOne();
                        for (int cd = 0; cd < repetittions; cd++) 
                        {
                            Integer dequeued = null;
                            do
                            {
                                dequeued = queue.TryDequeue();
                            } while (dequeued == null);
                        }
                        int remainingConsumers = Interlocked.Decrement(ref numberOfConsumers);
                        if (remainingConsumers == 0)
                            consumerCompleteEvent.Set();
                    }) {IsBackground = true};
                    consumer.Start();
                }

                for (int p = 0; p < numberOfProducers; p++)
                {
                    Thread producer = new Thread(() =>
                    {
                        int repetitions = Repetitions/ numberOfProducers;
                        waitEvent.WaitOne();
                        for (int pe = 0; pe < repetitions; pe++)
                        {
                            bool enqueued = false;
                            do
                            {
                                enqueued = queue.TryEnqueue(new Integer(pe));
                            } while (!enqueued);
                        }
                        int remainingProducers = Interlocked.Decrement(ref numberOfProducers);
                        if (remainingProducers == 0)
                            producerCompleteEvent.Set();
                    }) {IsBackground = true};
                    producer.Start();
                }

                waitEvent.Reset();
                producerCompleteEvent.Reset();
                consumerCompleteEvent.Reset();

                stopwatch.Restart();
                waitEvent.Set();

                producerCompleteEvent.WaitOne();
                consumerCompleteEvent.WaitOne();
                stopwatch.Stop();

                Console.WriteLine("{0} of 5: Ops/sec: {1:N}", testRuns + 1, Repetitions / stopwatch.Elapsed.TotalSeconds);
            }
        }
    }
}
