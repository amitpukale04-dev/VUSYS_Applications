using CommonLib;
using VUSYSAppHub.DACHelper;

namespace VUSYSAppHub.AppHubPageHelper
{
    public static class AppHubCommonHelper
    {
        public static string GetEncryptDecryptKey(string applicationName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(applicationName))
                    throw new Exception("Application name is required.");

                if (string.Equals(applicationName, "Sharpply", StringComparison.OrdinalIgnoreCase))
                    return CommonHelper.SHARPPLY_ENCRYPT_DECRYPT_KEY;

                if (string.Equals(applicationName, "Sharpply PWA", StringComparison.OrdinalIgnoreCase))
                    return CommonHelper.SHARPPLY_ENCRYPT_DECRYPT_KEY;

                if (string.Equals(applicationName, "Protectron", StringComparison.OrdinalIgnoreCase))
                    return CommonHelper.PROTECTRON_ENCRYPT_DECRYPT_KEY;
                if (string.Equals(applicationName, "Protectron HR", StringComparison.OrdinalIgnoreCase))
                    return CommonHelper.PROTECTRON_ENCRYPT_DECRYPT_KEY;

                if (string.Equals(applicationName, "Inspire Magnetics", StringComparison.OrdinalIgnoreCase))
                    return CommonHelper.INSPIREMAGNETICS_ENCRYPT_DECRYPT_KEY;
            }
            catch (Exception ex)
            {
                Helper.LogMessage(CommonHelper.KeyOption.ErrorLog, "VUSYSAppHub.Services", "UpdateDeploymentVersionService.GetEncryptDecryptKey()", ex.Message, false);
                throw new Exception($"Encryption key not configured for application: {applicationName}");
            }
            return "";
        }

        public static string NormalizeApplicationName(string applicationName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(applicationName))
                    return string.Empty;

                if (applicationName.Contains("Sharpply", StringComparison.OrdinalIgnoreCase))
                    return "Sharpply";

                return applicationName.Trim();
            }
            catch
            {
                return applicationName?.Trim() ?? string.Empty;
            }
        }
    }
}
