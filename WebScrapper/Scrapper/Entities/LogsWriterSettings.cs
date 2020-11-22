using System.Collections.Generic;
namespace WebApplication.Scrapper.Entities
{
    public class LogsWriterSettings
    {
        public string DefaultExtension { get; set; } 
        public string baseLogsDir { get; set; }
        
        public bool writeInfoLogs { get; set; }
        
        public bool writeWarningLogs { get; set; }
        
        public bool writeErrorLogs { get; set; }
    }
}