using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NWR.Creatures;
using NWR.Game;
using NWR.Items;
using NWR.Tests.Integration;
using ZRLib.Core;

namespace NWR.Tests.Integration.Scenarios
{
    /// <summary>
    /// Differential checks vs Java v0.11 (#18): static DB/RGF parity + C# op snapshots.
    /// Java has no headless harness; gameplay ops run on C# and are documented for manual Java A/B.
    /// </summary>
    public static class DifferentialScenarios
    {
        public static void AbDiff(string repoRoot)
        {
            string javaDir = Path.Combine(repoRoot, "nwr-dist-v0.11.0-win");
            string jar = Path.Combine(javaDir, "Ragnarok.jar");
            if (!File.Exists(jar)) {
                throw new InvalidOperationException(
                    "Java dist missing at " + javaDir + ". Run ./dev_info/fetch-java-dist.sh");
            }

            var checks = new List<DiffCheck>();
            CompareLanguageFiles(repoRoot, javaDir, checks);
            CompareJavaConstants(repoRoot, checks);
            RunCsharpOps(repoRoot, checks);

            string reportDir = Path.Combine(repoRoot, "dev_info", "fixtures", "ab");
            Directory.CreateDirectory(reportDir);
            string reportPath = Path.Combine(reportDir, "differential-report.json");
            WriteReport(reportPath, checks);

            int failed = 0;
            for (int i = 0; i < checks.Count; i++) {
                if (!checks[i].Ok) {
                    failed++;
                    Console.WriteLine("  FAIL " + checks[i].Name + ": " + checks[i].Detail);
                } else {
                    Console.WriteLine("  OK   " + checks[i].Name + (string.IsNullOrEmpty(checks[i].Detail) ? "" : " — " + checks[i].Detail));
                }
            }
            Console.WriteLine("wrote " + reportPath + " checks=" + checks.Count + " failed=" + failed);
            if (failed > 0) {
                throw new InvalidOperationException("ab-diff: " + failed + " check(s) failed");
            }
        }

        private static void CompareLanguageFiles(string repoRoot, string javaDir, List<DiffCheck> checks)
        {
            // DBs + dialog scripts must match. UI text XML may drift in the C# tree — record as known diff.
            string[] critical = { "en_db.xml", "ru_db.xml", "ru_dlg_oldman.xml", "langs.xml" };
            string[] soft = { "en_texts.xml", "ru_texts.xml" };

            for (int i = 0; i < critical.Length; i++) {
                CompareOneLang(repoRoot, javaDir, critical[i], true, checks);
            }
            for (int i = 0; i < soft.Length; i++) {
                CompareOneLang(repoRoot, javaDir, soft[i], false, checks);
            }
        }

        private static void CompareOneLang(string repoRoot, string javaDir, string name, bool critical, List<DiffCheck> checks)
        {
            string csPath = Path.Combine(repoRoot, "languages", name);
            string javaPath = Path.Combine(javaDir, "languages", name);
            if (!File.Exists(csPath) || !File.Exists(javaPath)) {
                checks.Add(DiffCheck.Fail("lang:" + name, "missing cs or java file"));
                return;
            }
            string csHash = Sha256Hex(File.ReadAllBytes(csPath));
            string javaHash = Sha256Hex(File.ReadAllBytes(javaPath));
            long csLen = new FileInfo(csPath).Length;
            long javaLen = new FileInfo(javaPath).Length;
            if (csHash == javaHash) {
                checks.Add(DiffCheck.Pass("lang:" + name, "sha256 match " + csHash.Substring(0, 12)));
                return;
            }
            string detail = "sha256 cs=" + csHash.Substring(0, 12) + " java=" + javaHash.Substring(0, 12) +
                " bytes " + csLen + "/" + javaLen;
            if (critical) {
                checks.Add(DiffCheck.Fail("lang:" + name, detail));
            } else {
                checks.Add(DiffCheck.Pass("lang:" + name,
                    "KNOWN_DIFF " + detail + " (UI strings; see ab-test-java-vs-cs.txt)"));
            }
        }

        private static void CompareJavaConstants(string repoRoot, List<DiffCheck> checks)
        {
            string script = Path.Combine(repoRoot, "dev_info", "ab-java-constants.sh");
            if (!File.Exists(script)) {
                checks.Add(DiffCheck.Fail("java-constants", "missing " + script));
                return;
            }

            var psi = new ProcessStartInfo {
                FileName = script,
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            string stdout;
            string stderr;
            int exit;
            using (Process p = Process.Start(psi)) {
                stdout = p.StandardOutput.ReadToEnd();
                stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(60000);
                exit = p.ExitCode;
            }
            if (exit != 0) {
                checks.Add(DiffCheck.Fail("java-constants", "exit " + exit + " " + stderr.Trim()));
                return;
            }

            int release = ParseIntKV(stdout, "Release");
            int revision = ParseIntKV(stdout, "Revision");
            string rgp = ParseStrKV(stdout, "RGP");
            string rgt = ParseStrKV(stdout, "RGT");

            FileVersion csVer = NWGameSpace.RGF_Version;
            if (release == csVer.Release && revision == csVer.Revision) {
                checks.Add(DiffCheck.Pass("rgf-version", "Java+C# " + release + "." + revision));
            } else {
                checks.Add(DiffCheck.Fail("rgf-version",
                    "Java " + release + "." + revision + " vs C# " + csVer.Release + "." + csVer.Revision));
            }

            string csRgp = new string(NWGameSpace.RGP_Sign);
            string csRgt = new string(NWGameSpace.RGT_Sign);
            if (rgp == csRgp && rgt == csRgt) {
                checks.Add(DiffCheck.Pass("save-signs", "RGP/RGT match"));
            } else {
                checks.Add(DiffCheck.Fail("save-signs", "Java " + rgp + "/" + rgt + " vs C# " + csRgp + "/" + csRgt));
            }
        }

        private static void RunCsharpOps(string repoRoot, List<DiffCheck> checks)
        {
            NWGameSpace game = HarnessBootstrap.Init(repoRoot);
            SaveLoadScenarios.CopyFixtureToSlot(repoRoot, "slot8", SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);

            PlayerSnapshot meta = PlayerSnapshot.Capture(game.Player);
            if (string.IsNullOrEmpty(meta.Name) || meta.HPMax <= 0) {
                checks.Add(DiffCheck.Fail("cs:player-metadata", "invalid snapshot name/hp"));
            } else {
                checks.Add(DiffCheck.Pass("cs:player-metadata",
                    "name=" + meta.Name + " hp=" + meta.HPCur + "/" + meta.HPMax +
                    " items=" + meta.ItemCount + " pos=(" + meta.PosX + "," + meta.PosY + ")" +
                    " effects=" + meta.EffectCount));
            }

            int itemsBefore = game.Player.Items.Count;
            Item potion = TestWorld.SpawnItem(game.Player, "Potion_Curing", 1, true);
            if (potion == null || game.Player.Items.Count != itemsBefore + 1) {
                checks.Add(DiffCheck.Fail("cs:item-spawn", "Potion_Curing spawn failed"));
            } else {
                checks.Add(DiffCheck.Pass("cs:item-spawn",
                    "Potion_Curing items " + itemsBefore + "→" + game.Player.Items.Count));
            }

            PlayerSnapshot before = PlayerSnapshot.Capture(game.Player);
            game.SaveGame(SaveLoadScenarios.TestSlot);
            game.LoadGame(SaveLoadScenarios.TestSlot);
            PlayerSnapshot after = PlayerSnapshot.Capture(game.Player);
            try {
                before.AssertMatches(after, "cs:save-load");
                if (after.EffectCount != before.EffectCount) {
                    throw new InvalidOperationException("effect count mismatch");
                }
                checks.Add(DiffCheck.Pass("cs:save-load-roundtrip",
                    "hp=" + after.HPCur + " items=" + after.ItemCount + " effects=" + after.EffectCount));
            } catch (Exception ex) {
                checks.Add(DiffCheck.Fail("cs:save-load-roundtrip", ex.Message));
            }

            LogAssert.RequireLogMarkers(HarnessBootstrap.LogPath, "playerLoad(): ok", "terrainsLoad(): ok");
            checks.Add(DiffCheck.Pass("cs:load-log-markers", "playerLoad/terrainsLoad ok"));
        }

        private static void WriteReport(string path, List<DiffCheck> checks)
        {
            var jb = new JsonBuilder();
            jb.BeginObject();
            jb.Property("generated", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            jb.Property("issue", 18);
            jb.Key("checks");
            jb.BeginArray();
            for (int i = 0; i < checks.Count; i++) {
                jb.BeginObject();
                jb.Property("name", checks[i].Name);
                jb.Property("ok", checks[i].Ok);
                jb.Property("detail", checks[i].Detail ?? "");
                jb.EndObject();
            }
            jb.EndArray();
            int fail = 0;
            for (int i = 0; i < checks.Count; i++) {
                if (!checks[i].Ok) fail++;
            }
            jb.Property("failed", fail);
            jb.Property("total", checks.Count);
            jb.EndObject();
            File.WriteAllText(path, jb.ToString(), Encoding.UTF8);
        }

        private static string Sha256Hex(byte[] data)
        {
            using (var sha = SHA256.Create()) {
                byte[] hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) {
                    sb.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
        }

        private static int ParseIntKV(string text, string key)
        {
            string line = FindLine(text, key + "=");
            if (line == null) {
                throw new InvalidOperationException("missing " + key);
            }
            return int.Parse(line.Substring(key.Length + 1).Trim(), CultureInfo.InvariantCulture);
        }

        private static string ParseStrKV(string text, string key)
        {
            string line = FindLine(text, key + "=");
            if (line == null) {
                throw new InvalidOperationException("missing " + key);
            }
            return line.Substring(key.Length + 1).Trim();
        }

        private static string FindLine(string text, string prefix)
        {
            using (var reader = new StringReader(text)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (line.StartsWith(prefix, StringComparison.Ordinal)) {
                        return line;
                    }
                }
            }
            return null;
        }

        private struct DiffCheck
        {
            public string Name;
            public bool Ok;
            public string Detail;

            public static DiffCheck Pass(string name, string detail)
            {
                return new DiffCheck { Name = name, Ok = true, Detail = detail };
            }

            public static DiffCheck Fail(string name, string detail)
            {
                return new DiffCheck { Name = name, Ok = false, Detail = detail };
            }
        }
    }
}
