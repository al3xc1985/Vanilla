﻿using Vanilla.World.Database;

namespace Vanilla.World.Components.Misc
{
    using System;
    using System.Linq;

    using Vanilla.Core.DBC.Structs;
    using Vanilla.Core.Opcodes;
    using Vanilla.World.Components.Entity;
    using Vanilla.World.Components.Misc.Constants;
    using Vanilla.World.Components.Misc.Packets.Incoming;
    using Vanilla.World.Components.Misc.Packets.Outgoing;
    using Vanilla.World.Game.Entity;
    using Vanilla.World.Game.Entity.Object.Creature;
    using Vanilla.World.Network;

    public class MiscComponent : WorldServerComponent
    {
        public MiscComponent(VanillaWorld vanillaWorld)
            : base(vanillaWorld)
        {
            Router.AddHandler<PCNameQuery>(WorldOpcodes.CMSG_NAME_QUERY, OnNameQuery);
            Router.AddHandler<PCCreatureQuery>(WorldOpcodes.CMSG_CREATURE_QUERY, OnCreatureQuery);
            Router.AddHandler<PCTextEmote>(WorldOpcodes.CMSG_TEXT_EMOTE, OnTextEmote);
            Router.AddHandler<PCEmote>(WorldOpcodes.CMSG_EMOTE, OnEmote);
            Router.AddHandler<PCZoneUpdate>(WorldOpcodes.CMSG_ZONEUPDATE, OnZoneUpdate);
            Router.AddHandler<PCAreaTrigger>(WorldOpcodes.CMSG_AREATRIGGER, OnAreaTrigger);
            Router.AddHandler<PCPing>(WorldOpcodes.CMSG_PING, OnPing);
            Router.AddHandler<PCSetSelection>(WorldOpcodes.CMSG_SET_SELECTION, OnSetSelection);
        }

        public void OnNameQuery(WorldSession session, PCNameQuery packet)
        {
            WorldSession target = Server.Sessions.Find(sesh => sesh.Player.ObjectGUID.RawGUID == packet.GUID);

            if (target != null)
            {
                session.SendPacket(new PSNameQueryResponse(target.Player.Character));
            }
        }

        public void OnCreatureQuery(WorldSession session, PCCreatureQuery packet)
        {
            CreatureEntity entity = Core.GetComponent<EntityComponent>().CreatureEntities.SingleOrDefault(ce => ce.Creature.guid == (long)packet.GUID);

            session.SendPacket(new PSCreatureQueryResponse(packet.Entry, entity));
        }

        private void OnEmote(WorldSession session, PCEmote packet)
        {
            session.SendPacket(new PSEmote(packet.EmoteID, session.Player.ObjectGUID.RawGUID));
        }

        public void OnTextEmote(WorldSession session, PCTextEmote packet)
        {
            //TODO Get the targetname from the packet.GUID
            String targetName = session.Player.Target != null ? session.Player.Target.Name : null;

            Server.TransmitToAll(new PSTextEmote((int)session.Player.Character.guid, (int)packet.EmoteID, (int)packet.TextID, targetName));

            EmotesText textEmote = Core.DBC.GetDBC<EmotesText>().SingleOrDefault(e => e.textid == packet.TextID);

            switch ((Emote)textEmote.textid)
            {

                case Emote.EMOTE_STATE_SLEEP:
                case Emote.EMOTE_STATE_SIT:
                case Emote.EMOTE_STATE_KNEEL:
                case Emote.EMOTE_ONESHOT_NONE:
                    break;
                default:
                    Server.Sessions.ForEach(s => s.SendPacket(new PSEmote(textEmote.textid, session.Player.ObjectGUID.RawGUID)));
                    session.SendPacket(new PSEmote(textEmote.textid, session.Player.ObjectGUID.RawGUID));
                    break;
            }
        }

        public void OnZoneUpdate(WorldSession session, PCZoneUpdate packet)
        {
            unsafe
            {
                //var areaName = new string(Core.DBC.GetDBC<AreaTable>().SingleOrDefault(a => a.id == packet.ZoneID).areaName);
                //session.SendMessage("[ZoneUpdate] ID:" + packet.ZoneID + " " + areaName);
            }
        }

        public void OnAreaTrigger(WorldSession session, PCAreaTrigger packet)
        {
            areatrigger_teleport areaTrigger = Core.WorldDatabase.GetRepository<areatrigger_teleport>().SingleOrDefault(at => at.id == packet.TriggerID);

            if (areaTrigger != null)
            {
                session.SendMessage("[AreaTrigger] ID:" + packet.TriggerID + " " + areaTrigger.name);
                session.Player.Location.MapID = areaTrigger.target_map;
                session.Player.Location.X = areaTrigger.target_position_x;
                session.Player.Location.Y = areaTrigger.target_position_y;
                session.Player.Location.Z = areaTrigger.target_position_z;
                session.Player.Location.Orientation = areaTrigger.target_orientation;

                session.SendPacket(new PSTransferPending(areaTrigger.target_map));
                session.SendPacket(new PSNewWorld(areaTrigger.target_map, areaTrigger.target_position_x, areaTrigger.target_position_y, areaTrigger.target_position_z, areaTrigger.target_orientation));
            }
            else
            {
                session.SendMessage("[AreaTrigger] ID:" + packet.TriggerID);
            }
        }

        public void OnPing(WorldSession session, PCPing packet)
        {
            session.SendMessage("Ping: " + packet.Ping + " Latancy: " + packet.Latency);

            session.SendPacket(new PSPong(packet.Ping));
        }

        public void OnSetSelection(WorldSession session, PCSetSelection packet)
        {
            IUnitEntity target = null;

            WorldSession targetSession = Core.Server.Sessions.SingleOrDefault(s => s.Player.ObjectGUID.RawGUID == packet.GUID);
            if (targetSession != null) target = targetSession.Player;

            if (target == null) target = Core.GetComponent<EntityComponent>().CreatureEntities.SingleOrDefault(e => e.ObjectGUID.RawGUID == packet.GUID);

            if (target != null)
            {
                session.Player.Target = target;
                session.SendMessage("Target: " + target.Name);
            }
            else
            {
                session.SendMessage("Couldnt find target!");
                session.Player.Target = null;
            }
        }
    }
}
