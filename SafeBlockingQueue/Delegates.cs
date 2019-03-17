using System;
using SafeBlockingQueue.Objects;

namespace SafeBlockingQueue
{
    public delegate void Complete(object sender, EventArgs e);

    public delegate void ItemTimeOut<T>(object sender, QueueItem<T> item);
}