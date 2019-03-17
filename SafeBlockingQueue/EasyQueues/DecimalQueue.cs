using System;

namespace SafeBlockingQueue.EasyQueues
{
    public class DecimalQueue : Queue<decimal>
    {
        public DecimalQueue(Guid id, string name)
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