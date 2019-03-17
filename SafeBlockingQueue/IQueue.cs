using System;
using SafeBlockingQueue.Objects;

namespace SafeBlockingQueue
{
    public interface IQueue<T> : IDisposable
    {
        /// <summary>
        ///     The number of the items in the main queue.
        /// </summary>
        int Length { get; }

        /// <summary>
        ///     The number of the items that have locks taken out on them but have not been confirmed.
        /// </summary>
        int LockedListLength { get; }

        /// <summary>
        ///     The number of items that are scheduled to be resent because a loc was taken but never confirmed.
        /// </summary>
        int TimeoutQueueLength { get; }

        /// <summary>
        ///     The unique id of this instance.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        ///     The friendly name of this instance.
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     Raised when all items have been confirmed.
        /// </summary>
        event Complete OnComplete;

        /// <summary>
        ///     Raised when an item has been taken but not confirmed in time.
        /// </summary>
        event ItemTimeOut<T> OnItemTimeout;

        /// <summary>
        ///     Take an item from the queue.
        /// </summary>
        /// <param name="lockMinutes">The number of minutes to wait for ConfirmTake to be called.</param>
        /// <returns></returns>
        QueueItem<T> Take(int lockMinutes);

        /// <summary>
        ///     Call this to confirm that a taken item ready to be taken out of lock state and discarded.
        ///     If this is not called before the lock time on the taken item expires the item will be sent back
        ///     out on a take.
        /// </summary>
        /// <param name="itemId">The unique id of the item to confirm.</param>
        /// <returns></returns>
        bool ConfirmTake(Guid itemId);

        /// <summary>
        ///     Add an item to the queue.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        void Add(QueueItem<T> item);

        /// <summary>
        ///     Add an item to the queue with a result.
        /// </summary>
        /// <param name="item">The item to insert.</param>
        /// <returns>True on insertions.</returns>
        bool TryAdd(QueueItem<T> item);

        /// <summary>
        ///     Add all the given items to the queue.
        /// </summary>
        /// <param name="items">The items to insert.</param>
        void AddAll(QueueItem<T>[] items);

        /// <summary>
        ///     Adds more time to a locked item to prevent it from timing out and being sent back out on a take.
        /// </summary>
        /// <param name="itemId">The locked item.</param>
        /// <param name="lockMinutes">The number of minutes to add to the lock time.</param>
        /// <returns></returns>
        bool RefreshLock(Guid itemId, int lockMinutes);

        QueueItem<T>[] DumpQueue();
        QueueItem<T>[] DumpTimeoutQueue();
        QueueItem<T>[] DumpLockList();
        QueueDump<T> DumpAll();
    }
}