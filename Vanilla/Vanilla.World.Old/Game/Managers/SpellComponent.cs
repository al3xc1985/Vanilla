﻿using System.Collections.Generic;
using System.Timers;
using Vanilla.World.Communication.Incoming.World.Spell;
using Vanilla.World.Communication.Outgoing.World;
using Vanilla.World.Communication.Outgoing.World.Spell;
using Vanilla.World.Game.Entitys;
using Vanilla.World.Game.Handlers;
using Vanilla.World.Game.Spells;
using Vanilla.World.Tools.DBC;
using Vanilla.World.Tools.DBC.Tables;

namespace Vanilla.World.Game.Managers
{
    using Database.Character.Models;
    using Vanilla.Core.Opcodes;
    using Vanilla.World.Network;

    public class SpellComponent
    {
        public Dictionary<Character, SpellCollection> SpellCollections = new Dictionary<Character, SpellCollection>();

        public static void Boot()
        {
            WorldDataRouter.AddHandler<PCCastSpell>(WorldOpcodes.CMSG_CAST_SPELL, OnCastSpell);
            WorldDataRouter.AddHandler<PCCancelSpell>(WorldOpcodes.CMSG_CANCEL_CAST, OnCancelSpell);
        }

        public static void SendInitialSpells(WorldSession session)
        {
            //TODO Fix spellCollection DBC
            //session.SendPacket(new PSInitialSpells(session.Entity.SpellCollection));
        }

        private static void OnCastSpell(WorldSession session, PCCastSpell packet)
        {
            PrepareSpell(session, packet);

            ObjectEntity target = (session.Entity.Target != null) ? session.Entity.Target : session.Entity;

            //WorldServer.TransmitToAll(new PSSpellGo(session.Entity, target, packet.spellID));
            session.SendPacket(new PSCastFailed(packet.spellID));

            SpellEntry spell = DBC.Spells.GetSpellByID((int)packet.spellID);
            float spellSpeed = spell.speed;
            
            /*
            float distance =  (float)Math.Sqrt((session.Entity.X - session.Entity.Target.X) * (session.Entity.X - session.Entity.Target.X) +
                                               (session.Entity.Y - session.Entity.Target.Y) * (session.Entity.Y - session.Entity.Target.Y) +
                                               (session.Entity.Z - session.Entity.Target.Z) * (session.Entity.Z - session.Entity.Target.Z));

            if (distance < 5) distance = 5;
            
            float dx = session.Entity.X - target.X;
            float dy = session.Entity.Y - target.Y;
            float dz = session.Entity.Z - target.Target.Z;
            float radius = 5;
            float distance = (float)Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz)) - radius;

            //if (distance < 5) distance = 5;
            float timeToHit = (spellSpeed > 0) ? (float)Math.Floor(distance / spellSpeed * 1000f) : 0;

            session.sendMessage("Cast [" + spell.Name + "] Distance: " + distance + " Speed: " + spellSpeed + " Time: " + timeToHit);
            float radians = (float)(Math.Atan2(session.Entity.Y - session.Entity.Target.Y, session.Entity.X - session.Entity.Target.X));
            
            if(spellSpeed > 0)
            {
                DoTimer(timeToHit, (s, e) =>
                {
                    WorldServer.TransmitToAll(new PSMoveKnockBack(target, (float)Math.Cos(radians), (float)Math.Sin(radians), -10, -10));
                });
            }
           

           */
        }

        private static void PrepareSpell(WorldSession session, PCCastSpell packet)
        {
            UnitEntity target = session.Entity.Target ?? session.Entity;
        }

        public static void DoTimer(double interval, ElapsedEventHandler elapseEvent)
        {
            Timer aTimer = new Timer(interval);
            aTimer.Elapsed += new ElapsedEventHandler(elapseEvent);
            aTimer.Elapsed += new ElapsedEventHandler((s, e) => ((Timer)s).Stop());
            aTimer.Start();
        }

        private static void OnCancelSpell(WorldSession session, PCCancelSpell packet)
        {
            //throw new NotImplementedException();
        }

        public static void OnLearnSpell(WorldSession session, int spellID)
        {
            session.Entity.SpellCollection.AddSpell(spellID);
        }
    }
}
