using CommonLibCore10.Constants;
using Microsoft.AspNetCore.StaticFiles;
using VUSYSTimeProWebAPI.Application.Abstractions.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Common;

public sealed class ContentTypeService : IContentTypeService
{
    private readonly FileExtensionContentTypeProvider _contentTypeProvider;

    public ContentTypeService()
    {
        _contentTypeProvider = new FileExtensionContentTypeProvider();
    }

    /// <summary>
    /// This Method Is Used To Get File Content Type.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string GetFileContentType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ContentTypeConstants.BINARY_CONTENTTYPE;
        }

        return _contentTypeProvider.TryGetContentType(fileName, out string? contentType)
            ? contentType
            : ContentTypeConstants.BINARY_CONTENTTYPE;
    }
}