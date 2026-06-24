namespace WorldBeat.Api.Models
{
    public sealed class UserEntity
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public int PasswordVersion { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string DetailAddr { get; set; }

        // [추가] 관리자 여부를 나타내는 컬럼
        // null = 일반 고객
        // 1    = 관리자
        // int? 로 선언해서 DB에서 NULL 값도 받을 수 있도록 했어
        public int? AdminRole { get; set; }

        // [추가] 요금제
        // null 또는 "" = 미가입
        // "일반", "VIP" = 가입
        public string PlanType { get; set; }
    }
}