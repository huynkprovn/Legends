﻿using System;
using System.Collections.Generic;
using System.Linq;
using Legends.Core.Protocol;
using System.Threading.Tasks;
using Legends.Core.IO;

namespace Legends.Protocol.GameClient.LoadingScreen
{
    public class QueryStatusAnswerMessage : BaseMessage
    {
        public static PacketCmd PACKET_CMD = PacketCmd.PKT_S2C_QueryStatusAns;
        public override PacketCmd Cmd => PACKET_CMD;

        public static Channel CHANNEL = Channel.CHL_S2C;
        public override Channel Channel => CHANNEL;

        public byte state;

        public QueryStatusAnswerMessage(uint netId, byte state) : base(netId)
        {
            this.state = state;
        }
        public QueryStatusAnswerMessage()
        {

        }
        public override void Deserialize(LittleEndianReader reader)
        {
            this.state = reader.ReadByte();
        }

        public override void Serialize(LittleEndianWriter writer)
        {
            writer.WriteByte(state);
        }
    }
}
