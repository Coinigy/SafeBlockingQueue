using System;

namespace SafeBlockingQueue.Objects
{
    [Serializable]
    public class QueueDump<T>
    {
        public Guid Id { get; set; }
        public QueueItem<T>[] MainQueue { get; set; }
        public QueueItem<T>[] TimeoutQueue { get; set; }
        public QueueItem<T>[] LockList { get; set; }
    }
}