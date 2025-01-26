using launcher.Classes.BranchUtils;
using launcher.Classes.Utilities;
using System.Diagnostics;
using System.IO;
using static launcher.Classes.Game.LaunchParameters;
using static launcher.Classes.Utilities.Logger;
using static launcher.Classes.Global.References;

namespace launcher.Classes.Game
{
    public static class Game
    {
        public static void Launch()
        {
            appDispatcher.Invoke(new Action(() =>
            {
                Play_Button.IsEnabled = false;
                Play_Button.Content = "LAUNCHING...";
            }));

            eMode mode = (eMode)(int)Ini.Get(Ini.Vars.Mode);

            string exeName = mode switch
            {
                eMode.HOST => "r5apex.exe",
                eMode.SERVER => "r5apex_ds.exe",
                eMode.CLIENT => "r5apex.exe",
                _ => "r5apex.exe"
            };

            if (!File.Exists($"{GetBranch.Directory()}\\{exeName}"))
                return;

            string gameArguments = BuildParameters();

            var startInfo = new ProcessStartInfo
            {
                FileName = $"{GetBranch.Directory()}\\{exeName}",
                WorkingDirectory = GetBranch.Directory(),
                Arguments = gameArguments,
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process gameProcess = Process.Start(startInfo);

            gameProcess.WaitForInputIdle();

            if (gameProcess != null)
                SetProcessorAffinity(gameProcess);

            LogInfo(Source.Launcher, $"Launched game with arguments: {gameArguments}");

            appDispatcher.Invoke(new Action(() =>
            {
                Play_Button.IsEnabled = true;
                Play_Button.Content = "PLAY";
            }));
        }

        private static void SetProcessorAffinity(Process gameProcess)
        {
            try
            {
                int coreCount = int.Parse((string)Ini.Get(Ini.Vars.Processor_Affinity));
                int processorCount = Environment.ProcessorCount;

                if (coreCount == -1 || coreCount == 0)
                    return;

                if (coreCount > processorCount)
                    coreCount = processorCount;

                if (coreCount >= 1 && coreCount <= processorCount)
                {
                    // Set processor affinity to the first 'coreCount' cores
                    int affinityMask = 0;

                    // Set bits for the first 'coreCount' cores
                    for (int i = 0; i < coreCount; i++)
                        affinityMask |= 1 << i;  // Set the bit corresponding to core 'i'

                    gameProcess.ProcessorAffinity = affinityMask;

                    LogInfo(Source.Launcher, $"Processor affinity set to the first {coreCount} cores.");
                }
                else
                    LogError(Source.Launcher, $"Invalid core index: {coreCount}. Must be between -1 and {processorCount}.");
            }
            catch (Exception ex)
            {
                LogError(Source.Launcher, $"Failed to set processor affinity: {ex.Message}");
            }
        }
    }
}