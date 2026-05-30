using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private void ShowMacroDetails(string? macroFileName, Panel panel)
        {
            if (string.IsNullOrEmpty(macroFileName) || panel == null) return;
            
            panel.Controls.Clear();
            
            string filePath = Path.Combine(macrosDirectory, macroFileName);
            if (!File.Exists(filePath))
            {
                Label lblError = new Label
                {
                    Text = "Файл не найден",
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    AutoSize = true
                };
                panel.Controls.Add(lblError);
                return;
            }
            
            var events = recorder?.LoadFromFile(filePath);
            if (events == null)
            {
                Label lblError = new Label
                {
                    Text = "Ошибка загрузки файла",
                    Font = new System.Drawing.Font("Segoe UI", 12F),
                    AutoSize = true
                };
                panel.Controls.Add(lblError);
                return;
            }
            
            currentEditingEvents = new List<InputEvent>(events);
            currentEditingMacroFile = macroFileName;
            currentMacroDetailsPanel = panel;
            
            LoadMacroMetadata(filePath);
            
            if (currentEditingEvents.Count == 0)
            {
                currentEditingEvents = new List<InputEvent>();
            }
            
            Label lblMacroNameLabel = new Label
            {
                Text = "Название макроса:",
                Font = new System.Drawing.Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(0, 0),
            };
            panel.Controls.Add(lblMacroNameLabel);
            
            TextBox txtMacroName = new TextBox
            {
                Text = Path.GetFileNameWithoutExtension(macroFileName),
                Font = new System.Drawing.Font("Segoe UI", 12F),
                Location = new System.Drawing.Point(0, 25),
                Size = new System.Drawing.Size(450, 35),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = System.Drawing.Color.White,
            };
            panel.Controls.Add(txtMacroName);
            
            Button btnDeleteMacro = new Button
            {
                Text = "Удалить запись",
                Size = new System.Drawing.Size(180, 29),
                Location = new System.Drawing.Point(460, 25),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnDeleteMacro.Click += (s, e) => {
                DeleteMacro(macroFileName);
            };
            btnDeleteMacro.MouseEnter += (s, e) => {
                btnDeleteMacro.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
            };
            btnDeleteMacro.MouseLeave += (s, e) => {
                btnDeleteMacro.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
            };
            panel.Controls.Add(btnDeleteMacro);
            
            int yPos = 80;
            Label lblActions = new Label
            {
                Text = "Действия:",
                Font = new System.Drawing.Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                AutoSize = true,
                Location = new System.Drawing.Point(0, yPos)
            };
            panel.Controls.Add(lblActions);
            
            Button btnAddAction = new Button
            {
                Text = "Добавить действие",
                Size = new System.Drawing.Size(180, 35),
                Location = new System.Drawing.Point(150, yPos - 2),
                Font = new System.Drawing.Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };
            btnAddAction.Click += (s, e) => {
                StartAppendingToMacro(panel);
            };
            panel.Controls.Add(btnAddAction);
            
            yPos += 45;
            
            var groupedEvents = new List<object>();
            List<InputEvent> mouseMoveGroup = new List<InputEvent>();
            
            Dictionary<string, int> actionCounters = new Dictionary<string, int>();
            int mouseMoveGroupCounter = 0;
            
            for (int i = 0; i < currentEditingEvents.Count; i++)
            {
                if (currentEditingEvents[i].EventType == "MouseMove")
                {
                    mouseMoveGroup.Add(currentEditingEvents[i]);
                }
                else
                {
                    if (mouseMoveGroup.Count > 0)
                    {
                        mouseMoveGroupCounter++;
                        groupedEvents.Add(new { 
                            Event = mouseMoveGroup.ToList(), 
                            DisplayText = $"🖱️ Движения курсора №{mouseMoveGroupCounter}", 
                            Index = i - mouseMoveGroup.Count,
                            IsGroup = true
                        });
                        mouseMoveGroup.Clear();
                    }
                    
                    string displayText = FormatEventForDisplayWithNumber(currentEditingEvents[i], actionCounters);
                    groupedEvents.Add(new { 
                        Event = currentEditingEvents[i], 
                        DisplayText = displayText, 
                        Index = i,
                        IsGroup = false
                    });
                }
            }
            
            if (mouseMoveGroup.Count > 0)
            {
                mouseMoveGroupCounter++;
                groupedEvents.Add(new { 
                    Event = mouseMoveGroup.ToList(), 
                    DisplayText = $"🖱️ Движения курсора №{mouseMoveGroupCounter}", 
                    Index = currentEditingEvents.Count - mouseMoveGroup.Count,
                    IsGroup = true
                });
            }
            
            Panel actionsPanel = new Panel
            {
                Location = new System.Drawing.Point(0, yPos),
                Size = new System.Drawing.Size(750, 450),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            panel.Controls.Add(actionsPanel);
            
            int actionYPos = 0;
            
            foreach (var item in groupedEvents)
            {
                dynamic eventItem = item;
                
                Panel actionContainer = new Panel
                {
                    Location = new System.Drawing.Point(0, actionYPos),
                    Size = new System.Drawing.Size(730, 45),
                    BackColor = System.Drawing.Color.White
                };
                
                Button btnAction = new Button
                {
                    Text = eventItem.DisplayText,
                    Font = new System.Drawing.Font("Segoe UI", 11F),
                    Location = new System.Drawing.Point(0, 0),
                    Size = new System.Drawing.Size(600, 45),
                    TextAlign = ContentAlignment.MiddleLeft,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = System.Drawing.Color.FromArgb(250, 250, 250),
                    ForeColor = System.Drawing.Color.FromArgb(66, 66, 66),
                    Padding = new Padding(15, 0, 0, 0),
                    Cursor = Cursors.Hand,
                    Tag = eventItem,
                    Dock = DockStyle.Left
                };
                btnAction.Click += (s, e) => {
                    if (eventItem.IsGroup)
                    {
                        EditMacroMouseMoves(eventItem.Event, btnAction);
                    }
                    else
                    {
                        EditMacroAction(eventItem.Event, btnAction);
                    }
                };
                btnAction.MouseEnter += (s, e) => {
                    btnAction.BackColor = System.Drawing.Color.FromArgb(240, 240, 240);
                };
                btnAction.MouseLeave += (s, e) => {
                    btnAction.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
                };
                
                int actionIndexInGroup = groupedEvents.IndexOf(item);
                Button btnDelete = new Button
                {
                    Text = "Удалить действие",
                    Font = new System.Drawing.Font("Segoe UI", 9F),
                    Location = new System.Drawing.Point(605, 5),
                    Size = new System.Drawing.Size(120, 35),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = System.Drawing.Color.FromArgb(244, 67, 54),
                    ForeColor = System.Drawing.Color.White,
                    FlatAppearance = { BorderSize = 0 },
                    Cursor = Cursors.Hand,
                    Tag = new { EventItem = eventItem, Index = actionIndexInGroup }
                };
                btnDelete.Click += (s, e) => {
                    var tagData = (dynamic)btnDelete.Tag;
                    DeleteMacroActionWithConfirmation(tagData.EventItem, panel);
                };
                btnDelete.MouseEnter += (s, e) => {
                    btnDelete.BackColor = System.Drawing.Color.FromArgb(229, 57, 53);
                };
                btnDelete.MouseLeave += (s, e) => {
                    btnDelete.BackColor = System.Drawing.Color.FromArgb(244, 67, 54);
                };
                
                actionContainer.Controls.Add(btnDelete);
                actionContainer.Controls.Add(btnAction);
                actionsPanel.Controls.Add(actionContainer);
                
                actionYPos += 50;
            }
            
            yPos += 460;
            
            Button btnSaveMacro = new Button
            {
                Text = "💾 Сохранить изменения",
                Size = new System.Drawing.Size(250, 40),
                Location = new System.Drawing.Point(0, yPos + 20),
                Font = new System.Drawing.Font("Segoe UI", 11F, FontStyle.Bold),
                BackColor = System.Drawing.Color.FromArgb(76, 175, 80),
                ForeColor = System.Drawing.Color.White,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };
            btnSaveMacro.Click += (s, e) => {
                SaveMacroChanges(macroFileName, txtMacroName.Text, panel);
            };
            panel.Controls.Add(btnSaveMacro);
        }
        
        private void SaveMacroChanges(string oldFileName, string newName, Panel panel)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Имя макроса не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                newName = newName.Replace(c.ToString(), "");
            }
            newName = newName.Trim();
            
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Имя макроса содержит только недопустимые символы", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string oldFilePath = Path.Combine(macrosDirectory, oldFileName);
            string newFileName = $"{newName}.json";
            string newFilePath = Path.Combine(macrosDirectory, newFileName);
            
            try
            {
                if (oldFileName == newFileName)
                {
                    AddLog("ℹ️ Имя макроса не изменилось");
                    return;
                }
                
                if (File.Exists(newFilePath) && oldFileName != newFileName)
                {
                    var result = MessageBox.Show(
                        $"Файл с именем '{newFileName}' уже существует. Перезаписать?",
                        "Файл уже существует",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }
                
                if (oldFileName != newFileName && File.Exists(oldFilePath))
                {
                    SaveMacroEvents(newFilePath);
                    File.Delete(oldFilePath);
                    
                    if (savedMacroFiles.Contains(oldFileName))
                    {
                        savedMacroFiles.Remove(oldFileName);
                    }
                    
                    if (!savedMacroFiles.Contains(newFileName))
                    {
                        savedMacroFiles.Add(newFileName);
                        savedMacroFiles.Sort();
                    }
                    
                    if (currentMacroFile == oldFileName)
                    {
                        currentMacroFile = newFileName;
                    }
                    
                    UpdateMacroFilesComboBox();
                    
                    AddLog($"✏️ Макрос переименован: {oldFileName} → {newFileName}");
                }
                else
                {
                    SaveMacroEvents(oldFilePath);
                    AddLog($"💾 Изменения макроса сохранены");
                }
                
                ShowMacrosSettings();
                
                MessageBox.Show(oldFileName == newFileName ? "Изменения макроса сохранены" : "Макрос успешно переименован и сохранен", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при переименовании макроса: {ex.Message}");
                MessageBox.Show($"Ошибка при переименовании: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private string FormatEventForDisplay(InputEvent evt)
        {
            switch (evt.EventType)
            {
                case "KeyPress":
                    string keyName = GetKeyDisplayName(evt.KeyCode);
                    return evt.IsPressed ? $"⌨️ Нажатие клавиши: {keyName}" : $"⌨️ Отпускание клавиши: {keyName}";
                case "MouseClick":
                    string button = evt.MouseButton == 1 ? "ЛКМ" : evt.MouseButton == 2 ? "ПКМ" : "СКМ";
                    return evt.IsPressed ? $"🖱️ Нажатие {button} ({evt.X}, {evt.Y})" : $"🖱️ Отпускание {button} ({evt.X}, {evt.Y})";
                case "MouseMove":
                    return $"🖱️ Движения курсора";
                default:
                    return evt.EventType;
            }
        }
        
        private string FormatEventForDisplayWithNumber(InputEvent evt, Dictionary<string, int> counters)
        {
            switch (evt.EventType)
            {
                case "KeyPress":
                    string keyName = GetKeyDisplayName(evt.KeyCode);
                    string keyCounterKey = $"KeyPress_{keyName}";
                    if (!counters.ContainsKey(keyCounterKey))
                        counters[keyCounterKey] = 0;
                    counters[keyCounterKey]++;
                    return $"⌨️ Нажатие клавиши {keyName} №{counters[keyCounterKey]}";
                case "MouseClick":
                    string button = evt.MouseButton == 1 ? "ЛКМ" : evt.MouseButton == 2 ? "ПКМ" : "СКМ";
                    string clickCounterKey = $"MouseClick_{button}";
                    if (!counters.ContainsKey(clickCounterKey))
                        counters[clickCounterKey] = 0;
                    counters[clickCounterKey]++;
                    return $"🖱️ Нажатие {button} №{counters[clickCounterKey]}";
                case "MouseMove":
                    return $"🖱️ Движения курсора";
                default:
                    return evt.EventType;
            }
        }
        
        private string GetKeyDisplayName(int keyCode)
        {
            if (keyCode >= 0x30 && keyCode <= 0x39) return ((char)keyCode).ToString();
            if (keyCode >= 0x41 && keyCode <= 0x5A) return ((char)keyCode).ToString();
            
            return keyCode switch
            {
                0x08 => "Backspace",
                0x09 => "Tab",
                0x0D => "Enter",
                0x20 => "Space",
                0x1B => "Escape",
                0x2E => "Delete",
                0x25 => "←",
                0x26 => "↑",
                0x27 => "→",
                0x28 => "↓",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x23 => "End",
                0x24 => "Home",
                0x2C => "PrintScreen",
                0x2D => "Insert",
                0x14 => "CapsLock",
                0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
                0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
                0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
                0xA0 => "Shift", 0xA1 => "Shift",
                0xA2 => "Ctrl", 0xA3 => "Ctrl",
                0xA4 => "Alt", 0xA5 => "Alt",
                0xBC => ",", 0xBD => "-", 0xBE => ".", 0xBF => "/",
                0xBA => ";", 0xBB => "=", 0xDB => "[", 0xDC => "\\",
                0xDD => "]", 0xDE => "'", 0xC0 => "`",
                0x13 => "Pause",
                _ => $"Key(0x{keyCode:X2})"
            };
        }
        
        private void EditMacroAction(InputEvent evt, Button btn)
        {
            if (currentEditingEvents == null || currentEditingMacroFile == null) return;
            
            int actionIndex = -1;
            for (int i = 0; i < currentEditingEvents.Count; i++)
            {
                var currentEvt = currentEditingEvents[i];
                if (currentEvt.EventType == evt.EventType &&
                    currentEvt.Timestamp == evt.Timestamp &&
                    (evt.EventType == "KeyPress" ? currentEvt.KeyCode == evt.KeyCode : true) &&
                    (evt.EventType == "MouseClick" || evt.EventType == "MouseMove" 
                        ? currentEvt.X == evt.X && currentEvt.Y == evt.Y && currentEvt.MouseButton == evt.MouseButton 
                        : true))
                {
                    actionIndex = i;
                    break;
                }
            }
            
            if (actionIndex < 0)
            {
                MessageBox.Show("Не удалось найти действие для редактирования", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            Form infoDialog = new Form
            {
                Text = "Замена действия",
                Size = new System.Drawing.Size(550, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            string stopRecordKey = GetHotkeyName(HOTKEY_STOP_RECORD, Keys.F10);
            string stopPlayKey = GetHotkeyName(HOTKEY_STOP_PLAY, (Keys)0xC0);
            
            Label lblInfo = new Label
            {
                Text = $"Когда это окно закроется, запишите новое действие для замены текущего.\n\n" +
                       $"Для отмены замены нажмите горячую клавишу остановки воспроизведения ({stopPlayKey}).\n" +
                       $"Для сохранения нового действия нажмите горячую клавишу сохранения записи ({stopRecordKey}).\n\n" +
                       $"Окно закроется автоматически через 3 секунды...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(510, 130),
                AutoSize = false
            };
            infoDialog.Controls.Add(lblInfo);
            
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000,
                Enabled = true
            };
            
            closeTimer.Tick += (s, e) => {
                closeTimer.Stop();
                closeTimer.Dispose();
                infoDialog.DialogResult = DialogResult.OK;
                infoDialog.Close();
            };
            
            infoDialog.ShowDialog();
            
            isReplacingAction = true;
            actionToReplaceIndex = actionIndex;
            
            AddLog($"🔄 Начата замена действия. Нажмите горячую клавишу для остановки.");
            
            StartRecording();
            HideSettingsPanel();
        }
        
        private void DeleteMacro(string macroFileName)
        {
            if (string.IsNullOrEmpty(macroFileName)) return;
            
            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить запись \"{Path.GetFileNameWithoutExtension(macroFileName)}\"?\n\nЗапись будет удалена безвозвратно.",
                "Подтверждение удаления",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            
            if (result != DialogResult.Yes) return;
            
            try
            {
                string filePath = Path.Combine(macrosDirectory, macroFileName);
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    AddLog($"🗑️ Запись удалена: {macroFileName}");
                }
                
                if (savedMacroFiles.Contains(macroFileName))
                {
                    savedMacroFiles.Remove(macroFileName);
                }
                
                if (currentMacroFile == macroFileName)
                {
                    currentMacroFile = "";
                }
                
                if (currentEditingMacroFile == macroFileName)
                {
                    currentEditingMacroFile = null;
                    currentEditingEvents = null;
                    currentMacroDetailsPanel = null;
                }
                
                UpdateMacroFilesComboBox();
                ShowMacrosSettings();
                
                MessageBox.Show("Запись успешно удалена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при удалении записи: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении записи: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void StartAppendingToMacro(Panel panel)
        {
            if (currentEditingMacroFile == null || currentEditingEvents == null)
            {
                MessageBox.Show("Не выбран макрос для редактирования", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            string stopPlayKey = GetHotkeyName(HOTKEY_STOP_PLAY, (Keys)0xC0);
            string stopRecordKey = GetHotkeyName(HOTKEY_STOP_RECORD, Keys.F10);
            
            Form infoDialog = new Form
            {
                Text = "Добавление действий",
                Size = new System.Drawing.Size(600, 220),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblInfo = new Label
            {
                Text = $"Когда это окно закроется, запишите нужные вам действия.\n\n" +
                       $"Для отмены записи нажмите горячую клавишу остановки воспроизведения ({stopPlayKey}).\n" +
                       $"Для сохранения записи нажмите горячую клавишу сохранения записи ({stopRecordKey}).\n\n" +
                       $"Окно закроется автоматически через 3 секунды...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(550, 140),
                AutoSize = false
            };
            infoDialog.Controls.Add(lblInfo);
            
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000,
                Enabled = true
            };
            
            closeTimer.Tick += (s, e) => {
                closeTimer.Stop();
                closeTimer.Dispose();
                infoDialog.DialogResult = DialogResult.OK;
                infoDialog.Close();
            };
            
            infoDialog.ShowDialog();
            
            isAppendingToMacro = true;
            AddLog("➕ Начата дозапись в макрос. Нажмите горячую клавишу для остановки.");
            
            StartRecording();
            HideSettingsPanel();
        }
        
        private string GetHotkeyName(int hotkeyId, Keys defaultKey)
        {
            if (savedHotkeys.ContainsKey(hotkeyId))
            {
                uint mod = savedHotkeyModifiers.ContainsKey(hotkeyId) ? savedHotkeyModifiers[hotkeyId] : 0;
                return FormatKeysToString(savedHotkeys[hotkeyId], mod);
            }
            return FormatKeysToString(defaultKey, 0);
        }
        
        private void DeleteMacroActionWithConfirmation(dynamic eventItem, Panel panel)
        {
            if (currentEditingEvents == null) return;
            
            if (skipDeleteConfirmation)
            {
                DeleteMacroAction(eventItem, panel);
                return;
            }
            
            Form confirmDialog = new Form
            {
                Text = "Подтверждение удаления",
                Size = new System.Drawing.Size(450, 180),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label lblMessage = new Label
            {
                Text = "Вы уверены, что хотите удалить это действие из записи? Действие удалится безвозвратно.",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(400, 50),
                AutoSize = false
            };
            confirmDialog.Controls.Add(lblMessage);
            
            CheckBox chkDontAsk = new CheckBox
            {
                Text = "Больше не спрашивать",
                Location = new System.Drawing.Point(20, 80),
                AutoSize = true
            };
            confirmDialog.Controls.Add(chkDontAsk);
            
            Button btnYes = new Button
            {
                Text = "Да",
                DialogResult = DialogResult.Yes,
                Location = new System.Drawing.Point(250, 110),
                Size = new System.Drawing.Size(80, 30)
            };
            confirmDialog.Controls.Add(btnYes);
            
            Button btnNo = new Button
            {
                Text = "Нет",
                DialogResult = DialogResult.No,
                Location = new System.Drawing.Point(340, 110),
                Size = new System.Drawing.Size(80, 30)
            };
            confirmDialog.Controls.Add(btnNo);
            confirmDialog.CancelButton = btnNo;
            confirmDialog.AcceptButton = btnYes;
            
            var result = confirmDialog.ShowDialog();
            
            if (chkDontAsk.Checked && result == DialogResult.Yes)
            {
                skipDeleteConfirmation = true;
                SaveSettings();
            }
            
            if (result == DialogResult.Yes)
            {
                DeleteMacroAction(eventItem, panel);
            }
        }
        
        private void SaveSettings()
        {
            try
            {
                var settings = new Dictionary<string, object>
                {
                    ["SkipDeleteConfirmation"] = skipDeleteConfirmation
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                string settingsPath = Path.Combine(Application.StartupPath, "settings.json");
                File.WriteAllText(settingsPath, json);
            }
            catch { }
        }
        
        private void DeleteMacroAction(dynamic eventItem, Panel panel)
        {
            if (currentEditingEvents == null || currentEditingMacroFile == null) return;
            
            try
            {
                if (eventItem.IsGroup)
                {
                    var mouseMoves = (List<InputEvent>)eventItem.Event;
                    if (mouseMoves == null || mouseMoves.Count == 0) return;
                    
                    var firstMove = mouseMoves[0];
                    int startIndex = -1;
                    
                    for (int i = 0; i < currentEditingEvents.Count; i++)
                    {
                        var evt = currentEditingEvents[i];
                        if (evt.EventType == "MouseMove" &&
                            evt.X == firstMove.X &&
                            evt.Y == firstMove.Y &&
                            Math.Abs(evt.Timestamp - firstMove.Timestamp) < 1000000)
                        {
                            startIndex = i;
                            break;
                        }
                    }
                    
                    if (startIndex >= 0)
                    {
                        int removed = 0;
                        for (int i = startIndex; i < currentEditingEvents.Count && removed < mouseMoves.Count;)
                        {
                            if (currentEditingEvents[i].EventType == "MouseMove")
                            {
                                currentEditingEvents.RemoveAt(i);
                                removed++;
                            }
                            else
                            {
                                i++;
                            }
                        }
                    }
                }
                else
                {
                    var evt = (InputEvent)eventItem.Event;
                    for (int i = currentEditingEvents.Count - 1; i >= 0; i--)
                    {
                        var currentEvt = currentEditingEvents[i];
                        bool matches = false;
                        
                        if (currentEvt.EventType == evt.EventType)
                        {
                            if (evt.EventType == "KeyPress")
                            {
                                matches = currentEvt.KeyCode == evt.KeyCode;
                            }
                            else if (evt.EventType == "MouseClick")
                            {
                                matches = currentEvt.X == evt.X && 
                                         currentEvt.Y == evt.Y && 
                                         currentEvt.MouseButton == evt.MouseButton;
                            }
                            else if (evt.EventType == "MouseMove")
                            {
                                matches = currentEvt.X == evt.X && currentEvt.Y == evt.Y;
                            }
                        }
                        
                        if (matches)
                        {
                            currentEditingEvents.RemoveAt(i);
                            break;
                        }
                    }
                }
                
                AddLog($"🗑️ Действие удалено");
                
                string filePath = Path.Combine(macrosDirectory, currentEditingMacroFile);
                SaveMacroEvents(filePath);
                
                RefreshMacroDetailsPanel();
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при удалении действия: {ex.Message}");
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void EditMacroMouseMoves(List<InputEvent> mouseMoves, Button btn)
        {
            if (currentEditingEvents == null || currentEditingMacroFile == null) return;
            
            int startIndex = -1;
            if (mouseMoves.Count > 0)
            {
                var firstMove = mouseMoves[0];
                for (int i = 0; i < currentEditingEvents.Count; i++)
                {
                    var evt = currentEditingEvents[i];
                    if (evt.EventType == "MouseMove" &&
                        evt.X == firstMove.X &&
                        evt.Y == firstMove.Y &&
                        evt.Timestamp == firstMove.Timestamp)
                    {
                        startIndex = i;
                        break;
                    }
                }
            }
            
            if (startIndex < 0)
            {
                MessageBox.Show("Не удалось найти группу движений для редактирования", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            Form infoDialog = new Form
            {
                Text = "Замена движений мыши",
                Size = new System.Drawing.Size(550, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            string stopRecordKey = GetHotkeyName(HOTKEY_STOP_RECORD, Keys.F10);
            string stopPlayKey = GetHotkeyName(HOTKEY_STOP_PLAY, (Keys)0xC0);
            
            Label lblInfo = new Label
            {
                Text = $"Когда это окно закроется, запишите новые движения мыши для замены текущих.\n\n" +
                       $"Для отмены замены нажмите горячую клавишу остановки воспроизведения ({stopPlayKey}).\n" +
                       $"Для сохранения новых движений нажмите горячую клавишу сохранения записи ({stopRecordKey}).\n\n" +
                       $"Окно закроется автоматически через 3 секунды...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(510, 130),
                AutoSize = false
            };
            infoDialog.Controls.Add(lblInfo);
            
            System.Windows.Forms.Timer closeTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000,
                Enabled = true
            };
            
            closeTimer.Tick += (s, e) => {
                closeTimer.Stop();
                closeTimer.Dispose();
                infoDialog.DialogResult = DialogResult.OK;
                infoDialog.Close();
            };
            
            infoDialog.ShowDialog();
            
            isReplacingMouseMoves = true;
            mouseMovesStartIndex = startIndex;
            mouseMovesCountToReplace = mouseMoves.Count;
            
            AddLog($"🔄 Начата замена движений мыши. Нажмите горячую клавишу для остановки.");
            
            StartRecording();
            HideSettingsPanel();
        }
    }
}


