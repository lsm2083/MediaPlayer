namespace WorldBeat.Api.Models
{
    public sealed class SongEntity
    {
        public int SongId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public int GenreId { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int Duration { get; set; }
        public string FilePath { get; set; }
        public string AlbumArtPath { get; set; }
        public int PlayCount { get; set; }

        // [추가] 가사 속성
        // null 또는 빈 문자열 = 가사 없음
        public string Lyrics { get; set; }
    }
}
