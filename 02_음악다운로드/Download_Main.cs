#nullable disable
// ──────────────────────────────────────────────────────────────────────
// [발췌] 음악 다운로드 버튼 — Main.cs 에서 내가 작성한 부분만 추출
//
//  다운로드 버튼 클릭 → 로그인/현재곡 확인 → SaveFileDialog 로 저장 위치 선택
//  → DatabaseHelper.DownloadSongForOfflineAsync 호출.
//  (원본은 팀 공용 파일 Main.cs. 그대로는 컴파일되지 않음)
// ──────────────────────────────────────────────────────────────────────
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace num1_Project
{
    public partial class MainForm
    {
        private async void song_DownLoad_Click(object sender, EventArgs e)
        {
            //노래 다운로드 버튼

            if (_currentSong == null)
            {
                MessageBox.Show("현재 재생 중인 곡이 없습니다.", "다운로드");
                return;
            }

            if (DatabaseHelper.CurrentUser == null)
            {
                MessageBox.Show("로그인 후 다운로드할 수 있습니다.", "다운로드");
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "현재 재생 중인 곡 다운로드";
                sfd.FileName = DatabaseHelper.GetSuggestedSongFileName(_currentSong);
                sfd.Filter = "Audio Files|*.mp3;*.wav;*.flac;*.aac;*.m4a|All Files|*.*";

                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                Control btn = sender as Control;
                if (btn != null)
                    btn.Enabled = false;

                try
                {
                    var result = await DatabaseHelper.DownloadSongForOfflineAsync(_currentSong, sfd.FileName);
                    MessageBox.Show(
                        result.Message,
                        result.Success ? "다운로드 완료" : "다운로드 실패");
                }
                finally
                {
                    if (btn != null)
                        btn.Enabled = true;
                }
            }
        }
    }
}
