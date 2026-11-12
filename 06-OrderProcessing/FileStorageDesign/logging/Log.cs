using System;
using System.Collections.Generic;
using System.Text;

namespace FileStorageDesign.logging
{
    public class Log
    {
        private static readonly Lock _lock = new();

        public static void Info(string component, string message)
        {
            WriteColored(component, message, ConsoleColor.Gray);
        }

        public static void Success(string component, string message)
        {
            WriteColored(component, message, ConsoleColor.Green);
        }

        public static void Error(string component, string message)
        {
            WriteColored(component, message, ConsoleColor.Red);
        }

        private static void WriteColored( string component,string message,ConsoleColor color)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var threadId = Environment.CurrentManagedThreadId;

            var logMessage = $"[{timestamp}] [Thread {threadId}] {component} -> {message}";

            lock (_lock)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(logMessage);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }
        public static async Task OpenConnectionAsync(string RepositoryName, CancellationToken ct)
        {
            Log.Info(RepositoryName, "Opening connection...");
            await Task.Delay(200, ct);
        }

        public static async Task BeginTransactionAsync(string RepositoryName, CancellationToken ct)
        {
            Log.Info(RepositoryName, "Beginning transaction...");
            await Task.Delay(100, ct);
        }

        public static async Task SimulateUpdateAsync(string RepositoryName, CancellationToken ct)
        {
            Log.Info(RepositoryName, "Updating product stock...");
            await Task.Delay(400, ct);
        }

        public static async Task CommitAsync(string RepositoryName, CancellationToken ct)
        {
            Log.Info(RepositoryName, "Committing transaction...");
            await Task.Delay(200, ct);
        }
        public  static async Task ExecutingAsync(string RepositoryName, CancellationToken cancellationToken)
        {
            Log.Info(RepositoryName, "Executing query...");
            await Task.Delay(Random.Shared.Next(300, 601), cancellationToken);
        }

    }
}
