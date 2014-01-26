﻿using Vanilla.Core.Network.IO;

namespace Vanilla.World.Communication.Chat.Channel
{
    internal class PCChannel : PacketReader
    {
        public PCChannel(byte[] data)
            : base(data)
        {
            this.ChannelName = ReadCString();
            this.Password = ReadCString();
        }

        public string ChannelName { get; private set; }
        public string Password { get; private set; }
    }
}