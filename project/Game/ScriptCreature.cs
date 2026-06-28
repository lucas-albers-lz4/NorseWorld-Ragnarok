/*
 *  "NorseWorld: Ragnarok", a roguelike game for PCs.
 *  Copyright (C) 2002-2008, 2014, 2020 by Serg V. Zhdanovskih.
 *
 *  JavaScript dialog scripts call camelCase methods on player/NPC (see languages/*.xml).
 *  C# NWCreature uses PascalCase; this wrapper matches the Java Nashorn binding surface.
 */

using NWR.Creatures;

namespace NWR.Game
{
    public sealed class ScriptCreature
    {
        private readonly NWCreature fCreature;

        public ScriptCreature(NWCreature creature)
        {
            fCreature = creature;
        }

        public NWCreature Creature
        {
            get { return fCreature; }
        }

        public bool hasItem(string sign)
        {
            return fCreature.HasItem(sign);
        }

        public bool transferItem(object acceptor, string sign)
        {
            NWCreature target = Unwrap(acceptor);
            if (target == null) {
                return false;
            }
            return fCreature.TransferItem(target, sign);
        }

        public bool isFieldCleared()
        {
            return fCreature.FieldCleared;
        }

        private static NWCreature Unwrap(object acceptor)
        {
            ScriptCreature wrapped = acceptor as ScriptCreature;
            if (wrapped != null) {
                return wrapped.fCreature;
            }
            return acceptor as NWCreature;
        }
    }
}
