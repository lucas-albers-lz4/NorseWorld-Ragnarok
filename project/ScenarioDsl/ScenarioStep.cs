using System;

namespace NWR.ScenarioDsl
{
    public enum ScenarioOp
    {
        LoadFixture,
        SpawnItem,
        SpawnCreature,
        UseItem,
        ApplyEffect,
        WaitTurns,
        SaveGame,
        LoadGame,
        SaveLoadRoundtrip,
        Capture,
        Check,
        RequireLog,
        HalfHp,
        Param
    }

    /// <summary>
    /// One declarative step. Lambdas (CheckFunc) are C#-only and omitted from JSON.
    /// </summary>
    public sealed class ScenarioStep
    {
        public ScenarioOp Op;
        public string Arg;
        public int N = 1;
        public bool Flag = true;
        public Func<ScenarioContext, bool> CheckFunc;
        public string CheckFailMessage;

        public ScenarioStep Clone()
        {
            return new ScenarioStep {
                Op = Op,
                Arg = Arg,
                N = N,
                Flag = Flag,
                CheckFunc = CheckFunc,
                CheckFailMessage = CheckFailMessage
            };
        }
    }
}
