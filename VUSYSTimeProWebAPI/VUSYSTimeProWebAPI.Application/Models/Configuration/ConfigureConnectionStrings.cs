namespace VUSYSTimeProWebAPI.Application.Models.Configuration
{
    public class ConfigureConnectionStrings
    {
        /// <summary>
        /// Environment
        /// </summary>
        public string Environment { get; set; }
        
        /// <summary>
        /// Connections
        /// </summary>
        public Dictionary<string, string> Connections { get; set; }
    }

    public class ConnectionStringsRoot
    {
        /// <summary>
        /// Connection Strings
        /// </summary>
        public List<ConfigureConnectionStrings> ConnectionStrings { get; set; }
    }
}
