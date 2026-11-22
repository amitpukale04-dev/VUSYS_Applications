using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommonLibCore10;
using CommonLibCore10.Constants;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Api.Models.FileUploads;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Infrastructure.Common;
using IFileUploadAbstractService = VUSYSTimeProWebAPI.Application.Abstractions.FileUploads.IFileUploadService;

namespace VUSYSTimeProWebAPI.Api.Services.FileUploads
{
    public sealed class FileUploadService : IFileUploadService
    {
        private const string FileUploadServiceLogClassName = "FileUploadService";
        private readonly IFileUploadAbstractService _FileUploadAbstractSvc;
        private readonly IContentTypeService _ContentTypeSvc;

        /// <summary>
        /// File Upload Service Class Constructor
        /// </summary>
        /// <param name="FileUploadAbstractSvc"></param>
        /// <param name="ContentTypeSvc"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FileUploadService(IFileUploadAbstractService FileUploadAbstractSvc, IContentTypeService ContentTypeSvc)
        {
            _FileUploadAbstractSvc = FileUploadAbstractSvc ?? throw new ArgumentNullException(nameof(FileUploadAbstractSvc));
            _ContentTypeSvc = ContentTypeSvc ?? throw new ArgumentNullException(nameof(ContentTypeSvc));
        }

        #region [File Upload]

        /// <summary>
        /// This Method is Used to Upload The Files.
        /// </summary>
        /// <param name="_objCommonFileUploadRequestParam"></param>
        /// <returns></returns>
        public async Task<DataSet> UploadFiles(CommonFileUploadRequestParam _objCommonFileUploadRequestParam)
        {
            DataSet finalDataSet = new DataSet();

            // Local helper to return your standard message dataset
            DataSet BuildError(string message)
            {
                List<string> cols = new List<string> { "Status", "Message" };
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), message }
                };
                return Helper.GenerateCommonMessage(cols, rows, _objCommonFileUploadRequestParam.TableName);
            }
            try
            {
                // Validate file data
                if (_objCommonFileUploadRequestParam.FileData == null || !_objCommonFileUploadRequestParam.FileData.Any())
                {
                    return BuildError("No Files Found In The Request. !!!");
                }

                // Validate base path
                if (string.IsNullOrWhiteSpace(_objCommonFileUploadRequestParam.BasePath))
                {
                    return BuildError(_objCommonFileUploadRequestParam.BasePathErrorMessage);
                }

                // Normalize paths (important for UNC)
                _objCommonFileUploadRequestParam.BasePath =
                    _objCommonFileUploadRequestParam.BasePath.Trim().Replace("/", "\\");

                _objCommonFileUploadRequestParam.FolderFullPath =
                    (_objCommonFileUploadRequestParam.FolderFullPath ?? string.Empty).Trim().Replace("/", "\\");

                // Allow BOTH: UNC path and Local rooted path
                bool isUNCPath = _objCommonFileUploadRequestParam.BasePath.StartsWith(@"\\");
                bool isLocalRooted = Path.IsPathFullyQualified(_objCommonFileUploadRequestParam.BasePath);

                if (!isUNCPath && !isLocalRooted)
                {
                    return BuildError(@"Invalid FileUploadPath. It must be a local path (C:\Folder) or a shared UNC path (\\Server\Share).");
                }

                // Ensure FolderFullPath is within BasePath (prevents path traversal / wrong combine)
                try
                {
                    var baseFull = Path.GetFullPath(_objCommonFileUploadRequestParam.BasePath)
                                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                       + Path.DirectorySeparatorChar;

                    var folderFull = Path.GetFullPath(_objCommonFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    if (!folderFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return BuildError("Invalid folder path. FolderFullPath must be inside BasePath. !!!");
                    }
                }
                catch
                {
                    return BuildError("Invalid folder path format. !!!");
                }

                // Create directory with good error handling (UNC failures are common)
                try
                {
                    Directory.CreateDirectory(_objCommonFileUploadRequestParam.FolderFullPath);
                }
                catch (UnauthorizedAccessException)
                {
                    return BuildError("Access denied to the folder. Please grant Read/Write permission to the API service/IIS AppPool identity. !!!");
                }
                catch (DirectoryNotFoundException)
                {
                    return BuildError("Folder path not found. Please verify the path and connectivity. !!!");
                }
                catch (IOException ioEx)
                {
                    return BuildError($"Unable to create/access folder. {ioEx.Message}");
                }

                var saved = new List<object>();

                foreach (var file in _objCommonFileUploadRequestParam.FileData)
                {
                    if (file == null || file.Length <= 0)
                    {
                        saved.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = "",
                            Message = "Empty file found in request. !!!"
                        });
                        continue;
                    }

                    var ext = Path.GetExtension(file.FileName);
                    var safeBase = CommonHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
                    if (string.IsNullOrWhiteSpace(safeBase)) safeBase = "File";

                    string uniqueName;
                    if (!string.IsNullOrEmpty(_objCommonFileUploadRequestParam.CustomFileName))
                    {
                        var safeCustom = CommonHelper.SanitizeFileName(_objCommonFileUploadRequestParam.CustomFileName);
                        if (string.IsNullOrWhiteSpace(safeCustom)) safeCustom = "File";
                        uniqueName = $"{safeCustom}{ext}";
                    }
                    else
                    {
                        uniqueName = $"{safeBase}_{DateTime.UtcNow.ToString(DateTimeFormatConstants.YYYYMMDDHHMMSSFFF_COMPACT_DATETIMEFORMAT)}{ext}";
                    }

                    var fullPath = Path.Combine(_objCommonFileUploadRequestParam.FolderFullPath, uniqueName);

                    try
                    {
                        using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                        {
                            await file.CopyToAsync(fs);
                        }

                        saved.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Success),
                            FileName = uniqueName,
                            Message = "File Uploaded Successfully !!!"
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        saved.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = uniqueName,
                            Message = "Access denied while saving file. Please check folder permissions. !!!"
                        });
                    }
                    catch (IOException ioEx)
                    {
                        saved.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = uniqueName,
                            Message = $"File save failed. {ioEx.Message}"
                        });
                    }
                }

                // Folder details dataset
                var firstRecord = saved.FirstOrDefault();
                string SavedStatus = string.Empty;
                if (firstRecord != null)
                {
                    SavedStatus = (string)firstRecord.GetType().GetProperty("Status")?.GetValue(firstRecord);
                }

                List<string> HeaderColumns = new List<string> { "Status" };
                HeaderColumns.AddRange(_objCommonFileUploadRequestParam.DynamicHeaders ?? new List<string>());
                HeaderColumns.Add("Folder");

                List<string> headerRow = new List<string> { SavedStatus };
                headerRow.AddRange((_objCommonFileUploadRequestParam.DynamicRows ?? new List<string>()).Select(x => x?.ToString() ?? ""));
                headerRow.Add(_objCommonFileUploadRequestParam.FolderFullPath);

                List<List<string>> HeaderRows = new List<List<string>> { headerRow };
                var FolderDetailsDataSet = Helper.GenerateCommonMessage(HeaderColumns, HeaderRows, "FOLDER_DETAILS");

                // Uploaded response dataset
                List<string> UploadedColumns = new List<string> { "Status", "FileName", "Message" };
                List<List<string>> UploadedRows = new();

                foreach (var fileObj in saved)
                {
                    var type = fileObj.GetType();
                    string Status = type.GetProperty("Status")?.GetValue(fileObj)?.ToString() ?? "";
                    string FileName = type.GetProperty("FileName")?.GetValue(fileObj)?.ToString() ?? "";
                    string Message = type.GetProperty("Message")?.GetValue(fileObj)?.ToString() ?? "";

                    UploadedRows.Add(new List<string> { Status, FileName, Message });
                }

                var FileUploadedResponse = Helper.GenerateCommonMessage(UploadedColumns, UploadedRows, "UPLOADED_RESPONSE");
                if (Helper.CheckDataSetIsNullOrEmpty(FolderDetailsDataSet))
                {
                    finalDataSet.Merge(FolderDetailsDataSet);
                }
                if (Helper.CheckDataSetIsNullOrEmpty(FileUploadedResponse))
                {
                    finalDataSet.Merge(FileUploadedResponse);
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.UploadFiles()", ex.ToString(), false);
                throw;
            }

            return finalDataSet;
        }



        /// <summary>
        /// This Method is Used to Upload The Email Files.
        /// </summary>
        /// <param name="_objCommonFileUploadRequestParam"></param>
        /// <returns></returns>
        public async Task<DataSet> UploadMailFiles(CommonFileUploadRequestParam _objCommonFileUploadRequestParam)
        {
            DataSet finalDataSet = new DataSet();

            DataSet BuildError(string message)
            {
                List<string> cols = new List<string> { "Status", "Message" };
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), message }
                };
                return Helper.GenerateCommonMessage(cols, rows, _objCommonFileUploadRequestParam.TableName);
            }
            try
            {
                // Basic validations
                if (_objCommonFileUploadRequestParam.FileData == null || !_objCommonFileUploadRequestParam.FileData.Any())
                    return BuildError("No Files Found In The Request. !!!");

                if (string.IsNullOrWhiteSpace(_objCommonFileUploadRequestParam.BasePath))
                    return BuildError(_objCommonFileUploadRequestParam.BasePathErrorMessage);

                if (string.IsNullOrWhiteSpace(_objCommonFileUploadRequestParam.FolderFullPath))
                    return BuildError("FolderFullPath is empty. !!!");

                // Normalize paths (important for UNC)
                _objCommonFileUploadRequestParam.BasePath =
                    _objCommonFileUploadRequestParam.BasePath.Trim().Replace("/", "\\");

                _objCommonFileUploadRequestParam.FolderFullPath =
                    _objCommonFileUploadRequestParam.FolderFullPath.Trim().Replace("/", "\\");

                // Allow BOTH local rooted path + UNC path
                bool isUNCPath = _objCommonFileUploadRequestParam.BasePath.StartsWith(@"\\");
                bool isLocalRooted = Path.IsPathFullyQualified(_objCommonFileUploadRequestParam.BasePath);

                if (!isUNCPath && !isLocalRooted)
                    return BuildError("Invalid FileUploadPath. It must be a local path (C:\\Folder) or a shared UNC path (\\\\Server\\Share).");

                // Ensure FolderFullPath is inside BasePath (prevents wrong/unsafe write)
                try
                {
                    var baseFull = Path.GetFullPath(_objCommonFileUploadRequestParam.BasePath)
                                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                       + Path.DirectorySeparatorChar;

                    var folderFull = Path.GetFullPath(_objCommonFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    if (!folderFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                        return BuildError("Invalid folder path. FolderFullPath must be inside BasePath. !!!");
                }
                catch
                {
                    return BuildError("Invalid folder path format. !!!");
                }

                // Create directory with proper UNC error handling
                try
                {
                    Directory.CreateDirectory(_objCommonFileUploadRequestParam.FolderFullPath);
                }
                catch (UnauthorizedAccessException)
                {
                    return BuildError("Access denied to the folder. Please grant permission to the API service/IIS AppPool identity. !!!");
                }
                catch (DirectoryNotFoundException)
                {
                    return BuildError("Shared folder path not found. Please verify the network share path and connectivity. !!!");
                }
                catch (IOException ioEx)
                {
                    return BuildError($"Unable to create/access folder. {ioEx.Message}");
                }

                // Save files
                var savedForMeta = new List<object>();     // for meta.json (Name/Size/Extension)
                var savedForResponse = new List<object>(); // for UPLOADED_RESPONSE (Status/FileName/Message)

                foreach (var file in _objCommonFileUploadRequestParam.FileData)
                {
                    if (file == null || file.Length <= 0)
                    {
                        savedForResponse.Add(new { Status = Helper.ToProperCase(MessageTypeConstants.Error), FileName = "", Message = "Empty file found in request. !!!" });
                        continue;
                    }

                    var ext = Path.GetExtension(file.FileName);
                    var safeBase = CommonHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
                    if (string.IsNullOrWhiteSpace(safeBase)) safeBase = "File";

                    var uniqueName = $"{safeBase}_{DateTime.UtcNow.ToString(DateTimeFormatConstants.YYYYMMDDHHMMSSFFF_COMPACT_DATETIMEFORMAT)}{ext}";
                    var fullPath = Path.Combine(_objCommonFileUploadRequestParam.FolderFullPath, uniqueName);

                    try
                    {
                        using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                        {
                            await file.CopyToAsync(fs);
                        }

                        savedForResponse.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Success),
                            FileName = uniqueName,
                            Message = "File Uploaded Successfully !!!"
                        });

                        savedForMeta.Add(new
                        {
                            Name = uniqueName,
                            Size = file.Length,
                            Extension = ext
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        savedForResponse.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = uniqueName,
                            Message = "Access denied while saving file. Please check folder permissions. !!!"
                        });
                    }
                    catch (IOException ioEx)
                    {
                        savedForResponse.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = uniqueName,
                            Message = $"File save failed. {ioEx.Message}"
                        });
                    }
                }

                // 6) Write meta.json (Id + UploadedFiles + UploadedUtc)
                // Keep ID stable if meta.json already exists
                var folder = _objCommonFileUploadRequestParam.FolderFullPath;
                var metaPath = Path.Combine(folder, "meta.json");

                string id = Guid.NewGuid().ToString("N");
                try
                {
                    if (File.Exists(metaPath))
                    {
                        var existing = await File.ReadAllTextAsync(metaPath);
                        if (!string.IsNullOrWhiteSpace(existing))
                        {
                            using var doc = JsonDocument.Parse(existing);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("Id", out var idEl))
                            {
                                var existingId = idEl.GetString();
                                if (!string.IsNullOrWhiteSpace(existingId))
                                    id = existingId;
                            }
                        }
                    }
                }
                catch
                {
                    // ignore meta read errors; we will overwrite meta.json
                }

                try
                {
                    await File.WriteAllTextAsync(metaPath,
                        JsonSerializer.Serialize(new
                        {
                            Id = id,
                            UploadedFiles = savedForMeta,
                            UploadedUtc = DateTime.UtcNow
                        })
                    );
                }
                catch (UnauthorizedAccessException)
                {
                    // Upload succeeded but meta write failed -> still return upload results
                    Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, "UploadMailFiles.MetaJson", "Access denied while writing meta.json", false);
                }
                catch (Exception metaEx)
                {
                    Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, "UploadMailFiles.MetaJson", metaEx.ToString(), false);
                }

                // Build datasets (keep your existing output format)
                var firstRecord = savedForResponse.FirstOrDefault();
                string savedStatus = string.Empty;
                if (firstRecord != null)
                {
                    savedStatus = (string)firstRecord.GetType().GetProperty("Status")?.GetValue(firstRecord);
                }

                List<string> HeaderColumns = new List<string> { "Status" };
                HeaderColumns.AddRange(_objCommonFileUploadRequestParam.DynamicHeaders ?? new List<string>());
                HeaderColumns.Add("Folder");

                List<string> headerRow = new List<string> { savedStatus };
                headerRow.AddRange((_objCommonFileUploadRequestParam.DynamicRows ?? new List<string>()).Select(x => x?.ToString() ?? ""));
                headerRow.Add(_objCommonFileUploadRequestParam.FolderFullPath);

                var FolderDetailsDataSet = Helper.GenerateCommonMessage(
                    HeaderColumns,
                    new List<List<string>> { headerRow },
                    "FOLDER_DETAILS"
                );

                List<string> UploadedColumns = new List<string> { "Status", "FileId", "FileName", "Message" };
                List<List<string>> UploadedRows = new();

                foreach (var fileObj in savedForResponse)
                {
                    var type = fileObj.GetType();
                    string Status = type.GetProperty("Status")?.GetValue(fileObj)?.ToString() ?? "";
                    string FileName = type.GetProperty("FileName")?.GetValue(fileObj)?.ToString() ?? "";
                    string Message = type.GetProperty("Message")?.GetValue(fileObj)?.ToString() ?? "";

                    UploadedRows.Add(new List<string> { Status, id, FileName, Message });
                }

                var FileUploadedResponse = Helper.GenerateCommonMessage(UploadedColumns, UploadedRows, "UPLOADED_RESPONSE");
                if (Helper.CheckDataSetIsNullOrEmpty(FolderDetailsDataSet))
                    finalDataSet.Merge(FolderDetailsDataSet);
                if (Helper.CheckDataSetIsNullOrEmpty(FileUploadedResponse))
                    finalDataSet.Merge(FileUploadedResponse);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.UploadMailFiles()", ex.ToString(), false);
                throw;
            }

            return finalDataSet;
        }

        /// <summary>
        /// This Method is Used to View Uploaded Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        public async Task<DataSet> ViewUploadedFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam)
        {
            DataSet finalDataSet = new DataSet();

            DataSet BuildError(string message)
            {
                List<string> columns = new List<string> { "Status", "Message" };
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), message }
                };
                return Helper.GenerateCommonMessage(columns, rows, _objCommonViewFileUploadRequestParam.TableName);
            }
            try
            {
                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.BasePath))
                {
                    return BuildError(_objCommonViewFileUploadRequestParam.BasePathErrorMessage);
                }
                _objCommonViewFileUploadRequestParam.BasePath = _objCommonViewFileUploadRequestParam.BasePath.Trim().Replace("/", "\\");
                _objCommonViewFileUploadRequestParam.FolderFullPath = (_objCommonViewFileUploadRequestParam.FolderFullPath ?? string.Empty).Trim().Replace("/", "\\");

                // Allow BOTH: local rooted path + UNC path
                bool isUNCPath = _objCommonViewFileUploadRequestParam.BasePath.StartsWith(@"\\");
                bool isLocalRooted = Path.IsPathFullyQualified(_objCommonViewFileUploadRequestParam.BasePath);

                if (!isUNCPath && !isLocalRooted)
                {
                    return BuildError(@"Invalid FileUploadPath. It must be a local path (C:\Folder) or a shared UNC path (\\Server\Share).");
                }

                // Ensure FolderFullPath is within BasePath
                try
                {
                    var baseFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.BasePath)
                                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                       + Path.DirectorySeparatorChar;

                    var folderFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    if (!folderFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return BuildError("Invalid folder path. FolderFullPath must be inside BasePath. !!!");
                    }
                }
                catch
                {
                    return BuildError("Invalid folder path format. !!!");
                }

                // Don't rely on Directory.Exists for UNC. It can return false on permission issues.
                try
                {
                    _ = Directory.EnumerateFileSystemEntries(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                 .Take(1)
                                 .ToList();
                }
                catch (UnauthorizedAccessException)
                {
                    return BuildError("Access denied to the folder. Please grant permission to the API service/IIS AppPool identity. !!!");
                }
                catch (DirectoryNotFoundException)
                {
                    return BuildError("Files Not Found !!!");
                }
                catch (IOException ioEx)
                {
                    return BuildError($"Unable to access folder. {ioEx.Message}");
                }

                List<string> files = null;

                try
                {
                    if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.FileName))
                    {
                        if (!string.IsNullOrEmpty(_objCommonViewFileUploadRequestParam.CustomFileName))
                        {
                            string searchToken = $"_{_objCommonViewFileUploadRequestParam.CustomFileName}.";
                            files = Directory.EnumerateFiles(_objCommonViewFileUploadRequestParam.FolderFullPath, "*", SearchOption.TopDirectoryOnly)
                                    .Where(x => Path.GetFileName(x).Contains(searchToken, StringComparison.OrdinalIgnoreCase))
                                    .OrderBy(x => x)
                                    .ToList();
                        }
                        else
                        {
                            if (!_objCommonViewFileUploadRequestParam.IsIncludeCustomFileName)
                            {
                                var excludeRegex = new Regex("_FileName[1-6]", RegexOptions.IgnoreCase);
                                files = Directory.EnumerateFiles(_objCommonViewFileUploadRequestParam.FolderFullPath, "*", SearchOption.TopDirectoryOnly)
                                    .Where(f =>
                                    {
                                        var fileName = Path.GetFileName(f);
                                        return !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                                               && !excludeRegex.IsMatch(fileName);
                                    })
                                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                                    .ToList();
                            }
                            else
                            {
                                files = Directory.EnumerateFiles(_objCommonViewFileUploadRequestParam.FolderFullPath, "*", SearchOption.TopDirectoryOnly)
                                    .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                                    .ToList();
                            }
                        }
                    }
                    else
                    {
                        string filePath = !string.IsNullOrEmpty(_objCommonViewFileUploadRequestParam.CustomFileName)
                            ? Path.Combine(_objCommonViewFileUploadRequestParam.FolderFullPath, _objCommonViewFileUploadRequestParam.CustomFileName)
                            : Path.Combine(_objCommonViewFileUploadRequestParam.FolderFullPath, _objCommonViewFileUploadRequestParam.FileName);

                        if (File.Exists(filePath))
                        {
                            files = new List<string> { filePath };
                        }
                        else
                        {
                            return BuildError("File Not Found !!!");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    return BuildError("Access denied to the folder. Please grant read permission to the API service/IIS AppPool identity. !!!");
                }
                catch (IOException ioEx)
                {
                    return BuildError($"Unable to read files from folder. {ioEx.Message}");
                }

                if (files == null || files.Count == 0)
                {
                    return BuildError("No Files Found !!!");
                }

                var results = new List<object>();

                foreach (var path in files)
                {
                    var fi = new FileInfo(path);
                    var contentType = _ContentTypeSvc.GetFileContentType(fi.Name);
                    var modifiedDate = File.GetLastWriteTime(path);
                    string formattedModifiedDate = modifiedDate.ToString(CommonHelper.KeyOption.FileUploadedModifiedDateFormat, CultureInfo.InvariantCulture);

                    var extension = fi.Extension.ToLowerInvariant();
                    string base64 = string.Empty;

                    if (extension != ".eml" && extension != ".msg")
                    {
                        try
                        {
                            var bytes = File.ReadAllBytes(path);
                            base64 = Convert.ToBase64String(bytes);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            base64 = string.Empty;
                        }
                    }
                    else if (extension == ".msg")
                    {
                        try
                        {
                            var bytes = File.ReadAllBytes(path);
                            base64 = Convert.ToBase64String(bytes);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            base64 = string.Empty;
                        }
                    }

                    results.Add(new
                    {
                        Status = Helper.ToProperCase(MessageTypeConstants.Success),
                        FileName = fi.Name,
                        ReceivedDate = formattedModifiedDate,
                        ContentType = contentType,
                        Base64 = base64
                    });
                }

                List<string> ViewUploadedColumns = new List<string> { "Status", "FileName", "ReceivedDate", "ContentType", "Base64" };
                List<List<string>> ViewUploadedRows = new();

                foreach (var fileObj in results)
                {
                    var type = fileObj.GetType();
                    string Status = type.GetProperty("Status")?.GetValue(fileObj)?.ToString() ?? "";
                    string FileName = type.GetProperty("FileName")?.GetValue(fileObj)?.ToString() ?? "";
                    string ReceivedDate = type.GetProperty("ReceivedDate")?.GetValue(fileObj)?.ToString() ?? "";
                    string ContentType = type.GetProperty("ContentType")?.GetValue(fileObj)?.ToString() ?? "";
                    string Base64 = type.GetProperty("Base64")?.GetValue(fileObj)?.ToString() ?? "";

                    ViewUploadedRows.Add(new List<string> { Status, FileName, ReceivedDate, ContentType, Base64 });
                }

                finalDataSet = Helper.GenerateCommonMessage(ViewUploadedColumns, ViewUploadedRows, _objCommonViewFileUploadRequestParam.TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.ViewUploadedFiles()", ex.ToString(), false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method is Used to View Uploaded Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        public async Task<DataSet> ViewUploadedMailFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam)
        {
            DataSet finalDataSet = new DataSet();

            DataSet BuildError(string message)
            {
                List<string> cols = new List<string> { "Status", "Message" };
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), message }
                };
                return Helper.GenerateCommonMessage(cols, rows, _objCommonViewFileUploadRequestParam.TableName);
            }
            try
            {
                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.BasePath))
                {
                    return BuildError(_objCommonViewFileUploadRequestParam.BasePathErrorMessage);
                }

                // Normalize (important for UNC + accidental slashes/spaces)
                _objCommonViewFileUploadRequestParam.BasePath =
                    _objCommonViewFileUploadRequestParam.BasePath.Trim().Replace("/", "\\");

                _objCommonViewFileUploadRequestParam.FolderFullPath =
                    (_objCommonViewFileUploadRequestParam.FolderFullPath ?? string.Empty).Trim().Replace("/", "\\");

                _objCommonViewFileUploadRequestParam.FileName =
                    (_objCommonViewFileUploadRequestParam.FileName ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.FolderFullPath))
                    return BuildError("FolderFullPath is empty. !!!");

                // Allow BOTH: local rooted path + UNC path
                bool isUNCPath = _objCommonViewFileUploadRequestParam.BasePath.StartsWith(@"\\");
                bool isLocalRooted = Path.IsPathFullyQualified(_objCommonViewFileUploadRequestParam.BasePath);
                if (!isUNCPath && !isLocalRooted)
                {
                    return BuildError("Invalid FileUploadPath. It must be a local path (C:\\Folder) or a shared UNC path (\\\\Server\\Share).");
                }

                // Ensure FolderFullPath is inside BasePath (prevents reading outside allowed root)
                try
                {
                    var baseFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.BasePath)
                                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                       + Path.DirectorySeparatorChar;

                    var folderFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    if (!folderFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return BuildError("Invalid folder path. FolderFullPath must be inside BasePath. !!!");
                    }
                }
                catch
                {
                    return BuildError("Invalid folder path format. !!!");
                }

                // IMPORTANT: Do NOT rely only on Directory.Exists (returns false on permission issues in UNC).
                // Instead, do a permission-aware access probe.
                try
                {
                    _ = Directory.EnumerateFileSystemEntries(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                 .Take(1)
                                 .ToList();
                }
                catch (UnauthorizedAccessException)
                {
                    return BuildError("Access denied to the folder. Please grant permission to the API service/IIS AppPool identity. !!!");
                }
                catch (DirectoryNotFoundException)
                {
                    return BuildError("Files Not Found !!!");
                }
                catch (IOException ioEx)
                {
                    return BuildError($"Unable to access folder. {ioEx.Message}");
                }

                // Read FileID from meta.json (keep your logic)
                string fileId = string.Empty;
                string metaFilePath = Path.Combine(_objCommonViewFileUploadRequestParam.FolderFullPath, "meta.json");

                if (File.Exists(metaFilePath))
                {
                    try
                    {
                        string metaJson = await File.ReadAllTextAsync(metaFilePath);
                        var metaObj = JObject.Parse(metaJson);
                        fileId = (string?)metaObj["Id"] ?? string.Empty;
                    }
                    catch
                    {
                        fileId = string.Empty;
                    }
                }

                List<string> files = null;

                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.FileName))
                {
                    // List all non-json files
                    try
                    {
                        files = Directory.EnumerateFiles(_objCommonViewFileUploadRequestParam.FolderFullPath, "*", SearchOption.TopDirectoryOnly)
                            .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                            .ToList();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return BuildError("Access denied while listing files. Please check folder permissions. !!!");
                    }
                    catch (IOException ioEx)
                    {
                        return BuildError($"Unable to list files. {ioEx.Message}");
                    }
                }
                else
                {
                    // Build file path safely
                    string filePath = Path.Combine(_objCommonViewFileUploadRequestParam.FolderFullPath, _objCommonViewFileUploadRequestParam.FileName);

                    // Extra safety: prevent ..\..\ in filename
                    try
                    {
                        var folderFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                             .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                             + Path.DirectorySeparatorChar;

                        var fileFull = Path.GetFullPath(filePath);

                        if (!fileFull.StartsWith(folderFull, StringComparison.OrdinalIgnoreCase))
                        {
                            return BuildError("Invalid FileName. File must be inside the folder. !!!");
                        }

                        filePath = fileFull;
                    }
                    catch
                    {
                        return BuildError("Invalid file path format. !!!");
                    }

                    if (File.Exists(filePath))
                    {
                        files = new List<string> { filePath };
                    }
                    else
                    {
                        return BuildError("File Not Found !!!");
                    }
                }

                if (files == null || files.Count == 0)
                {
                    // keep same meaning
                    return BuildError("No Files Found !!!");
                }

                var results = new List<object>();

                foreach (var path in files)
                {
                    var fi = new FileInfo(path);
                    var contentType = _ContentTypeSvc.GetFileContentType(fi.Name);

                    var modifiedDate = File.GetLastWriteTime(path);
                    string formattedModifiedDate = modifiedDate.ToString(CommonHelper.KeyOption.FileUploadedModifiedDateFormat, CultureInfo.InvariantCulture);
                    var extension = fi.Extension.ToLowerInvariant();

                    string base64 = string.Empty;

                    // keep your rule: no base64 for eml/msg
                    if (extension != ".eml" && extension != ".msg")
                    {
                        try
                        {
                            var bytes = await File.ReadAllBytesAsync(path);
                            base64 = Convert.ToBase64String(bytes);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            base64 = string.Empty;
                        }
                        catch
                        {
                            base64 = string.Empty;
                        }
                    }

                    results.Add(new
                    {
                        Status = Helper.ToProperCase(MessageTypeConstants.Success),
                        FileName = fi.Name,
                        ReceivedDate = formattedModifiedDate,
                        ContentType = contentType,
                        Base64 = base64,
                        FileID = fileId
                    });
                }

                List<string> ViewUploadedColumns = new List<string> { "Status", "FileID", "FileName", "ReceivedDate", "ContentType", "Base64" };
                List<List<string>> ViewUploadedRows = new();

                foreach (var file in results)
                {
                    var type = file.GetType();
                    string Status = type.GetProperty("Status")?.GetValue(file)?.ToString() ?? "";
                    string FileID = type.GetProperty("FileID")?.GetValue(file)?.ToString() ?? "";
                    string FileName = type.GetProperty("FileName")?.GetValue(file)?.ToString() ?? "";
                    string ReceivedDate = type.GetProperty("ReceivedDate")?.GetValue(file)?.ToString() ?? "";
                    string ContentType = type.GetProperty("ContentType")?.GetValue(file)?.ToString() ?? "";
                    string Base64 = type.GetProperty("Base64")?.GetValue(file)?.ToString() ?? "";

                    ViewUploadedRows.Add(new List<string>
                    {
                        Status,
                        FileID,
                        FileName,
                        ReceivedDate,
                        ContentType,
                        Base64
                    });
                }
                finalDataSet = Helper.GenerateCommonMessage(ViewUploadedColumns, ViewUploadedRows, _objCommonViewFileUploadRequestParam.TableName);
            }
            catch (Exception ex)
            {
                // log full details
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.ViewUploadedMailFiles()", ex.ToString(), false);
                throw;
            }
            return finalDataSet;
        }


        /// <summary>
        /// This Method is Used to Delete Uploaded Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        public async Task<DataSet> DeleteUploadedFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam)
        {
            DataSet finalDataSet = new DataSet();
            DataSet BuildError(string message)
            {
                List<string> cols = new List<string> { "Status", "Message" };
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), message }
                };
                return Helper.GenerateCommonMessage(cols, rows, _objCommonViewFileUploadRequestParam.TableName);
            }
            try
            {
                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.BasePath))
                {
                    return BuildError(_objCommonViewFileUploadRequestParam.BasePathErrorMessage);
                }
                _objCommonViewFileUploadRequestParam.BasePath =
                    _objCommonViewFileUploadRequestParam.BasePath.Trim().Replace("/", "\\");

                _objCommonViewFileUploadRequestParam.FolderFullPath =
                    (_objCommonViewFileUploadRequestParam.FolderFullPath ?? string.Empty).Trim().Replace("/", "\\");

                _objCommonViewFileUploadRequestParam.FileName =
                    (_objCommonViewFileUploadRequestParam.FileName ?? string.Empty).Trim();

                bool isUNCPath = _objCommonViewFileUploadRequestParam.BasePath.StartsWith(@"\\");
                bool isLocalRooted = Path.IsPathFullyQualified(_objCommonViewFileUploadRequestParam.BasePath);

                if (!isUNCPath && !isLocalRooted)
                {
                    return BuildError("Invalid FileUploadPath. It must be a local path (C:\\Folder) or a shared UNC path (\\\\Server\\Share).");
                }

                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.FolderFullPath))
                {
                    return BuildError("FolderFullPath is empty. !!!");
                }

                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.FileName))
                {
                    return BuildError("FileName is required to delete a file. !!!");
                }
                try
                {
                    var baseFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.BasePath)
                                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                       + Path.DirectorySeparatorChar;

                    var folderFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    if (!folderFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return BuildError("Invalid folder path. FolderFullPath must be inside BasePath. !!!");
                    }
                }
                catch
                {
                    return BuildError("Invalid folder path format. !!!");
                }
                try
                {
                    _ = Directory.EnumerateFileSystemEntries(_objCommonViewFileUploadRequestParam.FolderFullPath).Take(1).ToList();
                }
                catch (UnauthorizedAccessException)
                {
                    return BuildError("Access denied to the folder. Please grant permission to the API service/IIS AppPool identity. !!!");
                }
                catch (DirectoryNotFoundException)
                {
                    return BuildError("Files Not Found !!!");
                }
                catch (IOException ioEx)
                {
                    return BuildError($"Unable to access folder. {ioEx.Message}");
                }

                // Build full file path safely
                // NOTE: Use Path.Combine instead of string + "\\" to avoid malformed paths
                string fullPath = Path.Combine(_objCommonViewFileUploadRequestParam.FolderFullPath, _objCommonViewFileUploadRequestParam.FileName);
                try
                {
                    var folderFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    var fileFull = Path.GetFullPath(fullPath);

                    if (!fileFull.StartsWith(folderFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return BuildError("Invalid FileName. File must be inside the folder. !!!");
                    }

                    fullPath = fileFull; // normalized final path
                }
                catch
                {
                    return BuildError("Invalid file path format. !!!");
                }

                var results = new List<object>();
                List<string> DeleteUploadedColumns = new List<string> { "Status", "FileName", "FilePath", "Message" };
                List<List<string>> DeleteUploadedRows = new();

                var fi = new FileInfo(fullPath);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        // Prefer direct delete (instead of helper) so we can catch exact exceptions
                        // If you want to keep Helper.DeleteFile, you can, but then you lose exception detail.
                        File.Delete(fullPath);

                        // Verify deletion
                        if (!File.Exists(fullPath))
                        {
                            results.Add(new
                            {
                                Status = Helper.ToProperCase(MessageTypeConstants.Success),
                                FileName = fi.Name,
                                FilePath = fullPath,
                                Message = "File Deleted Successfully !!!"
                            });
                        }
                        else
                        {
                            results.Add(new
                            {
                                Status = Helper.ToProperCase(MessageTypeConstants.Error),
                                FileName = fi.Name,
                                FilePath = fullPath,
                                Message = "Failed To Delete File !!!"
                            });
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        results.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = fi.Name,
                            FilePath = fullPath,
                            Message = "Access denied while deleting file. Please check permissions. !!!"
                        });
                    }
                    catch (IOException ioEx)
                    {
                        results.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = fi.Name,
                            FilePath = fullPath,
                            Message = $"File delete failed. {ioEx.Message}"
                        });
                    }
                }
                else
                {
                    results.Add(new
                    {
                        Status = Helper.ToProperCase(MessageTypeConstants.Error),
                        FileName = fi.Name,
                        FilePath = fullPath,
                        Message = "File Not Found !!!"
                    });
                }

                foreach (var file in results)
                {
                    var type = file.GetType();
                    string Status = type.GetProperty("Status")?.GetValue(file)?.ToString() ?? "";
                    string FileName = type.GetProperty("FileName")?.GetValue(file)?.ToString() ?? "";
                    string FilePath = type.GetProperty("FilePath")?.GetValue(file)?.ToString() ?? "";
                    string Message = type.GetProperty("Message")?.GetValue(file)?.ToString() ?? "";
                    DeleteUploadedRows.Add(new List<string> { Status, FileName, FilePath, Message });
                }

                finalDataSet = Helper.GenerateCommonMessage(DeleteUploadedColumns, DeleteUploadedRows, _objCommonViewFileUploadRequestParam.TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.DeleteUploadedFiles()", ex.ToString(), false);
                throw;
            }
            return finalDataSet;
        }


        /// <summary>
        /// This Method is Used to Delete Uploaded Mail Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        public async Task<DataSet> DeleteUploadedMailFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam)
        {
            DataSet finalDataSet = new DataSet();

            DataSet BuildError(string message)
            {
                List<string> cols = new List<string> { "Status", "Message" };
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), message }
                };
                return Helper.GenerateCommonMessage(cols, rows, _objCommonViewFileUploadRequestParam.TableName);
            }

            try
            {
                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.BasePath))
                {
                    return BuildError(_objCommonViewFileUploadRequestParam.BasePathErrorMessage);
                }

                // Normalize inputs (important for UNC paths & trimming hidden spaces)
                _objCommonViewFileUploadRequestParam.BasePath =
                    _objCommonViewFileUploadRequestParam.BasePath.Trim().Replace("/", "\\");

                _objCommonViewFileUploadRequestParam.FolderFullPath =
                    (_objCommonViewFileUploadRequestParam.FolderFullPath ?? string.Empty).Trim().Replace("/", "\\");

                _objCommonViewFileUploadRequestParam.FileName =
                    (_objCommonViewFileUploadRequestParam.FileName ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.FolderFullPath))
                    return BuildError("FolderFullPath is empty. !!!");

                if (string.IsNullOrWhiteSpace(_objCommonViewFileUploadRequestParam.FileName))
                    return BuildError("FileName is required to delete the file. !!!");

                // Allow BOTH local rooted path and UNC base path
                bool isUNCPath = _objCommonViewFileUploadRequestParam.BasePath.StartsWith(@"\\");
                bool isLocalRooted = Path.IsPathFullyQualified(_objCommonViewFileUploadRequestParam.BasePath);
                if (!isUNCPath && !isLocalRooted)
                {
                    return BuildError("Invalid FileUploadPath. It must be a local path (C:\\Folder) or a shared UNC path (\\\\Server\\Share).");
                }

                // Ensure FolderFullPath is inside BasePath (prevents wrong deletes)
                try
                {
                    var baseFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.BasePath)
                                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                       + Path.DirectorySeparatorChar;

                    var folderFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    if (!folderFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return BuildError("Invalid folder path. FolderFullPath must be inside BasePath. !!!");
                    }
                }
                catch
                {
                    return BuildError("Invalid folder path format. !!!");
                }

                // Permission-aware folder accessibility check (better than Directory.Exists for UNC)
                try
                {
                    _ = Directory.EnumerateFileSystemEntries(_objCommonViewFileUploadRequestParam.FolderFullPath).Take(1).ToList();
                }
                catch (UnauthorizedAccessException)
                {
                    return BuildError("Access denied to the folder. Please grant permission to the API service/IIS AppPool identity. !!!");
                }
                catch (DirectoryNotFoundException)
                {
                    return BuildError("Files Not Found !!!");
                }
                catch (IOException ioEx)
                {
                    return BuildError($"Unable to access folder. {ioEx.Message}");
                }

                List<string> DeleteUploadedColumns = new List<string> { "Status", "FileName", "FilePath", "Message" };
                List<List<string>> DeleteUploadedRows = new();
                var results = new List<object>();

                // Build file path safely
                string fullPath = Path.Combine(_objCommonViewFileUploadRequestParam.FolderFullPath, _objCommonViewFileUploadRequestParam.FileName);

                // Extra protection: stop FileName like ..\..\something.exe
                try
                {
                    var folderFull = Path.GetFullPath(_objCommonViewFileUploadRequestParam.FolderFullPath)
                                         .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                         + Path.DirectorySeparatorChar;

                    var fileFull = Path.GetFullPath(fullPath);

                    if (!fileFull.StartsWith(folderFull, StringComparison.OrdinalIgnoreCase))
                    {
                        return BuildError("Invalid FileName. File must be inside the folder. !!!");
                    }

                    fullPath = fileFull;
                }
                catch
                {
                    return BuildError("Invalid file path format. !!!");
                }

                var fi = new FileInfo(fullPath);

                if (File.Exists(fullPath))
                {
                    try
                    {
                        // Keep your helper if you prefer
                        var isFileDeleted = Helper.DeleteFile(fullPath);

                        if (isFileDeleted && !File.Exists(fullPath))
                        {
                            results.Add(new
                            {
                                Status = Helper.ToProperCase(MessageTypeConstants.Success),
                                FileName = fi.Name,
                                FilePath = fullPath,
                                Message = "File Deleted Successfully !!!"
                            });

                            // REMOVE ENTRY FROM META.JSON
                            string metaPath = Path.Combine(_objCommonViewFileUploadRequestParam.FolderFullPath, "meta.json");

                            if (File.Exists(metaPath))
                            {
                                try
                                {
                                    var jsonText = await File.ReadAllTextAsync(metaPath);

                                    using var doc = JsonDocument.Parse(jsonText);
                                    var root = doc.RootElement;

                                    string id = root.TryGetProperty("Id", out var idEl) ? (idEl.GetString() ?? "") : "";
                                    if (string.IsNullOrWhiteSpace(id)) id = Guid.NewGuid().ToString("N");

                                    // Keep existing objects as JsonElement dictionaries if you want,
                                    var updatedFiles = new List<object>();

                                    if (root.TryGetProperty("UploadedFiles", out var filesEl) &&
                                        filesEl.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var f in filesEl.EnumerateArray())
                                        {
                                            var name = f.TryGetProperty("Name", out var nEl) ? nEl.GetString() : null;

                                            // Skip deleted file
                                            if (!string.IsNullOrWhiteSpace(name) &&
                                                name.Equals(fi.Name, StringComparison.OrdinalIgnoreCase))
                                                continue;

                                            long size = f.TryGetProperty("Size", out var sEl) && sEl.ValueKind == JsonValueKind.Number
                                                ? sEl.GetInt64()
                                                : 0;

                                            string ext = f.TryGetProperty("Extension", out var eEl) ? eEl.GetString() : null;
                                            if (string.IsNullOrWhiteSpace(ext) && !string.IsNullOrWhiteSpace(name))
                                                ext = Path.GetExtension(name);

                                            updatedFiles.Add(new
                                            {
                                                Name = name,
                                                Size = size,
                                                Extension = ext
                                            });
                                        }
                                    }

                                    // Write back meta.json (overwrite)
                                    var updatedMeta = new
                                    {
                                        Id = id,
                                        UploadedFiles = updatedFiles,
                                        UploadedUtc = DateTime.UtcNow
                                    };

                                    await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(updatedMeta));

                                    // OPTIONAL: if no files left, you can delete meta.json
                                    // if (updatedFiles.Count == 0) File.Delete(metaPath);
                                }
                                catch (Exception metaEx)
                                {
                                    Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, "DeleteUploadedMailFiles.MetaJsonUpdate", metaEx.ToString(), false);
                                }
                            }
                        }
                        else
                        {
                            results.Add(new
                            {
                                Status = Helper.ToProperCase(MessageTypeConstants.Error),
                                FileName = fi.Name,
                                FilePath = fullPath,
                                Message = "Failed To Delete File !!!"
                            });
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        results.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = fi.Name,
                            FilePath = fullPath,
                            Message = "Access denied while deleting file. Please check permissions. !!!"
                        });
                    }
                    catch (IOException ioEx)
                    {
                        results.Add(new
                        {
                            Status = Helper.ToProperCase(MessageTypeConstants.Error),
                            FileName = fi.Name,
                            FilePath = fullPath,
                            Message = $"File delete failed. {ioEx.Message}"
                        });
                    }
                }
                else
                {
                    results.Add(new
                    {
                        Status = Helper.ToProperCase(MessageTypeConstants.Error),
                        FileName = fi.Name,
                        FilePath = fullPath,
                        Message = "File Not Found !!!"
                    });
                }

                foreach (var file in results)
                {
                    var type = file.GetType();
                    DeleteUploadedRows.Add(new List<string>
                    {
                        type.GetProperty("Status")?.GetValue(file)?.ToString() ?? "",
                        type.GetProperty("FileName")?.GetValue(file)?.ToString() ?? "",
                        type.GetProperty("FilePath")?.GetValue(file)?.ToString() ?? "",
                        type.GetProperty("Message")?.GetValue(file)?.ToString() ?? ""
                    });
                }

                finalDataSet = Helper.GenerateCommonMessage(DeleteUploadedColumns, DeleteUploadedRows, _objCommonViewFileUploadRequestParam.TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.DeleteUploadedMailFiles()", ex.ToString(), false);
                throw;
            }
            return finalDataSet;
        }

        /// <summary>
        /// This Method is Used to Get Uploaded Base64 Files.
        /// </summary>
        /// <param name="BasePath"></param>
        /// <param name="BasePathErrorMessage"></param>
        /// <param name="FolderFullPath"></param>
        /// <param name="FileName"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        public async Task<DataSet> GetUploadedBase64Files(string BasePath, string BasePathErrorMessage, string FolderFullPath, string FileName, string TableName)
        {
            DataSet finalDataSet = new DataSet();
            try
            {
                finalDataSet = await _FileUploadAbstractSvc.GetUploadedBase64Files(BasePath, BasePathErrorMessage, FolderFullPath, FileName, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.ViewUploadedFiles()", ex.ToString(), false);
                throw;
            }
            return finalDataSet;
        }

        #endregion [File Upload]
    }
}
