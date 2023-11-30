using System;
using UnityEngine;

namespace GameServer.Resource
{
    // <ObjectID, Position>
    public class ObjectStatus
    {
        public int ObjectID { get; set; }
        public int ObjectType { get; set; }
        public int HP { get; set; }
        public Vector3 position { get; set; }
    }
}

