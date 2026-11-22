using VUSYSAppHub.Contracts;
using VUSYSAppHub.Models;

namespace VUSYSAppHub.Services
{
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
