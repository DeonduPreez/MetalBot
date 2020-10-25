using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;


namespace MetalBot.Services
{
    // TODO : Utilize LoggingService
    public class LoggingService : ILogger<BaseDiscordClient>, ILoggerFactory
    {
        private readonly object _lock;
        private const string LogFile = "data/MetalBot.log";

        public LoggingService()
        {
            _lock = new object();
        }

        internal void PrintVersion()
        {
            Info("--------------------------------------------");
            Info("--------------------------------------------");
            Info($"Currently running MetalBot V{Version.FullVersion}.");
        }

        private void Log(LogLevel s, string message, Exception e = null)
        {
            lock (_lock)
            {
                if (s is LogLevel.Debug)
                {
                    // if (!Config.EnableDebugLogging) return;
                }

                Execute(s, message, e);
            }
        }

        /// <summary>
        ///     Prints a <see cref="Trace"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        public void Debug(string message)
            => Log(LogLevel.Debug, message);

        /// <summary>
        ///     Prints a <see cref="Information"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        public void Info(string message)
            => Log(LogLevel.Information, message);

        /// <summary>
        ///     Prints a <see cref="StructureMap.Diagnostics.Error"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message, with the specified <paramref name="e"/> exception if provided.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        /// <param name="e">Optional Exception to print.</param>
        public void Error(string message, Exception e = null)
            => Log(LogLevel.Error, message, e);

        /// <summary>
        ///     Prints a <see cref="LogLevel.Critical"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message, with the specified <paramref name="e"/> exception if provided.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        /// <param name="e">Optional Exception to print.</param>
        public void Critical(string message, Exception e = null)
            => Log(LogLevel.Critical, message, e);

        /// <summary>
        ///     Prints a <see cref="LogLevel.Warning"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message, with the specified <paramref name="e"/> exception if provided.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        /// <param name="e">Optional Exception to print.</param>
        public void Warn(string message, Exception e = null)
            => Log(LogLevel.Warning, message, e);

        /// <summary>
        ///     Prints a <see cref="Information"/> message to the console from the specified <paramref name="src"/> source, with the given <paramref name="message"/> message.
        /// </summary>
        /// <param name="src">Source to print the message from.</param>
        /// <param name="message">Message to print.</param>
        public void Verbose(string message)
            => Log(LogLevel.Information, message);

        /// <summary>
        ///     Prints a <see cref="StructureMap.Diagnostics.Error"/> message to the console from the specified <paramref name="e"/> exception.
        /// </summary>
        /// <param name="e">Exception to print.</param>
        public void Exception(Exception e, IServiceProvider provider = null)
            => Execute(LogLevel.Critical, string.Empty, e, provider);


        private void Execute(LogLevel s, string message, Exception e, IServiceProvider provider = null)
        {
            var content = new StringBuilder();

            var (color, value) = VerifySeverity(s);
            Append($"{value}:".PadRight(10), color);
            var dto = DateTimeOffset.UtcNow;
            content.Append($"[{dto:MM/dd/yyyy} | {dto:hh:mm:ss tt}] {value} -> ");

            Append($"[{value}]".PadRight(15), color);
            content.Append($"{value} -> ");

            if (!string.IsNullOrWhiteSpace(message))
            {
                Append(message, ConsoleColor.White);
                content.Append(message);
            }

            if (e != null)
            {
                var toWrite = new StringBuilder($"{e.GetType()}: {e.Message}{Environment.NewLine}{e.StackTrace}");

                var cause = e;
                while ((cause = cause.InnerException) != null)
                {
                    toWrite.Append($"{Environment.NewLine}Caused by {cause.GetType()}: {cause.Message}{Environment.NewLine}{cause.StackTrace}");
                }

                Append(toWrite.ToString(), ConsoleColor.DarkRed);
                content.Append(toWrite);

                Console.WriteLine(); // End the line before LogExceptionInDiscord as it can log to console.
                content.AppendLine();

                if (provider != null)
                {
                    // LogExceptionInDiscord(e, provider);
                }
            }
            else
            {
                Console.WriteLine();
                content.AppendLine();
            }

            // if (Config.EnabledFeatures.LogToFile)
            // {
            //     File.AppendAllText(LogFile, content.ToString());
            // }
        }

        private void Append(string m, ConsoleColor c)
        {
            Console.ForegroundColor = c;
            Console.Write(m);
        }


        private (ConsoleColor Color, string Level) VerifySeverity(LogLevel severity) =>
            severity switch
            {
                LogLevel.Critical => (ConsoleColor.DarkRed, "CRITICAL"),
                LogLevel.Error => (ConsoleColor.Red, "ERROR"),
                LogLevel.Warning => (ConsoleColor.Yellow, "WARN"),
                LogLevel.Information => (ConsoleColor.Green, "INFO"),
                LogLevel.Trace => (ConsoleColor.Blue, "TRACE"),
                LogLevel.Debug => (ConsoleColor.Cyan, "TRACE"),
                LogLevel.None => (ConsoleColor.White, "NONE"),
                _ => throw new InvalidOperationException($"The specified {nameof(LogLevel)} ({severity}) is invalid.")
            };

        // TODO : Log Exception In Discord
        // private void LogExceptionInDiscord(Exception e, IServiceProvider provider)
        // {
        //     var client = provider.GetRequiredService<DiscordSocketClient>();
        //     var http = provider.Get<HttpService>();
        //
        //     if (!Config.GuildLogging.EnsureValidConfiguration(client, out var channel))
        //     {
        //         Error("Could not send an exception report to Discord as the GuildLogging configuration is invalid.");
        //         return;
        //     }
        //
        //     _ = Task.Run(async () =>
        //     {
        //         var url = await http.PostToGreemPasteAsync(e.StackTrace);
        //         await new DiscordEmbedBuilder()
        //             .WithErrorColor()
        //             .WithTitle(
        //                 $"Exception at {DateTimeOffset.UtcNow.FormatDate()}, {DateTimeOffset.UtcNow.FormatFullTime()} UTC")
        //             .AddField("Exception Type", e.GetType().AsPrettyString(), true)
        //             .AddField("Exception Message", e.Message, true)
        //             .WithDescription($"View the full Stack Trace [here]({url}).")
        //             .SendToAsync(channel);
        //     });
        // }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            // if (IsEnabled(logLevel))
            // {
            Log(logLevel, state.ToString(), exception);
            // }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            // if (logLevel is LogLevel.Trace && !(Version.ReleaseType is Version.DevelopmentStage.Development || Config.EnableDebugLogging)) return false;

            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new ServiceCollection().BuildServiceProvider();
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string _) => this;

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public Task LogSystemMessage(LogMessage arg)
        {
            Info($"System Message: {arg.Message}");
            return Task.CompletedTask;
        }
    }
}