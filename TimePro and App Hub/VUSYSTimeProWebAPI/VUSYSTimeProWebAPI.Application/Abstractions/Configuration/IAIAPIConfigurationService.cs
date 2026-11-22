using VUSYSTimeProWebAPI.Application.Models.Configuration;

namespace VUSYSTimeProWebAPI.Application.Abstractions.Configuration;

public interface IAIAPIConfigurationService
{
    /// <summary>
    /// This Method Is Used To Get AI API Configuration Details
    /// </summary>
    /// <param name="AIProviderName"></param>
    /// <returns></returns>
    AIAPIConfiguration GetAIAPIConfigurationDetails(string AIProviderName);
}

