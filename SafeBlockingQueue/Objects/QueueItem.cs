using System;

namespace SafeBlockingQueue.Objects
{
    [Serializable]
    public class QueueItem<T>
    {
        public Guid Id { get; set; }

        public T Data { get; set; }

        public DateTime? TimeOut { get; set; }
    }
}