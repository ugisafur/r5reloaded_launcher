using launcher.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static launcher.Core.AppControllerService;
using static launcher.Services.LoggerService;
using static launcher.Services.LaunchParameterService;

namespace launcher.GameManagement
{
    /// <summary>
    /// Represents the result of a game launch attempt.
    /// </summary>
    public enum LaunchResult
    {
        Success,
        EAAppNotInstalled,
        EAAppNotRunning,
        ExecutableNotFound,
        LaunchFailed
    }

    /// <summary>
    /// Provides services for managing and launching the game.
    /// </summary>
    public static class GameManager
    {
        /// <summary>
        /// Asynchronously launches the game after performing necessary checks.
        /// </summary>
        /// <returns>A LaunchResult indicating the outcome of the launch attempt.</returns>
        public static async Task<LaunchResult> LaunchAsync()
        {
            var eaAppStatus = IsEAAppRunning();
            if (eaAppStatus == EAAppCodes.Not_Installed)
                return LaunchResult.EAAppNotInstalled;
            if (eaAppStatus == EAAppCodes.Installed_And_Not_Running)
                return LaunchResult.EAAppNotRunning;

            var mode = (eMode)(int)SettingsService.Get(SettingsService.Vars.Mode);
            string exeName = mode == eMode.SERVER ? "r5apex_ds.exe" : "r5apex.exe";
            string releaseChannelDirectory = ReleaseChannelService.GetDirectory();
            string exePath = Path.Combine(releaseChannelDirectory, exeName);

            if (!File.Exists(exePath))
            {
                LogError(LogSource.Launcher, $"Executable not found at path: {exePath}");
                return LaunchResult.ExecutableNotFound;
            }

            try
            {
                string gameArguments = BuildParameters();
                var startInfo = new ProcessStartInfo(exePath)
                {
                    WorkingDirectory = releaseChannelDirectory,
                    Arguments = gameArguments,
                    UseShellExecute = true,
                };

                using Process gameProcess = Process.Start(startInfo);

                if (gameProcess == null)
                {
                    LogError(LogSource.Launcher, "Process.Start() returned null.");
                    return LaunchResult.LaunchFailed;
                }

                // Wait for the process to create a window before continuing
                await Task.Run(() => gameProcess.WaitForInputIdle());

                SetProcessorAffinity(gameProcess);

                LogInfo(LogSource.Launcher, $"Launched game with arguments: {gameArguments}");
                return LaunchResult.Success;
            }
            catch (Exception ex)
            {
                LogException("Failed to launch game process", LogSource.Launcher, ex);
                return LaunchResult.LaunchFailed;
            }
        }

        private static void SetProcessorAffinity(Process gameProcess)
        {
            try
            {
                if (!int.TryParse((string)SettingsService.Get(SettingsService.Vars.Processor_Affinity), out int coreCount))
                {
                    LogWarning(LogSource.Launcher, "Could not parse Processor_Affinity setting.");
                    return;
                }

                if (coreCount <= 0) return; // Disabled or invalid setting

                int processorCount = Environment.ProcessorCount;
                coreCount = Math.Min(coreCount, processorCount); // Clamp to max available cores

                // Calculate affinity mask: (1L << N) - 1 gives a mask for the first N cores.
                long affinityMask = (1L << coreCount) - 1;
                gameProcess.ProcessorAffinity = (IntPtr)affinityMask;

                LogInfo(LogSource.Launcher, $"Processor affinity set to the first {coreCount} cores.");
            }
            catch (Exception ex)
            {
                LogException("Failed to set processor affinity", LogSource.Launcher, ex);
            }
        }
    }
}