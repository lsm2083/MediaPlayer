namespace WorldBeat.Api.Configuration
{
    public sealed class ApiOptions
    {
        public string DatabasePath { get; set; }
        public string MusicStorageRoot { get; set; }
        public string AdminKey { get; set; }
    }
}