namespace WorldBeat.Api.Contracts
{
    public sealed class SongCreateCommand
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int Duration { get; set; }
        public string FilePath { get; set; }
        public string AlbumArtPath { get; set; }

        // [추가] 가사 (업로드 시 입력, 없으면 null)
        public string Lyrics { get; set; }
    }

    public sealed class SongUpdateRequest
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int Duration { get; set; }

        // [추가] 가사 수정도 가능하도록 추가
        public string Lyrics { get; set; }
    }

    public sealed class SongResponse
    {
        public int SongId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int Duration { get; set; }
        public int PlayCount { get; set; }
        public string FileUrl { get; set; }
        public string AlbumArtUrl { get; set; }

        // [추가] 가사를 클라이언트(윈폼)에 전달하는 필드
        // null 또는 빈 문자열 = 가사 없음
        public string Lyrics { get; set; }

        public string DurationText =>
            Duration > 0 ? $"{Duration / 60}:{Duration % 60:D2}" : "--:--";
    }

    public sealed class SongStreamInfo
    {
        public Stream Stream { get; set; }
        public string ContentType { get; set; }
        public string DownloadName { get; set; }
    }
}
