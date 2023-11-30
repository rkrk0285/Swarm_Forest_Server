namespace GameServer.Room
{
    // Singleton
    public class RoomManager
    {
        private static RoomManager instance = new RoomManager();
        public static RoomManager Instance { get { return instance; } }

        int roomId = 0;
        object roomLock = new object();
        Dictionary<int, GameRoom> rooms = new Dictionary<int, GameRoom>();

        private RoomManager() { }

        public GameRoom Create()
        {
            lock (roomLock)
            {
                // 
                return new GameRoom();
            }
        }

        public void Remove(int roomId)
        {
            rooms.Remove(roomId);
        }

        public GameRoom? Find(int roomId)
        {
            GameRoom? room = null;
            rooms.TryGetValue(roomId, out room);
            return room;
        }

        public void Update()
        {
            lock (roomLock)
            {
                foreach (var room in rooms.Values)
                {
                    room.Update();
                }
            }
        }
    }
}
