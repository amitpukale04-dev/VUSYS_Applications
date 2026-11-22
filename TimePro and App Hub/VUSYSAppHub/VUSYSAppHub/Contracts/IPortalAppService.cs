using VUSYSAppHub.Models;

namespace VUSYSAppHub.Contracts
{
    public interface IPortalAppService
    {
        IReadOnlyList<PortalApp> GetApplications();
    }
}
