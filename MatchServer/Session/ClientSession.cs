﻿using Google.Protobuf;
using Google.Protobuf.MatchProtocol;
using MatchServer.Configuration;
using NetworkLibrary;
using Server.Packet;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.Session
{
    public class ClientSession : PacketSession
    {
        public override async Task OnConnected(EndPoint endPoint)
        {
            SessionId = await Authorize();
            if (SessionId <= 0)
            {
                Disconnect();
                return;
            }
            ReceiveLoop();
            Debug.WriteLine($"Client {endPoint} is connected.");
        }

        public override void OnPacketReceived(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnPacketReceived(this, buffer);
        }

        public override void OnSent(int bytesTransferred)
        {
            //Console.WriteLine($"OnSent : {bytesTransferred}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            SessionManager.Instance.Remove(SessionId);

            Debug.WriteLine($"OnDisconnected : {endPoint}");
        }

        // Return value
        // - Authorized: returns userId
        // - Not Authorized: returns -1
        public override async Task<int> Authorize()
        {
            // TODO: Refactor
            int totalLen = 0;
            ReceiveBuffer recvBuffer = new ReceiveBuffer(40);

            while (totalLen < 40)
            {
                try
                {
                    int bytesTransferred = await socket.ReceiveAsync(recvBuffer.WriteSegment, SocketFlags.None);
                    recvBuffer.OnWrite(bytesTransferred);
                    totalLen += bytesTransferred;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    return -1;
                }
            }

            // Parsing [userId:4][sessionId:36]
            int userId = BitConverter.ToInt32(recvBuffer.ReadSegment.Array, recvBuffer.ReadSegment.Offset);
            recvBuffer.OnRead(4);
            string sessionId = Encoding.UTF8.GetString(recvBuffer.ReadSegment.Array, recvBuffer.ReadSegment.Offset, 36);
            recvBuffer.OnRead(36);

            // Redis
            IDatabase db = RedisConfig.Redis.GetDatabase();
            string _sessionId = await db.StringGetAsync($"session:{userId}");
            if (sessionId != _sessionId)
            {
                return -1;
            }

            // Session Manager
            return SessionManager.Instance.Add(userId, this) ? userId : -1;
        }

        public void Send(IMessage packet)
        {
            string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
            PacketId msgId = (PacketId)Enum.Parse(typeof(PacketId), msgName);
            ushort size = (ushort)packet.CalculateSize();
            byte[] sendBuffer = new byte[size + 4];
            Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
            Send(new ArraySegment<byte>(sendBuffer));
        }
    }
}
