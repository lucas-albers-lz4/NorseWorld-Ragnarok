using System;
using System.Globalization;
using System.Text;

namespace NWR.ScenarioDsl
{
    /// <summary>
    /// Minimal JSON encode/decode for Scenario (no third-party JSON on Mono 4.5.2).
    /// </summary>
    public static class ScenarioJson
    {
        public static string ToJson(Scenario scenario)
        {
            var sb = new StringBuilder();
            sb.Append('{');
            WriteProp(sb, "name", scenario.Name, true);
            sb.Append(",\"steps\":[");
            bool first = true;
            for (int i = 0; i < scenario.Steps.Count; i++) {
                ScenarioStep step = scenario.Steps[i];
                if (step.CheckFunc != null && string.IsNullOrEmpty(step.Arg)) {
                    // C#-only lambda check — skip in JSON
                    continue;
                }
                if (!first) {
                    sb.Append(',');
                }
                first = false;
                WriteStep(sb, step);
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private static void WriteStep(StringBuilder sb, ScenarioStep step)
        {
            sb.Append('{');
            WriteProp(sb, "op", OpToString(step.Op), true);
            if (!string.IsNullOrEmpty(step.Arg)) {
                sb.Append(',');
                WriteProp(sb, "arg", step.Arg, false);
            }
            if (step.N != 1) {
                sb.Append(",\"n\":");
                sb.Append(step.N.ToString(CultureInfo.InvariantCulture));
            }
            if (!step.Flag) {
                sb.Append(",\"flag\":false");
            }
            if (!string.IsNullOrEmpty(step.CheckFailMessage) && step.Op != ScenarioOp.Param) {
                // For Check named predicates, CheckFailMessage holds optional arg (effect name).
                // For Param, CheckFailMessage holds the param value — use "value" key.
            }
            if (step.Op == ScenarioOp.Param) {
                sb.Append(',');
                WriteProp(sb, "value", step.CheckFailMessage ?? "", false);
            } else if (!string.IsNullOrEmpty(step.CheckFailMessage)) {
                sb.Append(',');
                WriteProp(sb, "checkArg", step.CheckFailMessage, false);
            }
            sb.Append('}');
        }

        private static void WriteProp(StringBuilder sb, string key, string value, bool first)
        {
            if (!first) {
                // caller handles commas
            }
            sb.Append('"');
            sb.Append(key);
            sb.Append("\":");
            WriteString(sb, value);
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (s != null) {
                for (int i = 0; i < s.Length; i++) {
                    char c = s[i];
                    if (c == '\\' || c == '"') {
                        sb.Append('\\');
                        sb.Append(c);
                    } else if (c == '\n') {
                        sb.Append("\\n");
                    } else {
                        sb.Append(c);
                    }
                }
            }
            sb.Append('"');
        }

        public static Scenario FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) {
                throw new InvalidOperationException("empty scenario JSON");
            }
            int i = 0;
            SkipWs(json, ref i);
            Expect(json, ref i, '{');
            string name = "unnamed";
            var steps = new System.Collections.Generic.List<ScenarioStep>();

            while (true) {
                SkipWs(json, ref i);
                if (Peek(json, i) == '}') {
                    break;
                }
                string key = ReadString(json, ref i);
                SkipWs(json, ref i);
                Expect(json, ref i, ':');
                SkipWs(json, ref i);

                if (string.Equals(key, "name", StringComparison.OrdinalIgnoreCase)) {
                    name = ReadString(json, ref i);
                } else if (string.Equals(key, "steps", StringComparison.OrdinalIgnoreCase)) {
                    Expect(json, ref i, '[');
                    while (true) {
                        SkipWs(json, ref i);
                        if (Peek(json, i) == ']') {
                            i++;
                            break;
                        }
                        steps.Add(ReadStep(json, ref i));
                        SkipWs(json, ref i);
                        if (Peek(json, i) == ',') {
                            i++;
                            continue;
                        }
                        if (Peek(json, i) == ']') {
                            i++;
                            break;
                        }
                        throw new InvalidOperationException("expected , or ] in steps at " + i);
                    }
                } else {
                    SkipValue(json, ref i);
                }

                SkipWs(json, ref i);
                if (Peek(json, i) == ',') {
                    i++;
                    continue;
                }
                if (Peek(json, i) == '}') {
                    break;
                }
                throw new InvalidOperationException("expected , or } at " + i);
            }

            Scenario scenario = Scenario.Create(name);
            for (int s = 0; s < steps.Count; s++) {
                scenario.Steps.Add(steps[s]);
            }
            return scenario;
        }

        private static ScenarioStep ReadStep(string json, ref int i)
        {
            Expect(json, ref i, '{');
            var step = new ScenarioStep { N = 1, Flag = true };
            while (true) {
                SkipWs(json, ref i);
                if (Peek(json, i) == '}') {
                    i++;
                    break;
                }
                string key = ReadString(json, ref i);
                SkipWs(json, ref i);
                Expect(json, ref i, ':');
                SkipWs(json, ref i);

                if (string.Equals(key, "op", StringComparison.OrdinalIgnoreCase)) {
                    step.Op = ParseOp(ReadString(json, ref i));
                } else if (string.Equals(key, "arg", StringComparison.OrdinalIgnoreCase)) {
                    step.Arg = ReadString(json, ref i);
                } else if (string.Equals(key, "value", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(key, "checkArg", StringComparison.OrdinalIgnoreCase)) {
                    step.CheckFailMessage = ReadString(json, ref i);
                } else if (string.Equals(key, "n", StringComparison.OrdinalIgnoreCase)) {
                    step.N = ReadInt(json, ref i);
                } else if (string.Equals(key, "flag", StringComparison.OrdinalIgnoreCase)) {
                    step.Flag = ReadBool(json, ref i);
                } else {
                    SkipValue(json, ref i);
                }

                SkipWs(json, ref i);
                if (Peek(json, i) == ',') {
                    i++;
                    continue;
                }
                if (Peek(json, i) == '}') {
                    i++;
                    break;
                }
                throw new InvalidOperationException("expected , or } in step at " + i);
            }
            return step;
        }

        private static ScenarioOp ParseOp(string op)
        {
            if (string.IsNullOrEmpty(op)) {
                throw new InvalidOperationException("empty op");
            }
            string n = op.Trim().ToLowerInvariant();
            if (n == "loadfixture") return ScenarioOp.LoadFixture;
            if (n == "spawnitem") return ScenarioOp.SpawnItem;
            if (n == "spawncreature") return ScenarioOp.SpawnCreature;
            if (n == "useitem") return ScenarioOp.UseItem;
            if (n == "applyeffect") return ScenarioOp.ApplyEffect;
            if (n == "waitturns") return ScenarioOp.WaitTurns;
            if (n == "savegame") return ScenarioOp.SaveGame;
            if (n == "loadgame") return ScenarioOp.LoadGame;
            if (n == "saveloadroundtrip") return ScenarioOp.SaveLoadRoundtrip;
            if (n == "capture") return ScenarioOp.Capture;
            if (n == "check") return ScenarioOp.Check;
            if (n == "requirelog") return ScenarioOp.RequireLog;
            if (n == "halfhp") return ScenarioOp.HalfHp;
            if (n == "param") return ScenarioOp.Param;
            throw new InvalidOperationException("unknown op: " + op);
        }

        private static string OpToString(ScenarioOp op)
        {
            switch (op) {
                case ScenarioOp.LoadFixture: return "loadFixture";
                case ScenarioOp.SpawnItem: return "spawnItem";
                case ScenarioOp.SpawnCreature: return "spawnCreature";
                case ScenarioOp.UseItem: return "useItem";
                case ScenarioOp.ApplyEffect: return "applyEffect";
                case ScenarioOp.WaitTurns: return "waitTurns";
                case ScenarioOp.SaveGame: return "saveGame";
                case ScenarioOp.LoadGame: return "loadGame";
                case ScenarioOp.SaveLoadRoundtrip: return "saveLoadRoundtrip";
                case ScenarioOp.Capture: return "capture";
                case ScenarioOp.Check: return "check";
                case ScenarioOp.RequireLog: return "requireLog";
                case ScenarioOp.HalfHp: return "halfHp";
                case ScenarioOp.Param: return "param";
                default: return op.ToString();
            }
        }

        private static void SkipWs(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i])) {
                i++;
            }
        }

        private static char Peek(string json, int i)
        {
            if (i >= json.Length) {
                throw new InvalidOperationException("unexpected end of JSON");
            }
            return json[i];
        }

        private static void Expect(string json, ref int i, char c)
        {
            SkipWs(json, ref i);
            if (Peek(json, i) != c) {
                throw new InvalidOperationException("expected '" + c + "' at " + i);
            }
            i++;
        }

        private static string ReadString(string json, ref int i)
        {
            SkipWs(json, ref i);
            Expect(json, ref i, '"');
            var sb = new StringBuilder();
            while (i < json.Length) {
                char c = json[i++];
                if (c == '"') {
                    return sb.ToString();
                }
                if (c == '\\' && i < json.Length) {
                    char e = json[i++];
                    if (e == 'n') sb.Append('\n');
                    else if (e == 'r') sb.Append('\r');
                    else if (e == 't') sb.Append('\t');
                    else sb.Append(e);
                } else {
                    sb.Append(c);
                }
            }
            throw new InvalidOperationException("unterminated string");
        }

        private static int ReadInt(string json, ref int i)
        {
            SkipWs(json, ref i);
            int start = i;
            if (Peek(json, i) == '-') {
                i++;
            }
            while (i < json.Length && char.IsDigit(json[i])) {
                i++;
            }
            return int.Parse(json.Substring(start, i - start), CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(string json, ref int i)
        {
            SkipWs(json, ref i);
            if (json.Substring(i).StartsWith("true", StringComparison.Ordinal)) {
                i += 4;
                return true;
            }
            if (json.Substring(i).StartsWith("false", StringComparison.Ordinal)) {
                i += 5;
                return false;
            }
            throw new InvalidOperationException("expected bool at " + i);
        }

        private static void SkipValue(string json, ref int i)
        {
            SkipWs(json, ref i);
            char c = Peek(json, i);
            if (c == '"') {
                ReadString(json, ref i);
            } else if (c == '{') {
                i++;
                int depth = 1;
                while (depth > 0 && i < json.Length) {
                    char ch = json[i++];
                    if (ch == '"') {
                        i--;
                        ReadString(json, ref i);
                    } else if (ch == '{') depth++;
                    else if (ch == '}') depth--;
                }
            } else if (c == '[') {
                i++;
                int depth = 1;
                while (depth > 0 && i < json.Length) {
                    char ch = json[i++];
                    if (ch == '"') {
                        i--;
                        ReadString(json, ref i);
                    } else if (ch == '[') depth++;
                    else if (ch == ']') depth--;
                }
            } else if (char.IsDigit(c) || c == '-') {
                ReadInt(json, ref i);
            } else if (json.Substring(i).StartsWith("true")) {
                i += 4;
            } else if (json.Substring(i).StartsWith("false")) {
                i += 5;
            } else if (json.Substring(i).StartsWith("null")) {
                i += 4;
            } else {
                throw new InvalidOperationException("cannot skip value at " + i);
            }
        }
    }
}
