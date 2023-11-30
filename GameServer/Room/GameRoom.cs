using Domino.Networking.TCP;
using GameServer.Job;
using Google.Protobuf;
using Server.Session;
using GameServer.Resource;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using System;

namespace GameServer.Room
{
    public class GameRoom : JobQueue
    {
        public int RoomID { get; set; }

        public List<int> PlayerIDs { get; set; } = new List<int>();

        public List<ClientSession> Players { get; set; } = new List<ClientSession>();

        const int MaxPlayer = 4;

        public Dictionary<int, ObjectStatus> Status { get; set; } = new Dictionary<int, ObjectStatus>();
        public Dictionary<int, DateTime> ObjectMoveBeginTime = new();
        public Dictionary<int, DateTime> ObjectMoveExecuteTime = new();
        public Dictionary<int, KeyValuePair<DateTime, bool>> EliteMonsterSpawnTimer = new Dictionary<int, KeyValuePair<DateTime, bool>>();

        public void Enter(ClientSession session, MatchJoin packet)
        {
            //if (PlayerIDs.Contains(session.SessionId)) return;
            //PlayerIDs.Add(session.SessionId)
            session.Room = this;
            Players.Add(session);


            if (Players.Count == MaxPlayer) Start();
        }

        public void MoveObject(ClientSession _, MoveObject packet)
        {
            if (!Status.ContainsKey(packet.ObjectID))
            {
                return;
            }

            //float velocity = 10f;

            var now = DateTime.Now;
            var objectID = packet.ObjectID;
            ObjectMoveBeginTime[objectID] = now;
            ObjectMoveExecuteTime[objectID] = now;
            Push(MoveToTarget, objectID, now, Status[objectID].position, packet.Position);

            //Broadcast(packet);
        }



        public void MoveToTarget(int ObjectID, DateTime movementTime, Vector3 current, Vector3 target)
        {
            // 목적지에 이동했을 경우 종료 
            if ((current - target).sqrMagnitude <= 0) return;
            // 새로운 이동 명령이 들어왔을 경우 이전에 실행중이던 이동명령을 무시
            if (ObjectMoveBeginTime[ObjectID] != movementTime) return;
            // 33ms에 한번 씩 실행하도록 제
            if (ObjectMoveExecuteTime[ObjectID] + TimeSpan.FromMilliseconds(33) > DateTime.Now)
            {
                Push(MoveToTarget, ObjectID, movementTime, current, target);
                return;
            }


            var now = DateTime.Now;
            var deltaTime = (float)(now - ObjectMoveExecuteTime[ObjectID]).TotalMilliseconds;
            var afterPosition = current + (target - current).normalized * 10f / deltaTime;

            Status[ObjectID].position = afterPosition;

            Broadcast(Domino.Networking.TCP.MoveObject.Factory.Create(
                ObjectID, afterPosition
            )) ;

            ObjectMoveExecuteTime[ObjectID] = now;

            Push(MoveToTarget, ObjectID, movementTime, afterPosition, target);
        }

        public void ObjectDead(ClientSession _, ObjectDead packet)
        {
            var objectID = packet.ObjectID;
            var objectType = Status[objectID].ObjectType;

            if (MonsterInformation.Instance.Get(objectType) != null)
            {
                EliteMonsterSpawnTimer[objectType] = new(DateTime.Now + GetEliteMonsterCooldown(objectType), false);
            }

            Status.Remove(objectID);
            ObjectIDManager.Instance.Return(objectID);
            
            Broadcast(packet);
        }

        public void ObjectIDReq(ClientSession session, ObjectIDReq _)
        {
            session.Send(ObjectIDRes.Factory.Create(ObjectIDManager.Instance.Get()));
        }

        public void InstantiateObject(ClientSession _, InstantiateObject packet)
        {
            Status.Add(packet.ObjectID, new ObjectStatus()
            {
                ObjectID = packet.ObjectID,
                ObjectType = packet.ObjectType,
                HP = packet.HP,
                position = packet.Position
            });

            Broadcast(packet);
        }

        public void UpdateObjectStatus(ClientSession _, UpdateObjectStatus packet)
        {
            if (!Status.ContainsKey(packet.ObjectID)) return;

            Status[packet.ObjectID].HP = packet.HP;
            //Status[packet.ObjectID].Level = packet.Level;

            Broadcast(packet);
        }

        public bool IsGameRunning { get; private set; } = false;

        public void Update()
        {
            if (!IsGameRunning) return;

            CheckEliteMonsterSpawnTimer();
            Flush();
        }

        public void Start()
        {
            IsGameRunning = true;
            ResetEliteMonsterSpawnTimer();
        }

        private TimeSpan GetEliteMonsterCooldown(int objectType)
        {
            switch (objectType)
            {
                case 1:
                    return TimeSpan.FromSeconds(300);
                case 2:
                case 3:
                case 4:
                case 5:
                    return TimeSpan.FromSeconds(120);
                default:
                    return TimeSpan.MaxValue;
            }
        }

        private void ResetEliteMonsterSpawnTimer()
        {
            EliteMonsterSpawnTimer.Clear();

            // Centre Elite Monster
            EliteMonsterSpawnTimer.Add(1, new KeyValuePair<DateTime, bool>(DateTime.Now + GetEliteMonsterCooldown(1), false));
            // Each Corner
            EliteMonsterSpawnTimer.Add(2, new KeyValuePair<DateTime, bool>(DateTime.Now + GetEliteMonsterCooldown(2), false));
            EliteMonsterSpawnTimer.Add(3, new KeyValuePair<DateTime, bool>(DateTime.Now + GetEliteMonsterCooldown(3), false));
            EliteMonsterSpawnTimer.Add(4, new KeyValuePair<DateTime, bool>(DateTime.Now + GetEliteMonsterCooldown(4), false));
            EliteMonsterSpawnTimer.Add(5, new KeyValuePair<DateTime, bool>(DateTime.Now + GetEliteMonsterCooldown(5), false));
        }

        public DateTime? LastExecuteTime_CheckEliteMonsterSpawnTimer = null;

        private void CheckEliteMonsterSpawnTimer()
        {
            if (!LastExecuteTime_CheckEliteMonsterSpawnTimer.HasValue)
            {
                LastExecuteTime_CheckEliteMonsterSpawnTimer = DateTime.Now;
            }
            else if
                (
                DateTime.Now <
                LastExecuteTime_CheckEliteMonsterSpawnTimer.Value + TimeSpan.FromMilliseconds(333)
                )
            {
                return;
            }

            foreach(var objectType in EliteMonsterSpawnTimer.Keys)
            {
                var nextSpawnTime = EliteMonsterSpawnTimer[objectType].Key;
                var isSpawned = EliteMonsterSpawnTimer[objectType].Value;

                if (isSpawned) continue;
                if(nextSpawnTime <= DateTime.Now)
                {
                    EliteMonsterSpawnTimer[objectType] = new KeyValuePair<DateTime, bool>
                    (
                        DateTime.Now, true
                    );

                    var objectID = ObjectIDManager.Instance.Get();
                    var monsterInfo = MonsterInformation.Instance.Get(objectType);
                    monsterInfo.ObjectID = objectID;
                    Broadcast(Domino.Networking.TCP.InstantiateObject.Factory.Create(
                        objectID,
                        monsterInfo.ObjectType,
                        monsterInfo.HP,
                        monsterInfo.position
                    ));

                    Status.Add(objectID, monsterInfo);
                }
                else
                {
                    var remains = nextSpawnTime - DateTime.Now;
                    Broadcast(EliteMonsterTimer.Factory.Create(objectType, remains.TotalSeconds));
                }
            }
        }

        private void Broadcast(PacketBase packet)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Send(packet);
            }
        }

        private void Broadcast(PacketBase packet, ClientSession except)
        {
            foreach(var player in Players)
            {
                if (player.SessionId == except.SessionId) continue;

                player.Send(packet);
            }
        }
    }
}
