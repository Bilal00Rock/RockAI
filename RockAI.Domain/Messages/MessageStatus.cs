using Ardalis.SmartEnum;
using System;
using System.Collections.Generic;
using System.Text;

namespace RockAI.Domain.Messages
{
    public class MessageStatus : SmartEnum<MessageStatus>
    {
        public static readonly MessageStatus Pending = new(nameof(Pending), 0);
        public static readonly MessageStatus Streaming = new(nameof(Streaming), 1);
        public static readonly MessageStatus Completed = new(nameof(Completed), 2);
        public static readonly MessageStatus Failed = new(nameof(Failed), 3);
        public static readonly MessageStatus Cancelled = new(nameof(Cancelled), 4);
        public MessageStatus(string name, int value)
            : base(name, value)
        {
        }
    }
}
