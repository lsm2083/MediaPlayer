#nullable disable
// ──────────────────────────────────────────────────────────────────────
// [발췌] 서버 곡 불러오기 — DatabaseHelper.cs 에서 내가 작성한 부분만 추출
//
//  곡 등록(추가)은 내가 만든 서버(ASP.NET) 쪽 API 에서 처리하고,
//  클라이언트는 이 메서드들로 서버에 등록된 곡/장르를 HTTP 로 받아온다.
//  (원본은 팀 공용 파일 DatabaseHelper.cs. 그대로는 컴파일되지 않음)
// ──────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace num1_Project
{
    public static partial class DatabaseHelper_LoadSongsExtract
    {
        // 서버에 등록된 장르 목록 조회
        public static async Task<List<string>> GetAllGenresAsync()
        {
            EnsureInitialized();

            try
            {
                var list = await _http.GetFromJsonAsync<List<string>>(
                    "/api/songs/genres", _jsonOptions);

                return list ?? new List<string>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("장르 목록을 불러오지 못했습니다. " + ex.Message, ex);
            }
        }

        // 서버에 등록된 곡 목록 조회 (장르 필터 가능, "전체"면 전체)
        public static async Task<List<SongInfo>> GetSongsByGenreAsync(string genreName)
        {
            EnsureInitialized();

            try
            {
                string url = "/api/songs";
                if (!string.IsNullOrWhiteSpace(genreName) && genreName != "전체")
                    url += "?genre=" + Uri.EscapeDataString(genreName);

                var list = await _http.GetFromJsonAsync<List<SongInfo>>(url, _jsonOptions);
                return list ?? new List<SongInfo>();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("음악 목록을 불러오지 못했습니다. " + ex.Message, ex);
            }
        }
    }
}
