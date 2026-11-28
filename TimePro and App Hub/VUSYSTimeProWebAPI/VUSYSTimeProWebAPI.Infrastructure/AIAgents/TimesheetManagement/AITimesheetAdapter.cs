using Microsoft.EntityFrameworkCore;
using VUSYSTimeProWebAPI.Application.Abstractions.AIAgents.TimesheetManagement;
using VUSYSTimeProWebAPI.Application.Abstractions.Users;
using VUSYSTimeProWebAPI.Application.Models.AIAgents.TimesheetManagement;
using VUSYSTimeProWebAPI.Infrastructure.Persistence.EntityFramework;

namespace VUSYSTimeProWebAPI.Infrastructure.AIAgents.TimesheetManagement;

public sealed class AITimesheetSecurityContext(IUserHelper userHelper, AppDbContext db) : IAITimesheetSecurityContext
{
    /// <summary>This Method Is Used To Get User Identity And Approver Permission.</summary>
    public async Task<AITimesheetActor> GetActorAsync(CancellationToken cancellationToken = default)
    {
        string employeeCode = userHelper.GetUserEmployeeCode();
        string email = userHelper.GetUserEmailID();
        if (string.IsNullOrWhiteSpace(employeeCode) || string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedAccessException("Authenticated employee is required.");

        var users = await db.UserList.AsNoTracking()
            .Where(x => x.EmployeeCode == employeeCode && !x.IsDeleted)
            .Select(x => new { x.Id, x.EmployeeCode, x.IsApprover, x.EmailID })
            .ToListAsync(cancellationToken);
        var matches = users.Where(x => string.Equals(x.EmailID, email, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count != 1 || matches[0].Id <= 0)
            throw new UnauthorizedAccessException("Employee identity cannot be verified.");

        var user = matches[0];
        return new AITimesheetActor(user.Id, user.EmployeeCode, user.IsApprover == true);
    }
}
