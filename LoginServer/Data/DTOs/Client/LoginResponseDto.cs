﻿namespace LoginServer.Data.DTOs.Client
{
    public class LoginResponseDto
    {
        public int UserId { get; set; }
        public string SessionId { get; set; }
        public string MatchServerAddress { get; set; }
        public string GameServerAddress { get; set; }
    }
}
