using Google.Protobuf;
using Google.Protobuf.MatchProtocol;
using MatchServer.WaitingQueue;
using NetworkLibrary;
using Server.Session;
using Domino.Networking.TCP;

namespace Server.Packet
{
    internal class PacketHandler
    {
        //public static void C_JoinHandler(PacketSession session, IMessage packet)
        //{
        //    C_Join cJoinPacket = packet as C_Join;
        //    ClientSession clientSession = session as ClientSession;

        //    //Console.WriteLine($"UserId: {clientSession.SessionId} Added to queue!");

        //    UserQueue.Instance.Add(clientSession);
        //}

        //public static void C_CancelHandler(PacketSession session, IMessage packet)
        //{
        //    C_Cancel cCancelPacket = packet as C_Cancel;
        //    ClientSession clientSession = session as ClientSession;

        //    //Console.WriteLine($"UserId: {clientSession.SessionId} Removed from queue!");

        //    UserQueue.Instance.Remove(clientSession.SessionId);
        //}

        public static void RegisterHandler(PacketSession session, PacketBase packet)
        {
            MatchRegister cJoinPacket = packet as MatchRegister;
            ClientSession clientSession = session as ClientSession;

            //Console.WriteLine($"UserId: {clientSession.SessionId} Added to queue!");

            UserQueue.Instance.Add(clientSession);
        }

        public static void CancelHandler(PacketSession session, PacketBase packet)
        {
            MatchCancel cCancelPacket = packet as MatchCancel;
            ClientSession clientSession = session as ClientSession;

            //Console.WriteLine($"UserId: {clientSession.SessionId} Removed from queue!");

            UserQueue.Instance.Remove(clientSession.SessionId);
        }
    }
}
