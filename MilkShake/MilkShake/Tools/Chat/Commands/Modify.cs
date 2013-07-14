﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Milkshake.Net;
using Milkshake.Game.Entitys;
using System.Reflection;
using Milkshake.Game.Constants.Game.Update;
using Milkshake.Game.Constants.Character;
using Milkshake.Game.Managers;
using Milkshake.Communication.Outgoing.World.Update;
using Milkshake.Tools.Update;
using Milkshake.Communication;
using Milkshake.Tools.Database;
using Milkshake.Tools.Database.Tables;

namespace Milkshake.Tools.Chat.Commands
{
    [ChatCommandNode("modify", "Modify commands")]
    public class Modify
    {
        public static void Default(WorldSession session, string[] args)
        {
            if (args.Length == 1 && args[0].ToLower() == "list")
            {
                session.sendMessage("List");
            }
            else if(args.Length == 2)
            {
                string attributeName = args[0].ToLower();
                string attributeValue = args[1];

                // If player isn't targeting. Target self
                PlayerEntity entity = session.Entity.Target ?? session.Entity;

                bool unknownAttribute = false;

                switch (attributeName)
                {
                    case "scale":
                        entity.Scale = float.Parse(attributeValue);
                        break;

                    case "health":
                        entity.Health = int.Parse(attributeValue);
                        break;

                    case "level":
                        entity.Level = int.Parse(attributeValue);
                        break;

                    case "xp":
                        entity.XP = int.Parse(attributeValue);
                        break;

                    case "gender":
                        entity.SetUpdateField<byte>((int)EUnitFields.UNIT_FIELD_BYTES_0, (byte)int.Parse(attributeValue), 2);
                        break;

                    case "model":
                        entity.SetUpdateField<Int32>((int)EUnitFields.UNIT_FIELD_DISPLAYID, int.Parse(attributeValue));
                        break;

                    case "state":
                        entity.SetUpdateField<byte>((int)EUnitFields.UNIT_NPC_EMOTESTATE, (byte)int.Parse(attributeValue));
                        break;

                    case "unit":
                        PSUpdateObject packet = PSUpdateObject.CreateUnitUpdate(entity);

                        try
                        {
                            UpdateReader.ProccessLog(packet.Packet);
                        }
                        catch (Exception e) { }

                        // Send Packet
                        session.sendPacket(packet);

                        List<CreatureEntry> mobs = DB.World.Table<CreatureEntry>().ToList();



                        List<CreatureEntry>  AWESOME = mobs.FindAll(m => m.map == entity.Character.MapID).FindAll(m => Helper.Distance(m.position_x, m.position_y, entity.X, entity.Y) < 50);

                        AWESOME.ForEach(a => 
                            {
                                //entity.Session.sendMessage(a.id.ToString())
                                //PSUpdateObject abaa = PSUpdateObject.CreateUnitUpdate(a);
                               // session.sendPacket(abaa);


                            });






                        break;

                    default:
                        unknownAttribute = true;
                        break;
                }

                if (unknownAttribute)
                {
                    session.sendMessage("Attribute '" + attributeName + "' was unknown");
                }
                else
                {
                    session.sendMessage("Applied " + attributeName + " = " + attributeValue + "");
                }
            }
        }
    }

}