using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace launcher
{
    public static class LaunchParameters
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
            if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.HostName, "")))
            {
                AppendParameter(ref svParameters, "+hostname", Ini.Get(Ini.Vars.HostName, ""));
                AppendParameter(ref svParameters, "+sv_pylonVisibility", Ini.Get(Ini.Vars.Visibility, 0).ToString());
            }
        }

        private static void AppendVideoParameters(ref string svParameters)
        {
            if (Ini.Get(Ini.Vars.Windowed, false))
                AppendParameter(ref svParameters, "-windowed");
            else
                AppendParameter(ref svParameters, "-fullscreen");

            if (Ini.Get(Ini.Vars.Borderless, false))
                AppendParameter(ref svParameters, "-noborder");
            else
                AppendParameter(ref svParameters, "-forceborder");

            AppendParameter(ref svParameters, "+fps_max", Ini.Get(Ini.Vars.Max_FPS, "-1"));

            if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Resolution_Width, "")))
                AppendParameter(ref svParameters, "-w", Ini.Get(Ini.Vars.Resolution_Width, ""));

            if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Resolution_Height, "")))
                AppendParameter(ref svParameters, "-h", Ini.Get(Ini.Vars.Resolution_Height, ""));
        }

        private static void AppendProcessorParameters(ref string svParameters)
        {
            int nReservedCores = int.Parse(Ini.Get(Ini.Vars.Reserved_Cores, "-1"));
            if (nReservedCores > -1) // A reserved core count of 0 seems to crash the game on some systems.
                AppendParameter(ref svParameters, "-numreservedcores", Ini.Get(Ini.Vars.Reserved_Cores, "-1"));

            int nWorkerThreads = int.Parse(Ini.Get(Ini.Vars.Worker_Threads, "-1"));
            if (nWorkerThreads > -1)
                AppendParameter(ref svParameters, "-numworkerthreads", Ini.Get(Ini.Vars.Worker_Threads, "-1"));
        }

        private static void AppendNetParameters(ref string svParameters)
        {
            AppendParameter(ref svParameters, "+net_encryptionEnable", Ini.Get(Ini.Vars.Encrypt_Packets, false) == true ? "1" : "0");
            AppendParameter(ref svParameters, "+net_useRandomKey", Ini.Get(Ini.Vars.Random_Netkey, false) == true ? "1" : "0");
            AppendParameter(ref svParameters, "+net_queued_packet_thread", Ini.Get(Ini.Vars.Queued_Packets, false) == true ? "1" : "0");

            if (Ini.Get(Ini.Vars.No_Timeout, false))
                AppendParameter(ref svParameters, "-notimeout");
        }

        private static void AppendConsoleParameters(ref string svParameters)
        {
            eMode mode = (eMode)Ini.Get(Ini.Vars.Mode, 0);

            if (Ini.Get(Ini.Vars.Show_Console, false) || mode == eMode.SERVER)
                AppendParameter(ref svParameters, "-wconsole");
            else
                AppendParameter(ref svParameters, "-noconsole");

            if (Ini.Get(Ini.Vars.Color_Console, false))
                AppendParameter(ref svParameters, "-ansicolor");

            if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Playlists_File, "playlists_r5_patch.txt")))
                AppendParameter(ref svParameters, "-playlistfile", Ini.Get(Ini.Vars.Playlists_File, "playlists_r5_patch.txt"));
        }

        public static string BuildParameter()
        {
            string svParameters = "";

            AppendProcessorParameters(ref svParameters);
            AppendConsoleParameters(ref svParameters);
            AppendNetParameters(ref svParameters);

            eMode mode = (eMode)Ini.Get(Ini.Vars.Mode, 0);
            switch (mode)
            {
                case eMode.HOST:
                    {
                        // GAME ###############################################################
                        if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Map, "")))
                            AppendParameter(ref svParameters, "+map", Ini.Get(Ini.Vars.Map, ""));

                        if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Playlist, "")))
                            AppendParameter(ref svParameters, "+launchplaylist", Ini.Get(Ini.Vars.Playlist, ""));

                        if (Ini.Get(Ini.Vars.Enable_Developer, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if (Ini.Get(Ini.Vars.Enable_Cheats, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        // ENGINE ###############################################################
                        if (Ini.Get(Ini.Vars.No_Async, false))
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

                        if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Command_Line, "")))
                            AppendParameter(ref svParameters, Ini.Get(Ini.Vars.Command_Line, ""));

                        return svParameters;
                    }
                case eMode.SERVER:
                    {
                        // GAME ###############################################################
                        if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Map, "")))
                            AppendParameter(ref svParameters, "+map", Ini.Get(Ini.Vars.Map, ""));

                        if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Playlist, "")))
                            AppendParameter(ref svParameters, "+launchplaylist", Ini.Get(Ini.Vars.Playlist, ""));

                        if (Ini.Get(Ini.Vars.Enable_Developer, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if (Ini.Get(Ini.Vars.Enable_Cheats, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        // ENGINE ###############################################################
                        if (Ini.Get(Ini.Vars.No_Async, false))
                        {
                            AppendParameter(ref svParameters, "-noasync");
                            AppendParameter(ref svParameters, "+async_serialize", "0");
                            AppendParameter(ref svParameters, "+sv_asyncAIInit", "0");
                            AppendParameter(ref svParameters, "+sv_asyncSendSnapshot", "0");
                            AppendParameter(ref svParameters, "+sv_scriptCompileAsync", "0");
                            AppendParameter(ref svParameters, "+physics_async_sv", "0");
                        }

                        AppendHostParameters(ref svParameters);

                        if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Command_Line, "")))
                            AppendParameter(ref svParameters, Ini.Get(Ini.Vars.Command_Line, ""));

                        return svParameters;
                    }
                case eMode.CLIENT:
                    {
                        // Tells the loader module to only load the client dll.
                        AppendParameter(ref svParameters, "-noserverdll");

                        // GAME ###############################################################
                        if (Ini.Get(Ini.Vars.Enable_Developer, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-devsdk");
                        }

                        if (Ini.Get(Ini.Vars.Enable_Cheats, false))
                        {
                            AppendParameter(ref svParameters, "-dev");
                            AppendParameter(ref svParameters, "-showdevmenu");
                        }

                        // ENGINE ###############################################################
                        if (Ini.Get(Ini.Vars.No_Async, false))
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
                        if (!string.IsNullOrEmpty(Ini.Get(Ini.Vars.Command_Line, "")))
                            AppendParameter(ref svParameters, Ini.Get(Ini.Vars.Command_Line, ""));

                        return svParameters;
                    }
                default:
                    return "";
            }
        }
    }
}