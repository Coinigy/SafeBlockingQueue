using System;

namespace SafeBlockingQueue.EasyQueues
{
    public class IntQueue : Queue<int>
    {
        public IntQueue(Guid id, string name)
        {
            Id = id;
            Name = name;
        }

        /// <inheritdoc />
        public override Guid Id { get; }

        /// <inheritdoc />
        public override string Name { get; }
    }
}