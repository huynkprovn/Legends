﻿using System;
using System.Collections.Generic;
using System.Linq;
using Legends.Core.Protocol;
using System.Threading.Tasks;
using Legends.Core.IO;

namespace Legends.Protocol.GameClient.Other
{
    public class ExitMessage : Message
    {
        public static PacketCmd PACKET_CMD = PacketCmd.PKT_C2S_Exit;
        public override PacketCmd Cmd => PACKET_CMD;

        public static Channel CHANNEL = Channel.CHL_C2S;
        public override Channel Channel => CHANNEL;

        public ExitMessage()
        {

        }

        public override void Serialize(LittleEndianWriter writer)
        {
           
        }

        public override void Deserialize(LittleEndianReader reader)
        {

        }
    }
}
