namespace Dealoware.Domain.Admin;

/// <summary>
/// SC-9: reset mail leaves the request path through this queue.
/// </summary>
public interface IAdminMailDispatcher
{
    void Enqueue(AdminMailMessage message);
}
