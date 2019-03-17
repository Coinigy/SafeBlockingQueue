using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SafeBlockingQueue.Objects;

namespace SafeBlockingQueue
{
    /// <inheritdoc />
    public abstract class Queue<T> : IQueue<T>
    {
        private readonly object _listLock;

        private readonly Timer _lockCheckTimer;
        private readonly Dictionary<Guid, QueueKeyItem<T>> _locks;

        private readonly BlockingCollection<QueueItem<T>> _mainQueue;
        private readonly BlockingCollection<QueueItem<T>> _timeoutQueue;
        private int _lockCheckTimerIntervalSeconds = 4;

        protected Queue()
        {
            _mainQueue = new BlockingCollection<QueueItem<T>>();
            _timeoutQueue = new BlockingCollection<QueueItem<T>>();
            _locks = new Dictionary<Guid, QueueKeyItem<T>>();
            _listLock = new object();
            // set the first fire to be be 2X the normal time to allow for startup
            _lockCheckTimer = new Timer(CheckLockTimes, null, TimeSpan.FromSeconds(LockCheckTimerIntervalSeconds * 2), TimeSpan.FromSeconds(LockCheckTimerIntervalSeconds));
        }

        public int MaxLockMinutes { get; set; } = 20;

        public int LockCheckTimerIntervalSeconds
        {
            get => _lockCheckTimerIntervalSeconds;
            set => LockCheckTimerIntervalSecondsChanged(value);
        }

        public int LockedListLength => _locks.Count;
        public int TimeoutQueueLength => _timeoutQueue.Count;
        public int Length => _mainQueue.Count + _timeoutQueue.Count;

        public abstract Guid Id { get; }
        public abstract string Name { get; }

        public event Complete OnComplete;
        public event ItemTimeOut<T> OnItemTimeout;

        public void Add(QueueItem<T> item)
        {
            _mainQueue.Add(item);
        }

        public bool TryAdd(QueueItem<T> item)
        {
            return _mainQueue.TryAdd(item);
        }

        public void AddAll(QueueItem<T>[] items)
        {
            foreach (var item in items) Add(item);
        }

        public QueueItem<T> Take(int lockMinutes)
        {
            if ((lockMinutes < 1) || (lockMinutes > MaxLockMinutes))
                lockMinutes = MaxLockMinutes;
            if (Length <= 0)
            {
                var spin = new SpinWait();
                while (Length <= 0) spin.SpinOnce();
            }

            // take one from the timeout or main queue
            var result = _timeoutQueue.Count > 0
                ? _timeoutQueue.Take()
                : _mainQueue.Take();

            lock (_listLock)
            {
                // add the item to the lock list
                _locks.Add(result.Id, new QueueKeyItem<T>(result.Id, DateTime.UtcNow.AddMinutes(lockMinutes), result));
            }

            return result;
        }

        public bool ConfirmTake(Guid itemId)
        {
            try
            {
                lock (_listLock)
                {
                    if (!_locks.ContainsKey(itemId))
                        return false;

                    _locks.Remove(itemId);
                    return true;
                }
            }
            finally
            {
                lock (_listLock)
                {
                    // if that was the last item we have raise the completed event
                    if ((_mainQueue.Count <= 0) && (_locks.Count <= 0) && (_timeoutQueue.Count <= 0))
                        OnComplete?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public QueueItem<T>[] DumpQueue()
        {
            lock (_locks)
            {
                return _mainQueue.ToArray();
            }
        }

        public QueueItem<T>[] DumpTimeoutQueue()
        {
            lock (_listLock)
            {
                return _timeoutQueue.ToArray();
            }
        }

        public QueueItem<T>[] DumpLockList()
        {
            lock (_listLock)
            {
                return _locks.Select(x => x.Value.Item).ToArray();
            }
        }

        public QueueDump<T> DumpAll()
        {
            lock (_listLock)
            {
                return new QueueDump<T>
                {
                    Id = Id,
                    LockList = _locks.Select(x => x.Value.Item).ToArray(),
                    MainQueue = _mainQueue.ToArray(),
                    TimeoutQueue = _timeoutQueue.ToArray()
                };
            }
        }

        public bool RefreshLock(Guid itemId, int lockMinutes)
        {
            if ((lockMinutes < 1) || (lockMinutes > MaxLockMinutes))
                lockMinutes = MaxLockMinutes;

            lock (_listLock)
            {
                // ensure the item is in the lock list
                if (!_locks.ContainsKey(itemId))
                    return false;

                // since we are using a tuple and can not modify the item we create a new item and delete the old
                var item = _locks[itemId];
                var newItem = item.LockTimeout != null
                    ? new QueueKeyItem<T>(item.Key, item.LockTimeout.Value.AddMinutes(lockMinutes), item.Item)
                    : new QueueKeyItem<T>(item.Key, DateTime.UtcNow.AddMinutes(lockMinutes), item.Item);

                _locks.Remove(itemId);
                _locks.Add(itemId, newItem);

                return true;
            }
        }

        private void CheckLockTimes(object nothing)
        {
            var st = DateTime.UtcNow;
            lock (_listLock)
            {
                // allow a 1 second leeway before declaring an item as timed out
                var expiredLocks = _locks.Where(x => (x.Value.LockTimeout != null) && (x.Value.LockTimeout.Value.AddSeconds(1) < DateTime.UtcNow)).ToArray();
                foreach (var expiredLock in expiredLocks)
                {
                    // fire off the event
                    OnItemTimeout?.Invoke(this, expiredLock.Value.Item);
                    // add the item to the timeout queue and remove from the lock list
                    _timeoutQueue.Add(expiredLock.Value.Item);
                    _locks.Remove(expiredLock.Key);
                }
            }

            var timeTaken = (DateTime.UtcNow - st).TotalSeconds;
            if (timeTaken > LockCheckTimerIntervalSeconds)
                LockCheckTimerIntervalSeconds = (int) timeTaken + 1;
        }

        private void LockCheckTimerIntervalSecondsChanged(int value)
        {
            _lockCheckTimerIntervalSeconds = value;
            _lockCheckTimer.Change(TimeSpan.FromSeconds(LockCheckTimerIntervalSeconds), TimeSpan.FromSeconds(LockCheckTimerIntervalSeconds));
        }

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _lockCheckTimer.Dispose();
                    _locks.Clear();
                    _timeoutQueue.Dispose();
                    _mainQueue.Dispose();
                }

                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}