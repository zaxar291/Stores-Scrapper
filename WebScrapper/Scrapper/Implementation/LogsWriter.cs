using System;
using System.IO;
using WebApplication.Scrapper.Abstraction;
using WebApplication.Scrapper.Entities;

namespace WebApplication.Scrapper.Implementation
{
    public class LogsWriter : BaseLogger
    {

        private LogsWriterSettings WriterSettings;

        public LogsWriter(LogsWriterSettings settings)
        {
            if (settings.DefaultExtension.Equals(String.Empty))
            {
                settings.DefaultExtension = ".log";
            }

            if (settings.baseLogsDir.Equals(String.Empty))
            {
                settings.baseLogsDir = Directory.GetCurrentDirectory();
            }

            this.WriterSettings = settings;
        }

        public override int info(string message)
        {
            if (this.WriterSettings.writeInfoLogs)
            {
                return this.Write($"[INFO][{DateTime.Now}] - {message}", "ScrapperInfoLogs");
            }

            return 1;
        }

        public override int warn(string message)
        {
            if (this.WriterSettings.writeWarningLogs)
            {
                return this.Write($"[WARNING][{DateTime.Now}] - {message}", "ScrapperWarningLogs");
            }

            return 1;
        }

        public override int error(string message)
        {
            
            if (this.WriterSettings.writeErrorLogs)
            {
                return this.Write($"[ERROR][{DateTime.Now}] - {message}", "ScrapperErrorLogs");
            }

            return 1;
        }

        protected override int Write(
            string data, 
            string fileName = "", 
            string dir = "", 
            string ext = "")
        {
            if (fileName.Equals(String.Empty))
            {
                fileName = $"info-log{this.WriterSettings.DefaultExtension}";
            }

            if (dir.Equals(String.Empty))
            {
                dir = WriterSettings.baseLogsDir;
            }

            return base.Write(data, fileName, dir, ext);
        }
    }
}