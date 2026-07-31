using System;
using Jint;
using NWR.Creatures;
using NWR.Game;

namespace NWR.Tests.Integration
{
    /// <summary>
    /// Headless Jint dialog scripts (same surface as NWMainWindow.ExecuteScript + NPCWindow.CheckScript).
    /// </summary>
    public static class DialogScript
    {
        public static object Eval(NWCreature npc, NWCreature player, string script)
        {
            if (string.IsNullOrEmpty(script)) {
                return null;
            }
            var engine = new Engine();
            var snpc = new ScriptCreature(npc);
            var spl = new ScriptCreature(player);
            engine.SetValue("NPC", snpc);
            engine.SetValue("player", spl);
            engine.SetValue("PC", spl);
            engine.Execute(script);
            return engine.GetCompletionValue().ToObject();
        }

        public static bool Check(NWCreature npc, NWCreature player, string script)
        {
            if (string.IsNullOrEmpty(script)) {
                return true;
            }
            object res = Eval(npc, player, script);
            if (res == null) {
                return false;
            }
            if (res is bool) {
                return (bool)res;
            }
            return true.Equals(res);
        }

        public static void RunAction(NWCreature npc, NWCreature player, string script)
        {
            if (string.IsNullOrEmpty(script)) {
                return;
            }
            Eval(npc, player, script);
        }
    }
}
