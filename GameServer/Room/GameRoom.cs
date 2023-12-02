using GameServer.Job;
using Google.Protobuf;
using Google.Protobuf.GameProtocol;
using Server.Session;
using GameServer.Resource;
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
            if (!Status.ContainsKey(packet.ObjectId))
            {
                return;
            }

            //float velocity = 10f;

            var now = DateTime.Now;
            var objectID = packet.ObjectId;
            ObjectMoveBeginTime[objectID] = now;
            ObjectMoveExecuteTime[objectID] = now;
            Push(MoveToTarget, objectID, now, Status[objectID].packetPosition, packet.Position);

            //Broadcast(packet);
        }

        public void MoveToTarget(int ObjectID, DateTime movementTime, Vector3 current, Vector3 target)
        {
            var subVector = (current.ToUnityVector3() - target.ToUnityVector3());

            // 목적지에 이동했을 경우 종료 
            if (subVector.sqrMagnitude <= 0) return;
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
            var afterPosition = current.ToUnityVector3() + subVector.normalized * (10f / deltaTime);

            Status[ObjectID].position = afterPosition;

            Broadcast(new MoveObject()
            {
                ObjectId = ObjectID,
                Position = afterPosition.ToPacketVector3()
            });

            ObjectMoveExecuteTime[ObjectID] = now;

            Push(MoveToTarget, ObjectID, movementTime, afterPosition.ToPacketVector3(), target);
        }

        public void ObjectDead(ClientSession session, ObjectDead packet)
        {
            var objectID = packet.ObjectId;
            var objectType = Status[objectID].ObjectType;

            if (MonsterInformation.Instance.Get(objectType) != null)
            {
                EliteMonsterSpawnTimer[objectType] = new(DateTime.Now + GetEliteMonsterCooldown(objectType), false);
            }

            Status.Remove(objectID);
            ObjectIDManager.Instance.Return(objectID);
            
            Broadcast(packet, session);
        }

        public void ObjectIDReq(ClientSession session, ObjectIDReq _)
        {
            session.Send(new ObjectIDRes()
            {
                ObjectId = ObjectIDManager.Instance.Get()
            });
        }

        public void InstantiateObject(ClientSession _, InstantiateObject packet)
        {
            Status.Add(packet.ObjectId, new ObjectStatus()
            {
                ObjectID = packet.ObjectId,
                ObjectType = packet.ObjectType,
                HP = packet.HP,
                position = packet.Position.ToUnityVector3()
            });

            Broadcast(packet);
        }

        public void UpdateObjectStatus(ClientSession _, UpdateObjectStatus packet)
        {
            if (!Status.ContainsKey(packet.ObjectId)) return;

            Status[packet.ObjectId].HP = packet.HP;
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
            var now = DateTime.Now;

            if (!LastExecuteTime_CheckEliteMonsterSpawnTimer.HasValue)
            {
                LastExecuteTime_CheckEliteMonsterSpawnTimer = now;
            }
            else if(now < LastExecuteTime_CheckEliteMonsterSpawnTimer.Value + TimeSpan.FromMilliseconds(333))
            {
                return;
            }

            foreach(var objectType in EliteMonsterSpawnTimer.Keys)
            {
                var nextSpawnTime = EliteMonsterSpawnTimer[objectType].Key;
                var isSpawned = EliteMonsterSpawnTimer[objectType].Value;

                if (isSpawned) continue;
                if(nextSpawnTime <= now)
                {
                    EliteMonsterSpawnTimer[objectType] = new KeyValuePair<DateTime, bool>
                    (
                        now, true
                    );

                    var objectID = ObjectIDManager.Instance.Get();
                    var monsterInfo = MonsterInformation.Instance.Get(objectType);
                    monsterInfo.ObjectID = objectID;
                    Broadcast(new InstantiateObject()
                    {
                        ObjectId = objectID,
                        ObjectType = monsterInfo.ObjectType,
                        HP = monsterInfo.HP,
                        Position = monsterInfo.position.ToPacketVector3()
                    });

                    Status.Add(objectID, monsterInfo);
                }
                else
                {
                    var remains = (float)(nextSpawnTime - now).TotalSeconds;
                    Broadcast(new EliteSpawnTimer()
                    {
                        ObjectType = objectType,
                        Remains = remains
                    });
                }
            }

            LastExecuteTime_CheckEliteMonsterSpawnTimer = now;
        }

        private void Broadcast(IMessage packet)
        {
            for (int i = 0; i < Players.Count; i++)
            {
                Players[i].Send(packet);
            }
        }

        private void Broadcast(IMessage packet, ClientSession except)
        {
            foreach(var player in Players)
            {
                if (player.SessionId == except.SessionId) continue;

                player.Send(packet);
            }
        }
    }
}
