using VUSYSAppHub.Models;

namespace VUSYSAppHub.Services
{
    public interface IPortalAppService
    {
        IReadOnlyList<PortalApp> GetApplications();
    }

    public sealed class PortalAppService : IPortalAppService
    {
        public IReadOnlyList<PortalApp> GetApplications()
        {
            return new List<PortalApp>
            {
            };
        }
    }
}
