﻿using Google.Protobuf;
using NetworkLibrary;

namespace Server.Packet
{
    // Singleton
    public class PacketManager
    {
        static PacketManager instance = new PacketManager();
        public static PacketManager Instance { get { return instance; } }

        Dictionary<ushort, Func<ArraySegment<byte>, IMessage>> packetMakers = new Dictionary<ushort, Func<ArraySegment<byte>, IMessage>>();
        Dictionary<ushort, Action<PacketSession, IMessage>> packetHandlers = new Dictionary<ushort, Action<PacketSession, IMessage>>();

        private PacketManager()
        {

        }

        // [size(2)][packetId(2)][...]
        public void OnPacketReceived(PacketSession session, ArraySegment<byte> buffer)
        {
            ushort count = 0;
            ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            // Make packet
            Func<ArraySegment<byte>, IMessage> packetMaker = null;
            if (packetMakers.TryGetValue(packetId, out packetMaker))
            {
                IMessage packet = packetMaker.Invoke(buffer);

                // Invoke packet handler
                Action<PacketSession, IMessage> packetHandler = null;
                packetHandlers.TryGetValue(packetId, out packetHandler);
                packetHandler.Invoke(session, packet);
            }
            else
            {
                session.Disconnect();
            }
        }

        T MakePacket<T>(ArraySegment<byte> buffer) where T : IMessage, new()
        {
            T packet = new T();
            packet.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);
            return packet;
        }
    }
}
