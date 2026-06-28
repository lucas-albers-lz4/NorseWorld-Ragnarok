/*
 *  "NorseWorld: Ragnarok", a roguelike game for PCs.
 *  Copyright (C) 2002-2008, 2014 by Serg V. Zhdanovskih.
 *
 *  Host interface for game logic (real window or headless test harness).
 */

using NWR.Creatures;
using NWR.Database;
using NWR.Effects;
using NWR.Game.Ghosts;
using NWR.Game.Types;
using NWR.GUI;
using ZRLib.Engine;

namespace NWR.Game
{
    public interface INwrHost
    {
        void DoEvent(EventID eventID, object sender, object receiver, object extData);

        void ShowText(string text);
        void ShowText(object sender, string text);
        void ShowText(object sender, string text, LogFeatures features);
        void ShowTextAux(string text);
        void ShowMessage(string text);

        void ProgressInit(int stageCount);
        void ProgressLabel(string stageLabel);
        void ProgressStep();
        void ProgressDone();

        void InitTarget(EffectID effectID, object source, InvokeMode invokeMode, EffectExt ext);
        bool ShowNPCDialog(NWCreature collocutor);
        void ShowInventory(NWCreature collocutor);
        void HideInventory();
        void ShowInput(string caption, IInputAcceptProc acceptProc);

        bool AutoPickup { get; set; }
        bool CircularFOV { get; set; }
        bool ExtremeMode { get; set; }
        int Style { get; set; }
        string LangExt { get; }
        GameState GameState { get; set; }

        GhostsList GhostsList { get; }

        void SetScreen(GameScreen screen);
        void Repaint(int delayInterval);
        void PlaySound(string fileName, int kind, int sX, int sY);
        SoundEngine.Reverb SoundsReverb { set; }

        void ShowDivination();
        void ShowTextRes(object sender, int textID, object[] args);
    }
}
