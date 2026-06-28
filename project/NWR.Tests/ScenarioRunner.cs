using System;
using System.Collections.Generic;
using System.IO;
using NWR.Game;
using NWR.Tests.Integration;
using NWR.Tests.Integration.Scenarios;

namespace NWR.Tests
{
    public static class ScenarioRunner
    {
        private delegate void Scenario(string repoRoot);

        private static readonly Dictionary<string, Scenario> Scenarios = new Dictionary<string, Scenario>(StringComparer.OrdinalIgnoreCase) {
            { "bootstrap-check", BootstrapCheck },
            { "save-load-roundtrip", SaveLoadScenarios.SaveLoadRoundtrip },
            { "save-overwrite", SaveLoadScenarios.SaveOverwrite },
            { "save-erase", SaveLoadScenarios.SaveErase },
            { "player-metadata", SaveLoadScenarios.PlayerMetadata },
            { "container-roundtrip", GameplayScenarios.ContainerRoundtrip },
            { "effect-persist", GameplayScenarios.EffectPersist },
            { "wait-turns", GameplayScenarios.WaitTurns },
            { "item-use-potion", GameplayScenarios.ItemUsePotion },
            { "teleport-trap", GameplayScenarios.TeleportTrap },
            { "build-fixture", FixtureBuilder.BuildSlot8Fixture },
            { "build-container-fixture", FixtureBuilder.BuildContainerFixture },
            { "build-effects-fixture", FixtureBuilder.BuildEffectsFixture },
        };

        public static int Run(string[] args)
        {
            string repoRoot = ResolveRepoRoot(args);
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
                names.Remove("build-fixture");
                names.Remove("build-container-fixture");
                names.Remove("build-effects-fixture");
            }

            if (names.Count == 0) {
                return -1;
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
            return failures;
        }

        private static void BootstrapCheck(string repoRoot)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            Console.WriteLine("layers=" + game.LayersCount);
        }

        private static string ResolveRepoRoot(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++) {
                if (args[i] == "--repo") {
                    return Path.GetFullPath(args[i + 1]);
                }
            }
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 6; i++) {
                if (File.Exists(Path.Combine(dir, "play-cs.sh"))) {
                    return dir;
                }
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
