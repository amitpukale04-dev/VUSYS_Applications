using System.Security.Claims;
namespace VUSYSTimeProWebAPI.Api.Middlewares
{
    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// User Context Middleware Class Constructor
        /// </summary>
        /// <param name="next"></param>
        public UserContextMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                if (!context.Items.ContainsKey("email_id") || context.Items["email_id"] == null)
                {
                    string emailId = ExtractEmailId(context);
                    context.Items["email_id"] = string.IsNullOrWhiteSpace(emailId) ? "UnknownUser" : emailId;
                }
                if (!context.Items.ContainsKey("employee_code") || context.Items["employee_code"] == null)
                {
                    string employeeCode = ExtractEmployeeCode(context);
                    context.Items["employee_code"] = string.IsNullOrWhiteSpace(employeeCode) ? null : employeeCode;
                }
                if (!context.Items.ContainsKey("role_id") || context.Items["role_id"] == null)
                {
                    string roleId = ExtractRoleID(context);
                    context.Items["role_id"] = string.IsNullOrWhiteSpace(roleId) ? null : roleId;
                }
            }
            catch
            {
                if (!context.Items.ContainsKey("email_id") || context.Items["email_id"] == null)
                {
                    context.Items["email_id"] = "UnknownUser";
                }
                if (!context.Items.ContainsKey("employee_code") || context.Items["employee_code"] == null)
                {
                    context.Items["employee_code"] = null;
                }
                if (!context.Items.ContainsKey("role_id") || context.Items["role_id"] == null)
                {
                    context.Items["role_id"] = null;
                }
            }
            await _next(context);
        }

        /// <summary>
        /// This Method Is Used To Extract Email ID.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string ExtractEmailId(HttpContext context)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                string claimValue =
                    user.FindFirst("EmailID")?.Value ??
                    user.FindFirst("EmailId")?.Value ??
                    user.FindFirst("emailid")?.Value ??
                    user.FindFirst("email_id")?.Value ??
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    user.FindFirst("sub")?.Value;

                if (!string.IsNullOrWhiteSpace(claimValue))
                    return claimValue;
            }
            return null;
        }

        /// <summary>
        /// This Method Is Used To Extract Employee Code.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string ExtractEmployeeCode(HttpContext context)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                string claimValue =
                    user.FindFirst("EmployeeCode")?.Value ??
                    user.FindFirst("employeecode")?.Value ??
                    user.FindFirst("employee_code")?.Value ??
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    user.FindFirst("sub")?.Value;

                if (!string.IsNullOrWhiteSpace(claimValue))
                    return claimValue;
            }
            return null;
        }

        /// <summary>
        /// This Method Is Used To Extract Role ID.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string ExtractRoleID(HttpContext context)
        {
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                string claimValue =
                    user.FindFirst("RoleID")?.Value ??
                    user.FindFirst("RoleId")?.Value ??
                    user.FindFirst("roleid")?.Value ??
                    user.FindFirst("role_id")?.Value ??
                    user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                    user.FindFirst("sub")?.Value;

                if (!string.IsNullOrWhiteSpace(claimValue))
                    return claimValue;
            }
            return null;
        }
    }
}
