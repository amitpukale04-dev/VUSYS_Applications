namespace VUSYSTimeProWebAPI.Application.Abstractions.Common
{
    public interface IContentTypeService
    {
        /// <summary>
        /// This Method Is Used To Get File Content Type.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string GetFileContentType(string fileName);
    }
}
