using CommonLibCore10;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VUSYSTimeProWebAPI.Application.Abstractions.Configuration;
using VUSYSTimeProWebAPI.Application.Models.Configuration;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Infrastructure.Configuration
{
    public sealed class AIAPIConfigurationService : IAIAPIConfigurationService
    {
        private const string AIAPIConfigurationServiceLogClassName = "AIAPIConfigurationService";
        private readonly IHostEnvironment _hostEnvironment;

        /// <summary>
        /// AI API Configuration Service Class Constructor.
        /// </summary>
        /// <param name="hostEnvironment"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AIAPIConfigurationService(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
        }

        /// <summary>
        /// This Method Is Used To Get AI API Configuration Details
        /// </summary>
        /// <param name="AIProviderName"></param>
        /// <returns></returns>
        public AIAPIConfiguration GetAIAPIConfigurationDetails(string AIProviderName)
        {
            AIAPIConfiguration ConfigurationDetails = null;
            try
            {
                string configurationFilesFolderPath = CommonHelper.GetActualPath(_hostEnvironment, "ConfigurationFiles");
                string configurationFilesPath = configurationFilesFolderPath + "\\AIAPIConfiguration.json";

                if (File.Exists(configurationFilesPath))
                {
                    // Read and parse JSON
                    string jsonData = File.ReadAllText(configurationFilesPath);
                    JObject objJsonResponseResult = JObject.Parse(jsonData);
                    string strConfigurationDetails = string.Join(",", objJsonResponseResult["AIProviders"]);

                    if (!string.IsNullOrWhiteSpace(strConfigurationDetails))
                    {
                        string strstrConfigurationDetailsJson = "[" + strConfigurationDetails + "]";
                        if (!string.IsNullOrWhiteSpace(strstrConfigurationDetailsJson))
                        {
                            List<AIAPIConfiguration> lstConfiguration = JsonConvert.DeserializeObject<List<AIAPIConfiguration>>(strstrConfigurationDetailsJson);

                            if (lstConfiguration != null && lstConfiguration.Count > 0)
                            {
                                ConfigurationDetails = lstConfiguration.FirstOrDefault(f => f.AIProviderName.Equals(AIProviderName, StringComparison.OrdinalIgnoreCase));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConfigurationDetails = null;
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.InfrastructureConfigurationLogNamespace, $"{AIAPIConfigurationServiceLogClassName}.GetPEPLFolderConfigurationDetails()", ex.Message, false);
            }
            return ConfigurationDetails;
        }
    }
}
