using RockAI.Application.Common.Models;

namespace RockAI.Application.Common.Interfaces;

public interface ICurrentUserProvider
{
    CurrentUser GetCurrentUser();
}