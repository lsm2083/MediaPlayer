namespace WorldBeat.Api.Contracts
{
    public sealed class SongUploadForm
    {
        public IFormFile File { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int Duration { get; set; }

        // [추가] Swagger AdminSongs 업로드 폼에서 Lyrics 입력란이 표시돼
        // Duration 아래에 위치하게 됨
        public string Lyrics { get; set; }
    }
}
