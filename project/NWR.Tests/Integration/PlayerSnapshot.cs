using System;
using NWR.Creatures;
using NWR.Game;

namespace NWR.Tests.Integration
{
    public struct PlayerSnapshot
    {
        public string Name;
        public int HPCur;
        public int HPMax;
        public int ItemCount;
        public int LayerID;
        public int PosX;
        public int PosY;
        public int EffectCount;

        public static PlayerSnapshot Capture(Player player)
        {
            return new PlayerSnapshot {
                Name = player.Name,
                HPCur = player.HPCur,
                HPMax = player.HPMax_Renamed,
                ItemCount = player.Items.Count,
                LayerID = player.LayerID,
                PosX = player.PosX,
                PosY = player.PosY,
                EffectCount = player.Effects.Count
            };
        }

        public void AssertMatches(PlayerSnapshot other, string label)
        {
            if (Name != other.Name || HPCur != other.HPCur || HPMax != other.HPMax ||
                ItemCount != other.ItemCount || LayerID != other.LayerID ||
                PosX != other.PosX || PosY != other.PosY) {
                throw new InvalidOperationException(
                    label + " snapshot mismatch: " +
                    string.Format("name {0}/{1} hp {2}/{3} items {4}/{5} pos ({6},{7})/({8},{9})",
                        Name, other.Name, HPCur, other.HPCur, ItemCount, other.ItemCount,
                        PosX, PosY, other.PosX, other.PosY));
            }
        }
    }
}
