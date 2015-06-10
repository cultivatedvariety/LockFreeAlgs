using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.cultivatedvariety.lockfree.Tests;

namespace ConsoleRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            TestSPSCPerformance();
            TestMPMCPerformance();
            Console.Write("Done. Press enter to exit");
            Console.ReadLine();
        }

        private static void TestSPSCPerformance()
        {
            Console.WriteLine("Testing SingleProducerSingleConsumer performance");
            SingleProducerSingleConsumerQueueTest singleConsumerQueueTest = new SingleProducerSingleConsumerQueueTest();
            singleConsumerQueueTest.Test_Performance();
            Console.WriteLine("Finishd testing SingleProducerSingleConsumer performance");

        }

        private static void TestMPMCPerformance()
        {
            Console.WriteLine("Testing MultiProducerMultiConsumer performance");
            MultiProducerMultiConsumerSingleQueueTest multiProducerMultiConsumerSingleQueueTest = new MultiProducerMultiConsumerSingleQueueTest();
            multiProducerMultiConsumerSingleQueueTest.MultiProducerMultiConsumerSingleQueueTestTest_Performance();
            Console.WriteLine("Finished testing MultiProducerMultiConsumer performance");
        }
    }
}
