using System;
using System.Collections.Generic;

namespace WorldBeat.Api.Models
{
    public sealed class CapsuleEntity
    {
        public int CapsuleId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public DateTime OpenDate { get; set; }
        public bool NoticeShown { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SongEntity> Songs { get; set; } = new List<SongEntity>();
    }
}