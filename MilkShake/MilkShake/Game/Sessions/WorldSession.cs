﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Milkshake.Game.Sessions;
using Milkshake.Network;
using Milkshake.Tools;
using System.Net.Sockets;
using System.IO;
using ICSharpCode.SharpZipLib.Zip.Compression;
using Milkshake.Communication.Outgoing.World;
using Milkshake.Communication.Outgoing.Weather;
using Milkshake.Tools.Cryptography;
using Milkshake.Communication;
using Milkshake.Tools.Database;
using Milkshake.Communication.Outgoing.Auth;
using Milkshake.Communication.Incoming.Character;
using Milkshake.Game.Constants;
using Milkshake.Game.Constants.Character;
using Milkshake.Game.Constants.Login;
using System.Globalization;

namespace Milkshake.Net
{
    public class WorldSession : ISession
    {
        public const int BUFFER_SIZE = 20480;
        public const int TIMEOUT = 1000;

        private int connectionID;
        private Socket connectionSocket;
        private byte[] dataBuffer;

        public string ConnectionRemoteIP { get { return connectionSocket.RemoteEndPoint.ToString(); } }
        public int ConnectionID { get { return connectionID; } }

        public UInt32 seed;

        public WorldSession(int _connectionID, Socket _connectionSocket)
        {
            connectionID = _connectionID;
            connectionSocket = _connectionSocket;
            dataBuffer = new byte[BUFFER_SIZE];          

            connectionSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(dataArrival), null);

            sendPacket(Opcodes.SMSG_AUTH_CHALLENGE, new byte[] { 0x33, 0x18, 0x34, 0xC8  } );

           
        }
        VanillaCrypt crypt;
        private byte[] encode(int size, int opcode)
        {
            int index = 0;
            int newSize = size + 2;
            byte[] header = new byte[4];
            if (newSize > 0x7FFF)
                header[index++] = (byte)(0x80 | (0xFF & (newSize >> 16)));

            header[index++] = (byte)(0xFF & (newSize >> 8));
            header[index++] = (byte)(0xFF & newSize);
            header[index++] = (byte)(0xFF & opcode);
            header[index] = (byte)(0xFF & (opcode >> 8));


            if (crypt != null) header = crypt.encrypt(header);

            return header;
        }

        private void dataArrival(IAsyncResult _asyncResult)
        {
            int bytesRecived = 0;

            try { bytesRecived = connectionSocket.EndReceive(_asyncResult); }
            catch (Exception e) { Disconnect(e.Source); }

            if (bytesRecived != 0)
            {
                byte[] data = new byte[bytesRecived];
                Array.Copy(dataBuffer, data, bytesRecived);

                onPacket(data);

                try
                {
                    connectionSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(dataArrival), null);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Meh");
                }
            }
            else
            {
                Disconnect();
            }
        }

        private void sendPacket(Opcodes opcode, byte data)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            byte[] header = encode(1, (int)opcode);

            writer.Write(header);
            writer.Write(data);

            Log.Print(LogType.Database, connectionID + "Server -> Client [" + (Opcodes)opcode + "] [0x" + opcode.ToString("X") + "] [Ec: " + (crypt != null) + "] Length:" + 1);

            sendData((writer.BaseStream as MemoryStream).ToArray());
        }

        private void sendPacket(Opcodes opcode, byte[] data)
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            byte[] header = encode(data.Length, (int)opcode);

            writer.Write(header);
            writer.Write(data);

            Log.Print(LogType.Database, connectionID +  "Server -> Client [" + (Opcodes)opcode + "] [0x" + opcode.ToString("X") + "] [Ec: " + (crypt != null) + "] Length:" + data.Length);

            sendData((writer.BaseStream as MemoryStream).ToArray());
        }

        private void sendData(byte[] send)
        {
            byte[] buffer = new byte[send.Length];
            Buffer.BlockCopy(send, 0, buffer, 0, send.Length);
            
            try
            {

                connectionSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, delegate(IAsyncResult result) {  }, null);
            }
            catch (SocketException)
            {
                Disconnect();
            }
            catch (NullReferenceException)
            {
                Disconnect();
            }
        }

        private void sentData(IAsyncResult iAr)
        {
            try { connectionSocket.EndSend(iAr); }
            catch { Disconnect(); }
        }

        private void Disconnect(object _obj = null)
        {
            try
            {
                Log.Print(LogType.Server, ConnectionRemoteIP + " User Disconnected");

                connectionSocket.Close();
                //Server.FreeConnectionID(connectionID);
            }
            catch (Exception socketException)
            {
                Log.Print(LogType.Error, socketException.ToString());
            }
        }

        public static string byteArrayToHex(byte[] data, int legnth)
        {
            string packetOutput = "";
            byte[] outputData = data;
            for (int i = 0; i < legnth; i++)
            {
                string append = (i == legnth - 1) ? "" : "-";

                packetOutput += outputData[i].ToString("X2") + append;
            }

            return packetOutput;
        }

        private void proccessHeader(byte[] header, out ushort length, out short opcode)
        {
            if (crypt != null)
            {
                crypt.decrypt(header, 6);
            }

            PacketReader reader = new PacketReader(header);

            if (crypt == null)
            {
                length = BitConverter.ToUInt16(new byte[] { header[1], header[0] }, 0);
                opcode = BitConverter.ToInt16(header, 2);
            }
            else
            {
                var aheader = BitConverter.ToUInt32(header, 0);

                length = BitConverter.ToUInt16(new byte[] { header[1], header[0] }, 0);
                opcode = BitConverter.ToInt16(new byte[] { header[2], header[3] }, 0);
            }
        }

        private void onPacket(byte[] data)
        {


                for (int index = 0; index < data.Length; index++)
                {

                    byte[] headerData = new byte[6];
                    Array.Copy(data, index, headerData, 0, 6);

                    ushort length = 0;
                    short opcode = 0;

                    proccessHeader(headerData, out length, out opcode);                
                 

                    

                        Opcodes code = (Opcodes)opcode;

                      


                        byte[] packetDate = new byte[length];
                        Console.WriteLine("aa " + length);
                        Array.Copy(data, index + 6, packetDate, 0, length - 4);
                        Log.Print(LogType.Database, "Server <- Client [" + code + "] Packet Length: " + length + " " + encCount);
                        onPacketOLD(code, packetDate);
                       
                        index += 2 + (length - 1);
                        

                }
            


            Console.WriteLine("done");
        }
        int encCount = 0;
        Accounts account;
        private void onPacketOLD(Opcodes code, byte[] data)
        {
            

            if (code == Opcodes.CMSG_AUTH_SESSION)
            {
                PacketReader reader = new PacketReader(data);
                reader.ReadInt32(); // ClientBuild
                reader.ReadInt32(); // unk2
                String accountName = reader.ReadCString();
                account = DBAccounts.GetAccount(accountName);

                crypt = new VanillaCrypt();
                crypt.init(StringToByteArray(account.SessionKey));
                //crypt.init(StringToByteArray(Console.ReadLine()));

                Log.Print(LogType.Error, "Started Encryption");


             

                
                

                sendHexPacket(Opcodes.SMSG_AUTH_RESPONSE, "0C 00 00 00 00 00 00 00 00 00 ");
            }

            if (code == Opcodes.CMSG_CHAR_ENUM)
            {

                List<Character> characters = DBCharacters.GetCharacters(account.Username);
                /*characters.Add(new Character() { Name = "FreyaSmells", GUID = 12, Class = ClassID.Druid, Race = RaceID.Gnome, Gender = Gender.Male,  MapID = 1, X = -566, Y = -1496, Z = 100 });*/
                //characters.Add(new Character() { Name = "FreyaSmells4000", GUID = 13, Class = 1, Race = 8, MapID = 1, X = -5626, Y = -1496, Z = 100 });

                sendPacket(Opcodes.SMSG_CHAR_ENUM, new PSCharEnum(characters).PacketData);
                //sendHexPacket(Opcodes.SMSG_CHAR_ENUM, "03 01 00 00 00 00 00 00 00 44 61 76 65 00 06 01 00 07 00 07 01 02 01 D7 00 00 00 01 00 00 00 7B 34 37 C5 E7 3B 85 C3 06 52 56 42 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00 4C 75 63 61 73 00 04 01 01 03 05 01 02 08 01 00 00 00 00 01 00 00 00 33 1D 21 46 A2 1D 50 44 1F CD A5 44 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 00 00 00 00 00 00 43 6F 6F 6C 67 75 79 00 01 01 00 08 07 01 05 05 01 0C 00 00 00 00 00 00 00 66 96 0B C6 2D 92 03 C3 F1 23 A6 42 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ");
            }

            if (code == Opcodes.CMSG_CHAR_CREATE)
            {
                PCCharCreate newCharacter = new PCCharCreate(data);

                DBCharacters.CreateCharacter(account, new Character() { Name = Helper.Normalize(newCharacter.Name),
                                                                        Race = (RaceID)newCharacter.Race,
                                                                        Class = (ClassID)newCharacter.Class,
                                                                        Gender = (Gender)newCharacter.Gender,
                                                                        Skin = newCharacter.Skin,
                                                                        Face = newCharacter.Face,
                                                                        HairStyle = newCharacter.HairStyle,
                                                                        HairColor = newCharacter.HairColor,
                                                                        Accessory = newCharacter.Accessorie,
                                                                        Level = 1 });

                sendPacket(Opcodes.SMSG_CHAR_CREATE, (byte)LoginErrorCode.CHAR_CREATE_SUCCESS);
            }

            if (code == Opcodes.CMSG_CHAR_DELETE)
            {
                PCCharDelete deleteCharacter = new PCCharDelete(data);

                DBCharacters.DeleteCharacter(deleteCharacter.Character);

                sendPacket(Opcodes.SMSG_CHAR_DELETE, (byte)LoginErrorCode.CHAR_DELETE_SUCCESS);
            }

            if (code == Opcodes.CMSG_PLAYER_LOGIN)
            {
                /*
                sendHexPacket(Opcodes.SMSG_UPDATE_AURA_DURATION, "00 FF FF FF FF ");
                sendHexPacket(Opcodes.SMSG_SET_EXTRA_AURA_INFO, "01 01 00 99 09 00 00 FF FF FF FF FF FF FF FF");
                
                sendHexPacket(Opcodes.SMSG_LOGIN_VERIFY_WORLD, "01 00 00 00 7B 34 37 C5 E7 3B 85 C3 06 52 56 42 CA A9 49 3F ");
                */
                sendPacket(Opcodes.SMSG_LOGIN_VERIFY_WORLD, new LoginVerifyWorld(1, 618.518f, -4251.67f, 38.718f, 0).Packet);

                sendHexPacket(Opcodes.SMSG_ACCOUNT_DATA_TIMES, "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ");
                
                
                sendHexPacket(Opcodes.SMSG_SET_REST_START, "00 00 00 00 ");
                sendHexPacket(Opcodes.SMSG_BINDPOINTUPDATE, "48 59 36 C5 71 FD 80 C3 B9 FC 53 42 01 00 00 00 D7 00 00 00 ");
                sendHexPacket(Opcodes.SMSG_TUTORIAL_FLAGS, "06 00 40 00 00 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ");

                /*
                sendHexPacket(Opcodes.SMSG_INITIAL_SPELLS, "00 28 00 46 50 00 00 C6 00 00 00 4E 19 00 00 4E 09 00 00 4E 00 00 00 51 00 00 00 0B 56 00 00 CB 19 00 00 CB 00 00 00 6B 00 00 00 CC 00 00 00 C4 00 00 00 47 50 00 00 C7 00 00 00 0A 02 00 00 9D 02 00 00 9E 02 00 00 59 18 00 00 99 09 00 00 AF 09 00 00 EA 0B 00 00 A5 23 00 00 25 0D 00 00 75 23 00 00 B5 14 00 00 66 18 00 00 67 18 00 00 4D 19 00 00 62 1C 00 00 63 1C 00 00 BB 1C 00 00 C2 20 00 00 21 22 00 00 76 23 00 00 9C 23 00 00 45 50 00 00 48 50 00 00 93 54 00 00 94 54 00 00 1A 59 00 00 00 00 ");
                sendHexPacket(Opcodes.SMSG_SEND_UNLEARN_SPELLS, "00 00 00 00 ");
                sendHexPacket(Opcodes.SMSG_ACTION_BUTTONS, "00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 CB 19 00 00 4E 00 00 00 45 50 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ");

                sendHexPacket(Opcodes.SMSG_INITIALIZE_FACTIONS, "40 00 00 00 02 00 00 00 00 00 00 00 00 00 02 00 00 00 00 02 00 00 00 00 10 00 00 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00 00 00 16 00 00 00 00 00 00 00 00 00 08 00 00 00 00 0E 00 00 00 00 09 00 00 00 00 00 00 00 00 00 11 00 00 00 00 11 00 00 00 00 11 00 00 00 00 11 00 00 00 00 06 00 00 00 00 06 00 00 00 00 06 00 00 00 00 06 00 00 00 00 04 00 00 00 00 04 00 00 00 00 04 00 00 00 00 04 00 00 00 00 04 00 00 00 00 06 00 00 00 00 00 00 00 00 00 04 00 00 00 00 04 00 00 00 00 04 00 00 00 00 04 00 00 00 00 04 00 00 00 00 04 00 00 00 00 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 14 00 00 00 00 02 00 00 00 00 10 00 00 00 00 00 00 00 00 00 10 00 00 00 00 10 00 00 00 00 06 00 00 00 00 10 00 00 00 00 0E 00 00 00 00 18 00 00 00 00 00 00 00 00 00 10 00 00 00 00 10 00 00 00 00 10 00 00 00 00 02 00 00 00 00 02 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ");
                */
               sendHexPacket(Opcodes.SMSG_LOGIN_SETTIMESPEED, "E6 A4 11 0D 8A 88 88 3C ");
               sendHexPacket(Opcodes.SMSG_INIT_WORLD_STATES, "01 00 00 00 6C 00 AE 07 01 00 32 05 01 00 31 05 00 00 2E 05 00 00 F9 06 00 00 F3 06 00 00 F1 06 00 00 EE 06 00 00 ED 06 00 00 71 05 00 00 70 05 00 00 67 05 01 00 66 05 01 00 50 05 01 00 44 05 00 00 36 05 00 00 35 05 01 00 C6 03 00 00 C4 03 00 00 C2 03 00 00 A8 07 00 00 A3 07 0F 27 74 05 00 00 73 05 00 00 72 05 00 00 6F 05 00 00 6E 05 00 00 6D 05 00 00 6C 05 00 00 6B 05 00 00 6A 05 01 00 69 05 01 00 68 05 01 00 65 05 00 00 64 05 00 00 63 05 00 00 62 05 00 00 61 05 00 00 60 05 00 00 5F 05 00 00 5E 05 00 00 5D 05 00 00 5C 05 00 00 5B 05 00 00 5A 05 00 00 59 05 00 00 58 05 00 00 57 05 00 00 56 05 00 00 55 05 00 00 54 05 01 00 53 05 01 00 52 05 01 00 51 05 01 00 4F 05 00 00 4E 05 00 00 4D 05 01 00 4C 05 00 00 4B 05 00 00 45 05 00 00 43 05 01 00 42 05 00 00 40 05 00 00 3F 05 00 00 3E 05 00 00 3D 05 00 00 3C 05 00 00 3B 05 00 00 3A 05 01 00 39 05 00 00 38 05 00 00 37 05 00 00 34 05 00 00 33 05 00 00 30 05 00 00 2F 05 00 00 2D 05 01 00 16 05 01 00 15 05 00 00 B6 03 00 00 45 07 02 00 36 07 01 00 35 07 01 00 34 07 01 00 33 07 01 00 32 07 01 00 02 07 00 00 01 07 00 00 00 07 00 00 FE 06 00 00 FD 06 00 00 FC 06 00 00 FB 06 00 00 F8 06 00 00 F7 06 00 00 F6 06 00 00 F4 06 D0 07 F2 06 00 00 F0 06 00 00 EF 06 00 00 EC 06 00 00 EA 06 00 00 E9 06 00 00 E8 06 00 00 E7 06 00 00 18 05 00 00 17 05 00 00 03 07 00 00 ");
               sendHexPacket(Opcodes.SMSG_UPDATE_OBJECT, "01" + "00 00 00 00" + "03" + "01 01 04 71 00 00 00 00 AD 5E 00 00 7B 34 37 C5 E7 3B 85 C3 06 52 56 42 CA A9 49 3F 00 00 00 00 00 00 20 40 00 00 E0 40 00 00 90 40 71 1C 97 40 00 00 20 40 E0 0F 49 40 01 00 00 00 29 15 00 40 54 1D C0 00 00 00 00 00 80 20 00 00 C0 D9 04 C2 4F 38 19 00 00 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 E0 B6 6D DB B6 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 6C 80 00 00 00 00 00 00 80 00 40 00 00 80 3F 00 00 00 00 20 00 00 00 00 00 00 01 00 00 00 19 00 00 00 CD CC AC 3F 54 00 00 00 64 00 00 00 54 00 00 00 E8 03 00 00 64 00 00 00 01 00 00 00 06 00 00 00 06 01 00 01 08 00 00 00 99 09 00 00 09 00 00 00 01 00 00 00 D0 07 00 00 D0 07 00 00 D0 07 00 00 3B 00 00 00 3B 00 00 00 25 49 D2 40 25 49 F2 40 00 EE 11 00 00 00 80 3F 1C 00 00 00 0F 00 00 00 18 00 00 00 0F 00 00 00 16 00 00 00 1E 00 00 00 0A 00 00 00 14 00 00 00 00 28 00 00 27 00 00 00 06 00 00 00 DC B6 ED 3F 6E DB 36 40 07 00 07 01 02 00 00 01 90 01 00 00 1A 00 00 00 01 00 01 00 2C 00 00 00 01 00 05 00 36 00 00 00 01 00 05 00 5F 00 00 00 01 00 05 00 6D 00 00 00 2C 01 2C 01 73 00 00 00 2C 01 2C 01 A0 00 00 00 01 00 05 00 A2 00 00 00 01 00 05 00 9D 01 00 00 01 00 01 00 9E 01 00 00 01 00 01 00 9F 01 00 00 01 00 01 00 B1 01 00 00 01 00 01 00 02 00 00 00 48 E1 9A 40 3E 0A 17 3F 3E 0A 17 3F CD CC 0C 3F 00 00 04 00 29 00 00 00 0A 00 00 00 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F FF FF FF FF ");

                //
            }

            if (code == Opcodes.CMSG_UPDATE_ACCOUNT_DATA)
            {
                

                //Log.Print(LogType.Map, "Length: " + length + " Real Length: " + _dataBuffer.Length);

                
                
    
                //crypt.decrypt(new byte[(int)2 * 6]);
            }

            if (code == Opcodes.CMSG_MESSAGECHAT)
            {
                PacketReader reader = new PacketReader(data);

                uint type = reader.ReadUInt32();
                uint lang = reader.ReadUInt32();
                string message = reader.ReadCString();


                BinaryWriter writer = new BinaryWriter(new MemoryStream());
                writer.Write((byte)0x0A); // TYPE
                writer.Write((UInt32)0); // Lang
                writer.Write((UInt32)0); // GUID
                writer.Write((UInt32)0); // GUID
                writer.Write((UInt32)message.Length );
                byte[] dataa = Encoding.UTF8.GetBytes(message + '\0');
                writer.Write(dataa);

                WorldServer.Sessions.ForEach(session => session.sendPacket(Opcodes.SMSG_MESSAGECHAT, (writer.BaseStream as MemoryStream).ToArray()));

                

                //                                      Type   Lang              Guid                         // Len
                //sendHexPacket(Opcodes.SMSG_MESSAGECHAT, "0A" + " 00 00 00 00 " + "00 00 00 00 00 00 00 00 " + "47 00 00 00" + "57 65 6C 63 6F 6D 65 20 74 6F 20 22 53 65 72 76 65 72 22 2E 20 50 6F 77 65 72 65 64 20 62 79 20 41 6C 74 65 72 57 6F 57 20 33 2E 35 20 66 6F 72 20 43 6C 69 65 6E 74 20 56 65 72 73 69 6F 6E 20 31 2E 31 32 2E 78 00 " + "00 ");
                Console.WriteLine("Chat: " + message);
               // sendPacket(Opcodes.SMSG_WEATHER, new Weather(WeatherState.WEATHER_STATE_LIGHT_RAIN, 0.999f, WeatherSounds.WEATHER_SNOWMEDIUM).Packet);

                if (message.ToLower() == "spawn")
                {
                    sendHexPacket(Opcodes.SMSG_UPDATE_OBJECT, "01" + "00 00 00 00" + "03" + "01 01 04 71 00 00 00 00 AD 5E 00 00 7B 34 37 C5 E7 3B 85 C3 06 52 56 42 CA A9 49 3F 00 00 00 00 00 00 20 40 00 00 E0 40 00 00 90 40 71 1C 97 40 00 00 20 40 E0 0F 49 40 01 00 00 00 29 15 00 40 54 1D C0 00 00 00 00 00 80 20 00 00 C0 D9 04 C2 4F 38 19 00 00 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 E0 B6 6D DB B6 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 80 6C 80 00 00 00 00 00 00 80 00 40 00 00 80 3F 00 00 00 00 20 00 00 00 00 00 00 01 00 00 00 19 00 00 00 CD CC AC 3F 54 00 00 00 64 00 00 00 54 00 00 00 E8 03 00 00 64 00 00 00 01 00 00 00 06 00 00 00 06 01 00 01 08 00 00 00 99 09 00 00 09 00 00 00 01 00 00 00 D0 07 00 00 D0 07 00 00 D0 07 00 00 3B 00 00 00 3B 00 00 00 25 49 D2 40 25 49 F2 40 00 EE 11 00 00 00 80 3F 1C 00 00 00 0F 00 00 00 18 00 00 00 0F 00 00 00 16 00 00 00 1E 00 00 00 0A 00 00 00 14 00 00 00 00 28 00 00 27 00 00 00 06 00 00 00 DC B6 ED 3F 6E DB 36 40 07 00 07 01 02 00 00 01 90 01 00 00 1A 00 00 00 01 00 01 00 2C 00 00 00 01 00 05 00 36 00 00 00 01 00 05 00 5F 00 00 00 01 00 05 00 6D 00 00 00 2C 01 2C 01 73 00 00 00 2C 01 2C 01 A0 00 00 00 01 00 05 00 A2 00 00 00 01 00 05 00 9D 01 00 00 01 00 01 00 9E 01 00 00 01 00 01 00 9F 01 00 00 01 00 01 00 B1 01 00 00 01 00 01 00 02 00 00 00 48 E1 9A 40 3E 0A 17 3F 3E 0A 17 3F CD CC 0C 3F 00 00 04 00 29 00 00 00 0A 00 00 00 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F 00 00 80 3F FF FF FF FF ");
                    //sendHexPacket(Opcodes.SMSG_UPDATE_OBJECT, "01 00 00 00 00 00 01 04 29 00 00 40 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 54 00 00 00 ");
                    
                }
            }




        }


        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        
        public void sendHexPacket(Opcodes opcde, string hex)
        {
            string end = hex.Replace(" ", "").Replace("\n", "");

            byte[] data = StringToByteArray(end);

            sendPacket(opcde, data);
        }

    }
}