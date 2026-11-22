namespace VUSYSTimeProWebApp.Models
{
    public class CommonError
    {
        #region [Common Error Properties]

        /// <summary>
        /// Image Name
        /// </summary>
        public string ImageName { get; set; }

        /// <summary>
        /// Controller Name
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// Action Name
        /// </summary>
        public string ActionName { get; set; }

        /// <summary>
        /// Link Text
        /// </summary>
        public string LinkText { get; set; }

        /// <summary>
        /// Error Message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Is Show Footer
        /// </summary>
        public bool IsShowFooter { get; set; }

        #endregion [Common Error Properties]
    }
}
