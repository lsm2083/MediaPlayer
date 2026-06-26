# MediaPlayer (WorldBeat)

C# WinForms 음악 플레이어 + ASP.NET Core 서버로 구성된 팀 프로젝트(1차).
이 저장소는 **내가 작업한 부분**을 모은 것입니다.

```
.
├─ client/   # 클라이언트(WinForms)에서 내가 구현한 기능만 발췌
└─ server/   # 서버(ASP.NET Core) — 전체 내가 구현
```

## server/ — 서버 (전체 본인 구현)
ASP.NET Core (.NET 8) Web API. SQLite + Repository/Service 계층 구조.
Minimal Hosting 모델 · Swagger(Swashbuckle) 문서화.
- `Controllers/` — 곡/뉴스/타임캡슐 API, 관리자 API(곡 업로드·등록 등)
- `Services/`, `Repositories/` — 비즈니스 로직 / 데이터 접근
- `Models/`, `Contracts/` — 엔티티 / 요청·응답 DTO
- `Infrastructure/` — DB 초기화, SQLite 연결 팩토리
- 곡 추가(등록)·파일 저장(`LocalFileStorageService`)·인증(`AuthService`, 비밀번호 해시) 등

> 빌드 산출물(bin/obj/.vs)과 런타임 DB·음원 파일은 제외했습니다.
> `appsettings.json`의 `AdminKey`는 개발용 placeholder("1")이며 실제 비밀이 아닙니다.

## client/ — 클라이언트 내 기여분
팀 공용 파일과 섞인 부분은 통째로 올리지 않고 **내가 작성한 코드만 발췌**했습니다.
(발췌 파일은 단독으로는 컴파일되지 않으며, 기여 내용을 보여주기 위한 것입니다.)

- **`client/01_좋아요_플레이리스트/`** — 좋아요(♥)·플레이리스트(우클릭 추가).
  `UserLibrary.cs`, `Main.Library.cs` 는 전체 본인 작업.
- **`client/02_음악다운로드/`** — 곡 캐시/오프라인 다운로드, 다운로드 기록.
- **`client/03_서버곡_불러오기/`** — 서버에 등록된 곡을 받아와 화면에 표시.

자세한 내용은 [`client/README.md`](client/README.md) 참고.

## 기술 스택
- **클라이언트**: C# / WinForms / .NET 8 · HttpClient(REST) · System.Text.Json
- **서버**: ASP.NET Core (.NET 8) Web API · SQLite · Repository/Service 패턴 · Swagger(Swashbuckle)
