namespace WorldBeat.Api.Contracts
{
    public sealed class YearNewsResponse
    {
        public int    NewsId      { get; set; }
        public int    Year        { get; set; }
        public int    Month       { get; set; } // [추가] 월 (0 = 미지정)
        public string Headline    { get; set; }
        public string Description { get; set; }
        public string Category    { get; set; }
        public string ImageUrl    { get; set; }
    }

    public sealed class YearNewsCreateRequest
    {
        public int    Year        { get; set; }
        public int    Month       { get; set; } // [추가] 월 (0 = 미지정)
        public string Headline    { get; set; }
        public string Description { get; set; }
        public string Category    { get; set; }
    }

    public sealed class YearNewsUpdateRequest
    {
        public int    Month       { get; set; } // [추가] 월 (0 = 미지정)
        public string Headline    { get; set; }
        public string Description { get; set; }
        public string Category    { get; set; }
    }
}
