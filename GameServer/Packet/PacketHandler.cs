﻿using GameServer.Room;
using Google.Protobuf;
using NetworkLibrary;
using Server.Session;
using Google.Protobuf.GameProtocol;

namespace Server.Packet
{
    public class PacketHandler
    {
        public static void MatchJoinHandler(PacketSession session, IMessage packet)
        {
            MatchJoin matchJoinPacket = packet as MatchJoin;
            ClientSession clientSession = session as ClientSession;


            var room = RoomManager.Instance.Find(matchJoinPacket.RoomId);
            room?.Push(room.Enter, clientSession, matchJoinPacket);
        }

        public static void MoveObjectHandler(PacketSession session, IMessage packet)
        {
            MoveObject moveObjectPacket = packet as MoveObject;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.MoveObject, clientSession, moveObjectPacket);
        }

        public static void ObjectDeadHandler(PacketSession session, IMessage packet)
        {
            ObjectDead objectDeadPacket = packet as ObjectDead;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.ObjectDead, clientSession, objectDeadPacket);
        }

        public static void ObjectIDReqHandler(PacketSession session, IMessage packet)
        {
            ObjectIDReq objectDeadPacket = packet as ObjectIDReq;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.ObjectIDReq, clientSession, objectDeadPacket);
        }

        public static void CastSkillHandler(PacketSession session, IMessage packet)
        {
            CastSkill castSkillPacket = packet as CastSkill;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.CastSkill, clientSession, castSkillPacket);
        }

        public static void InstantiateObjectHandler(PacketSession session, IMessage packet)
        {
            InstantiateObject objectDeadPacket = packet as InstantiateObject;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.InstantiateObject, clientSession, objectDeadPacket);
        }

        public static void UpdateObjectStatusHandler(PacketSession session, IMessage packet)
        {
            UpdateObjectStatus objectDeadPacket = packet as UpdateObjectStatus;
            ClientSession clientSession = session as ClientSession;

            var room = clientSession.Room;
            room?.Push(room.UpdateObjectStatus, clientSession, objectDeadPacket);
        }
    }
}
