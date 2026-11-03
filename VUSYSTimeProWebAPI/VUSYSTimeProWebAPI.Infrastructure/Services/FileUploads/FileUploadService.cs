using System.Data;
using CommonLibCore10;
using CommonLibCore10.Constants;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;
using VUSYSTimeProWebAPI.Application.Abstractions.FileUploads;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Services.FileUploads
{
    public class FileUploadService : IFileUploadService
    {
        private const string FileUploadServiceLogClassName = "FileUploadService";
        private readonly IContentTypeService _ContentTypeService;

        /// <summary>
        /// File Upload Service Class Constructor
        /// </summary>
        /// <param name="contentTypeService"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FileUploadService(IContentTypeService contentTypeService)
        {
            _ContentTypeService = contentTypeService ?? throw new ArgumentNullException(nameof(contentTypeService));
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

            DataSet BuildError(string message)
            {
                List<string> columns = new List<string> { "Status", "Message" };
                List<List<string>> rows = new List<List<string>>
                {
                    new List<string> { Helper.ToProperCase(MessageTypeConstants.Error), message }
                };
                return Helper.GenerateCommonMessage(columns, rows, TableName);
            }
            try
            {
                if (string.IsNullOrWhiteSpace(BasePath))
                {
                    return BuildError(BasePathErrorMessage);
                }
                BasePath = BasePath.Trim().Replace("/", "\\");
                FolderFullPath = (FolderFullPath ?? string.Empty).Trim().Replace("/", "\\");

                // Allow BOTH: local rooted path + UNC path
                bool isUNCPath = BasePath.StartsWith(@"\\");
                bool isLocalRooted = Path.IsPathFullyQualified(BasePath);

                if (!isUNCPath && !isLocalRooted)
                {
                    return BuildError(@"Invalid FileUploadPath. It must be a local path (C:\Folder) or a shared UNC path (\\Server\Share).");
                }

                // Ensure FolderFullPath is within BasePath
                try
                {
                    var baseFull = Path.GetFullPath(BasePath)
                                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                                       + Path.DirectorySeparatorChar;

                    var folderFull = Path.GetFullPath(FolderFullPath)
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
                    _ = Directory.EnumerateFileSystemEntries(FolderFullPath)
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
                    if (string.IsNullOrWhiteSpace(FileName))
                    {
                        files = Directory.EnumerateFiles(FolderFullPath, "*", SearchOption.TopDirectoryOnly)
                                    .Where(f => !f.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                                    .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                                    .ToList();
                    }
                    else
                    {
                        string filePath = !string.IsNullOrEmpty(FileName) ? Path.Combine(FolderFullPath, FileName) : "";

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
                    var contentType = _ContentTypeService.GetFileContentType(fi.Name);
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

                    results.Add(new
                    {
                        Status = Helper.ToProperCase(MessageTypeConstants.Success),
                        FileName = fi.Name,
                        ContentType = contentType,
                        Base64 = base64
                    });
                }

                List<string> ViewUploadedColumns = new List<string> { "Status", "FileName", "ContentType", "Base64" };
                List<List<string>> ViewUploadedRows = new();

                foreach (var fileObj in results)
                {
                    var type = fileObj.GetType();
                    string Status = type.GetProperty("Status")?.GetValue(fileObj)?.ToString() ?? "";
                    string strFileName = type.GetProperty("FileName")?.GetValue(fileObj)?.ToString() ?? "";
                    string ContentType = type.GetProperty("ContentType")?.GetValue(fileObj)?.ToString() ?? "";
                    string Base64 = type.GetProperty("Base64")?.GetValue(fileObj)?.ToString() ?? "";

                    ViewUploadedRows.Add(new List<string> { Status, strFileName, ContentType, Base64 });
                }

                finalDataSet = Helper.GenerateCommonMessage(ViewUploadedColumns, ViewUploadedRows, TableName);
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureFileUploadsLogNamespace, $"{FileUploadServiceLogClassName}.GetUploadedBase64Files()", ex.ToString(), false);
                throw;
            }
            return finalDataSet;
        }
    }
}
