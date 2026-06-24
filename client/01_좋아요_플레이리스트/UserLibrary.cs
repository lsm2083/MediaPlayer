#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace num1_Project
{
    // ──────────────────────────────────────────────────────────────
    // [추가] 사용자별 라이브러리(좋아요 / 내 플레이리스트) 로컬 저장소
    //
    // 서버 변경 없이 자체 완결되도록 download_history.json 과 동일하게
    // 실행 폴더에 사용자별 JSON 파일로 저장한다. (library_{userId}.json)
    // 비로그인 상태는 userId 0 으로 취급한다.
    // ──────────────────────────────────────────────────────────────
    public sealed class UserPlaylist
    {
        public string Name { get; set; } = "";
        public List<int> SongIds { get; set; } = new List<int>();
    }

    internal sealed class LibraryData
    {
        public List<int> Liked { get; set; } = new List<int>();
        public List<UserPlaylist> Playlists { get; set; } = new List<UserPlaylist>();
    }

    public static class UserLibrary
    {
        private static readonly JsonSerializerOptions _json = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static int _loadedUserId = int.MinValue;
        private static LibraryData _data = new LibraryData();

        private static int CurrentUserId =>
            DatabaseHelper.CurrentUser?.UserId ?? 0;

        private static string PathFor(int userId) =>
            System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                $"library_{userId}.json");

        // 현재 로그인 사용자 기준으로 캐시를 보장한다.
        private static void EnsureLoaded()
        {
            int uid = CurrentUserId;
            if (uid == _loadedUserId)
                return;

            _loadedUserId = uid;
            _data = new LibraryData();

            try
            {
                string path = PathFor(uid);
                if (File.Exists(path))
                {
                    string raw = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(raw))
                        _data = JsonSerializer.Deserialize<LibraryData>(raw) ?? new LibraryData();
                }
            }
            catch
            {
                _data = new LibraryData();
            }

            _data.Liked ??= new List<int>();
            _data.Playlists ??= new List<UserPlaylist>();
        }

        private static void Save()
        {
            try
            {
                File.WriteAllText(PathFor(_loadedUserId), JsonSerializer.Serialize(_data, _json));
            }
            catch
            {
            }
        }

        // ── 좋아요 ──────────────────────────────────────────────
        public static bool IsLiked(int songId)
        {
            EnsureLoaded();
            return _data.Liked.Contains(songId);
        }

        // true = 좋아요 상태가 됨, false = 해제됨
        public static bool ToggleLike(int songId)
        {
            EnsureLoaded();
            bool nowLiked;
            if (_data.Liked.Contains(songId))
            {
                _data.Liked.Remove(songId);
                nowLiked = false;
            }
            else
            {
                _data.Liked.Add(songId);
                nowLiked = true;
            }
            Save();
            return nowLiked;
        }

        public static List<int> GetLikedSongIds()
        {
            EnsureLoaded();
            return new List<int>(_data.Liked);
        }

        // ── 플레이리스트 ────────────────────────────────────────
        public static List<string> GetPlaylistNames()
        {
            EnsureLoaded();
            return _data.Playlists.Select(p => p.Name).ToList();
        }

        public static UserPlaylist GetPlaylist(string name)
        {
            EnsureLoaded();
            return _data.Playlists.FirstOrDefault(
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // 성공 시 true, 이미 같은 이름이 있거나 빈 이름이면 false
        public static bool CreatePlaylist(string name)
        {
            EnsureLoaded();
            name = name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return false;
            if (GetPlaylist(name) != null)
                return false;

            _data.Playlists.Add(new UserPlaylist { Name = name });
            Save();
            return true;
        }

        public static void DeletePlaylist(string name)
        {
            EnsureLoaded();
            _data.Playlists.RemoveAll(
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            Save();
        }

        // 곡을 플레이리스트에 추가. 이미 있으면 false
        public static bool AddToPlaylist(string name, int songId)
        {
            EnsureLoaded();
            var pl = GetPlaylist(name);
            if (pl == null)
                return false;
            if (pl.SongIds.Contains(songId))
                return false;

            pl.SongIds.Add(songId);
            Save();
            return true;
        }

        public static void RemoveFromPlaylist(string name, int songId)
        {
            EnsureLoaded();
            var pl = GetPlaylist(name);
            if (pl == null)
                return;
            pl.SongIds.Remove(songId);
            Save();
        }

        // 플레이리스트 안에서 곡 순서를 delta(-1=위, +1=아래)만큼 이동.
        // 실제로 이동했으면 true.
        public static bool MoveInPlaylist(string name, int songId, int delta)
        {
            EnsureLoaded();
            var pl = GetPlaylist(name);
            if (pl == null)
                return false;

            int idx = pl.SongIds.IndexOf(songId);
            if (idx < 0)
                return false;

            int target = idx + delta;
            if (target < 0 || target >= pl.SongIds.Count)
                return false;

            (pl.SongIds[idx], pl.SongIds[target]) = (pl.SongIds[target], pl.SongIds[idx]);
            Save();
            return true;
        }
    }
}
