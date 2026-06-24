namespace WorldBeat.Api.Models
{
    public sealed class YearNewsEntity
    {
        public int      NewsId      { get; set; }
        public int      Year        { get; set; }
        public int      Month       { get; set; } // [추가] 월 (0 = 미지정)
        public string   Headline    { get; set; }
        public string   Description { get; set; }
        public string   Category    { get; set; }
        public string   ImagePath   { get; set; }
        public DateTime CreatedAt   { get; set; }
    }
}
