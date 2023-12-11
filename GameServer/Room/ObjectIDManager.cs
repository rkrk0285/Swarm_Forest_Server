using System;
namespace GameServer.Room
{
	public class ObjectIDManager
	{
		public static ObjectIDManager Instance { get; private set; } = new ObjectIDManager();

		private readonly PriorityQueue<int, int> returnedIDs;
		private int Id;

		public ObjectIDManager()
		{
			returnedIDs = new PriorityQueue<int, int>();
			Id = 1;
        }

		public int Get()
		{
            return Id++;
            //if(returnedIDs.Count > 0)
            //{
            //	return returnedIDs.Dequeue();
            //}
            //else
            //{
            //             return Id++;
            //         }
        }

		public void Return(int ObjectID)
		{
			returnedIDs.Enqueue(ObjectID, ObjectID);
		}
	}
}

