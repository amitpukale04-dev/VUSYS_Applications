using CommonLibCore10;
using CommonLibCore10.Constants;
using Microsoft.AspNetCore.StaticFiles;
using VUSYSTimeProWebAPI.Application.Common.Models;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Services.Files
{
    public sealed class FormFileService : IFormFileService
    {
        private const string FormFileServiceLogClassName = "FormFileService";
        private static readonly FileExtensionContentTypeProvider FileExtensionContentTypeProvider = new();
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Form File Service Class Constructor
        /// </summary>
        /// <param name="webHostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public FormFileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        /// <summary>
        /// This Method Is Used To Convert IFormFile To Byte Array.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<byte[]> ConvertToBytesAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            byte[] returnBytes = Array.Empty<byte>();
            try
            {
                ArgumentNullException.ThrowIfNull(file);
                cancellationToken.ThrowIfCancellationRequested();
                await using Stream inputStream = file.OpenReadStream();
                using MemoryStream memoryStream = new();
                await inputStream.CopyToAsync(memoryStream, cancellationToken);
                returnBytes = memoryStream.ToArray();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FilesLogNamespace, $"{FormFileServiceLogClassName}.ConvertToBytesAsync()", ex.Message, false);
                throw;
            }
            return returnBytes;
        }

        /// <summary>
        /// This Method Is Used To Read File From Full File Path And Convert It To Byte Array.
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public async Task<byte[]> ConvertToBytesAsync(string fullFilePath, CancellationToken cancellationToken = default)
        {
            byte[] returnBytes = Array.Empty<byte>();
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(fullFilePath);
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(fullFilePath))
                {
                    throw new FileNotFoundException("File not found.", fullFilePath);
                }
                returnBytes = await File.ReadAllBytesAsync(fullFilePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FilesLogNamespace, $"{FormFileServiceLogClassName}.ConvertToBytesAsync()", ex.Message, false);
                throw;
            }
            return returnBytes;
        }

        /// <summary>
        /// This Method Is Used To Get Default User Image And Convert It To Byte Array.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<byte[]> GetDefaultUserImageBytesAsync(string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
                cancellationToken.ThrowIfCancellationRequested();
                string fullFilePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Content", "Images", fileName);
                return await ConvertToBytesAsync(fullFilePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FilesLogNamespace, $"{FormFileServiceLogClassName}.GetDefaultUserImageBytesAsync()", ex.Message, false);
                throw;
            }
        }

        /// <summary>
        /// This Method Is Used To Get File Information List.
        /// </summary>
        /// <param name="uploadFiles"></param>
        /// <returns></returns>
        public List<FileInfoModel> GetFileInfoList(List<IFormFile> uploadFiles)
        {
            var result = new List<FileInfoModel>();
            try
            {
                if (uploadFiles == null || uploadFiles.Count == 0)
                    return result;

                foreach (var file in uploadFiles)
                {
                    if (file == null || string.IsNullOrWhiteSpace(file.FileName))
                        continue;
                    var ext = Path.GetExtension(file.FileName);
                    var safeBase = CommonHelper.SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
                    var uniqueName = $"{safeBase}_{DateTime.UtcNow.ToString(DateTimeFormatConstants.YYYYMMDDHHMMSSFFF_COMPACT_DATETIMEFORMAT)}{ext}";
                    result.Add(new FileInfoModel
                    {
                        FileName = uniqueName,
                        FileExtension = ext
                    });
                }
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.FilesLogNamespace, $"{FormFileServiceLogClassName}.GetFileInfoList()", ex.Message, false);
            }
            return result;
        }

        /// <summary>
        /// This Method Is Used To Get File Content Type.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public string GetFileContentType(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return ContentTypeConstants.BINARY_CONTENTTYPE;

            if (FileExtensionContentTypeProvider.TryGetContentType(fileName, out string contentType))
                return contentType;

            return ContentTypeConstants.BINARY_CONTENTTYPE; // Default fallback
        }
    }
}