#nullable disable
// ──────────────────────────────────────────────────────────────────────
// [발췌] 서버 곡 불러와 화면에 표시 — Main.cs 에서 내가 작성한 부분만 추출
//
//  LoadPlaylistAsync : 서버에서 곡을 받아와 _playlist 에 담고 RenderPlaylist 호출
//  RenderPlaylist    : 받아온 곡 목록을 패널에 곡 행으로 렌더 (비었을 때 안내문구)
//  (곡 행 CreateSongRow 안의 ♥ 좋아요·우클릭 메뉴 연결도 내 작업 →
//   01_좋아요_플레이리스트 / Main.Library.cs 참고)
//  (원본은 팀 공용 파일 Main.cs. 그대로는 컴파일되지 않음)
// ──────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace num1_Project
{
    public partial class MainForm
    {
        // 서버에 등록된 곡 전체를 받아와 화면에 표시
        private async System.Threading.Tasks.Task LoadPlaylistAsync()
        {
            try
            {
                _playlist = await DatabaseHelper.GetSongsByGenreAsync("전체");
                RenderPlaylist();
            }
            catch (System.Exception ex)
            {
                pnlPlaylist.Controls.Clear();

                Label lbl = new Label();
                lbl.Text = "서버에서 음악 목록을 불러오지 못했습니다.\n" + ex.Message;
                lbl.ForeColor = Color.FromArgb(220, 120, 120);
                lbl.Font = new Font("맑은 고딕", 10f);
                lbl.AutoSize = false;
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = ContentAlignment.MiddleCenter;

                pnlPlaylist.Controls.Add(lbl);
            }
        }

        // 받아온 곡 목록을 패널에 곡 행으로 렌더
        private void RenderPlaylist()
        {
            pnlPlaylist.SuspendLayout();
            pnlPlaylist.Controls.Clear();

            if (_playlist.Count == 0)
            {
                Label lbl = new Label();
                lbl.Text = "서버에 등록된 음악이 없습니다.";
                lbl.ForeColor = Color.FromArgb(139, 148, 158);
                lbl.Font = new Font("맑은 고딕", 10f);
                lbl.AutoSize = false;
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                pnlPlaylist.Controls.Add(lbl);
                pnlPlaylist.ResumeLayout();
                return;
            }

            for (int i = 0; i < _playlist.Count; i++)
            {
                var song = _playlist[i];
                Panel row = CreateSongRow(song, i);
                pnlPlaylist.Controls.Add(row);
            }

            RelayoutPlaylistRows();
            HighlightCurrentRow();

            pnlPlaylist.ResumeLayout();
        }
    }
}
