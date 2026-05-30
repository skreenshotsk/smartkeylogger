using System;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private void ShowHotkeysSettings()
        {
            if (settingsContentPanel == null) 
            {
                AddLog("❌ Ошибка: settingsContentPanel не инициализирован");
                return;
            }
            
            settingsContentPanel.Controls.Clear();
            settingsContentPanel.Padding = new Padding(30, 10, 30, 30);
            
            Panel scrollablePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                AutoScroll = true
            };
            
            Label lblTitle = new Label
            {
                Text = "Настройка горячих клавиш",
                Font = new System.Drawing.Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(0, 0),
                Margin = new Padding(0, 0, 0, 0)
            };
            scrollablePanel.Controls.Add(lblTitle);
            
            int yPos = 50;
            var hotkeyActions = new[]
            {
                new { Name = "Начать запись", DefaultKey = Keys.F9, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_START_RECORD },
                new { Name = "Сохранить запись", DefaultKey = Keys.F10, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_STOP_RECORD },
                new { Name = "Воспроизвести", DefaultKey = Keys.F11, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_PLAY },
                new { Name = "Воспроизвести в цикле", DefaultKey = Keys.F12, DefaultMod = (uint)0x0002, HotkeyId = HOTKEY_PLAY_LOOP },
                new { Name = "Пауза записи", DefaultKey = Keys.Pause, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_PAUSE_RECORD },
                new { Name = "Пауза воспроизведения", DefaultKey = Keys.Home, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_PAUSE_PLAYBACK },
                new { Name = "Остановить воспроизведение", DefaultKey = (Keys)0xC0, DefaultMod = (uint)MOD_NONE, HotkeyId = HOTKEY_STOP_PLAY }
            };
            
            foreach (var action in hotkeyActions)
            {
                if (!savedHotkeys.ContainsKey(action.HotkeyId))
                {
                    savedHotkeys[action.HotkeyId] = action.DefaultKey;
                    savedHotkeyModifiers[action.HotkeyId] = action.DefaultMod;
                }
            }
            
            foreach (var action in hotkeyActions)
            {
                Keys currentKey = savedHotkeys.ContainsKey(action.HotkeyId) ? savedHotkeys[action.HotkeyId] : action.DefaultKey;
                uint currentMod = savedHotkeyModifiers.ContainsKey(action.HotkeyId) ? savedHotkeyModifiers[action.HotkeyId] : action.DefaultMod;
                string keyText = FormatKeysToString(currentKey, currentMod);
                
                Panel rowPanel = new Panel
                {
                    Height = 50,
                    Width = scrollablePanel.ClientSize.Width,
                    Location = new System.Drawing.Point(0, yPos),
                    BackColor = System.Drawing.Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                
                Label lblAction = new Label
                {
                    Text = action.Name,
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    AutoSize = true,
                    Location = new System.Drawing.Point(0, 12)
                };
                rowPanel.Controls.Add(lblAction);
                
                Button btnHotkey = new Button
                {
                    Text = keyText,
                    Size = new System.Drawing.Size(220, 40),
                    Location = new System.Drawing.Point(280, 5),
                    Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                    BackColor = System.Drawing.Color.White,
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { 
                        BorderSize = 1, 
                        BorderColor = System.Drawing.Color.FromArgb(189, 189, 189)
                    },
                    Cursor = Cursors.Hand,
                    Tag = new { HotkeyId = action.HotkeyId, Key = currentKey, Mod = currentMod }
                };
                btnHotkey.Click += (s, e) => ChangeHotkey(btnHotkey, action.HotkeyId);
                btnHotkey.MouseEnter += (s, e) => {
                    btnHotkey.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(33, 150, 243);
                };
                btnHotkey.MouseLeave += (s, e) => {
                    if (waitingForHotkeyButton != btnHotkey)
                        btnHotkey.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(189, 189, 189);
                };
                rowPanel.Controls.Add(btnHotkey);
                
                scrollablePanel.Controls.Add(rowPanel);
                yPos += 55;
            }
            
            Button btnReset = new Button
            {
                Text = "Вернуться к изначальным настройкам",
                Size = new System.Drawing.Size(320, 45),
                Location = new System.Drawing.Point(0, yPos + 30),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnReset.Click += (s, e) => ResetHotkeysToDefault();
            scrollablePanel.Controls.Add(btnReset);
            
            Label lblWarning = new Label
            {
                Text = "⚠️ Предупреждение: некоторые клавиши (F1-F12 и др.) могут использоваться системой",
                Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = System.Drawing.Color.FromArgb(244, 67, 54),
                AutoSize = true,
                Location = new System.Drawing.Point(0, yPos + 90),
                MaximumSize = new System.Drawing.Size(scrollablePanel.ClientSize.Width - 20, 0)
            };
            scrollablePanel.Controls.Add(lblWarning);
            
            Panel contentContainer = new Panel
            {
                Height = yPos + 180,
                BackColor = System.Drawing.Color.Transparent,
                AutoSize = false
            };
            
            var controlsToMove = scrollablePanel.Controls.Cast<Control>().ToList();
            scrollablePanel.Controls.Clear();
            
            foreach (var ctrl in controlsToMove)
            {
                contentContainer.Controls.Add(ctrl);
            }
            
            scrollablePanel.Controls.Add(contentContainer);
            
            void UpdateContentWidth()
            {
                if (contentContainer != null && scrollablePanel != null)
                {
                    int newWidth = scrollablePanel.ClientSize.Width;
                    if (newWidth > 0)
                    {
                        contentContainer.Width = newWidth;
                        foreach (Control ctrl in contentContainer.Controls)
                        {
                            if (ctrl is Panel p && p.Anchor.HasFlag(AnchorStyles.Right))
                            {
                                p.Width = newWidth;
                            }
                        }
                    }
                }
            }
            
            scrollablePanel.Resize += (s, e) => UpdateContentWidth();
            settingsContentPanel.Resize += (s, e) => UpdateContentWidth();
            
            settingsContentPanel.Controls.Add(scrollablePanel);
            UpdateContentWidth();
        }
        
        private void ShowMacrosSettings()
        {
            if (settingsContentPanel == null) return;
            
            settingsContentPanel.Controls.Clear();
            settingsContentPanel.Padding = new Padding(0, 10, 0, 0);
            
            Panel leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = System.Drawing.Color.FromArgb(250, 250, 250),
                Padding = new Padding(15, 30, 15, 15),
                Margin = new Padding(0, 5, 0, 0)
            };
            
            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = System.Drawing.Color.White,
                Padding = new Padding(25),
                AutoScroll = true
            };
            
            settingsContentPanel.Controls.Add(rightPanel);
            settingsContentPanel.Controls.Add(leftPanel);
            
            Label lblMacrosList = new Label
            {
                Text = "Записи:",
                Font = new System.Drawing.Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(10, -5),
                Margin = new Padding(0, 0, 0, 10)
            };
            leftPanel.Controls.Add(lblMacrosList);
            
            Panel macrosListPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Location = new System.Drawing.Point(0, 30),
                AutoScroll = true
            };
            
            foreach (string macroFile in savedMacroFiles)
            {
                Button btnMacro = new Button
                {
                    Text = Path.GetFileNameWithoutExtension(macroFile),
                    Dock = DockStyle.Top,
                    Height = 45,
                    Font = new System.Drawing.Font("Segoe UI", 11F),
                    BackColor = System.Drawing.Color.White,
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(10, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Tag = macroFile
                };
                btnMacro.Click += (s, e) => {
                    foreach (Control ctrl in macrosListPanel.Controls)
                    {
                        if (ctrl is Button btn)
                        {
                            btn.BackColor = System.Drawing.Color.White;
                            btn.ForeColor = System.Drawing.Color.FromArgb(66, 66, 66);
                        }
                    }
                    btnMacro.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
                    btnMacro.ForeColor = System.Drawing.Color.White;
                    
                    currentMacroDetailsPanel = rightPanel;
                    ShowMacroDetails(macroFile, rightPanel);
                };
                btnMacro.MouseEnter += (s, e) => {
                    if (btnMacro.BackColor != System.Drawing.Color.FromArgb(33, 150, 243))
                        btnMacro.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
                };
                btnMacro.MouseLeave += (s, e) => {
                    if (btnMacro.BackColor != System.Drawing.Color.FromArgb(33, 150, 243))
                        btnMacro.BackColor = System.Drawing.Color.White;
                };
                macrosListPanel.Controls.Add(btnMacro);
                macrosListPanel.Controls.SetChildIndex(btnMacro, 0);
            }
            
            leftPanel.Controls.Add(macrosListPanel);
            
            if (savedMacroFiles.Count > 0)
            {
                var firstBtn = macrosListPanel.Controls[macrosListPanel.Controls.Count - 1] as Button;
                if (firstBtn != null)
                {
                    firstBtn.PerformClick();
                }
            }
            else
            {
                Label lblNoMacros = new Label
                {
                    Text = "Нет сохраненных макросов",
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    ForeColor = System.Drawing.Color.FromArgb(158, 158, 158),
                    AutoSize = true,
                    Location = new System.Drawing.Point(25, 100)
                };
                rightPanel.Controls.Add(lblNoMacros);
            }
        }
    }
}


