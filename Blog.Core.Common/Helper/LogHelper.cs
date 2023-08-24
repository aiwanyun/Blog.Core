using Blog.Core.Model;
using Dm;
using log4net;
using Serilog;
using System;

namespace Blog.Core.Common.Helper
{
    public static class LogHelper
    {
        public static readonly ILog logApp = LogManager.GetLogger(LogEnum.AppInfo.GetDisplayName());
        public static readonly ILog logNet = LogManager.GetLogger(LogEnum.RequestInfo.GetDisplayName());
        public static readonly ILog logSys = LogManager.GetLogger(LogEnum.AppInfo.GetDisplayName());
        public static void Debug(string msg, Exception exception = null)
        {
            if(exception == null)
                logApp.Debug(msg);
            else
                logApp.Debug(msg, exception);
        }
        public static void Info(string msg, Exception exception = null)
        {
            if (exception == null)
                logApp.Info(msg);
            else
                logApp.Info(msg, exception);
        }
        public static void Warn(string msg, Exception exception = null)
        {
            if (exception == null)
                logApp.Warn(msg);
            else
                logApp.Warn(msg, exception);
        }
        public static void Error(string msg, Exception exception = null)
        {
            if (exception == null)
                logApp.Error(msg);
            else
                logApp.Error(msg, exception);
        }

    }
}
