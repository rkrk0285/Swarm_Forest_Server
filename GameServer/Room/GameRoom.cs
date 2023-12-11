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

        const int MaxPlayer = 2;

        public Dictionary<int, ObjectStatus> Status { get; set; } = new Dictionary<int, ObjectStatus>();
        public Dictionary<int, DateTime> ObjectMoveBeginTime = new();
        public Dictionary<int, DateTime> ObjectMoveExecuteTime = new();
        public Dictionary<int, KeyValuePair<DateTime, bool>> EliteMonsterSpawnTimer = new();

        public void Enter(ClientSession? session, MatchJoin? packet)
        {
            //if (PlayerIDs.Contains(session.SessionId)) return;
            //PlayerIDs.Add(session.SessionId)
            session.Room = this;
            Players.Add(session);


            if (Players.Count == MaxPlayer) Start();
        }

        public void Leave(int sessionId)
        {
            Players.RemoveAll((e) => e.SessionId == sessionId);
            if(Players.Count == 0)
            {
                RoomManager.Instance.Remove(RoomID);
            }
        }

        public void CastSkill(ClientSession? session, CastSkill? packet)
        {
            InstantiateObject(session, new InstantiateObject()
            {
                CasterId = packet.CasterId,
                ObjectId = packet.ObjectId,
                ObjectType = packet.ObjectType,
                HP = packet.HP,
                Position = packet.Position
            });

            var ObjectId = Status.Last().Key;

            Thread.Sleep(10);
            MoveObject(session, new MoveObject()
            {
                ObjectId = ObjectId,
                Position = packet.Target
            });
        }

        public void MoveObject(ClientSession? _, MoveObject? packet)
        {
            if (!Status.ContainsKey(packet.ObjectId)) return;

            var now = DateTime.Now;
            var objectID = packet.ObjectId;
            if (!ObjectMoveBeginTime.ContainsKey(objectID))
            {
                ObjectMoveBeginTime.Add(objectID, now);
                ObjectMoveExecuteTime.Add(objectID, (now - TimeSpan.FromMilliseconds(MoveFPS)));
            }
            else
            {
                ObjectMoveBeginTime[objectID] = now;
                ObjectMoveExecuteTime[objectID] = (now - TimeSpan.FromMilliseconds(MoveFPS));
            }
            Push(MoveToTarget, objectID, now, Status[objectID].packetPosition, packet.Position, 1);
        }

        const int MoveFPS = 33;

        public void MoveToTarget(int ObjectID, DateTime movementTime, Vector3 current, Vector3 target, int count)
        {
            if (!Status.ContainsKey(ObjectID)) return;

            var current2D = new UnityEngine.Vector2(current.X, current.Z);
            var target2D = new UnityEngine.Vector2(target.X, target.Z);

            var subVector = (target2D - current2D);
            var now = DateTime.Now;

            // 목적지에 이동했을 경우 종료 
            if (subVector.magnitude <= 1f) return;
            // 새로운 이동 명령이 들어왔을 경우 이전에 실행중이던 이동명령을 무시
            if (ObjectMoveBeginTime[ObjectID] != movementTime) return;
            // 33ms에 한번 씩 실행하도록 제
            if (ObjectMoveExecuteTime[ObjectID] + TimeSpan.FromMilliseconds(MoveFPS) > now)
            {
                Push(MoveToTarget, ObjectID, movementTime, current, target, count + 1);
                return;
            }

            var velocity = 30f;
            if(Status[ObjectID].ObjectType >= 100 && Status[ObjectID].ObjectType <= 105)
            {
                velocity = 50f;
            }

            var deltaTime = (float)(now - ObjectMoveExecuteTime[ObjectID]).TotalMilliseconds;
            if (deltaTime < 0f) return;
            var afterPosition2D = current2D + subVector.normalized * (velocity * (deltaTime / 1000f));
            var afterPosition = new UnityEngine.Vector3(afterPosition2D.x, 0, afterPosition2D.y);

            Status[ObjectID].position = afterPosition;

            Broadcast(new MoveObject()
            {
                ObjectId = ObjectID,
                Position = afterPosition.ToPacketVector3()
            });

            Console.WriteLine($"[MOVEOBJECT] Count: {count}, ObjId: {ObjectID}\n\tMoved: [ X: {afterPosition.x}, Z: {afterPosition.z} ]\n\tTarget: [ X: {target.X}, Z: {target.Z} ]\n\tDistance: {subVector.magnitude}");

            ObjectMoveExecuteTime[ObjectID] = now;

            Push(MoveToTarget, ObjectID, movementTime, afterPosition.ToPacketVector3(), target, count);
        }

        public void ObjectDead(ClientSession? _, ObjectDead? packet)
        {
            var objectID = packet.ObjectId;
            var objectType = Status[objectID].ObjectType;

            if (MonsterInformation.Instance.Get(objectType) != null)
            {
                EliteMonsterSpawnTimer[objectType] = new(DateTime.Now + GetEliteMonsterCooldown(objectType), false);
            }

            Status.Remove(objectID);
            ObjectIDManager.Instance.Return(objectID);
            
            Broadcast(packet);
        }

        public void ObjectIDReq(ClientSession? session, ObjectIDReq? _)
        {
            session.Send(new ObjectIDRes()
            {
                ObjectId = ObjectIDManager.Instance.Get()
            });
        }

        public void InstantiateObject(ClientSession? _, InstantiateObject? packet)
        {
            if (Status.ContainsKey(packet.ObjectId)) return;

            var newObjectId = ObjectIDManager.Instance.Get();

            Status.Add(newObjectId, new ObjectStatus()
            {
                ObjectID = newObjectId,
                ObjectType = packet.ObjectType,
                HP = packet.HP,
                position = packet.Position.ToUnityVector3()
            });
            //var now = DateTime.Now;
            Console.WriteLine($"[INSTANTIATE] ObjId: {newObjectId}\n\tPosition: [ X: {packet.Position.X}, Z: {packet.Position.Z}]");

            packet.ObjectId = newObjectId;

            Broadcast(packet);
        }

        public void UpdateObjectStatus(ClientSession? _, UpdateObjectStatus? packet)
        {
            if (!Status.ContainsKey(packet.ObjectId)) return;

            Console.WriteLine($"[UPDATEOBJECTSTATUS] HP: {packet.HP}");

            Status[packet.ObjectId].HP = packet.HP;
            if (Status[packet.ObjectId].HP <= 0)
            {
                ObjectDead(_, new ObjectDead()
                {
                    ObjectId = packet.ObjectId
                });
            }
            else
            {
                Broadcast(packet);
            }
        }

        public bool IsGameRunning { get; private set; } = true;

        public void Update()
        {
            //if (!IsGameRunning) return;

            //CheckEliteMonsterSpawnTimer();
            Flush();
        }

        public void Start()
        {
            SendPlayerLocation();
            ResetEliteMonsterSpawnTimer();
        }

        private void SendPlayerLocation()
        {
            for(int i = 0; i < Players.Count; ++i)
            {
                //Send
                Players[i].Send(new PlayerLocation()
                {
                    Location = i + 1
                });
            }   
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

                    Status.Add(objectID, monsterInfo);
                    Broadcast(new InstantiateObject()
                    {
                        ObjectId = objectID,
                        ObjectType = monsterInfo.ObjectType,
                        HP = monsterInfo.HP,
                        Position = monsterInfo.position.ToPacketVector3()
                    });
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
    }
}
