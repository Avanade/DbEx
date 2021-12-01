using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace DbEx.Test
{
    public class UnitTest
    {
        /// <summary>
        /// Gets (builds) the <see cref="IConfigurationRoot"/>.
        /// </summary>
        /// <returns>The <see cref="IConfigurationRoot"/>.</returns>
        public static IConfigurationRoot GetConfig(string prefix) => new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory).AddJsonFile("appsettings.json").AddEnvironmentVariables(prefix).Build();

        /// <summary>
        /// Gets a console <see cref="ILogger"/>.
        /// </summary>
        /// <typeparam name="T">The logger <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="ILogger"/>.</returns>
        public static ILogger<T> GetLogger<T>() => LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.ClearProviders();
            b.AddProvider(new TestContextLoggerProvider());
        }).CreateLogger<T>();
    }
}