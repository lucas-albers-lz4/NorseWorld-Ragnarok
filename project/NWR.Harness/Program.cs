using System;
using System.Collections.Generic;
using NWR.Game;
using NWR.Harness.Scenarios;

namespace NWR.Harness
{
    public static class Program
    {
        private delegate void Scenario(string repoRoot);

        private static readonly Dictionary<string, Scenario> Scenarios = new Dictionary<string, Scenario>(StringComparer.OrdinalIgnoreCase) {
            { "save-load-roundtrip", SaveLoadScenarios.SaveLoadRoundtrip },
            { "save-overwrite", SaveLoadScenarios.SaveOverwrite },
            { "save-erase", SaveLoadScenarios.SaveErase },
            { "player-metadata", SaveLoadScenarios.PlayerMetadata },
            { "container-roundtrip", GameplayScenarios.ContainerRoundtrip },
            { "effect-persist", GameplayScenarios.EffectPersist },
            { "wait-turns", GameplayScenarios.WaitTurns },
            { "item-use-potion", GameplayScenarios.ItemUsePotion },
            { "bootstrap-check", BootstrapCheck },
            { "build-fixture", FixtureBuilder.BuildSlot8Fixture },
            { "build-container-fixture", FixtureBuilder.BuildContainerFixture },
            { "build-effects-fixture", FixtureBuilder.BuildEffectsFixture },
        };

        public static int Main(string[] args)
        {
            string repoRoot = ResolveRepoRoot();
            bool runAll = false;
            var names = new List<string>();

            for (int i = 0; i < args.Length; i++) {
                if (args[i] == "--all") {
                    runAll = true;
                } else if (args[i] == "--repo" && i + 1 < args.Length) {
                    repoRoot = args[++i];
                } else if (!args[i].StartsWith("-")) {
                    names.Add(args[i]);
                }
            }

            if (runAll) {
                names.Clear();
                names.AddRange(Scenarios.Keys);
            }

            if (names.Count == 0) {
                PrintUsage();
                return 1;
            }

            int failures = 0;
            foreach (string name in names) {
                Scenario scenario;
                if (!Scenarios.TryGetValue(name, out scenario)) {
                    Console.WriteLine("Unknown scenario: " + name);
                    failures++;
                    continue;
                }
                try {
                    scenario(repoRoot);
                    Console.WriteLine("OK  " + name);
                } catch (Exception ex) {
                    Console.WriteLine("FAIL " + name + ": " + ex.Message);
                    failures++;
                }
            }

            return failures == 0 ? 0 : 1;
        }

        private static string ResolveRepoRoot()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++) {
                if (System.IO.File.Exists(System.IO.Path.Combine(dir, "play-cs.sh"))) {
                    return dir;
                }
                dir = System.IO.Path.GetFullPath(System.IO.Path.Combine(dir, ".."));
            }
            return System.IO.Directory.GetCurrentDirectory();
        }

        private static void BootstrapCheck(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            Console.WriteLine("layers=" + game.LayersCount);
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: NWR.Harness [--repo PATH] [--all | scenario ...]");
            Console.WriteLine("Scenarios:");
            foreach (string name in Scenarios.Keys) {
                Console.WriteLine("  " + name);
            }
        }
    }
}
