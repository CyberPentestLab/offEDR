using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace EDRsystem123
{
    [SupportedOSPlatform("windows")]
    class Program
    {
        static void Main(string[] args)
        {
            if (!IsAdministrator())
            {
                Console.WriteLine("Run the program as administrator!");
                RestartAsAdmin();
                return;
            }

            string appName = "MyApp";
            string? appPath = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(appPath))
            {
                Console.WriteLine("ERROR: Could not get executable path");
                return;
            }

            StartupHelper.AddToStartup(appName, appPath);
            Console.WriteLine("Check autoload in the registry.");
            Console.ReadKey();
        }

        [SupportedOSPlatform("windows")]
        static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        [SupportedOSPlatform("windows")]
        static void RestartAsAdmin()
        {
            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(exePath))
            {
                Console.WriteLine("ERROR: Could not get executable path");
                return;
            }

            var startInfo = new ProcessStartInfo(exePath)
            {
                Verb = "runas",
                UseShellExecute = true,
                Arguments = "--restarted"
            };

            try
            {
                Process.Start(startInfo);
            }
            catch
            {
                Console.WriteLine("Canceled by user.");
            }
            Environment.Exit(0);
        }
    }

    [SupportedOSPlatform("windows")]
    public class StartupHelper
    {
        public static void AddToStartup(string appName, string appPath)
        {
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null)
                    {
                        Console.WriteLine("Error accessing registry!");
                        return;
                    }

                    // Delete the old entry
                    if (key.GetValue(appName) != null)
                    {
                        key.DeleteValue(appName);
                    }

                    // Add a new entry with quotes
                    key.SetValue(appName, $"\"{appPath}\"");

                    // Checking the success
                    if (key.GetValue(appName)?.ToString()?.Equals($"\"{appPath}\"", StringComparison.Ordinal) == true)
                    {
                        Console.WriteLine("[Success] The program has been added to startup.");
                    }
                    else
                    {
                        Console.WriteLine("[ERROR] Record not added.");
                    }
                }
#pragma warning restore CA1416
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("No rights to write to the registry!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
        }
    }
}