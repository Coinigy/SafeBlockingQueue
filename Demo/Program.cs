using System;
using System.Threading.Tasks;
using SafeBlockingQueue.EasyQueues;
using SafeBlockingQueue.Objects;

namespace Demo
{
    internal class Program
    {
        private const int DemoItemCount = 10;
        private static StringQueue _strQueue;
        private static bool _completed;

        private static async Task Main(string[] args)
        {
            Console.WriteLine("Setting up demo queue");
            // create a queue to hold out items
            _strQueue = new StringQueue(Guid.NewGuid(), "Demo Queue");
            // hook up to the complete event
            _strQueue.OnComplete += _strQueue_OnComplete;
            // lets also hook up to when an item times out
            _strQueue.OnItemTimeout += _strQueue_OnItemTimeout;
            Console.WriteLine("Filling demo queue with data.");
            // save the time here so we know how long we are taking to add our items
            var st = DateTime.UtcNow;
            for (var i = 1; i <= DemoItemCount; i++)
                // lets just use ticks to make some strings
                // each item needs a unique id to keep track of it so we just use a new guid
                _strQueue.Add(new QueueItem<string> {Data = $"TICK-{DateTime.UtcNow.Ticks}", Id = Guid.NewGuid()});

            var fillSeconds = (DateTime.UtcNow - st).TotalSeconds;
            Console.WriteLine($"Filled demo queue with {DemoItemCount} items in {fillSeconds} seconds");

            await Task.Delay(1500);

            Console.WriteLine("Reading queue items back. This may take a while!" + Environment.NewLine);
            await RunReader();

            Console.Write("Run Completed");
            Console.ReadLine();
        }

        private static void _strQueue_OnItemTimeout(object sender, QueueItem<string> item)
        {
            Console.WriteLine($"Item {item.Id} has timed out and will be sent back out for processing!");
        }

        private static void _strQueue_OnComplete(object sender, EventArgs e)
        {
            // all items have been processed, stop reading
            _completed = true;
        }

        private static async Task RunReader()
        {
            await Task.Run(() =>
            {
                // record our start time
                var st = DateTime.UtcNow;
                while (!_completed)
                {
                    // take an item from the queue with a confirm timeout of 1 minute
                    // this is a blocking operation and we will wait here for an item
                    var item = _strQueue.Take(1);
                    Console.WriteLine($"Took item {item.Id} {item.Data}");

                    // fake doing some task with the data. Since we can take up to 70 seconds we should get an item timeout every once in a while
                    // (this is done just to show how that works)
                    if (new Random().Next(1, 70000) > 60000)
                    {
                        // skip the item so it gets sent back to us
                        Console.WriteLine($"Skipping item {item.Id} {item.Data}");
                    }
                    else
                    {
                        // we have successfully completed working with the item so lets confirm it
                        // so that it does not time out and get send back out
                        _strQueue.ConfirmTake(item.Id);
                        Console.WriteLine($"Confirmed item {item.Id} {item.Data}");
                    }

                    Console.WriteLine(Environment.NewLine);
                }

                var readSeconds = (DateTime.UtcNow - st).TotalSeconds;
                Console.WriteLine($"Read and confirmed all queue items in {readSeconds} seconds.                                      ");
            });
        }
    }
}