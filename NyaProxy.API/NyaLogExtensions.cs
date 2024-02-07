using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging
{

    public static class NyaLogExtensions
    {

        /// <summary>
        /// 输出<paramref name="tilte"/>并在下一行输出<paramref name="message"/>
        /// </summary>
        public static void LogMultiLineInformation(this ILogger logger, string tilte, string message)
        {
            logger.LogInformation($"{tilte}{Environment.NewLine}{message}");
        }

        /// <summary>
        /// 以<see cref="Environment.NewLine"/>分割消息
        /// </summary>
        public static void LogMultiLineInformation(this ILogger logger, string message)
        {
            foreach (var item in message.Split(Environment.NewLine))
            {
                logger.LogInformation(item);
            }
        }

        /// <summary>
        /// 输出<paramref name="message"/>并在下一行输出<see cref="Exception.ToString"/>
        /// </summary>
        public static void LogMultiLineError(this ILogger logger, string message, Exception? exception)
        {
            logger.LogError($"{message}{Environment.NewLine}{exception.Message}");
        }

        /// <summary>
        /// 输出<see cref="Exception.ToString"/>
        /// </summary>
        public static void LogError(this ILogger logger, Exception? exception)
        {
            if (exception != null)
                logger.LogError(exception, exception.ToString());
        }
    }
}
