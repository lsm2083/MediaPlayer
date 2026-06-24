#nullable disable
// ──────────────────────────────────────────────────────────────────────
// [발췌] 음악 다운로드 기능 — DatabaseHelper.cs 에서 내가 작성한 부분만 추출
//
//  원본은 팀 공용 파일(DatabaseHelper.cs)이라 통째로 올리지 않고,
//  내가 구현한 다운로드 관련 메서드들만 모아 둠. (그대로는 컴파일되지 않음)
//
//  - DownloadSongToCacheAsync   : 재생용 캐시 다운로드
//  - DownloadSongForOfflineAsync: 사용자가 지정한 경로로 오프라인 저장
//  - 다운로드 기록(JSON) 저장/로드/집계
//  - GetSuggestedSongFileName   : 저장 대화상자 추천 파일명
// ──────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace num1_Project
{
    public static partial class DatabaseHelper_DownloadExtract
    {
        // 재생 시 서버 음원을 로컬 캐시로 받아온다.
        public static async Task<string> DownloadSongToCacheAsync(SongInfo song)
        {
            EnsureInitialized();

            if (song == null || string.IsNullOrWhiteSpace(song.FileUrl))
                return "";

            string ext = GetSongExtension(song);
            string localPath = Path.Combine(CacheFolder, $"{song.SongId}{ext}");
            string tempPath = localPath + ".tmp";

            try
            {
                foreach (var oldFile in Directory.GetFiles(CacheFolder, $"{song.SongId}.*"))
                {
                    try
                    {
                        File.Delete(oldFile);
                    }
                    catch
                    {
                    }
                }

                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                    }
                }

                using var response = await _http.GetAsync(
                    song.FileUrl,
                    HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                await using (var src = await response.Content.ReadAsStreamAsync())
                await using (var dst = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None))
                {
                    await src.CopyToAsync(dst);
                }

                if (File.Exists(localPath))
                {
                    try
                    {
                        File.Delete(localPath);
                    }
                    catch
                    {
                    }
                }

                File.Move(tempPath, localPath);
                return localPath;
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch
                {
                }

                throw new InvalidOperationException("음원 다운로드 실패: " + ex.Message, ex);
            }
        }

        // [추가] 다운로드 기록 모델
        private sealed class DownloadHistoryItem
        {
            public int UserId { get; set; }
            public int SongId { get; set; }
            public DateTime DownloadedAt { get; set; }
        }

        // [추가] 다운로드 기록 파일 로드
        private static List<DownloadHistoryItem> LoadDownloadHistory()
        {
            try
            {
                if (!File.Exists(DownloadHistoryPath))
                    return new List<DownloadHistoryItem>();

                string json = File.ReadAllText(DownloadHistoryPath);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<DownloadHistoryItem>();

                var list = JsonSerializer.Deserialize<List<DownloadHistoryItem>>(json, _jsonOptions);
                return list ?? new List<DownloadHistoryItem>();
            }
            catch
            {
                return new List<DownloadHistoryItem>();
            }
        }

        // [추가] 다운로드 기록 저장
        private static void SaveDownloadHistory(List<DownloadHistoryItem> list)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(list ?? new List<DownloadHistoryItem>(), options);
                File.WriteAllText(DownloadHistoryPath, json);
            }
            catch
            {
            }
        }

        // [추가] 최근 7일 다운로드 횟수
        public static int GetWeeklyDownloadUsedCount()
        {
            if (CurrentUser == null)
                return 0;

            DateTime from = DateTime.Now.AddDays(-7);
            var list = LoadDownloadHistory();

            return list.Count(x =>
                x.UserId == CurrentUser.UserId &&
                x.DownloadedAt >= from);
        }

        // [추가] 다운로드 기록 1건 추가
        private static void AddDownloadHistory(int songId)
        {
            if (CurrentUser == null)
                return;

            var list = LoadDownloadHistory();

            list = list
                .Where(x => x.DownloadedAt >= DateTime.Now.AddDays(-30))
                .ToList();

            list.Add(new DownloadHistoryItem
            {
                UserId = CurrentUser.UserId,
                SongId = songId,
                DownloadedAt = DateTime.Now
            });

            SaveDownloadHistory(list);
        }

        // [추가] 저장 대화상자에 보여줄 추천 파일명
        public static string GetSuggestedSongFileName(SongInfo song)
        {
            if (song == null)
                return "music.mp3";

            string ext = GetSongExtension(song);
            string baseName;

            if (!string.IsNullOrWhiteSpace(song.Artist))
                baseName = $"{song.Artist} - {song.Title}";
            else
                baseName = song.Title;

            if (string.IsNullOrWhiteSpace(baseName))
                baseName = "music";

            foreach (char ch in Path.GetInvalidFileNameChars())
                baseName = baseName.Replace(ch, '_');

            return baseName + ext;
        }

        // [추가] 현재 곡을 사용자가 지정한 경로로 다운로드
        public static async Task<ApiResult<string>> DownloadSongForOfflineAsync(SongInfo song, string savePath)
        {
            EnsureInitialized();

            if (song == null)
                return ApiResult<string>.Fail("다운로드할 곡 정보가 없습니다.");

            if (string.IsNullOrWhiteSpace(song.FileUrl))
                return ApiResult<string>.Fail("다운로드할 파일 주소가 없습니다.");

            if (string.IsNullOrWhiteSpace(savePath))
                return ApiResult<string>.Fail("저장 경로가 올바르지 않습니다.");

            if (CurrentUser == null)
                return ApiResult<string>.Fail("로그인 후 다운로드할 수 있습니다.");

            try
            {
                string dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using var response = await _http.GetAsync(song.FileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using (var src = await response.Content.ReadAsStreamAsync())
                await using (var dst = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await src.CopyToAsync(dst);
                }

                AddDownloadHistory(song.SongId);

                return ApiResult<string>.Ok(savePath, "다운로드 완료");
            }
            catch (Exception ex)
            {
                return ApiResult<string>.Fail("다운로드 실패: " + ex.Message);
            }
        }
    }
}
