using System.Data;
using CommonLibCore10;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using VUSYSTimeProWebAPI.Infrastructure.Common;

namespace VUSYSTimeProWebAPI.Api.Validation
{
    public static class ModelErrorValidation
    {
        private const string ModelErrorValidationLogClassName = "ModelErrorValidation";

        /// <summary>
        /// This Method Is Used To Get All Model Errors.
        /// </summary>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public static IEnumerable<ValidationErrors> GetAllErrors(this ModelStateDictionary modelState)
        {
            var result = from ms in modelState
                         where ms.Value.Errors.Any()
                         let fieldKey = ms.Key
                         let errors = ms.Value.Errors
                         from error in errors
                         select new ValidationErrors(fieldKey, error.ErrorMessage);

            return result;
        }

        /// <summary>
        /// This Method Is Used To Convert Model Errors To Json
        /// </summary>
        /// <param name="modelState"></param>
        /// <returns></returns>
        public static string ConvertModelErrorsToJson(this ModelStateDictionary modelState)
        {
            string ReturnResult = string.Empty;
            try
            {
                var ModelErrors = GetAllErrors(modelState);
                var ModelErrorList = ModelErrors.ToList();
                var dt1 = Helper.ConvertListToDataTable(ModelErrorList);
                if (dt1.Rows.Count > 0)
                {
                    Helper.AddNewColumnToDataTable(dt1, "Status", typeof(string));
                    foreach (DataRow row in dt1.Rows)
                    {
                        row.SetField("Status", "Error");
                    }
                    DataSet ds = new DataSet();
                    ds = Helper.ConvertDataTableToDataSet(dt1);
                    ReturnResult = ObjectToJson.ConvertDataSetToJson(ds);
                }
            }
            catch (Exception ex)
            {
                ReturnResult = string.Empty;
                Helper.LogMessage(CommonHelper.KeyOption.APIErrorLog, LogNameSpaceConstants.ValidationLogNamespace, $"{ModelErrorValidationLogClassName}.ConvertModelErrorsToJson()", ex.Message, false);
            }
            return ReturnResult;
        }
    }

    public class ValidationErrors
    {
        public ValidationErrors(string key, string message)
        {
            Key = key;
            Message = message;
        }

        /// <summary>
        /// Key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        public string Message { get; set; }
    }
}
