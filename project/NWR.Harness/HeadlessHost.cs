using System;
using System.Collections.Generic;
using System.IO;
using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Ghosts;
using NWR.Game.Types;
using NWR.GUI;
using ZRLib.Core;

namespace NWR.Harness
{
    public sealed class HeadlessHost : INwrHost
    {
        private readonly GhostsList fGhosts;

        public HeadlessHost(string appPath)
        {
            fGhosts = new GhostsList();
            string ghostsFile = appPath + StaticData.GHOSTS_FILE;
            if (File.Exists(ghostsFile)) {
                fGhosts.Load(ghostsFile);
            }

            LangExt = "en";
            GameState = GameState.gsDefault;
            Style = NWMainWindow.RGS_CLASSIC;
            Messages = new List<string>();
        }

        public List<string> Messages { get; private set; }

        public void DoEvent(EventID eventID, object sender, object receiver, object extData)
        {
            if (eventID < EventID.event_First || eventID > EventID.event_Last) {
                return;
            }
            var evtRec = StaticData.dbEvent[(int)eventID];
            if (evtRec.Flags.Contains(EventFlags.InQueue) && GlobalVars.nwrGame != null) {
                GlobalVars.nwrGame.SendEvent(eventID, evtRec.Priority, sender, receiver);
            }
            if (evtRec.Flags.Contains(EventFlags.InJournal) && GlobalVars.nwrGame != null) {
                string msg = GlobalVars.nwrGame.GetEventMessage(eventID, sender, receiver, extData);
                if (!string.IsNullOrEmpty(msg)) {
                    Messages.Add(msg);
                }
            }
        }

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
        public bool ShowNPCDialog(NWCreature collocutor) { return false; }
        public void ShowInventory(NWCreature collocutor) { }
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
        public void ShowTextRes(object sender, int textID, object[] args)
        {
            Messages.Add(BaseLocale.Format(textID, args));
        }
    }
}
