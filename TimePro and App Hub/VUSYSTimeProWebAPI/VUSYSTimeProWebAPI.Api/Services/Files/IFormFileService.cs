using VUSYSTimeProWebAPI.Application.Common.Models;

namespace VUSYSTimeProWebAPI.Api.Services.Files
{
    public interface IFormFileService
    {
        /// <summary>
        /// This Method Is Used To Convert IFormFile To Byte Array.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<byte[]> ConvertToBytesAsync(IFormFile file, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Read File From Full File Path And Convert It To Byte Array.
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<byte[]> ConvertToBytesAsync(string fullFilePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get Default User Image And Convert It To Byte Array.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<byte[]> GetDefaultUserImageBytesAsync(string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// This Method Is Used To Get File Information List.
        /// </summary>
        /// <param name="uploadFiles"></param>
        /// <returns></returns>
        List<FileInfoModel> GetFileInfoList(List<IFormFile> uploadFiles);

        /// <summary>
        /// This Method Is Used To Get File Content Type.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string GetFileContentType(string fileName);
    }
}