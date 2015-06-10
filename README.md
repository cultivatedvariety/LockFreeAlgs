# LockFreeAlgs

A non-blocking algorithm is **lock-free** if there is guaranteed system-wide progress, and **wait-free** if there is also guaranteed per-thread progress.

LockFreeAlgs contains to two different C# lock-free queue implementations:
* SingleProducerSingleConsumer - single producer/consumer queue consisting of an array with a head and tail that makes use of CAS operations to enqueue & dequeue
* MultiProducerMultiConsumer - multi producer/consumer queue that should scale with the number of cores that makes use of CAS operations to enqueue & dequeue

### Performance results

* Tests are for enqueue/dequeue operations on a 64bit Intel Core I5 2.4 Ghz

| Testing SingleProducerSingleConsumer performance |
|--------------------------------------------------|
| 1 of 5: Ops/sec: 18,521,076.14 |
| 2 of 5: Ops/sec: 19,007,918.20 |
| 3 of 5: Ops/sec: 17,423,995.10 |
| 4 of 5: Ops/sec: 18,960,202.76 |
| 5 of 5: Ops/sec: 17,158,112.18 |
 
| Testing MultiProducerMultiConsumer performance |
|------------------------------------------------|
| 1 of 5: Ops/sec: 3,635,008.02 |
| 2 of 5: Ops/sec: 4,096,926.65 |
| 3 of 5: Ops/sec: 4,202,301.52 |
| 4 of 5: Ops/sec: 3,942,216.57 |
| 5 of 5: Ops/sec: 4,226,592.46 |
