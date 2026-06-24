#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace num1_Project
{
    // ──────────────────────────────────────────────────────────────
    // [추가] 좋아요(♥) / 내 플레이리스트(📁) 기능
    //  - tabControl2 에 탭 두 개를 코드로 추가한다(디자이너 미수정).
    //  - 영속화는 UserLibrary(사용자별 로컬 JSON)를 사용한다.
    //  - 곡 행/카드 스타일은 메인 다크 테마와 동일하게 맞춘다.
    // ──────────────────────────────────────────────────────────────
    public partial class MainForm
    {
        private TabPage _tpLiked;
        private TabPage _tpMyPl;
        private Panel _pnlLiked;
        private Panel _pnlMyPl;
        private ComboBox _cboPlaylists;

        private bool _likedLoaded = false;
        private bool _myPlLoaded = false;

        // 곡 전체 목록 캐시 (SongId → SongInfo 해석용)
        private List<SongInfo> _allSongsCache;

        // 테마 색
        private static readonly Color CBg = Color.FromArgb(22, 27, 34);
        private static readonly Color CBgAlt = Color.FromArgb(17, 22, 30);
        private static readonly Color CText = Color.FromArgb(230, 237, 243);
        private static readonly Color CMuted = Color.FromArgb(139, 148, 158);
        private static readonly Color CAccent = Color.FromArgb(88, 130, 220);
        private static readonly Color CHeart = Color.FromArgb(240, 90, 120);

        // ── 탭 초기화 (생성자에서 호출) ───────────────────────
        private void InitLibraryTabs()
        {
            if (tabControl2 == null) return;

            // ♥ 좋아요 탭
            _tpLiked = new TabPage("♥ 좋아요");
            _tpLiked.BackColor = CBg;
            _pnlLiked = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CBg,
                AutoScroll = true,
                Padding = new Padding(2)
            };
            _pnlLiked.Resize += (s, e) => { if (_likedLoaded) RelayoutLibraryPanel(_pnlLiked); };

            var likedToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = Sc(40),
                BackColor = CBg,
                Padding = new Padding(Sc(6), Sc(6), Sc(6), Sc(4))
            };
            var btnPlayAllLiked = MakeToolButton("▶ 전체 재생", Sc(8));
            btnPlayAllLiked.Click += async (s, e) =>
            {
                var all = await GetAllSongsAsync();
                PlayAll(ResolveSongs(UserLibrary.GetLikedSongIds(), all));
            };
            likedToolbar.Controls.Add(btnPlayAllLiked);

            // Fill 을 먼저, Top 을 나중에 추가
            _tpLiked.Controls.Add(_pnlLiked);
            _tpLiked.Controls.Add(likedToolbar);

            // 📁 내 플레이리스트 탭
            _tpMyPl = new TabPage("📁 플레이리스트");
            _tpMyPl.BackColor = CBg;

            var toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = Sc(40),
                BackColor = CBg,
                Padding = new Padding(Sc(6), Sc(6), Sc(6), Sc(4))
            };

            _cboPlaylists = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(Sc(8), Sc(8)),
                Size = new Size(Sc(220), Sc(26)),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 36, 46),
                ForeColor = CText,
                Font = new Font("맑은 고딕", 9.5f)
            };
            _cboPlaylists.SelectedIndexChanged += (s, e) => RenderSelectedPlaylist();

            var btnNew = MakeToolButton("＋ 새로 만들기", Sc(236));
            btnNew.Click += (s, e) =>
            {
                string name = PromptText("새 플레이리스트", "플레이리스트 이름:");
                if (string.IsNullOrWhiteSpace(name)) return;
                if (!UserLibrary.CreatePlaylist(name))
                {
                    MessageBox.Show("같은 이름의 플레이리스트가 이미 있습니다.", "알림");
                    return;
                }
                RefreshPlaylistCombo(name);
            };

            var btnDel = MakeToolButton("🗑 삭제", Sc(236 + 116));
            btnDel.Click += (s, e) =>
            {
                string name = _cboPlaylists.SelectedItem?.ToString();
                if (string.IsNullOrWhiteSpace(name)) return;
                if (MessageBox.Show($"'{name}' 플레이리스트를 삭제할까요?", "삭제 확인",
                        MessageBoxButtons.YesNo) != DialogResult.Yes) return;
                UserLibrary.DeletePlaylist(name);
                RefreshPlaylistCombo();
            };

            var btnPlayAllPl = MakeToolButton("▶ 전체 재생", Sc(236 + 116 + 116));
            btnPlayAllPl.Click += async (s, e) =>
            {
                string name = _cboPlaylists.SelectedItem?.ToString();
                var pl = UserLibrary.GetPlaylist(name);
                var all = await GetAllSongsAsync();
                PlayAll(pl != null ? ResolveSongs(pl.SongIds, all) : new List<SongInfo>());
            };

            toolbar.Controls.Add(_cboPlaylists);
            toolbar.Controls.Add(btnNew);
            toolbar.Controls.Add(btnDel);
            toolbar.Controls.Add(btnPlayAllPl);

            _pnlMyPl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CBg,
                AutoScroll = true,
                Padding = new Padding(2)
            };
            _pnlMyPl.Resize += (s, e) => { if (_myPlLoaded) RelayoutLibraryPanel(_pnlMyPl); };

            // Dock 순서 주의: Fill 을 먼저 추가하고 Top 을 나중에 추가해야 한다.
            _tpMyPl.Controls.Add(_pnlMyPl);
            _tpMyPl.Controls.Add(toolbar);

            tabControl2.TabPages.Add(_tpLiked);
            tabControl2.TabPages.Add(_tpMyPl);

            tabControl2.SelectedIndexChanged += LibraryTabs_SelectedIndexChanged;
        }

        private Button MakeToolButton(string text, int x)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, Sc(7)),
                Size = new Size(Sc(110), Sc(27)),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 54, 62),
                ForeColor = CText,
                Font = new Font("맑은 고딕", 9f),
                Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private async void LibraryTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl2.SelectedTab == _tpLiked)
                await LoadLikedTabAsync();
            else if (tabControl2.SelectedTab == _tpMyPl)
                await LoadMyPlaylistTabAsync();
        }

        // ── 곡 전체 캐시 ──────────────────────────────────────
        private async Task<List<SongInfo>> GetAllSongsAsync(bool force = false)
        {
            if (_allSongsCache != null && !force)
                return _allSongsCache;
            try
            {
                _allSongsCache = await DatabaseHelper.GetSongsByGenreAsync("전체");
            }
            catch
            {
                _allSongsCache = new List<SongInfo>();
            }
            return _allSongsCache;
        }

        // [추가] 사용자가 "다음 곡으로 재생"으로 끼워넣은 우선 대기열.
        //  공유 _playlist 의 인덱스를 흐트러뜨리지 않기 위해 별도로 관리하고,
        //  다음 곡으로 넘어갈 때(자동/수동) 이 큐를 먼저 소비한다.
        private readonly List<SongInfo> _upNext = new List<SongInfo>();

        // 대기열에 곡이 있으면 한 곡을 꺼내 재생하고 true 반환.
        // (재생은 _playlist 끝에 덧붙여 PlaySong 인덱스 모델을 유지 → 기존 행 인덱스 안전)
        private bool TryPlayUpNext()
        {
            if (_upNext.Count == 0)
                return false;

            var next = _upNext[0];
            _upNext.RemoveAt(0);
            _playlist.Add(next);
            PlaySong(_playlist.Count - 1);
            return true;
        }

        // 주어진 목록을 큐로 삼아 처음부터 재생
        private void PlayAll(List<SongInfo> songs)
        {
            if (songs == null || songs.Count == 0)
            {
                MessageBox.Show("재생할 곡이 없습니다.", "전체 재생");
                return;
            }
            _playlist = new List<SongInfo>(songs);
            _currentIndex = -1;
            PlaySong(0);
        }

        private List<SongInfo> ResolveSongs(IEnumerable<int> ids, List<SongInfo> all)
        {
            var map = all.GroupBy(s => s.SongId).ToDictionary(g => g.Key, g => g.First());
            var result = new List<SongInfo>();
            foreach (int id in ids)
                if (map.TryGetValue(id, out var s))
                    result.Add(s);
            return result;
        }

        // ── ♥ 좋아요 탭 ───────────────────────────────────────
        private async Task LoadLikedTabAsync()
        {
            _likedLoaded = true;
            var all = await GetAllSongsAsync();
            var liked = ResolveSongs(UserLibrary.GetLikedSongIds(), all);
            RenderSongRows(_pnlLiked, liked,
                "아직 좋아요한 곡이 없습니다.\n곡 목록에서 ♡ 를 눌러 담아보세요.", null);
        }

        // ── 📁 플레이리스트 탭 ────────────────────────────────
        private async Task LoadMyPlaylistTabAsync()
        {
            _myPlLoaded = true;
            await GetAllSongsAsync();
            RefreshPlaylistCombo();
        }

        // 콤보박스 목록을 다시 채우고 선택 항목을 렌더한다.
        private void RefreshPlaylistCombo(string selectName = null)
        {
            if (_cboPlaylists == null) return;

            string keep = selectName ?? _cboPlaylists.SelectedItem?.ToString();
            _cboPlaylists.Items.Clear();
            var names = UserLibrary.GetPlaylistNames();
            foreach (var n in names)
                _cboPlaylists.Items.Add(n);

            if (names.Count == 0)
            {
                RenderSongRows(_pnlMyPl, new List<SongInfo>(),
                    "플레이리스트가 없습니다.\n위 '＋ 새로 만들기' 로 추가하세요.", null);
                return;
            }

            int idx = keep != null ? _cboPlaylists.Items.IndexOf(keep) : -1;
            _cboPlaylists.SelectedIndex = idx >= 0 ? idx : 0; // → RenderSelectedPlaylist 트리거
        }

        private void RenderSelectedPlaylist()
        {
            if (_cboPlaylists == null) return;
            string name = _cboPlaylists.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(name))
            {
                RenderSongRows(_pnlMyPl, new List<SongInfo>(), "플레이리스트를 선택하세요.", null);
                return;
            }

            var pl = UserLibrary.GetPlaylist(name);
            var all = _allSongsCache ?? new List<SongInfo>();
            var songs = pl != null ? ResolveSongs(pl.SongIds, all) : new List<SongInfo>();
            RenderSongRows(_pnlMyPl, songs,
                "이 플레이리스트가 비어 있습니다.\n곡을 우클릭해 추가하세요.", name);
        }

        // ── 공용 곡 목록 렌더 ─────────────────────────────────
        private void RenderSongRows(Panel panel, List<SongInfo> songs, string emptyMsg, string playlistName)
        {
            if (panel == null) return;
            panel.SuspendLayout();
            panel.Controls.Clear();

            if (songs == null || songs.Count == 0)
            {
                var lbl = new Label
                {
                    Text = emptyMsg,
                    ForeColor = CMuted,
                    Font = new Font("맑은 고딕", 10f),
                    AutoSize = false,
                    Dock = DockStyle.Top,
                    Height = 80,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                panel.Controls.Add(lbl);
                panel.AutoScrollMinSize = new Size(0, 0);
                panel.ResumeLayout();
                return;
            }

            int w = GetLibraryRowWidth(panel);
            for (int i = 0; i < songs.Count; i++)
            {
                Panel row = CreateLibraryRow(songs[i], i, songs, playlistName, w);
                row.Location = new Point(0, i * Sc(62));
                panel.Controls.Add(row);
            }
            panel.AutoScrollMinSize = new Size(0, songs.Count * Sc(62) + Sc(4));
            panel.ResumeLayout();
        }

        private int GetLibraryRowWidth(Panel panel)
        {
            int width = panel.ClientSize.Width
                - (panel.VerticalScroll.Visible ? SystemInformation.VerticalScrollBarWidth : 0) - 4;
            return Math.Max(260, width);
        }

        private void RelayoutLibraryPanel(Panel panel)
        {
            int w = GetLibraryRowWidth(panel);
            foreach (Control c in panel.Controls)
            {
                if (c is Panel row)
                    row.Width = w;
            }
        }

        // 좋아요 탭/플레이리스트 탭에서 쓰는 곡 행
        private Panel CreateLibraryRow(SongInfo song, int idx, List<SongInfo> sourceList, string playlistName, int rowWidth)
        {
            Panel row = new Panel
            {
                Size = new Size(rowWidth, Sc(60)),
                BackColor = idx % 2 == 0 ? CBg : CBgAlt,
                Cursor = Cursors.Hand,
                Tag = idx,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            PictureBox pic = new PictureBox
            {
                Size = new Size(Sc(46), Sc(46)),
                Location = new Point(Sc(8), Sc(7)),
                SizeMode = PictureBoxSizeMode.StretchImage
            };
            SetAlbumArtImage(pic, song.AlbumArtUrl);

            // 오른쪽 버튼 영역: 좋아요 탭 → [♥]
            //                   플레이리스트 탭 → [♥][▲][▼][✕]
            int rightX = rowWidth - Sc(36);
            var extraButtons = new List<Label>();
            if (playlistName != null)
            {
                string plName = playlistName;
                int songId = song.SongId;

                Label btnRemove = MakeIconLabel("✕", rightX, () =>
                {
                    UserLibrary.RemoveFromPlaylist(plName, songId);
                    RenderSelectedPlaylist();
                });
                extraButtons.Add(btnRemove);
                rightX -= Sc(30);

                Label btnDown = MakeIconLabel("▼", rightX, () =>
                {
                    if (UserLibrary.MoveInPlaylist(plName, songId, +1))
                        RenderSelectedPlaylist();
                });
                extraButtons.Add(btnDown);
                rightX -= Sc(30);

                Label btnUp = MakeIconLabel("▲", rightX, () =>
                {
                    if (UserLibrary.MoveInPlaylist(plName, songId, -1))
                        RenderSelectedPlaylist();
                });
                extraButtons.Add(btnUp);
                rightX -= Sc(30);
            }

            Label heart = MakeHeartLabel(song, rightX, Sc(17), () =>
            {
                // 좋아요 탭이면 해제 시 목록에서 즉시 제거
                if (playlistName == null && _likedLoaded)
                    _ = LoadLikedTabAsync();
            });

            int textRight = rightX - Sc(8);
            Label lblTitle = new Label
            {
                Text = song.Title,
                Font = new Font("맑은 고딕", 10f, FontStyle.Bold),
                ForeColor = CText,
                Location = new Point(Sc(64), Sc(8)),
                Size = new Size(textRight - Sc(64), Sc(22)),
                AutoEllipsis = true
            };
            Label lblArtist = new Label
            {
                Text = song.Artist,
                Font = new Font("맑은 고딕", 8.5f),
                ForeColor = CMuted,
                Location = new Point(Sc(64), Sc(32)),
                Size = new Size(textRight - Sc(64), Sc(18)),
                AutoEllipsis = true
            };

            row.Controls.Add(pic);
            row.Controls.Add(lblTitle);
            row.Controls.Add(lblArtist);
            row.Controls.Add(heart);
            foreach (var b in extraButtons) row.Controls.Add(b);

            // 더블클릭 → 이 목록을 큐로 삼아 재생
            EventHandler dbl = (s, e) =>
            {
                _playlist = new List<SongInfo>(sourceList);
                _currentIndex = -1;
                PlaySong(idx);
            };
            row.DoubleClick += dbl;
            pic.DoubleClick += dbl;
            lblTitle.DoubleClick += dbl;
            lblArtist.DoubleClick += dbl;

            // 우클릭 → 플레이리스트에 추가 메뉴
            AttachAddToPlaylistMenu(row, song);
            AttachAddToPlaylistMenu(lblTitle, song);
            AttachAddToPlaylistMenu(lblArtist, song);
            AttachAddToPlaylistMenu(pic, song);

            // hover
            Color baseColor = row.BackColor;
            EventHandler enter = (s, e) => row.BackColor = Color.FromArgb(40, 50, 85);
            EventHandler leave = (s, e) => row.BackColor = baseColor;
            foreach (Control c in new Control[] { row, pic, lblTitle, lblArtist })
            {
                c.MouseEnter += enter;
                c.MouseLeave += leave;
            }

            row.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (Pen p = new Pen(Color.FromArgb(40, 60, 110), 1f))
                    e.Graphics.DrawLine(p, 15, row.Height - 1, row.Width - 15, row.Height - 1);
            };

            return row;
        }

        // 작은 아이콘 버튼(라벨) 생성 — 제거/순서이동 등에 사용
        private Label MakeIconLabel(string text, int x, Action onClick)
        {
            var lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(Sc(26), Sc(26)),
                Location = new Point(x, Sc(17)),
                Font = new Font("맑은 고딕", 11f),
                ForeColor = CMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            lbl.MouseEnter += (s, e) => lbl.ForeColor = CText;
            lbl.MouseLeave += (s, e) => lbl.ForeColor = CMuted;
            lbl.Click += (s, e) => onClick();
            return lbl;
        }

        // ── ♥ 하트 라벨 ───────────────────────────────────────
        // 메인 플레이리스트(CreateSongRow)와 라이브러리 탭에서 공용으로 사용
        private Label MakeHeartLabel(SongInfo song, int x, int y, Action onAfterToggle = null)
        {
            var heart = new Label
            {
                Name = "heart",
                Tag = song.SongId,
                AutoSize = false,
                Size = new Size(Sc(26), Sc(26)),
                Location = new Point(x, y),
                Font = new Font("Segoe UI Emoji", 12f),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            ApplyHeartVisual(heart, UserLibrary.IsLiked(song.SongId));
            heart.Click += (s, e) =>
            {
                bool liked = UserLibrary.ToggleLike(song.SongId);
                ApplyHeartVisual(heart, liked);
                SyncHeartsForSong(song.SongId, liked);
                onAfterToggle?.Invoke();
            };
            return heart;
        }

        private void ApplyHeartVisual(Label heart, bool liked)
        {
            heart.Text = liked ? "♥" : "♡";
            heart.ForeColor = liked ? CHeart : CMuted;
        }

        // 같은 곡의 하트가 여러 패널에 떠 있을 수 있으니 모두 동기화
        private void SyncHeartsForSong(int songId, bool liked)
        {
            foreach (var panel in new Panel[] { pnlPlaylist, _pnlLiked, _pnlMyPl })
            {
                if (panel == null) continue;
                foreach (Control row in panel.Controls)
                {
                    foreach (Control child in row.Controls)
                    {
                        if (child is Label lbl && lbl.Name == "heart"
                            && lbl.Tag is int id && id == songId)
                        {
                            ApplyHeartVisual(lbl, liked);
                        }
                    }
                }
            }
        }

        // ── 우클릭: 플레이리스트에 추가 ────────────────────────
        private void AttachAddToPlaylistMenu(Control ctrl, SongInfo song)
        {
            ctrl.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                    ShowAddToPlaylistMenu(song);
            };
        }

        private void ShowAddToPlaylistMenu(SongInfo song)
        {
            var menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(30, 36, 46),
                ForeColor = CText
            };

            var header = new ToolStripMenuItem($"\"{song.Title}\"") { Enabled = false };
            menu.Items.Add(header);
            menu.Items.Add(new ToolStripSeparator());

            // 좋아요 토글
            bool liked = UserLibrary.IsLiked(song.SongId);
            var likeItem = new ToolStripMenuItem(liked ? "♥ 좋아요 해제" : "♡ 좋아요");
            likeItem.ForeColor = liked ? CHeart : CText;
            likeItem.Click += (s, e) =>
            {
                bool now = UserLibrary.ToggleLike(song.SongId);
                SyncHeartsForSong(song.SongId, now);
                if (_likedLoaded) _ = LoadLikedTabAsync();
            };
            menu.Items.Add(likeItem);
            menu.Items.Add(new ToolStripSeparator());

            // 대기열: 다음 곡으로 재생
            var playNext = new ToolStripMenuItem("▶ 다음 곡으로 재생");
            playNext.Click += (s, e) =>
            {
                _upNext.Add(song);
            };
            menu.Items.Add(playNext);
            menu.Items.Add(new ToolStripSeparator());

            var names = UserLibrary.GetPlaylistNames();
            if (names.Count == 0)
            {
                menu.Items.Add(new ToolStripMenuItem("(플레이리스트 없음)") { Enabled = false });
            }
            else
            {
                foreach (var n in names)
                {
                    string nn = n;
                    var it = new ToolStripMenuItem("📁 " + n);
                    it.Click += (s, e) =>
                    {
                        bool added = UserLibrary.AddToPlaylist(nn, song.SongId);
                        if (!added)
                            MessageBox.Show("이미 담겨 있는 곡입니다.", nn);
                        if (_myPlLoaded && _cboPlaylists?.SelectedItem?.ToString() == nn)
                            RenderSelectedPlaylist();
                    };
                    menu.Items.Add(it);
                }
            }

            menu.Items.Add(new ToolStripSeparator());
            var create = new ToolStripMenuItem("＋ 새 플레이리스트에 추가…");
            create.Click += (s, e) =>
            {
                string name = PromptText("새 플레이리스트", "플레이리스트 이름:");
                if (string.IsNullOrWhiteSpace(name)) return;
                UserLibrary.CreatePlaylist(name); // 이미 있으면 무시됨
                UserLibrary.AddToPlaylist(name, song.SongId);
                if (_myPlLoaded) RefreshPlaylistCombo(name);
            };
            menu.Items.Add(create);

            menu.Show(Cursor.Position);
        }

        // ── [추가] 재생 키보드 단축키 ─────────────────────────
        // 스페이스: 재생/일시정지, Ctrl+→/←: 다음/이전 곡, Ctrl+↑/↓: 볼륨
        // (검색창 등 텍스트 입력 중에는 동작하지 않음)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ActiveControl is TextBox)
                return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.Space:
                    btnPlay.PerformClick();
                    return true;
                case Keys.Control | Keys.Right:
                    btnNext.PerformClick();
                    return true;
                case Keys.Control | Keys.Left:
                    btnPrev.PerformClick();
                    return true;
                case Keys.Control | Keys.Up:
                    if (volBar != null)
                        volBar.Value = Math.Min(volBar.Maximum, volBar.Value + 5);
                    return true;
                case Keys.Control | Keys.Down:
                    if (volBar != null)
                        volBar.Value = Math.Max(volBar.Minimum, volBar.Value - 5);
                    return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── 다크 테마 입력 대화상자 (WinForms 기본 InputBox 부재 대체) ──
        private string PromptText(string title, string label, string defaultValue = "")
        {
            using var f = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ClientSize = new Size(Sc(330), Sc(140)),
                BackColor = CBg,
                ForeColor = CText,
                Font = new Font("맑은 고딕", 9.5f)
            };
            var lbl = new Label { Text = label, Location = new Point(Sc(14), Sc(14)), AutoSize = true, ForeColor = Color.FromArgb(200, 210, 220) };
            var txt = new TextBox
            {
                Location = new Point(Sc(14), Sc(40)),
                Size = new Size(Sc(302), Sc(24)),
                Text = defaultValue,
                BackColor = Color.FromArgb(30, 36, 46),
                ForeColor = CText,
                BorderStyle = BorderStyle.FixedSingle
            };
            var ok = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Location = new Point(Sc(150), Sc(88)),
                Size = new Size(Sc(80), Sc(34)),
                FlatStyle = FlatStyle.Flat,
                BackColor = CAccent,
                ForeColor = Color.White
            };
            ok.FlatAppearance.BorderSize = 0;
            var cancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Location = new Point(Sc(238), Sc(88)),
                Size = new Size(Sc(80), Sc(34)),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 54, 62),
                ForeColor = CText
            };
            cancel.FlatAppearance.BorderSize = 0;
            f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            return f.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : null;
        }
    }
}
