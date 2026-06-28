using NWR.Creatures;
using NWR.Effects;
using NWR.Game;
using NWR.Game.Ghosts;
using NWR.Game.Types;
using NWR.GUI;

namespace NWR.Harness
{
    /// <summary>Minimal host identical to NWR.Tests.TestHost for bootstrap debugging.</summary>
    internal sealed class MinimalHost : INwrHost
    {
        private readonly GhostsList fGhosts = new GhostsList();

        public MinimalHost()
        {
            LangExt = "en";
            GameState = GameState.gsDefault;
            Style = NWMainWindow.RGS_CLASSIC;
        }

        public void DoEvent(EventID eventID, object sender, object receiver, object extData) { }
        public void ShowText(string text) { }
        public void ShowText(object sender, string text) { }
        public void ShowText(object sender, string text, LogFeatures features) { }
        public void ShowTextAux(string text) { }
        public void ShowMessage(string text) { }
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
        public void ShowTextRes(object sender, int textID, object[] args) { }
    }
}
