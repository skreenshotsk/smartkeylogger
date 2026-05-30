using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private Dictionary<int, Keys> savedHotkeys = new Dictionary<int, Keys>();
        private Dictionary<int, uint> savedHotkeyModifiers = new Dictionary<int, uint>();

        private HashSet<Keys> systemKeys = new HashSet<Keys>
        {
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8,
            Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.Escape, Keys.CapsLock, Keys.NumLock, Keys.Scroll,
            Keys.PrintScreen, Keys.Insert, Keys.Delete,
            Keys.Alt, Keys.Control, Keys.Shift,
            Keys.LWin, Keys.RWin, Keys.Apps
        };

        private Button? waitingForHotkeyButton = null;
        private int? waitingForHotkeyId = null;
        private DateTime hotkeyCaptureStartedAt = DateTime.MinValue;

        private void RegisterGlobalHotkeys()
        {
            UnregisterGlobalHotkeys();
            
            RegisterSingleHotkey(HOTKEY_START_RECORD, 
                savedHotkeys.ContainsKey(HOTKEY_START_RECORD) ? savedHotkeys[HOTKEY_START_RECORD] : Keys.F9,
                savedHotkeyModifiers.ContainsKey(HOTKEY_START_RECORD) ? savedHotkeyModifiers[HOTKEY_START_RECORD] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_STOP_RECORD,
                savedHotkeys.ContainsKey(HOTKEY_STOP_RECORD) ? savedHotkeys[HOTKEY_STOP_RECORD] : Keys.F10,
                savedHotkeyModifiers.ContainsKey(HOTKEY_STOP_RECORD) ? savedHotkeyModifiers[HOTKEY_STOP_RECORD] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PLAY,
                savedHotkeys.ContainsKey(HOTKEY_PLAY) ? savedHotkeys[HOTKEY_PLAY] : Keys.F11,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PLAY) ? savedHotkeyModifiers[HOTKEY_PLAY] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PLAY_LOOP,
                savedHotkeys.ContainsKey(HOTKEY_PLAY_LOOP) ? savedHotkeys[HOTKEY_PLAY_LOOP] : Keys.F12,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PLAY_LOOP) ? savedHotkeyModifiers[HOTKEY_PLAY_LOOP] : 0x0002);
            
            RegisterSingleHotkey(HOTKEY_STOP_PLAY,
                savedHotkeys.ContainsKey(HOTKEY_STOP_PLAY) ? savedHotkeys[HOTKEY_STOP_PLAY] : (Keys)0xC0,
                savedHotkeyModifiers.ContainsKey(HOTKEY_STOP_PLAY) ? savedHotkeyModifiers[HOTKEY_STOP_PLAY] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PAUSE_RECORD,
                savedHotkeys.ContainsKey(HOTKEY_PAUSE_RECORD) ? savedHotkeys[HOTKEY_PAUSE_RECORD] : Keys.Pause,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PAUSE_RECORD) ? savedHotkeyModifiers[HOTKEY_PAUSE_RECORD] : MOD_NONE);
            
            RegisterSingleHotkey(HOTKEY_PAUSE_PLAYBACK,
                savedHotkeys.ContainsKey(HOTKEY_PAUSE_PLAYBACK) ? savedHotkeys[HOTKEY_PAUSE_PLAYBACK] : Keys.Home,
                savedHotkeyModifiers.ContainsKey(HOTKEY_PAUSE_PLAYBACK) ? savedHotkeyModifiers[HOTKEY_PAUSE_PLAYBACK] : MOD_NONE);
        }
        
        private void RegisterSingleHotkey(int hotkeyId, Keys key, uint modifier)
        {
            UnregisterHotKey(this.Handle, hotkeyId);
            uint vk = KeysToVirtualKey(key);
            bool result = RegisterHotKey(this.Handle, hotkeyId, modifier, vk);
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                AddLog($"⚠️ Не удалось зарегистрировать горячую клавишу {GetHotkeyActionName(hotkeyId)}: {key} (код ошибки: {error}, vk={vk:X}, mod={modifier})");
            }
        }
        
        private uint KeysToVirtualKey(Keys key)
        {
            uint vk = (uint)((int)key & 0xFFFF);
            
            if (vk == 0)
            {
                Keys cleanKey = GetKeyWithoutModifiers(key);
                vk = (uint)((int)cleanKey & 0xFFFF);
            }
            
            int keyCode = (int)key & 0xFFFF;
            
            if (keyCode == 0xC0 || key == Keys.Oemtilde) 
                vk = VK_BACKQUOTE;
            else if (keyCode == 0x13 || key == Keys.Pause) 
                vk = VK_PAUSE;
            else if (keyCode == 0x24 || key == Keys.Home) 
                vk = VK_HOME;
            else if (keyCode == 0x23 || key == Keys.End) 
                vk = 0x23;
            else if (keyCode == 0x21 || key == Keys.PageUp || key == Keys.Prior) 
                vk = 0x21;
            else if (keyCode == 0x22 || key == Keys.PageDown || key == Keys.Next) 
                vk = 0x22;
            else if (keyCode == 0x2D || key == Keys.Insert) 
                vk = 0x2D;
            else if (keyCode == 0x2E || key == Keys.Delete) 
                vk = 0x2E;
            else if (keyCode == 0x03 || key == Keys.Cancel)
                vk = 0x03;
            
            if (vk == 0x20 && (keyCode >= 0x70 && keyCode <= 0x7B))
            {
                vk = (uint)keyCode;
            }

            return vk;
        }

        private void UnregisterGlobalHotkeys()
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_START_RECORD);
                UnregisterHotKey(this.Handle, HOTKEY_STOP_RECORD);
                UnregisterHotKey(this.Handle, HOTKEY_PLAY);
                UnregisterHotKey(this.Handle, HOTKEY_PLAY_LOOP);
                UnregisterHotKey(this.Handle, HOTKEY_STOP_PLAY);
                UnregisterHotKey(this.Handle, HOTKEY_PAUSE_RECORD);
                UnregisterHotKey(this.Handle, HOTKEY_PAUSE_PLAYBACK);
            }
            catch (Exception ex)
            {
                AddLog($"Ошибка при отмене регистрации горячих клавиш: {ex.Message}");
            }
        }

        private void ChangeHotkey(Button btn, int hotkeyId)
        {
            if (panelSettings == null || !panelSettings.Visible)
            {
                return;
            }
            
            btn.Text = "Нажмите клавишу...";
            btn.BackColor = System.Drawing.Color.FromArgb(255, 235, 59);
            
            waitingForHotkeyButton = btn;
            waitingForHotkeyId = hotkeyId;
            hotkeyCaptureStartedAt = DateTime.UtcNow;
            
            this.KeyPreview = true;
            this.ActiveControl = null;
            this.Focus();
            this.BringToFront();
            this.Activate();
            
            AddLog($"⌨️ Ожидание нажатия клавиши для: {GetHotkeyActionName(hotkeyId)}");
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (waitingForHotkeyButton != null && waitingForHotkeyId.HasValue && panelSettings != null && panelSettings.Visible)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                if ((DateTime.UtcNow - hotkeyCaptureStartedAt).TotalMilliseconds < 200 &&
                    (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter))
                {
                    return;
                }

                if (IsModifierOnlyKey(e.KeyCode))
                {
                    uint modPreview = GetCurrentModifierMask();
                    string modText = FormatModifierMask(modPreview);
                    waitingForHotkeyButton.Text = string.IsNullOrEmpty(modText) ? "Нажмите клавишу..." : $"{modText}+...";
                    return;
                }

                uint finalModifier = GetCurrentModifierMask();
                ApplyHotkeySelection(e.KeyCode, finalModifier);
            }
        }

        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            if (waitingForHotkeyButton != null && waitingForHotkeyId.HasValue && panelSettings != null && panelSettings.Visible)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ApplyHotkeySelection(Keys finalKey, uint finalModifier)
        {
            if (waitingForHotkeyButton == null || !waitingForHotkeyId.HasValue)
            {
                return;
            }

            bool isSystemKey = systemKeys.Contains(finalKey);
            if (isSystemKey)
            {
                var result = MessageBox.Show(
                    "Предупреждение: эта клавиша может использоваться системой. Приложение всё равно попробует её зарегистрировать, но часть системных сочетаний Windows может остаться недоступной.\n\nПродолжить?",
                    "Предупреждение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    CancelHotkeyCaptureAndRestore();
                    return;
                }
            }

            int hotkeyId = waitingForHotkeyId.Value;
            savedHotkeys[hotkeyId] = finalKey;
            savedHotkeyModifiers[hotkeyId] = finalModifier;
            SaveHotkeysToFile();

            AddLog($"🔧 Попытка регистрации горячей клавиши {GetHotkeyActionName(hotkeyId)}: key={finalKey}, mod={finalModifier}");
            RegisterSingleHotkey(hotkeyId, finalKey, finalModifier);

            string keyString = FormatKeysToString(finalKey, finalModifier);
            waitingForHotkeyButton.Text = keyString;
            waitingForHotkeyButton.BackColor = System.Drawing.Color.White;
            waitingForHotkeyButton.Tag = new { HotkeyId = hotkeyId, Key = finalKey, Mod = finalModifier };

            AddLog($"⌨️ Установлена горячая клавиша для {GetHotkeyActionName(hotkeyId)}: {keyString}");
            UpdateUI();
            FinishHotkeyCapture();
        }

        private void CancelHotkeyCaptureAndRestore()
        {
            if (waitingForHotkeyButton == null || !waitingForHotkeyId.HasValue)
            {
                return;
            }

            string oldText = FormatKeysToString(
                savedHotkeys.ContainsKey(waitingForHotkeyId.Value) ? savedHotkeys[waitingForHotkeyId.Value] : Keys.None,
                savedHotkeyModifiers.ContainsKey(waitingForHotkeyId.Value) ? savedHotkeyModifiers[waitingForHotkeyId.Value] : 0);
            waitingForHotkeyButton.Text = oldText;
            waitingForHotkeyButton.BackColor = System.Drawing.Color.White;

            FinishHotkeyCapture();
        }

        private void FinishHotkeyCapture()
        {
            waitingForHotkeyButton = null;
            waitingForHotkeyId = null;
            hotkeyCaptureStartedAt = DateTime.MinValue;
            this.KeyPreview = false;
        }
        
        private string GetHotkeyActionName(int hotkeyId)
        {
            return hotkeyId switch
            {
                HOTKEY_START_RECORD => "Начать запись",
                HOTKEY_STOP_RECORD => "Сохранить запись",
                HOTKEY_PLAY => "Воспроизвести",
                HOTKEY_PLAY_LOOP => "Воспроизвести в цикле",
                HOTKEY_PAUSE_RECORD => "Пауза записи",
                HOTKEY_PAUSE_PLAYBACK => "Пауза воспроизведения",
                HOTKEY_STOP_PLAY => "Остановить воспроизведение",
                _ => "Неизвестное действие"
            };
        }
        
        private string FormatKeysToString(Keys keyData, uint modifier = 0)
        {
            string result = "";
            
            uint mods = modifier;
            if (mods == 0)
            {
                mods = GetModifierFromKeys(keyData);
            }
            
            if ((mods & 0x0002) != 0)
                result += "Ctrl+";
            if ((mods & 0x0001) != 0)
                result += "Alt+";
            if ((mods & 0x0004) != 0)
                result += "Shift+";
            if ((mods & 0x0008) != 0)
                result += "Win+";
            
            Keys key = keyData & ~Keys.Modifiers;
            
            if (key >= Keys.F1 && key <= Keys.F12)
            {
                result += key.ToString();
            }
            else if (key == Keys.Pause)
            {
                result += "Pause";
            }
            else if (key == Keys.Home)
            {
                result += "Home";
            }
            else if (key == Keys.Oemtilde || key == Keys.Oem3 || (int)key == 0xC0)
            {
                result += "`";
            }
            else
            {
                string keyStr = key.ToString();
                if (keyStr.StartsWith("D") && keyStr.Length == 2 && char.IsDigit(keyStr[1]))
                {
                    result += keyStr[1];
                }
                else
                {
                    result += keyStr;
                }
            }
            
            return result;
        }
        
        private uint GetModifierFromKeys(Keys keyData)
        {
            uint mod = MOD_NONE;
            if ((keyData & Keys.Control) == Keys.Control)
                mod |= 0x0002;
            if ((keyData & Keys.Alt) == Keys.Alt)
                mod |= 0x0001;
            if ((keyData & Keys.Shift) == Keys.Shift)
                mod |= 0x0004;
            return mod;
        }

        private uint GetCurrentModifierMask()
        {
            uint mod = MOD_NONE;
            Keys current = Control.ModifierKeys;

            if ((current & Keys.Control) == Keys.Control)
                mod |= 0x0002;
            if ((current & Keys.Alt) == Keys.Alt)
                mod |= 0x0001;
            if ((current & Keys.Shift) == Keys.Shift)
                mod |= 0x0004;
            if (IsWinModifierPressed())
                mod |= 0x0008;

            return mod;
        }

        private bool IsModifierOnlyKey(Keys keyCode)
        {
            return keyCode == Keys.ControlKey ||
                   keyCode == Keys.Menu ||
                   keyCode == Keys.ShiftKey ||
                   keyCode == Keys.LWin ||
                   keyCode == Keys.RWin;
        }

        private string FormatModifierMask(uint modifierMask)
        {
            string result = "";
            if ((modifierMask & 0x0002) != 0) result += "Ctrl";
            if ((modifierMask & 0x0001) != 0) result += (result.Length > 0 ? "+Alt" : "Alt");
            if ((modifierMask & 0x0004) != 0) result += (result.Length > 0 ? "+Shift" : "Shift");
            if ((modifierMask & 0x0008) != 0) result += (result.Length > 0 ? "+Win" : "Win");
            return result;
        }

        private bool IsWinModifierPressed()
        {
            return (GetAsyncKeyState(0x5B) & 0x8000) != 0 || (GetAsyncKeyState(0x5C) & 0x8000) != 0;
        }
        
        private Keys GetKeyWithoutModifiers(Keys keyData)
        {
            return keyData & ~(Keys.Control | Keys.Alt | Keys.Shift | Keys.LWin | Keys.RWin | Keys.Modifiers);
        }
        
        private void SaveHotkeysToFile()
        {
            try
            {
                var hotkeyData = new Dictionary<string, object>();
                var keysDict = new Dictionary<string, int>();
                var modsDict = new Dictionary<string, uint>();
                
                foreach (var kvp in savedHotkeys)
                {
                    keysDict[kvp.Key.ToString()] = (int)kvp.Value;
                }
                
                foreach (var kvp in savedHotkeyModifiers)
                {
                    modsDict[kvp.Key.ToString()] = kvp.Value;
                }
                
                hotkeyData["Keys"] = keysDict;
                hotkeyData["Modifiers"] = modsDict;
                
                string json = System.Text.Json.JsonSerializer.Serialize(hotkeyData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                string hotkeysPath = Path.Combine(Application.StartupPath, "hotkeys.json");
                File.WriteAllText(hotkeysPath, json);
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при сохранении горячих клавиш: {ex.Message}");
            }
        }
        
        private void LoadHotkeysFromFile()
        {
            try
            {
                string hotkeysPath = Path.Combine(Application.StartupPath, "hotkeys.json");
                if (!File.Exists(hotkeysPath))
                    return;
                
                string json = File.ReadAllText(hotkeysPath);
                var hotkeyData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                
                if (hotkeyData.TryGetProperty("Keys", out var keysElement))
                {
                    foreach (var prop in keysElement.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out int hotkeyId) && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            int keyValue = prop.Value.GetInt32();
                            Keys loadedKey = (Keys)keyValue;
                            
                            if (hotkeyId == HOTKEY_START_RECORD && (loadedKey == Keys.Space || keyValue == 0x20))
                            {
                                AddLog($"⚠️ Обнаружен пробел для начала записи, заменяем на F9");
                                continue;
                            }
                            
                            if (hotkeyId == HOTKEY_PLAY && (loadedKey == Keys.Space || keyValue == 0x20))
                            {
                                AddLog($"⚠️ Обнаружен пробел для воспроизведения, заменяем на F11");
                                continue;
                            }
                            
                            savedHotkeys[hotkeyId] = loadedKey;
                        }
                    }
                }
                
                if (hotkeyData.TryGetProperty("Modifiers", out var modsElement))
                {
                    foreach (var prop in modsElement.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out int hotkeyId) && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            savedHotkeyModifiers[hotkeyId] = prop.Value.GetUInt32();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при загрузке горячих клавиш: {ex.Message}");
            }
        }
        
        private void ResetHotkeysToDefault()
        {
            var result = MessageBox.Show(
                "Вы уверены, что хотите вернуть все горячие клавиши к настройкам по умолчанию?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                savedHotkeys.Clear();
                savedHotkeyModifiers.Clear();
                SaveHotkeysToFile();
                
                UnregisterGlobalHotkeys();
                RegisterGlobalHotkeys();
                AddLog("🔄 Горячие клавиши сброшены к настройкам по умолчанию");
                ShowHotkeysSettings();
            }
        }
    }
}

