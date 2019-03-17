using System;

namespace SafeBlockingQueue.Objects
{
    [Serializable]
    internal sealed class QueueKeyItem<T> : Tuple<Guid, DateTime, QueueItem<T>>
    {
        /// <inheritdoc />
        public QueueKeyItem(Guid key, DateTime lockTimeout, QueueItem<T> item) : base(key, lockTimeout, item)
        {
        }

        public Guid Key => Item1;
        public DateTime? LockTimeout => Item2;
        public QueueItem<T> Item => Item3;
    }
}