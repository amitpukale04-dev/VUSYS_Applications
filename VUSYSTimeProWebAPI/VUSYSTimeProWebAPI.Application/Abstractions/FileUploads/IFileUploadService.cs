using System.Data;

namespace VUSYSTimeProWebAPI.Application.Abstractions.FileUploads
{
    public interface IFileUploadService
    {
        /// <summary>
        /// This Method is Used to Get Uploaded Base64 Files.
        /// </summary>
        /// <param name="BasePath"></param>
        /// <param name="BasePathErrorMessage"></param>
        /// <param name="FolderFullPath"></param>
        /// <param name="FileName"></param>
        /// <param name="TableName"></param>
        /// <returns></returns>
        Task<DataSet> GetUploadedBase64Files(string BasePath, string BasePathErrorMessage, string FolderFullPath, string FileName, string TableName);
    }
}
