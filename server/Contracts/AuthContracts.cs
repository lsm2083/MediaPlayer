namespace WorldBeat.Api.Contracts
{
    public sealed class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string DetailAddr { get; set; }
    }

    public sealed class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public sealed class UserResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }

        // [추가] 관리자 여부를 클라이언트(윈폼)에 전달하는 필드
        // 이 값이 JSON 응답에 포함돼야 윈폼의 DatabaseHelper.UserDto.AdminRole 로 전달돼
        // null = 일반 고객, 1 = 관리자
        public int? AdminRole { get; set; }

        // [추가] 현재 사용자 요금제 전달
        // null 또는 "" = 미가입
        // "일반" / "VIP" = 가입 상태
        public string PlanType { get; set; }
    }

    public sealed class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserResponse User { get; set; }

        public static AuthResponse Fail(string message)
        {
            return new AuthResponse
            {
                Success = false,
                Message = message,
                User = null
            };
        }

        public static AuthResponse Ok(UserResponse user, string message = "OK")
        {
            return new AuthResponse
            {
                Success = true,
                Message = message,
                User = user
            };
        }
    }

    // ── 결제 완료 시 IsPaid, PlanType 업데이트 요청 모델
    public sealed class UpdatePaymentRequest
    {
        public int UserId { get; set; }
        public string PlanType { get; set; }
    }
}