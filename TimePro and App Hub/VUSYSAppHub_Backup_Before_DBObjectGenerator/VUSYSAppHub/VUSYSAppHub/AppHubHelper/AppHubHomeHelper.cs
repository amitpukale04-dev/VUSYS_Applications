using System.Text.Json;
using CommonLib;
using VUSYSAppHub.DACHelper;
using VUSYSAppHub.Models;
using VUSYSAppHub.Services;

namespace VUSYSAppHub.AppHubHelper
{
    public static class AppHubHomeHelper
    {
        /// <summary>
        /// This Method Is Used To Get The Environments.
        /// </summary>
        public static IReadOnlyList<string> Environments { get; } = new List<string>
        {
            "All",
            "Development",
            "Staging",
            "Production"
        };

        /// <summary>
        /// This Method Is Used To Get Filtered Apps.
        /// </summary>
        /// <param name="apps"></param>
        /// <param name="selectedProductGroup"></param>
        /// <param name="selectedEnvironment"></param>
        /// <param name="searchText"></param>
        /// <returns></returns>
        public static IEnumerable<PortalApp> FilteredApps(IReadOnlyList<PortalApp> apps, string selectedProductGroup, string selectedEnvironment, string searchText)
        {
            try
            {
                return apps
                .Where(app =>
                    app.IsActive &&
                    (selectedProductGroup == "All" || app.ProductGroup.Equals(selectedProductGroup, StringComparison.OrdinalIgnoreCase)) &&
                    (selectedEnvironment == "All" || app.Environment.Equals(selectedEnvironment, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(searchText)
                        || (!string.IsNullOrWhiteSpace(app.Name) && app.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(app.Description) && app.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(app.ProductGroup) && app.ProductGroup.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(app.Environment) && app.Environment.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        || (!string.IsNullOrWhiteSpace(app.Url) && app.Url.Contains(searchText, StringComparison.OrdinalIgnoreCase))))
                .OrderBy(app => app.ProductGroup)
                .ThenBy(app => GetEnvironmentOrder(app.Environment))
                .ThenBy(app => app.SortOrder)
                .ThenBy(app => app.Name);
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubHelper", "AppHubHomeHelper.FilteredApps()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
        /// This Method Is Used To Get Grouped Application Sections.
        /// </summary>
        /// <param name="filteredApps"></param>
        /// <returns></returns>
        public static IEnumerable<ApplicationSectionGroup> GroupedApplicationSections(IEnumerable<PortalApp> filteredApps)
        {
            try
            {
                return filteredApps
                .GroupBy(x => x.ProductGroup)
                .OrderBy(g => g.Key)
                .Select(g => new ApplicationSectionGroup
                {
                    ApplicationName = g.Key,
                    DevelopmentApps = g.Where(x => x.Environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
                                       .OrderBy(x => x.SortOrder)
                                       .ThenBy(x => x.Name)
                                       .ToList(),
                    StagingApps = g.Where(x => x.Environment.Equals("Staging", StringComparison.OrdinalIgnoreCase))
                                   .OrderBy(x => x.SortOrder)
                                   .ThenBy(x => x.Name)
                                   .ToList(),
                    ProductionApps = g.Where(x => x.Environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(x => x.SortOrder)
                                      .ThenBy(x => x.Name)
                                      .ToList()
                });
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubHelper", "AppHubHomeHelper.GroupedApplicationSections()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
        /// This Method Is Used To Get Enviroment Order.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static int GetEnvironmentOrder(string environment)
        {
            return environment switch
            {
                "Development" => 1,
                "Staging" => 2,
                "Production" => 3,
                _ => 999
            };
        }

        /// <summary>
        /// This Method Is Used To Load Application Names From JSON.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public static List<string> LoadApplicationNamesFromJson(IWebHostEnvironment env)
        {
            try
            {
                string filePath = GetConfigurationFilePath(env, "ApplicationName.json");

                if (!File.Exists(filePath))
                    return new List<string> { "All" };

                var json = File.ReadAllText(filePath);
                var names = new List<string>();

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Array)
                {
                    names = root.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x)
                        .ToList();
                }
                else if (root.ValueKind == JsonValueKind.Object &&
                         root.TryGetProperty("ApplicationNames", out var applicationNamesElement) &&
                         applicationNamesElement.ValueKind == JsonValueKind.Array)
                {
                    names = applicationNamesElement.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString() ?? string.Empty)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Where(x =>
                            string.Equals(x, "Sharpply", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x, "Protectron", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(x, "Inspire Magnetics", StringComparison.OrdinalIgnoreCase))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x)
                        .ToList();
                }

                var result = new List<string> { "All" };
                result.AddRange(names);

                return result;
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubHelper", "AppHubHomeHelper.LoadApplicationNamesFromJson()", ex.Message, false);
                return new List<string> { "All" };
            }
        }

        /// <summary>
        /// This Method Is Used To Load Portal Application Names From JSON.
        /// </summary>
        /// <param name="env"></param>
        /// <param name="portalAppService"></param>
        /// <returns></returns>
        public static IReadOnlyList<PortalApp> LoadPortalApplicationsFromJson(IWebHostEnvironment env,IPortalAppService portalAppService)
        {
            try
            {
                string filePath = GetConfigurationFilePath(env, "PortalApplication.json");

                if (!File.Exists(filePath))
                    return portalAppService.GetApplications();

                var json = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var portalConfig = JsonSerializer.Deserialize<PortalApplicationRoot>(json, options);

                if (portalConfig?.PortalApplications == null || !portalConfig.PortalApplications.Any())
                    return portalAppService.GetApplications();

                var flattenedApps = new List<PortalApp>();

                foreach (var applicationGroup in portalConfig.PortalApplications)
                {
                    if (applicationGroup?.Environments == null || !applicationGroup.Environments.Any())
                        continue;

                    foreach (var environmentGroup in applicationGroup.Environments)
                    {
                        if (environmentGroup?.Applications == null || !environmentGroup.Applications.Any())
                            continue;

                        foreach (var app in environmentGroup.Applications)
                        {
                            if (app == null)
                                continue;

                            flattenedApps.Add(new PortalApp
                            {
                                Id = app.Id,
                                Name = app.Name ?? string.Empty,
                                ProductGroup = !string.IsNullOrWhiteSpace(app.ProductGroup)
                                    ? app.ProductGroup
                                    : applicationGroup.ApplicationName ?? string.Empty,
                                Description = app.Description ?? string.Empty,
                                Url = app.Url ?? string.Empty,
                                IconCss = !string.IsNullOrWhiteSpace(app.IconCss)
                                    ? app.IconCss
                                    : "bi bi-grid-1x2-fill",
                                Environment = !string.IsNullOrWhiteSpace(app.Environment)
                                    ? app.Environment
                                    : environmentGroup.Environment ?? string.Empty,
                                IsActive = app.IsActive,
                                OpenInNewTab = app.OpenInNewTab,
                                SortOrder = app.SortOrder
                            });
                        }
                    }
                }

                return flattenedApps;
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubHelper", "AppHubHomeHelper.LoadPortalApplicationsFromJson()", ex.Message, false);
                return portalAppService.GetApplications();
            }
        }

        /// <summary>
        /// This Method Is Used To Load Application Names From JSON.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public static List<string> LoadDeploymentUpdateApplicationNamesFromJson(IWebHostEnvironment env)
        {
            try
            {
                var names = LoadApplicationNamesFromJson(env);

                var filteredNames = names
                    .Where(x => !string.Equals(x, "All", StringComparison.OrdinalIgnoreCase))
                    .Where(x =>
                        string.Equals(x, "Sharpply", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x, "Protectron", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(x, "Inspire Magnetics", StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                var result = new List<string> { "SELECT" };
                result.AddRange(filteredNames);

                return result;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubHelper", "AppHubHomeHelper.LoadDeploymentUpdateApplicationNamesFromJson()", ex.Message, false);
                return new List<string> { "SELECT" };
            }
        }

        /// <summary>
        /// This Method Is Used To Load Other Links From JSON.
        /// </summary>
        /// <param name="env"></param>
        /// <returns></returns>
        public static IReadOnlyList<PortalApp> LoadOtherLinksFromJson(IWebHostEnvironment env)
        {
            try
            {
                string filePath = GetConfigurationFilePath(env, "OtherLinks.json");

                if (string.IsNullOrWhiteSpace(filePath))
                    return new List<PortalApp>();

                var json = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var root = JsonSerializer.Deserialize<OtherLinksContainer>(json, options);
                var items = root?.OtherLinks ?? new List<PortalApp>();

                foreach (var item in items)
                {
                    item.ProductGroup = string.IsNullOrWhiteSpace(item.ProductGroup) ? "Other Links" : item.ProductGroup;
                    item.Environment = string.IsNullOrWhiteSpace(item.Environment) ? "Reference" : item.Environment;
                    item.IconCss = string.IsNullOrWhiteSpace(item.IconCss) ? "bi bi-link-45deg" : item.IconCss;
                    item.Status = string.IsNullOrWhiteSpace(item.Status) ? "Live" : item.Status;
                    item.IsActive = true;
                }

                return items;
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubHelper", "AppHubHomeHelper.LoadOtherLinksFromJson()", ex.Message, false);
                return new List<PortalApp>();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="env"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetConfigurationFilePath(IWebHostEnvironment env, string fileName)
        {
            var wwwrootPath = Path.Combine(env.WebRootPath ?? string.Empty, "Configurations", fileName);
            try
            {
                if (File.Exists(wwwrootPath))
                    return wwwrootPath;

                var contentRootPath = Path.Combine(env.ContentRootPath ?? string.Empty, "Configurations", fileName);
                if (File.Exists(contentRootPath))
                    return contentRootPath;
            }
            catch(Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.AppHubHelper", "AppHubHomeHelper.GetConfigurationFilePath()", ex.Message, false);
            }
            return wwwrootPath;
        }

        /// <summary>
        /// This Method Is Used To Get Environment Column Header Class.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static string GetEnvironmentColumnHeaderClass(string environment)
        {
            return environment switch
            {
                "Development" => "environment-column-header environment-column-header-dev",
                "Staging" => "environment-column-header environment-column-header-stage",
                "Production" => "environment-column-header environment-column-header-prod",
                _ => "environment-column-header"
            };
        }

        /// <summary>
        /// This Method Is Used To Get Environment Icon.
        /// </summary>
        /// <param name="environment"></param>
        /// <returns></returns>
        public static string GetEnvironmentIcon(string environment)
        {
            return environment switch
            {
                "Development" => "bi bi-code-slash",
                "Staging" => "bi bi-bezier2",
                "Production" => "bi bi-cloud-check",
                _ => "bi bi-grid"
            };
        }
    }
}
