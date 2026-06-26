# WorldBeat 미디어 플레이어 — 클라이언트 내 기여 코드

팀 프로젝트(1차) **WorldBeat** 음악 플레이어 클라이언트(C# WinForms, .NET 8)에서
**내가 직접 구현한 부분만** 모은 폴더입니다.

> 서버(ASP.NET Core) 전체는 내가 구현했고, 이 폴더는 **클라이언트 쪽 내 작업**만 정리한 것입니다.
> 곡 행 표시(`CreateSongRow`) 등 일부 공용 파일은 팀원 코드와 섞여 있어,
> 통째로 올리지 않고 **내가 작성한 메서드·블록만 발췌**했습니다.
> 따라서 발췌 파일(`*_Main.cs`, `*_DatabaseHelper.cs`)은 단독으로는 컴파일되지 않으며,
> 기여 내용을 보여주기 위한 것입니다.

## 구현한 기능

### 1. 좋아요(♥) / 플레이리스트 (우클릭 추가) — `01_좋아요_플레이리스트/`
- `UserLibrary.cs` — **전체 내 작업.** 사용자별 좋아요·플레이리스트를 서버 변경 없이
  로컬 JSON(`library_{userId}.json`)으로 영속화. 좋아요 토글, 플레이리스트 생성/삭제/
  곡 추가·제거·순서 이동.
- `Main.Library.cs` — **전체 내 작업.** ♥ 좋아요 탭 / 📁 플레이리스트 탭 UI를 코드로 생성,
  곡 우클릭 → "플레이리스트에 추가 / 좋아요 / 다음 곡으로 재생" 컨텍스트 메뉴,
  하트 표시 동기화, 전체재생·순서변경.

### 2. 음악 다운로드 — `02_음악다운로드/`
- `Download_DatabaseHelper.cs` (발췌) — 재생용 캐시 다운로드(`DownloadSongToCacheAsync`),
  사용자 지정 경로로 오프라인 저장(`DownloadSongForOfflineAsync`), 다운로드 기록(JSON)
  저장/집계, 추천 파일명 생성.
- `Download_Main.cs` (발췌) — 다운로드 버튼 클릭 처리(로그인/현재곡 확인 →
  SaveFileDialog → 다운로드 호출).

### 3. 서버 곡 불러오기 — `03_서버곡_불러오기/`
- 곡 등록(추가)은 **서버 API**에서 처리하고, 클라이언트는 그 곡들을 받아와 표시.
- `LoadSongs_DatabaseHelper.cs` (발췌) — 서버에 등록된 곡/장르 목록 HTTP 조회.
- `LoadSongs_Main.cs` (발췌) — 받아온 곡을 화면에 곡 행으로 렌더.

## 기술 스택
C# / WinForms / .NET 8 · HttpClient(REST) · System.Text.Json · 로컬 JSON 영속화
