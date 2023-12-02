using System;

namespace GameServer.Resource
{
    // <ObjectID, Position>
    public class ObjectStatus
    {
        public int ObjectID { get; set; }
        public int ObjectType { get; set; }
        public int HP { get; set; }
        public UnityEngine.Vector3 position { get; set; }
        public Google.Protobuf.GameProtocol.Vector3 packetPosition => new Google.Protobuf.GameProtocol.Vector3()
        {
            X  = position.x,
            Y = position.y,
            Z = position.z
        };
    }
}

