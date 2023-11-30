using System;
using GameServer.Resource;
using UnityEngine;

namespace GameServer.Resource
{
	public class MonsterInformation
	{
		public static MonsterInformation Instance { get; private set; } = new();

        readonly Dictionary<int, ObjectStatus> monsterInformations = new();

		public ObjectStatus? Get(int objectType)
		{
			if (!monsterInformations.ContainsKey(objectType)) return null;
			return monsterInformations[objectType];
		}

		private MonsterInformation()
		{
            const float centreX = 500, centreZ = 500;

            monsterInformations.Add(1, new ObjectStatus()
			{
				ObjectType = 1,
				HP = 2000,
				position = new Vector3(centreX, 0, centreZ)
			});

            const float cornerX = 7 * 33, cornerZ = 7 * 33;

            monsterInformations.Add(2, new ObjectStatus()
			{
				ObjectType = 2,
				HP = 2000,
				position = new(cornerX, 0, cornerZ)
			});
            monsterInformations.Add(3, new ObjectStatus()
            {
                ObjectType = 3,
                HP = 2000,
                position = new(centreX * 2 - cornerX, 0, cornerZ)
            });
            monsterInformations.Add(4, new ObjectStatus()
            {
                ObjectType = 4,
                HP = 2000,
                position = new(cornerX, 0, centreZ * 2 - cornerZ)
            });
            monsterInformations.Add(5, new ObjectStatus()
            {
                ObjectType = 5,
                HP = 2000,
                position = new(centreX * 2 - cornerX, 0, centreZ * 2 - cornerZ)
            });
        }
	}
}

