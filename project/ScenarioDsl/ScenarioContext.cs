using System;
using System.Collections.Generic;
using NWR.Creatures;
using NWR.Game;
using NWR.Items;

namespace NWR.ScenarioDsl
{
    public sealed class ScenarioContext
    {
        public string RepoRoot;
        public IScenarioEnv Env;
        public NWGameSpace Game;
        public Item LastItem;
        public NWCreature LastCreature;
        public readonly Dictionary<string, string> Params = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, int> Captured = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public Player Player
        {
            get { return Game != null ? Game.Player : null; }
        }

        public string Resolve(string value)
        {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }
            string result = value;
            foreach (KeyValuePair<string, string> kv in Params) {
                result = result.Replace("${" + kv.Key + "}", kv.Value);
            }
            return result;
        }

        public void Capture(string key)
        {
            if (Player == null) {
                throw new InvalidOperationException("capture before load");
            }
            string k = key != null ? key.Trim().ToLowerInvariant() : "";
            if (k == "turn") {
                Captured["turn"] = Player.Turn;
            } else if (k == "hp") {
                Captured["hp"] = Player.HPCur;
            } else if (k == "effects") {
                Captured["effects"] = Player.Effects.Count;
            } else {
                throw new InvalidOperationException("unknown capture key: " + key);
            }
        }

        public int GetCaptured(string key)
        {
            int value;
            if (!Captured.TryGetValue(key, out value)) {
                throw new InvalidOperationException("no capture for '" + key + "'");
            }
            return value;
        }
    }
}
