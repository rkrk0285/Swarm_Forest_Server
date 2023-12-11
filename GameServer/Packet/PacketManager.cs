using System.Diagnostics;
using Google.Protobuf;
using Google.Protobuf.GameProtocol;
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
        //Dictionary<PacketIDs, Action<PacketSession, PacketBase>> packetHandlers = new Dictionary<PacketIDs, Action<PacketSession, PacketBase>>();

        private PacketManager()
        {
            packetMakers.Add((ushort)PacketId.Matchjoin, MakePacket<MatchJoin>);
            packetMakers.Add((ushort)PacketId.Moveobject, MakePacket<MoveObject>);
            packetMakers.Add((ushort)PacketId.Objectdead, MakePacket<ObjectDead>);
            packetMakers.Add((ushort)PacketId.Objectidreq, MakePacket<ObjectIDReq>);
            packetMakers.Add((ushort)PacketId.Instantiateobject, MakePacket<InstantiateObject>);
            packetMakers.Add((ushort)PacketId.Updateobjectstatus, MakePacket<UpdateObjectStatus>);
            packetMakers.Add((ushort)PacketId.Castskill, MakePacket<CastSkill>);

            packetHandlers.Add((ushort)PacketId.Matchjoin, PacketHandler.MatchJoinHandler);
            packetHandlers.Add((ushort)PacketId.Moveobject, PacketHandler.MoveObjectHandler);
            packetHandlers.Add((ushort)PacketId.Objectdead, PacketHandler.ObjectDeadHandler);
            packetHandlers.Add((ushort)PacketId.Objectidreq, PacketHandler.ObjectIDReqHandler);
            packetHandlers.Add((ushort)PacketId.Instantiateobject, PacketHandler.InstantiateObjectHandler);
            packetHandlers.Add((ushort)PacketId.Updateobjectstatus, PacketHandler.UpdateObjectStatusHandler);
            packetHandlers.Add((ushort)PacketId.Castskill, PacketHandler.CastSkillHandler);
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
