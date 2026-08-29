using RockAI.Domain.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace RockAI.Application.Messages
{
    public sealed record SendMessageResult(Message Message, string? NewTitle);
}
