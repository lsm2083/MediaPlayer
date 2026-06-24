using Microsoft.AspNetCore.Mvc;
using WorldBeat.Api.Configuration;
using WorldBeat.Api.Contracts;
using WorldBeat.Api.Filters;
using WorldBeat.Api.Infrastructure;
using WorldBeat.Api.Repositories;
using WorldBeat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("App"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<AdminKeyHeaderOperationFilter>();

    // [추가] Swagger 문서 제목/설명 한글화
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "WorldBeat 음악 서버 API",
        Version = "v1",
        Description = "데스크톱 음악 플레이어용 서버 API입니다.\n" +
                      "회원/음악/관리자(곡·회원·뉴스)/타임캡슐 기능을 제공합니다."
    });

    // [추가] 엔드포인트를 경로에 따라 한글 섹션으로 묶어서 보기 쉽게 정리
    options.TagActionsBy(api =>
    {
        string path = "/" + (api.RelativePath ?? "");
        if (path.StartsWith("/api/admin/songs")) return new[] { "관리자 · 곡 관리/업로드" };
        if (path.StartsWith("/api/admin/users")) return new[] { "관리자 · 회원 관리" };
        if (path.StartsWith("/api/admin/news")) return new[] { "관리자 · 뉴스 관리" };
        if (path.StartsWith("/api/auth")) return new[] { "인증 · 회원가입/로그인" };
        if (path.StartsWith("/api/songs")) return new[] { "음악 · 목록/재생/스트리밍" };
        if (path.StartsWith("/api/news")) return new[] { "연도별 뉴스" };
        if (path.StartsWith("/api/capsules") || path.StartsWith("/api/Capsules")) return new[] { "타임캡슐" };
        return new[] { "기타" };
    });
    options.DocInclusionPredicate((name, api) => true);

    // [추가] 컨트롤러 XML 주석(/// <summary>)을 Swagger 설명으로 포함
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});
builder.Services.AddControllers();

builder.Services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<DbInitializer>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISongRepository, SongRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IYearNewsRepository, YearNewsRepository>();

builder.Services.AddScoped<AdminKeyFilter>();

builder.Services.AddScoped<ICapsuleRepository, CapsuleRepository>();
builder.Services.AddScoped<ICapsuleService, CapsuleService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await dbInitializer.InitializeAsync();
}

app.UseStaticFiles(); // [추가] wwwroot 정적 파일 (Swagger 자동완성 스크립트)

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // [추가] 곡 업로드 시 mp3 파일을 고르면 제목·가수·앨범·연도·재생시간 칸을
    // 브라우저에서 ID3 태그로 자동 채워주는 스크립트 주입
    c.InjectJavascript("/swagger-autofill.js");
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

var auth = app.MapGroup("/api/auth");

auth.MapPost("/register", async (
    RegisterRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var result = await authService.RegisterAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithSummary("회원가입").WithDescription("아이디·비밀번호·이름·연락처·주소로 신규 회원을 등록합니다.");

auth.MapPost("/login", async (
    LoginRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var result = await authService.LoginAsync(request, cancellationToken);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
})
.WithSummary("로그인").WithDescription("아이디·비밀번호로 로그인하고 사용자 정보를 반환합니다.");

auth.MapPost("/payment", async (
    UpdatePaymentRequest request,
    IUserRepository userRepo,
    CancellationToken cancellationToken) =>
{
    if (request == null || request.UserId <= 0)
        return Results.BadRequest(new AuthResponse { Success = false, Message = "잘못된 요청입니다." });

    if (string.IsNullOrWhiteSpace(request.PlanType))
        return Results.BadRequest(new AuthResponse { Success = false, Message = "요금제 정보가 없습니다." });

    await userRepo.UpdatePaymentAsync(request.UserId, request.PlanType, cancellationToken);

    return Results.Ok(new AuthResponse { Success = true, Message = "결제가 완료되었습니다." });
})
.WithSummary("[미사용] 결제/요금제 갱신").WithDescription("과거 요금제 기능용 엔드포인트입니다. 현재 클라이언트에서는 호출하지 않습니다.");

var songs = app.MapGroup("/api/songs");

songs.MapGet("/genres", async (
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    var genres = await songService.GetGenresAsync(cancellationToken);
    return Results.Ok(genres);
})
.WithSummary("장르 목록").WithDescription("등록된 음악 장르 목록을 반환합니다.");

songs.MapGet("/recent", async (
    HttpContext httpContext,
    ISongService songService,
    CancellationToken cancellationToken,
    [FromQuery] int limit = 50) =>
{
    if (limit <= 0) limit = 50;
    var items = await songService.GetRecentAsync(limit, httpContext, cancellationToken);
    return Results.Ok(items);
})
.WithSummary("최근 재생 곡").WithDescription("최근 재생된 곡을 limit 개수만큼 반환합니다. (기본 50)");

songs.MapGet("/top", async (
    HttpContext httpContext,
    ISongService songService,
    CancellationToken cancellationToken,
    [FromQuery] int limit = 50) =>
{
    if (limit <= 0) limit = 50;
    var items = await songService.GetTopAsync(limit, httpContext, cancellationToken);
    return Results.Ok(items);
})
.WithSummary("인기 곡 (많이 들은 순)").WithDescription("재생 횟수가 많은 순서로 곡을 limit 개수만큼 반환합니다. (기본 50)");

songs.MapGet("/", async (
    [FromQuery] string genre,
    HttpContext httpContext,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    var items = await songService.GetSongsAsync(genre, httpContext, cancellationToken);
    return Results.Ok(items);
})
.WithSummary("곡 목록 (장르 필터)").WithDescription("genre 쿼리로 장르를 지정하면 해당 장르, 비우면 전체 곡 목록을 반환합니다.");

songs.MapGet("/{songId:int}", async (
    int songId,
    HttpContext httpContext,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    var song = await songService.GetSongAsync(songId, httpContext, cancellationToken);
    return song == null ? Results.NotFound() : Results.Ok(song);
})
.WithSummary("곡 상세").WithDescription("songId로 곡 한 곡의 상세 정보를 반환합니다.");

songs.MapGet("/{songId:int}/stream", async (
    int songId,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    var streamInfo = await songService.OpenSongStreamAsync(songId, cancellationToken);
    if (streamInfo == null)
        return Results.NotFound();

    return Results.File(
        streamInfo.Stream,
        streamInfo.ContentType,
        fileDownloadName: streamInfo.DownloadName,
        enableRangeProcessing: true);
})
.WithSummary("곡 스트리밍/다운로드").WithDescription("songId의 음원 파일을 스트리밍합니다. 구간 재생(Range)을 지원합니다.");

songs.MapGet("/{songId:int}/art", async (
    int songId,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    var streamInfo = await songService.OpenAlbumArtStreamAsync(songId, cancellationToken);
    if (streamInfo == null)
        return Results.NotFound();

    return Results.File(
        streamInfo.Stream,
        streamInfo.ContentType,
        fileDownloadName: streamInfo.DownloadName,
        enableRangeProcessing: true);
})
.WithSummary("앨범 아트 이미지").WithDescription("songId의 앨범 커버 이미지를 반환합니다.");

songs.MapPost("/{songId:int}/play", async (
    int songId,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    bool ok = await songService.IncrementPlayCountAsync(songId, cancellationToken);
    return ok ? Results.Ok() : Results.NotFound();
})
.WithSummary("재생 횟수 +1").WithDescription("곡을 재생할 때 호출되어 재생 횟수를 1 증가시킵니다. (인기 곡 집계)");

var adminSongs = app.MapGroup("/api/admin/songs")
    .AddEndpointFilter<AdminKeyFilter>();

adminSongs.MapGet("/", async (
    [FromQuery] string genre,
    HttpContext httpContext,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    var items = await songService.GetSongsAsync(genre, httpContext, cancellationToken);
    return Results.Ok(items);
})
.WithSummary("[관리자] 곡 목록").WithDescription("관리자용 곡 목록 조회. X-Admin-Key 헤더가 필요합니다.");

adminSongs.MapGet("/{songId:int}", async (
    int songId,
    HttpContext httpContext,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    var song = await songService.GetSongAsync(songId, httpContext, cancellationToken);
    return song == null
        ? Results.NotFound(new { success = false, message = "곡을 찾을 수 없습니다." })
        : Results.Ok(song);
})
.WithSummary("[관리자] 곡 상세").WithDescription("관리자용 곡 상세 조회. X-Admin-Key 헤더가 필요합니다.");

adminSongs.MapPut("/{songId:int}", async (
    int songId,
    SongUpdateRequest request,
    HttpContext httpContext,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    bool updated = await songService.UpdateSongAsync(songId, request, httpContext, cancellationToken);
    if (!updated)
        return Results.NotFound(new { success = false, message = "곡을 찾을 수 없습니다." });

    var song = await songService.GetSongAsync(songId, httpContext, cancellationToken);
    return Results.Ok(song);
})
.WithSummary("[관리자] 곡 정보 수정").WithDescription("제목·가수·앨범·장르·가사 등 곡 정보를 수정합니다. X-Admin-Key 헤더 필요.");

adminSongs.MapDelete("/{songId:int}", async (
    int songId,
    ISongService songService,
    CancellationToken cancellationToken) =>
{
    bool deleted = await songService.DeleteSongAsync(songId, cancellationToken);
    return deleted
        ? Results.Ok(new { success = true, message = "삭제 완료" })
        : Results.NotFound(new { success = false, message = "곡을 찾을 수 없습니다." });
})
.WithSummary("[관리자] 곡 삭제").WithDescription("songId의 곡을 삭제합니다. X-Admin-Key 헤더가 필요합니다.");

// ──────────────────────────────────────────────────────────────
// [추가] AdminManager: 유저 관리자 권한 부여/해제 API
//
// AdminKeyFilter 로 보호되어 있어서 X-Admin-Key 헤더가 있어야 호출 가능해.
// Swagger 에서 AdminManager 섹션으로 표시돼.
//
// POST /api/admin/users/{userId}/grant  → 관리자로 지정 (AdminRole = 1)
// POST /api/admin/users/{userId}/revoke → 일반 고객으로 해제 (AdminRole = null)
// GET  /api/admin/users                 → 전체 유저 목록 조회 (관리자 여부 포함)
// ──────────────────────────────────────────────────────────────
var adminUsers = app.MapGroup("/api/admin/users")
    .AddEndpointFilter<AdminKeyFilter>()
    .WithTags("AdminManager"); // [추가] Swagger 에서 AdminManager 섹션으로 표시

// 전체 유저 목록 조회 (AdminRole 포함)
adminUsers.MapGet("/", async (
    IUserRepository userRepo,
    CancellationToken cancellationToken) =>
{
    var users = await userRepo.GetAllAsync(cancellationToken);
    var response = users.Select(u => new
    {
        u.UserId,
        u.Username,
        u.Name,
        u.Phone,
        AdminRole = u.AdminRole.HasValue ? u.AdminRole.Value.ToString() : "일반 고객"
    });
    return Results.Ok(response);
})
.WithSummary("[관리자] 전체 회원 목록").WithDescription("모든 회원과 관리자 여부를 조회합니다. X-Admin-Key 헤더 필요.");

// 특정 유저를 관리자로 지정 (AdminRole = 1)
adminUsers.MapPost("/{userId:int}/grant", async (
    int userId,
    IUserRepository userRepo,
    CancellationToken cancellationToken) =>
{
    // 유저 존재 여부 확인
    var user = await userRepo.GetByIdAsync(userId, cancellationToken);
    if (user == null)
        return Results.NotFound(new { success = false, message = $"UserId {userId} 를 찾을 수 없습니다." });

    // 이미 관리자인 경우
    if (user.AdminRole == 1)
        return Results.Ok(new { success = true, message = $"{user.Username} 은(는) 이미 관리자입니다." });

    await userRepo.UpdateAdminRoleAsync(userId, 1, cancellationToken);
    return Results.Ok(new { success = true, message = $"{user.Username} 을(를) 관리자로 지정했습니다." });
})
.WithSummary("[관리자] 관리자 권한 부여").WithDescription("해당 userId 회원을 관리자(AdminRole=1)로 지정합니다. X-Admin-Key 헤더 필요.");

// 특정 유저의 관리자 권한 해제 (AdminRole = null)
adminUsers.MapPost("/{userId:int}/revoke", async (
    int userId,
    IUserRepository userRepo,
    CancellationToken cancellationToken) =>
{
    // 유저 존재 여부 확인
    var user = await userRepo.GetByIdAsync(userId, cancellationToken);
    if (user == null)
        return Results.NotFound(new { success = false, message = $"UserId {userId} 를 찾을 수 없습니다." });

    // 이미 일반 고객인 경우
    if (user.AdminRole == null)
        return Results.Ok(new { success = true, message = $"{user.Username} 은(는) 이미 일반 고객입니다." });

    await userRepo.UpdateAdminRoleAsync(userId, null, cancellationToken);
    return Results.Ok(new { success = true, message = $"{user.Username} 의 관리자 권한을 해제했습니다." });
})
.WithSummary("[관리자] 관리자 권한 해제").WithDescription("해당 userId 회원을 일반 고객으로 되돌립니다. X-Admin-Key 헤더 필요.");

var news = app.MapGroup("/api/news");

news.MapGet("/{year:int}", async (
    int year,
    HttpContext httpContext,
    IYearNewsRepository newsRepo,
    CancellationToken cancellationToken) =>
{
    var items = await newsRepo.GetByYearAsync(year, cancellationToken);
    var response = items.Select(n => new YearNewsResponse
    {
        NewsId = n.NewsId,
        Year = n.Year,
        Month = n.Month,
        Headline = n.Headline,
        Description = n.Description,
        Category = n.Category,
        ImageUrl = string.IsNullOrWhiteSpace(n.ImagePath)
            ? ""
            : $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/news/{n.NewsId}/image"
    });
    return Results.Ok(response);
})
.WithSummary("연도별 뉴스 목록").WithDescription("지정한 연도의 가요·연예 뉴스 목록을 반환합니다.");

news.MapGet("/{newsId:int}/image", async (
    int newsId,
    IYearNewsRepository newsRepo,
    IFileStorageService storage,
    CancellationToken cancellationToken) =>
{
    var item = await newsRepo.GetByIdAsync(newsId, cancellationToken);
    if (item == null || string.IsNullOrWhiteSpace(item.ImagePath))
        return Results.NotFound();

    var stream = await storage.OpenNewsImageReadAsync(item.ImagePath, cancellationToken);
    if (stream == null)
        return Results.NotFound();

    return Results.File(stream, storage.GetNewsImageContentType(item.ImagePath));
})
.WithSummary("뉴스 이미지").WithDescription("newsId 뉴스의 이미지 파일을 반환합니다.");

app.Run();
