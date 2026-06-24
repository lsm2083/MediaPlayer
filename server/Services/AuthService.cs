using Microsoft.AspNetCore.Identity;
using WorldBeat.Api.Contracts;
using WorldBeat.Api.Models;
using WorldBeat.Api.Repositories;

namespace WorldBeat.Api.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher<UserEntity> _hasher = new PasswordHasher<UserEntity>();

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return AuthResponse.Fail("잘못된 요청입니다.");

            if (string.IsNullOrWhiteSpace(request.Username))
                return AuthResponse.Fail("아이디를 입력하세요.");

            if (string.IsNullOrWhiteSpace(request.Password))
                return AuthResponse.Fail("비밀번호를 입력하세요.");

            bool exists = await _userRepository.ExistsByUsernameAsync(request.Username.Trim(), cancellationToken);
            if (exists)
                return AuthResponse.Fail("이미 사용 중인 아이디입니다.");

            var user = new UserEntity
            {
                Username = request.Username.Trim(),
                Name = request.Name?.Trim() ?? "",
                Phone = request.Phone?.Trim() ?? "",
                Region = request.Region?.Trim() ?? "",
                City = request.City?.Trim() ?? "",
                DetailAddr = request.DetailAddr?.Trim() ?? "",
                PasswordVersion = 1
                // AdminRole 은 따로 지정하지 않으면 null (일반 고객) 로 저장돼
                // PlanType 도 따로 지정하지 않으면 null 또는 "" (미가입) 상태
            };

            user.PasswordHash = _hasher.HashPassword(user, request.Password);

            int userId = await _userRepository.CreateAsync(user, cancellationToken);

            // 회원가입 응답에는 AdminRole 불필요 (신규 가입자는 항상 일반 고객)
            return AuthResponse.Ok(new UserResponse
            {
                UserId = userId,
                Username = user.Username,
                DisplayName = string.IsNullOrWhiteSpace(user.Name) ? user.Username : user.Name,
                PlanType = user.PlanType
            }, "회원가입 완료");
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                return AuthResponse.Fail("잘못된 요청입니다.");

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return AuthResponse.Fail("아이디와 비밀번호를 모두 입력하세요.");

            var user = await _userRepository.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
            if (user == null)
                return AuthResponse.Fail("아이디 또는 비밀번호가 올바르지 않습니다.");

            var verifyResult = _hasher.VerifyHashedPassword(user, user.PasswordHash ?? "", request.Password);
            if (verifyResult == PasswordVerificationResult.Failed)
                return AuthResponse.Fail("아이디 또는 비밀번호가 올바르지 않습니다.");

            if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                string rehashed = _hasher.HashPassword(user, request.Password);
                await _userRepository.UpdatePasswordHashAsync(user.UserId, rehashed, cancellationToken);
            }

            // ──────────────────────────────────────────────────────
            // [수정] 로그인 성공 응답에 AdminRole 추가
            //
            // 기존에는 UserId, Username, DisplayName 만 응답했는데
            // AdminRole 도 같이 넘겨줘야 윈폼에서 관리자 여부를 판단할 수 있어.
            //
            // user.AdminRole 은 DB Users 테이블의 AdminRole 컬럼값이야:
            //   null → 일반 고객  → 윈폼에서 관리자 버튼 숨김
            //   1    → 관리자     → 윈폼에서 관리자 버튼 표시
            // ──────────────────────────────────────────────────────
            return AuthResponse.Ok(new UserResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                DisplayName = string.IsNullOrWhiteSpace(user.Name) ? user.Username : user.Name,
                AdminRole = user.AdminRole,  // [추가] DB에서 읽어온 AdminRole 값을 그대로 전달
                PlanType = user.PlanType     // [추가] DB에서 읽어온 요금제 값을 그대로 전달
            }, "로그인 성공");
        }
    }
}