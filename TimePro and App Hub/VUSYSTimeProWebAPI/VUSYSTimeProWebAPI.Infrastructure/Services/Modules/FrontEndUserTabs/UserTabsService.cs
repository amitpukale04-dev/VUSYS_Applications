using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.FrontEndUserTabs;
using VUSYSTimeProWebAPI.Application.Abstractions.Modules.Master;
using VUSYSTimeProWebAPI.Application.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.Modules.FrontEndUserTabs
{
    public sealed class UserTabsService : IUserTabsService
    {
        private const string UserTabsServiceLogClassName = "UserTabsService";
        private readonly IMasterService _MasterService;
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> FileLocks = new();
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
        private readonly string _FolderPath;

        /// <summary>
        /// User Tabs Service Class Constructor.
        /// </summary>
        /// <param name="MasterService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public UserTabsService(IMasterService MasterService)
        {
            _FolderPath = CommonHelper.KeyOption.FrontEndUserTabs;
            _MasterService = MasterService ?? throw new ArgumentNullException(nameof(MasterService));
        }

        #region [User Tabs]

        /// <summary>
        /// This Method Is Used To Get User Tabs.
        /// </summary>
        /// <param name="_objUserTabsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> GetUserTabs(UserTabsRequestParam _objUserTabsRequestParam, CancellationToken cancellationToken = default)
        {
            string TableName = "USER_TAB_RESPONSE";
            try
            {
                int userId = _objUserTabsRequestParam.UserId;
                int roleId = _objUserTabsRequestParam.RoleId;
                Dictionary<string, UserTabM> permitted = await GetPermittedRoutesAsync(userId, roleId, cancellationToken);
                SemaphoreSlim fileLock = FileLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
                await fileLock.WaitAsync(cancellationToken);
                try
                {
                    string path = GetFilePath(userId);
                    List<UserTabM> tabs = File.Exists(path)
                        ? FilterTabs((await ReadFileAsync(path, userId, cancellationToken)).Tabs, permitted)
                        : new List<UserTabM> { HomeTab() };
                    return TabsDataSet(userId, tabs);
                }
                finally
                {
                    fileLock.Release();
                }
            }
            catch (JsonException ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{UserTabsServiceLogClassName}.GetUserTabs()", ex.Message, false);
                return StatusDataSet(TableName, Helper.ToProperCase(MessageTypeConstants.Error), "Stored User Tabs JSON Is Corrupted. File Was Not Modified.");
            }
            catch (InvalidDataException ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{UserTabsServiceLogClassName}.GetUserTabs()", ex.Message, false);
                return StatusDataSet(TableName, Helper.ToProperCase(MessageTypeConstants.Error), ex.Message);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{UserTabsServiceLogClassName}.GetUserTabs()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
        /// This Method Is Used To Save User Tabs.
        /// </summary>
        /// <param name="_objSaveUserTabsRequestParam"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<DataSet> SaveUserTabs(SaveUserTabsRequestParam _objSaveUserTabsRequestParam, CancellationToken cancellationToken = default)
        {
            string TableName = "USER_TAB_SAVED_RESPONSE";
            try
            {
                int userId = _objSaveUserTabsRequestParam.UserId;
                int roleId = _objSaveUserTabsRequestParam.RoleId;
                if (_objSaveUserTabsRequestParam.Tabs == null || _objSaveUserTabsRequestParam.Tabs.Count > 50)
                    throw new ArgumentException("Tabs Must Be Provided And Cannot Exceed 50 Entries.");

                Dictionary<string, UserTabM> permitted = await GetPermittedRoutesAsync(userId, roleId, cancellationToken);
                List<UserTabM> tabs = ValidateTabs(_objSaveUserTabsRequestParam.Tabs, permitted);
                SemaphoreSlim fileLock = FileLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
                await fileLock.WaitAsync(cancellationToken);
                try
                {
                    string path = GetFilePath(userId);
                    if (File.Exists(path))
                        await ReadFileAsync(path, userId, cancellationToken);

                    Directory.CreateDirectory(_FolderPath);
                    string temporaryPath = Path.Combine(_FolderPath, $"User_{userId}.{Guid.NewGuid():N}.tmp");
                    try
                    {
                        string json = JsonSerializer.Serialize(new UserTabsFileM { UserId = userId, Tabs = tabs }, JsonOptions);
                        await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        File.Move(temporaryPath, path, true);
                    }
                    finally
                    {
                        if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
                    }
                    return StatusDataSet(TableName, Helper.ToProperCase(MessageTypeConstants.Success), "User Tabs Saved Successfully.", userId);
                }
                finally
                {
                    fileLock.Release();
                }
            }
            catch (JsonException ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{UserTabsServiceLogClassName}.SaveUserTabs()", ex.Message, false);
                return StatusDataSet(TableName, Helper.ToProperCase(MessageTypeConstants.Error), "Existing User Tabs JSON Is Corrupted. File Was Not Overwritten.");
            }
            catch (InvalidDataException ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{UserTabsServiceLogClassName}.SaveUserTabs()", ex.Message, false);
                return StatusDataSet(TableName, Helper.ToProperCase(MessageTypeConstants.Error), ex.Message);
            }
            catch (ArgumentException ex)
            {
                return StatusDataSet(TableName, Helper.ToProperCase(MessageTypeConstants.Error), ex.Message);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{UserTabsServiceLogClassName}.SaveUserTabs()", ex.Message, false);
                throw;
            }
        }

        #endregion [User Tabs]

        #region [Custom Methods]

        /// <summary>
        /// This Method Is Used To Get File Path.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private string GetFilePath(int userId) => Path.Combine(_FolderPath, $"User_{userId}.json");

        /// <summary>
        /// This Method Is Used For Home Tab.
        /// </summary>
        /// <returns></returns>
        private static UserTabM HomeTab() => new UserTabM
        {
            Path = "/",
            Label = "Home",
            SvgIcon = "home",
            Tone = "indigo",
            Closable = false
        };

        /// <summary>
        /// This Method Is Used To Filter Tabs.
        /// </summary>
        /// <param name="stored"></param>
        /// <param name="permitted"></param>
        /// <returns></returns>
        private static List<UserTabM> FilterTabs(IEnumerable<UserTabM> stored, Dictionary<string, UserTabM> permitted)
        {
            List<UserTabM> result = new() { HomeTab() };
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase) { "/" };
            foreach (UserTabM tab in stored)
            {
                string path = tab.Path?.Trim() ?? string.Empty;
                if (seen.Add(path) && permitted.TryGetValue(path, out UserTabM? trusted)) result.Add(trusted);
            }
            return result;
        }

        /// <summary>
        /// This Method Is Used To Validate Tabs.
        /// </summary>
        /// <param name="submitted"></param>
        /// <param name="permitted"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private static List<UserTabM> ValidateTabs(IEnumerable<UserTabM> submitted, Dictionary<string, UserTabM> permitted)
        {
            List<UserTabM> result = new() { HomeTab() };
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase) { "/" };
            foreach (UserTabM tab in submitted)
            {
                string path = tab.Path?.Trim() ?? string.Empty;
                if (path.Length == 0 || path.Length > 512 || !path.StartsWith('/') || path.StartsWith("//"))
                    throw new ArgumentException("Invalid User Tab Path.");
                if (path == "/") continue;
                if (!permitted.TryGetValue(path, out UserTabM? trusted))
                    throw new ArgumentException($"User Does Not Have Permission To Open Route: {path}");
                if (seen.Add(path)) result.Add(trusted);
            }
            return result;
        }

        /// <summary>
        /// This Method Is Used To Read File Async.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="userId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        private static async Task<UserTabsFileM> ReadFileAsync(string path, int userId, CancellationToken cancellationToken)
        {
            await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
            UserTabsFileM? stored = await JsonSerializer.DeserializeAsync<UserTabsFileM>(stream, JsonOptions, cancellationToken);
            if (stored == null || stored.UserId != userId || stored.Tabs == null ||
                stored.Tabs.Any(t => t == null || string.IsNullOrWhiteSpace(t.Path)))
                throw new InvalidDataException("Stored User Tabs File Is Invalid. File Was Not Modified.");
            return stored;
        }

        /// <summary>
        /// This Method Is Used To Get Permitted User Tab Routes.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Dictionary<string, UserTabM>> GetPermittedRoutesAsync(int userId, int roleId, CancellationToken cancellationToken = default)
        {
            Dictionary<string, UserTabM> permittedRoutes = new Dictionary<string, UserTabM>(StringComparer.OrdinalIgnoreCase);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                UserMenuAccessRequestParam _objUserMenuAccessRequestParam = new UserMenuAccessRequestParam
                {
                    UserId = userId,
                    RoleID = roleId,
                    ParamType = "Menu"
                };
                string jsonResponse = await _MasterService.GetUserMenuAccess(_objUserMenuAccessRequestParam, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(jsonResponse))
                {
                    JObject jsonObject = JObject.Parse(jsonResponse);
                    string status = jsonObject["Status"]?.ToString() ?? string.Empty;
                    if (!string.Equals(status, Helper.ToProperCase(MessageTypeConstants.Success), StringComparison.OrdinalIgnoreCase))
                    {
                        return permittedRoutes;
                    }
                    JToken? menuAccessData = jsonObject["data"]?["USER_MENU_ACCESS_RESPONSE"];
                    if (menuAccessData is JArray mainMenus)
                    {
                        foreach (JToken mainMenu in mainMenus)
                        {
                            if (mainMenu["SubMenus"] is not JArray subMenus)
                            {
                                continue;
                            }
                            foreach (JToken subMenu in subMenus)
                            {
                                string routePath = (subMenu["RoutePath"]?.ToString() ?? string.Empty).Trim();
                                bool hasOpenPermission = subMenu["SecurityAccessIds"] is JArray securityAccessIds && securityAccessIds.Count > 0;
                                if (!hasOpenPermission || string.IsNullOrWhiteSpace(routePath) || routePath == "/" || !routePath.StartsWith("/") || routePath.StartsWith("//"))
                                {
                                    continue;
                                }
                                if (!permittedRoutes.ContainsKey(routePath))
                                {
                                    permittedRoutes.Add(routePath, new UserTabM
                                    {
                                        Path = routePath,
                                        Label = subMenu["SubMenu"]?.ToString() ?? string.Empty,
                                        SvgIcon = subMenu["SvgIcon"]?.ToString() ?? string.Empty,
                                        Tone = subMenu["Tone"]?.ToString() ?? string.Empty,
                                        Closable = true
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureEmployeeLogNamespace, $"{UserTabsServiceLogClassName}.GetPermittedRoutesAsync()", ex.Message, false);
                throw;
            }
            return permittedRoutes;
        }

        /// <summary>
        /// This Method Is Used For Tabs DataSet.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="tabs"></param>
        /// <returns></returns>
        private static DataSet TabsDataSet(int userId, IEnumerable<UserTabM> tabs)
        {
            DataTable table = new("USER_TAB_RESPONSE");
            table.Columns.Add("UserId", typeof(int));
            table.Columns.Add("Path", typeof(string));
            table.Columns.Add("Label", typeof(string));
            table.Columns.Add("SvgIcon", typeof(string));
            table.Columns.Add("Tone", typeof(string));
            table.Columns.Add("Closable", typeof(bool));
            foreach (UserTabM tab in tabs)
                table.Rows.Add(userId, tab.Path, tab.Label, tab.SvgIcon, tab.Tone, tab.Closable);
            DataSet ds = new();
            ds.Tables.Add(table);
            return ds;
        }

        /// <summary>
        /// This Method Is Used For Status DataSet.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        private static DataSet StatusDataSet(string tableName, string status, string message, int userId = 0)
        {
            DataTable table = new(tableName);
            table.Columns.Add("ID", typeof(int));
            table.Columns.Add("Status", typeof(string));
            table.Columns.Add("Message", typeof(string));
            table.Rows.Add(userId, status, message);
            DataSet ds = new();
            ds.Tables.Add(table);
            return ds;
        }

        #endregion [Custom Methods]
    }
}
