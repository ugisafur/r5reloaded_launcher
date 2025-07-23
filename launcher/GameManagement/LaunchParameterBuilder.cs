using launcher.Configuration;
using static launcher.Core.UiReferences;

namespace launcher.GameManagement
{
    public static class LaunchParameterBuilder
    {
        public enum eMode
        {
            HOST,
            SERVER,
            CLIENT
        }

        private static void AppendParameter(ref string svParameters, string parameter, string value = "")
        {
            svParameters += value == "" ? $"{parameter} " : $"{parameter} {value} ";
        }

        private static void AppendHostParameters(ref string svParameters)
        {
            if (!string.IsNullOrEmpty((string)IniSettings.Get(IniSettings.Vars.HostName)))
            {
                AppendParameter(ref svParameters, "+hostname", (string)IniSettings.Get(IniSettings.Vars.HostName));
                AppendParameter(ref svParameters, "+sv_pylonVisibility", ((int)IniSettings.Get(IniSettings.Vars.Visibility)).ToString());
            }
        }

        private static void AppendVideoParameters(ref string svParameters)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Windowed))
                AppendParameter(ref svParameters, "-windowed");
            else
                AppendParameter(ref svParameters, "-fullscreen");

            if ((bool)IniSettings.Get(IniSettings.Vars.Borderless))
                AppendParameter(ref svParameters, "-noborder");
            else
                AppendParameter(ref svParameters, "-forceborder");

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Max_FPS), out int nMaxFps))
                AppendParameter(ref svParameters, "+fps_max", nMaxFps.ToString());
            else
                AppendParameter(ref svParameters, "+fps_max", "0");

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Resolution_Width), out int nResWidth))
                AppendParameter(ref svParameters, "-w", nResWidth.ToString());

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Resolution_Height), out int nResHeight))
                AppendParameter(ref svParameters, "-h", nResHeight.ToString());
        }

        private static void AppendProcessorParameters(ref string svParameters)
        {
            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Reserved_Cores), out int nReservedCores))
            {
                if (nReservedCores > -1) // A reserved core count of 0 seems to crash the game on some systems.
                    AppendParameter(ref svParameters, "-numreservedcores", (string)IniSettings.Get(IniSettings.Vars.Reserved_Cores));
            }

            if (int.TryParse((string)IniSettings.Get(IniSettings.Vars.Worker_Threads), out int nWorkerThreads))
            {
                if (nWorkerThreads > -1)
                    AppendParameter(ref svParameters, "-numworkerthreads", (string)IniSettings.Get(IniSettings.Vars.Worker_Threads));
            }
        }

        private static void AppendNetParameters(ref string svParameters)
        {
            AppendParameter(ref svParameters, "+net_encryptionEnable", (bool)IniSettings.Get(IniSettings.Vars.Encrypt_Packets) == true ? "1" : "0");
            AppendParameter(ref svParameters, "+net_useRandomKey", (bool)IniSettings.Get(IniSettings.Vars.Random_Netkey) == true ? "1" : "0");
            AppendParameter(ref svParameters, "+net_queued_packet_thread", (bool)IniSettings.Get(IniSettings.Vars.Queued_Packets) == true ? "1" : "0");

            if ((bool)IniSettings.Get(IniSettings.Vars.No_Timeout))
                AppendParameter(ref svParameters, "-notimeout");
        }

        private static void AppendConsoleParameters(ref string svParameters)
        {
            eMode mode = (eMode)(int)IniSettings.Get(IniSettings.Vars.Mode);

            if ((bool)IniSettings.Get(IniSettings.Vars.Show_Console) || mode == eMode.SERVER)
                AppendParameter(ref svParameters, "-wconsole");
            else
                AppendParameter(ref svParameters, "-noconsole");

            if ((bool)IniSettings.Get(IniSettings.Vars.Color_Console))
                AppendParameter(ref svParameters, "-ansicolor");

            if (!string.IsNullOrEmpty((string)IniSettings.Get(IniSettings.Vars.Playlists_File)))
                AppendParameter(ref svParameters, "-playlistfile", (string)IniSettings.Get(IniSettings.Vars.Playlists_File));
        }

        public static string BuildParameters()
        {
            string svParameters = "";

            AppendProcessorParameters(ref svParameters);
            AppendConsoleParameters(ref svParameters);
            AppendNetParameters(ref svParameters);

            eMode mode = (eMode)(int)IniSettings.Get(IniSettings.Vars.Mode);
            switch (mode)
            {
                case eMode.HOST:
                    {
                        // GAME ###############################################################
                        if ((int)IniSettings.Get(IniSettings.Vars.Map) > 0)
                            AppendParameter(ref svParameters, "+map", maps[(int)IniSettings.Get(IniSettings.Vars.Map)]);

                        if ((int)IniSettings.Get(IniSettings.Vars.Playlist) > 0)
                            AppendParameter(ref svParameters, "+launchplaylist", gamemodes[(int)IniSettings.Get(IniSettings.Vars.Playlist)]);

                        if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Developer))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Cheats))
                        {
                            AppendParameter(ref svParameters, "+sv_cheats", "1");
                            //AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        if ((bool)IniSettings.Get(IniSettings.Vars.Offline_Mode))
                            AppendParameter(ref svParameters, "-offline");

                        // ENGINE ###############################################################
                        if ((bool)IniSettings.Get(IniSettings.Vars.No_Async))
                        {
                            AppendParameter(ref svParameters, "-noasync");
                            AppendParameter(ref svParameters, "+async_serialize", "0");
                            AppendParameter(ref svParameters, "+buildcubemaps_async", "0");
                            AppendParameter(ref svParameters, "+sv_asyncAIInit", "0");
                            AppendParameter(ref svParameters, "+sv_asyncSendSnapshot", "0");
                            AppendParameter(ref svParameters, "+sv_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_async_bone_setup", "0");
                            AppendParameter(ref svParameters, "+cl_updatedirty_async", "0");
                            AppendParameter(ref svParameters, "+mat_syncGPU", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt_flushes_gpu", "1");
                            AppendParameter(ref svParameters, "+net_async_sendto", "0");
                            AppendParameter(ref svParameters, "+physics_async_sv", "0");
                            AppendParameter(ref svParameters, "+physics_async_cl", "0");
                        }

                        AppendHostParameters(ref svParameters);
                        AppendVideoParameters(ref svParameters);

                        if (!string.IsNullOrEmpty((string)IniSettings.Get(IniSettings.Vars.Command_Line)))
                            AppendParameter(ref svParameters, (string)IniSettings.Get(IniSettings.Vars.Command_Line));

                        return svParameters;
                    }
                case eMode.SERVER:
                    {
                        // GAME ###############################################################
                        if ((int)IniSettings.Get(IniSettings.Vars.Map) > 0)
                            AppendParameter(ref svParameters, "+map", maps[(int)IniSettings.Get(IniSettings.Vars.Map)]);

                        if ((int)IniSettings.Get(IniSettings.Vars.Playlist) > 0)
                            AppendParameter(ref svParameters, "+launchplaylist", gamemodes[(int)IniSettings.Get(IniSettings.Vars.Playlist)]);

                        if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Developer))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Cheats))
                        {
                            AppendParameter(ref svParameters, "+sv_cheats", "1");
                            //AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        if ((bool)IniSettings.Get(IniSettings.Vars.Offline_Mode))
                            AppendParameter(ref svParameters, "-offline");

                        // ENGINE ###############################################################
                        if ((bool)IniSettings.Get(IniSettings.Vars.No_Async))
                        {
                            AppendParameter(ref svParameters, "-noasync");
                            AppendParameter(ref svParameters, "+async_serialize", "0");
                            AppendParameter(ref svParameters, "+sv_asyncAIInit", "0");
                            AppendParameter(ref svParameters, "+sv_asyncSendSnapshot", "0");
                            AppendParameter(ref svParameters, "+sv_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+physics_async_sv", "0");
                        }

                        AppendHostParameters(ref svParameters);

                        if (!string.IsNullOrEmpty((string)IniSettings.Get(IniSettings.Vars.Command_Line)))
                            AppendParameter(ref svParameters, (string)IniSettings.Get(IniSettings.Vars.Command_Line));

                        return svParameters;
                    }
                case eMode.CLIENT:
                    {
                        // Tells the loader module to only load the client dll.
                        AppendParameter(ref svParameters, "-noserverdll");

                        // GAME ###############################################################
                        if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Developer))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Cheats))
                        {
                            AppendParameter(ref svParameters, "+sv_cheats", "1");
                            //AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        if ((bool)IniSettings.Get(IniSettings.Vars.Offline_Mode))
                            AppendParameter(ref svParameters, "-offline");

                        // ENGINE ###############################################################
                        if ((bool)IniSettings.Get(IniSettings.Vars.No_Async))
                        {
                            AppendParameter(ref svParameters, "-noasync");
                            AppendParameter(ref svParameters, "+async_serialize", "0");
                            AppendParameter(ref svParameters, "+buildcubemaps_async", "0");
                            AppendParameter(ref svParameters, "+sv_asyncAIInit", "0");
                            AppendParameter(ref svParameters, "+sv_asyncSendSnapshot", "0");
                            AppendParameter(ref svParameters, "+sv_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+cl_async_bone_setup", "0");
                            AppendParameter(ref svParameters, "+cl_updatedirty_async", "0");
                            AppendParameter(ref svParameters, "+mat_syncGPU", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt", "1");
                            AppendParameter(ref svParameters, "+mat_sync_rt_flushes_gpu", "1");
                            AppendParameter(ref svParameters, "+net_async_sendto", "0");
                            AppendParameter(ref svParameters, "+physics_async_sv", "0");
                            AppendParameter(ref svParameters, "+physics_async_cl", "0");
                        }

                        AppendVideoParameters(ref svParameters);

                        // MAIN ###############################################################
                        if (!string.IsNullOrEmpty((string)IniSettings.Get(IniSettings.Vars.Command_Line)))
                            AppendParameter(ref svParameters, (string)IniSettings.Get(IniSettings.Vars.Command_Line));

                        return svParameters;
                    }
                default:
                    return "";
            }
        }
    }
}