using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Ghosts;
using NWR.Game.Types;
using NWR.GUI;

namespace NWR.Tests
{
    /// <summary>
    /// Minimal INwrHost for unit tests that need NWGameSpace / DB.
    /// </summary>
    public sealed class TestHost : INwrHost
    {
        private readonly GhostsList fGhosts = new GhostsList();

        public TestHost()
        {
            LangExt = "en";
            GameState = GameState.gsDefault;
            Style = NWMainWindow.RGS_CLASSIC;
        }

        public System.Collections.Generic.List<string> Messages { get; } = new System.Collections.Generic.List<string>();

        public void DoEvent(EventID eventID, object sender, object receiver, object extData) { }

        public void ShowText(string text) { Messages.Add(text); }
        public void ShowText(object sender, string text) { Messages.Add(text); }
        public void ShowText(object sender, string text, LogFeatures features) { Messages.Add(text); }
        public void ShowTextAux(string text) { Messages.Add(text); }
        public void ShowMessage(string text) { Messages.Add(text); }

        public void ProgressInit(int stageCount) { }
        public void ProgressLabel(string stageLabel) { }
        public void ProgressStep() { }
        public void ProgressDone() { }

        public void InitTarget(EffectID effectID, object source, InvokeMode invokeMode, EffectExt ext) { }
        public bool ShowNPCDialog(NWR.Creatures.NWCreature collocutor) { return false; }
        public void ShowInventory(NWR.Creatures.NWCreature collocutor) { }
        public void HideInventory() { }
        public void ShowInput(string caption, IInputAcceptProc acceptProc) { }

        public bool AutoPickup { get; set; }
        public bool CircularFOV { get; set; }
        public bool ExtremeMode { get; set; }
        public int Style { get; set; }
        public string LangExt { get; private set; }
        public GameState GameState { get; set; }

        public GhostsList GhostsList { get { return fGhosts; } }

        public void SetScreen(GameScreen screen) { }
        public void Repaint(int delayInterval) { }
        public void PlaySound(string fileName, int kind, int sX, int sY) { }
        public SoundEngine.Reverb SoundsReverb { set { } }

        public void ShowDivination() { }
        public void ShowTextRes(object sender, int textID, object[] args) { }

        public static NWGameSpace CreateGameSpace(string logPath)
        {
            if (!string.IsNullOrEmpty(logPath)) {
                ZRLib.Core.Logger.LogInit(logPath);
            }
            var locale = new Locale();
            NWResourceManager.Load();
            locale.SetLang("English");
            GlobalVars.nwrHost = new TestHost();
            GlobalVars.Debug_DevMode = false;
            return new NWGameSpace(null);
        }
    }

    public static class TestBootstrap
    {
        public static NWGameSpace EnsureGame(string logPath)
        {
            if (GlobalVars.nwrGame != null && GlobalVars.nwrGame.LayersCount > 0) {
                return GlobalVars.nwrGame;
            }
            return TestHost.CreateGameSpace(logPath);
        }
    }
}
