using System;
using System.Collections.Generic;

namespace WorldBeat.Api.Contracts
{
    public sealed class CapsuleCreateRequest
    {
        public int UserId { get; set; }
        public string Title { get; set; }
        public DateTime OpenDate { get; set; }
        public List<int> SongIds { get; set; } = new List<int>();
    }

    public sealed class CapsuleOpenNoticeResponse
    {
        public int CapsuleId { get; set; }
        public string Title { get; set; }
        public DateTime OpenDate { get; set; }
    }

    public sealed class CapsuleResponse
    {
        public int CapsuleId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public DateTime OpenDate { get; set; }
        public bool NoticeShown { get; set; }
        public List<SongResponse> Songs { get; set; } = new List<SongResponse>();
    }
}