using launcher.Configuration;
using System.Text;
using static launcher.Core.UiReferences;

namespace launcher.GameManagement
{
    /// <summary>
    /// Builds the command-line parameters for launching the game.
    /// </summary>
    public static class LaunchParameterBuilder
    {
        public enum eMode
        {
            HOST,
            SERVER,
            CLIENT
        }

        /// <summary>
        /// Builds the command-line parameters based on the current settings.
        /// </summary>
        /// <returns>A string containing the launch parameters.</returns>
        public static string BuildParameters()
        {
            var svParameters = new StringBuilder();
            var mode = (eMode)(int)IniSettings.Get(IniSettings.Vars.Mode);

            AppendCommonParameters(svParameters);

            switch (mode)
            {
                case eMode.HOST:
                    AppendHostParameters(svParameters);
                    break;
                case eMode.SERVER:
                    AppendServerParameters(svParameters);
                    break;
                case eMode.CLIENT:
                    AppendClientParameters(svParameters);
                    break;
            }

            return svParameters.ToString().Trim();
        }

        private static void AppendCommonParameters(StringBuilder svParameters)
        {
            AppendProcessorParameters(svParameters);
            AppendConsoleParameters(svParameters);
            AppendNetParameters(svParameters);
            AppendGameSettingsParameters(svParameters);
            AppendDeveloperParameters(svParameters);
            AppendOfflineMode(svParameters);
            AppendCustomCommandLine(svParameters);
        }

        private static void AppendHostParameters(StringBuilder svParameters)
        {
            AppendHostName(svParameters);
            AppendVideoParameters(svParameters);
            AppendNoAsyncParameters(svParameters, isFullAsync: true);
        }

        private static void AppendServerParameters(StringBuilder svParameters)
        {
            AppendHostName(svParameters);
            AppendNoAsyncParameters(svParameters, isFullAsync: false);
        }

        private static void AppendClientParameters(StringBuilder svParameters)
        {
            svParameters.Append("-noserverdll ");
            AppendVideoParameters(svParameters);
            AppendNoAsyncParameters(svParameters, isFullAsync: true);
        }

        private static void AppendHostName(StringBuilder svParameters)
        {
            var hostName = (string)IniSettings.Get(IniSettings.Vars.HostName);
            if (!string.IsNullOrEmpty(hostName))
            {
                svParameters.Append($"+hostname \"{hostName}\" ");
                svParameters.Append($"+sv_pylonVisibility {(int)IniSettings.Get(IniSettings.Vars.Visibility)} ");
            }
        }

        private static void AppendVideoParameters(StringBuilder svParameters)
        {
            svParameters.Append((bool)IniSettings.Get(IniSettings.Vars.Windowed) ? "-windowed " : "-fullscreen ");
            svParameters.Append((bool)IniSettings.Get(IniSettings.Vars.Borderless) ? "-noborder " : "-forceborder ");

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Max_FPS), out int maxFps))
                svParameters.Append($"+fps_max {maxFps} ");
            else
                svParameters.Append("+fps_max 0 ");

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Resolution_Width), out int resWidth))
                svParameters.Append($"-w {resWidth} ");

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Resolution_Height), out int resHeight))
                svParameters.Append($"-h {resHeight} ");
        }

        private static void AppendProcessorParameters(StringBuilder svParameters)
        {
            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Reserved_Cores), out int reservedCores) && reservedCores > -1)
                svParameters.Append($"-numreservedcores {reservedCores} ");

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Worker_Threads), out int workerThreads) && workerThreads > -1)
                svParameters.Append($"-numworkerthreads {workerThreads} ");
        }

        private static void AppendNetParameters(StringBuilder svParameters)
        {
            svParameters.Append((bool)IniSettings.Get(IniSettings.Vars.Encrypt_Packets) ? "+net_encryptionEnable 1 " : "+net_encryptionEnable 0 ");
            svParameters.Append((bool)IniSettings.Get(IniSettings.Vars.Random_Netkey) ? "+net_useRandomKey 1 " : "+net_useRandomKey 0 ");
            svParameters.Append((bool)IniSettings.Get(IniSettings.Vars.Queued_Packets) ? "+net_queued_packet_thread 1 " : "+net_queued_packet_thread 0 ");

            if ((bool)IniSettings.Get(IniSettings.Vars.No_Timeout))
                svParameters.Append("-notimeout ");
        }

        private static void AppendConsoleParameters(StringBuilder svParameters)
        {
            var mode = (eMode)(int)IniSettings.Get(IniSettings.Vars.Mode);
            svParameters.Append((bool)IniSettings.Get(IniSettings.Vars.Show_Console) || mode == eMode.SERVER ? "-wconsole " : "-noconsole ");

            if ((bool)IniSettings.Get(IniSettings.Vars.Color_Console))
                svParameters.Append("-ansicolor ");

            var playlistFile = (string)IniSettings.Get(IniSettings.Vars.Playlists_File);
            if (!string.IsNullOrEmpty(playlistFile))
                svParameters.Append($"-playlistfile \"{playlistFile}\" ");
        }

        private static void AppendGameSettingsParameters(StringBuilder svParameters)
        {
            if ((int)IniSettings.Get(IniSettings.Vars.Map) > 0)
                svParameters.Append($"+map {maps[(int)IniSettings.Get(IniSettings.Vars.Map)]} ");

            if ((int)IniSettings.Get(IniSettings.Vars.Playlist) > 0)
                svParameters.Append($"+launchplaylist {gamemodes[(int)IniSettings.Get(IniSettings.Vars.Playlist)]} ");
        }

        private static void AppendDeveloperParameters(StringBuilder svParameters)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Developer))
                svParameters.Append("-dev -devsdk ");

            if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Cheats))
                svParameters.Append("+sv_cheats 1 ");
        }

        private static void AppendOfflineMode(StringBuilder svParameters)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Offline_Mode))
                svParameters.Append("-offline ");
        }

        private static void AppendNoAsyncParameters(StringBuilder svParameters, bool isFullAsync)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.No_Async))
            {
                svParameters.Append("-noasync ");
                svParameters.Append("+async_serialize 0 ");
                svParameters.Append("+sv_asyncAIInit 0 ");
                svParameters.Append("+sv_asyncSendSnapshot 0 ");
                svParameters.Append("+sv_scriptCompileAsync 0 ");
                svParameters.Append("+physics_async_sv 0 ");

                if (isFullAsync)
                {
                    svParameters.Append("+buildcubemaps_async 0 ");
                    svParameters.Append("+cl_scriptCompileAsync 0 ");
                    svParameters.Append("+cl_async_bone_setup 0 ");
                    svParameters.Append("+cl_updatedirty_async 0 ");
                    svParameters.Append("+mat_syncGPU 1 ");
                    svParameters.Append("+mat_sync_rt 1 ");
                    svParameters.Append("+mat_sync_rt_flushes_gpu 1 ");
                    svParameters.Append("+net_async_sendto 0 ");
                    svParameters.Append("+physics_async_cl 0 ");
                }
            }
        }

        private static void AppendCustomCommandLine(StringBuilder svParameters)
        {
            var customCommandLine = (string)IniSettings.Get(IniSettings.Vars.Command_Line);
            if (!string.IsNullOrEmpty(customCommandLine))
                svParameters.Append($"{customCommandLine} ");
        }
    }
}