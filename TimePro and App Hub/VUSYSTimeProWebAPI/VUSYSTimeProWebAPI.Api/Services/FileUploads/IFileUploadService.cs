using System.Data;
using VUSYSTimeProWebAPI.Api.Models.FileUploads;

namespace VUSYSTimeProWebAPI.Api.Services.FileUploads
{
    public interface IFileUploadService
    {
        #region [File Upload]

        /// <summary>
        /// This Method is Used to Upload The Files.
        /// </summary>
        /// <param name="_objCommonFileUploadRequestParam"></param>
        /// <returns></returns>
        Task<DataSet> UploadFiles(CommonFileUploadRequestParam _objCommonFileUploadRequestParam);

        /// <summary>
        /// This Method is Used to Upload The Email Files.
        /// </summary>
        /// <param name="_objCommonFileUploadRequestParam"></param>
        /// <returns></returns>
        Task<DataSet> UploadMailFiles(CommonFileUploadRequestParam _objCommonFileUploadRequestParam);

        /// <summary>
        /// This Method is Used to View Uploaded Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        Task<DataSet> ViewUploadedFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam);

        /// <summary>
        /// This Method is Used to Upload The Email Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        Task<DataSet> ViewUploadedMailFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam);

        /// <summary>
        /// This Method is Used to Delete Uploaded Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        Task<DataSet> DeleteUploadedFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam);

        /// <summary>
        /// This Method is Used to Delete Uploaded Mail Files.
        /// </summary>
        /// <param name="_objCommonViewFileUploadRequestParam"></param>
        /// <returns></returns>
        Task<DataSet> DeleteUploadedMailFiles(CommonViewFileUploadRequestParam _objCommonViewFileUploadRequestParam);

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

        #endregion [File Upload]
    }
}
