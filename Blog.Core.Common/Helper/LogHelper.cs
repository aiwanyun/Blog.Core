using Serilog;
using System;

namespace Blog.Core.Common.Helper
{
    public class LogHelper
    {
        public static void Debug(string msg, Exception exception = null)
        {
            Log.Debug(exception, "{msg}", msg);
        }
        public static void Information(string msg, Exception exception = null)
        {
            Log.Information(exception, "{msg}", msg);
        }
        public static void Warning(string msg, Exception exception = null)
        {
            Log.Warning(exception, "{msg}", msg);
        }
        public static void Error(string msg, Exception exception = null)
        {
            Log.Error(exception, "{msg}", msg);
        }
        public static ILogger  Logger{ 
            get {
                return Log.Logger;
            } 
            set {
                Log.Logger = value;
            } 
        }

    }
}
