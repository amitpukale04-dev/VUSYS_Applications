using CommonLibCore10;

namespace VUSYSTimeProWebAPI.Api.Extensions
{
    public static class ApiCorsPolicy
    {
        public const string PolicyName = "_myAllowSpecificOrigins";

        public static IServiceCollection AddApiCors(
            this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy(
                    name: PolicyName,
                    policy =>
                    {
                        policy.WithOrigins(Helper.AllowCorsURL);
                        policy.AllowAnyHeader();
                        policy.AllowAnyMethod();
                        policy.AllowCredentials();
                    });
            });

            return services;
        }
    }
}
