using System;
using System.IO;
using NWR.Game;
using ZRLib.Core;

namespace NWR.Harness
{
    public static class HarnessBootstrap
    {
        public static string RepoRoot { get; private set; }
        public static string LogPath { get; private set; }

        public static NWGameSpace Init(string repoRoot)
        {
            RepoRoot = Path.GetFullPath(repoRoot);

            LogPath = Path.Combine(RepoRoot, "harness.log");
            if (File.Exists(LogPath)) {
                File.Delete(LogPath);
            }
            Logger.LogInit(LogPath);

            var locale = new Locale();
            NWResourceManager.Load();
            locale.SetLang("English");

            GlobalVars.nwrHost = new HeadlessHost(NWResourceManager.GetAppPath());
            GlobalVars.Debug_DevMode = false;

            NWGameSpace game = new NWGameSpace(null);
            if (GlobalVars.nwrGame == null || GlobalVars.nwrGame.LayersCount == 0) {
                throw new InvalidOperationException("NWGameSpace failed to initialize (see harness.log)");
            }
            return game;
        }
    }
}
