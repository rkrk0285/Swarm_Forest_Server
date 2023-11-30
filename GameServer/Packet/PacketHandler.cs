using Domino.Networking.TCP;
using GameServer.Room;
using NetworkLibrary;
using Server.Session;

namespace Server.Packet
{
    public class PacketHandler
    {
        public static void MatchJoinHandler(PacketSession session, PacketBase packet)
        {
            MatchJoin matchJoinPacket = packet as MatchJoin;
            ClientSession clientSession = session as ClientSession;


            var room = RoomManager.Instance.Find(matchJoinPacket.RoomID);
            room?.Push(room.Enter, clientSession, matchJoinPacket);
        }

        public static void MoveObjectHandler(PacketSession session, PacketBase packet)
        {
            MoveObject moveObjectPacket = packet as MoveObject;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.MoveObject, clientSession, moveObjectPacket);
        }

        public static void ObjectDeadHandler(PacketSession session, PacketBase packet)
        {
            ObjectDead objectDeadPacket = packet as ObjectDead;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.ObjectDead, clientSession, objectDeadPacket);
        }

        public static void ObjectIDReqHandler(PacketSession session, PacketBase packet)
        {
            ObjectIDReq objectDeadPacket = packet as ObjectIDReq;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.ObjectIDReq, clientSession, objectDeadPacket);
        }

        public static void InstantiateObjectHandler(PacketSession session, PacketBase packet)
        {
            InstantiateObject objectDeadPacket = packet as InstantiateObject;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.InstantiateObject, clientSession, objectDeadPacket);
        }

        public static void UpdateObjectStatusHandler(PacketSession session, PacketBase packet)
        {
            UpdateObjectStatus objectDeadPacket = packet as UpdateObjectStatus;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.UpdateObjectStatus, clientSession, objectDeadPacket);
        }
    }
}
