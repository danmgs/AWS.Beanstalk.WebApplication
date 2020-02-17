using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace AWS.Beanstalk.WebApplication.Utils
{
    public class ConfigHelper
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(ConfigHelper));

        public static string Get(string envvariable)
        {
            NameValueCollection appConfig = ConfigurationManager.AppSettings;
            string envvalue = appConfig[envvariable];
            if (string.IsNullOrEmpty(envvalue))
                log.Error($"environment variable '{envvariable}' not found !");            
            else
                log.Info($"Find environment variable '{envvariable}'={envvalue}");

            return envvalue;
        }
    }
}
