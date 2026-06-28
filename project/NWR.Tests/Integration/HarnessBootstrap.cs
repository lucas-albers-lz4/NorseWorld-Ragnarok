using System;
using System.IO;
using NWR.Game;

namespace NWR.Tests.Integration
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
            NWGameSpace game = TestHost.CreateGameSpace(LogPath);
            if (game.LayersCount == 0) {
                throw new InvalidOperationException("NWGameSpace failed to initialize (see harness.log)");
            }
            return game;
        }
    }
}
