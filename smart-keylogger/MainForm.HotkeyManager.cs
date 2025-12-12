// MainForm.HotkeyManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        // Сохраненные горячие клавиши
        private Dictionary<int, Keys> savedHotkeys = new Dictionary<int, Keys>();
        private Dictionary<int, uint> savedHotkeyModifiers = new Dictionary<int, uint>();

        // Клавиши, которые могут использоваться системой
        private HashSet<Keys> systemKeys = new HashSet<Keys>
        {
            Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, Keys.F6, Keys.F7, Keys.F8,
            Keys.F9, Keys.F10, Keys.F11, Keys.F12,
            Keys.Escape, Keys.CapsLock, Keys.NumLock, Keys.Scroll,
            Keys.PrintScreen, Keys.Insert, Keys.Delete,
            Keys.Alt, Keys.Control, Keys.Shift,
            Keys.LWin, Keys.RWin, Keys.Apps
        };

        // Состояние для настройки горячих клавиш
        private Button? waitingForHotkeyButton = null;
        private int? waitingForHotkeyId = null;
        private Keys currentPressedKeys = Keys.None;
        private Keys maxPressedKeys = Keys.None;
        private HashSet<Keys> pressedKeysSet = new HashSet<Keys>();
        private Keys lastNonModifierKey = Keys.None;

        private void RegisterGlobalHotkeys()
        {
            UnregisterGlobalHotkeys();
            
            // Небольшая задержка после отмены всех горячих клавиш
            System.Threading.Thread.Sleep(50);
            
            // Используем сохраненные горячие клавиши или значения по умолчанию
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
                savedHotkeyModifiers.ContainsKey(HOTKEY_PLAY_LOOP) ? savedHotkeyModifiers[HOTKEY_PLAY_LOOP] : 0x0002); // MOD_CONTROL
            
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
            // Убеждаемся, что старая регистрация отменена (несколько раз для надежности)
            for (int i = 0; i < 3; i++)
            {
                UnregisterHotKey(this.Handle, hotkeyId);
            }
            
            // Небольшая задержка для гарантии, что система обработала отмену
            System.Threading.Thread.Sleep(50);
            
            // Используем KeysToVirtualKey для правильного преобразования
            Console.WriteLine($"DEBUG RegisterSingleHotkey: hotkeyId={hotkeyId}, key={key} (0x{(int)key:X} = {(int)key} decimal), modifier={modifier}");
            uint vk = KeysToVirtualKey(key);
            Console.WriteLine($"DEBUG RegisterSingleHotkey: после KeysToVirtualKey vk={vk:X} ({vk} decimal)");
            
            bool result = RegisterHotKey(this.Handle, hotkeyId, modifier, vk);
            Console.WriteLine($"Hotkey {hotkeyId} ({key}, mod={modifier}, vk={vk:X}) регистрация: {result}");
            if (!result)
            {
                int error = Marshal.GetLastWin32Error();
                AddLog($"⚠️ Не удалось зарегистрировать горячую клавишу {GetHotkeyActionName(hotkeyId)}: {key} (код ошибки: {error}, vk={vk:X}, mod={modifier})");
            }
            else
            {
                AddLog($"✅ Горячая клавиша {GetHotkeyActionName(hotkeyId)} успешно зарегистрирована: {key} с модификаторами {modifier} (vk={vk:X})");
            }
        }
        
        private uint KeysToVirtualKey(Keys key)
        {
            // Убираем модификаторы, оставляя только саму клавишу (нижние 16 бит)
            uint vk = (uint)((int)key & 0xFFFF);
            
            // Если vk == 0 после удаления модификаторов, значит были только модификаторы
            // Пробуем убрать модификаторы через GetKeyWithoutModifiers
            if (vk == 0)
            {
                Keys cleanKey = GetKeyWithoutModifiers(key);
                vk = (uint)((int)cleanKey & 0xFFFF);
            }
            
            // Специальные клавиши с явным указанием VK кода
            // Проверяем исходный key (нижние 16 бит)
            int keyCode = (int)key & 0xFFFF;
            
            if (keyCode == 0xC0 || key == Keys.Oemtilde) 
                vk = VK_BACKQUOTE; // 0xC0
            else if (keyCode == 0x13 || key == Keys.Pause) 
                vk = VK_PAUSE; // 0x13
            else if (keyCode == 0x24 || key == Keys.Home) 
                vk = VK_HOME; // 0x24
            else if (keyCode == 0x23 || key == Keys.End) 
                vk = 0x23; // VK_END
            else if (keyCode == 0x21 || key == Keys.PageUp || key == Keys.Prior) 
                vk = 0x21; // VK_PRIOR
            else if (keyCode == 0x22 || key == Keys.PageDown || key == Keys.Next) 
                vk = 0x22; // VK_NEXT
            else if (keyCode == 0x2D || key == Keys.Insert) 
                vk = 0x2D; // VK_INSERT
            else if (keyCode == 0x2E || key == Keys.Delete) 
                vk = 0x2E; // VK_DELETE
            else if (keyCode == 0x03 || key == Keys.Cancel)
                vk = 0x03; // VK_CANCEL (Ctrl+Break)
            // Для F-клавиш (F1-F12: 0x70-0x7B) и остальных клавиш vk уже установлен выше через нижние 16 бит
            
            // Если получили пробел (0x20) для F-клавиши, это ошибка - возвращаем исходный код
            if (vk == 0x20 && (keyCode >= 0x70 && keyCode <= 0x7B))
            {
                Console.WriteLine($"ERROR: KeysToVirtualKey получил пробел для F-клавиши! key={key} (0x{(int)key:X}), keyCode={keyCode:X}, используем keyCode как vk");
                vk = (uint)keyCode;
            }
            
            Console.WriteLine($"DEBUG KeysToVirtualKey: key={key} (0x{(int)key:X}), keyCode={keyCode:X}, vk={vk:X} ({vk} decimal)");
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
            // ВАЖНО: Разрешаем изменение горячей клавиши ТОЛЬКО если панель настроек видима
            if (panelSettings == null || !panelSettings.Visible)
            {
                return; // Не выполняем изменение, если панель настроек не видима
            }
            
            btn.Text = "Нажмите клавишу...";
            btn.BackColor = System.Drawing.Color.FromArgb(255, 235, 59);
            
            // Устанавливаем флаг ожидания нажатия клавиши
            waitingForHotkeyButton = btn;
            waitingForHotkeyId = hotkeyId;
            currentPressedKeys = Keys.None;
            maxPressedKeys = Keys.None;
            lastNonModifierKey = Keys.None;
            pressedKeysSet.Clear();
            
            // Форма должна быть в фокусе для захвата клавиш
            this.KeyPreview = true;
            this.Focus();
            this.BringToFront();
            this.Activate();
            
            AddLog($"⌨️ Ожидание нажатия клавиши для: {GetHotkeyActionName(hotkeyId)}");
        }

        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // Обрабатываем только если ожидаем настройку горячей клавиши И панель настроек видима
            if (waitingForHotkeyButton != null && waitingForHotkeyId.HasValue && panelSettings != null && panelSettings.Visible)
            {
                e.Handled = true; // Блокируем дальнейшую обработку
                
                // Извлекаем клавишу из события - используем именно ту клавишу, которая была нажата
                // e.KeyCode содержит код клавиши без модификаторов
                Keys keyCode = e.KeyCode;
                Keys modifierKeys = e.KeyData & Keys.Modifiers;
                
                // Добавляем не-модификаторную клавишу (используем KeyCode, а не KeyData)
                if (keyCode != Keys.None && keyCode != Keys.ControlKey && keyCode != Keys.Menu && keyCode != Keys.ShiftKey)
                {
                    pressedKeysSet.Add(keyCode);
                    // Сохраняем последнюю нажатую не-модификаторную клавишу
                    Keys excludedModifiers = Keys.Control | Keys.Alt | Keys.Shift | Keys.LWin | Keys.RWin | Keys.ControlKey | Keys.Menu | Keys.ShiftKey;
                    if ((keyCode & excludedModifiers) == 0)
                    {
                        lastNonModifierKey = keyCode;
                    }
                }
                
                // Добавляем модификаторы как отдельные клавиши (если они есть)
                if ((modifierKeys & Keys.Control) != 0)
                    pressedKeysSet.Add(Keys.Control);
                if ((modifierKeys & Keys.Alt) != 0)
                    pressedKeysSet.Add(Keys.Alt);
                if ((modifierKeys & Keys.Shift) != 0)
                    pressedKeysSet.Add(Keys.Shift);
                
                // Обновляем текущие нажатые клавиши
                currentPressedKeys = Keys.None;
                foreach (var k in pressedKeysSet)
                {
                    currentPressedKeys |= k;
                }
                
                // Сохраняем максимальную комбинацию всех одновременно нажатых клавиш
                if (currentPressedKeys != Keys.None)
                {
                    maxPressedKeys = currentPressedKeys; // Обновляем максимальную комбинацию
                    
                    // Обновляем текст кнопки в реальном времени
                    uint modifier = GetModifierFromKeys(currentPressedKeys);
                    string previewText = FormatKeysToString(currentPressedKeys, modifier);
                    waitingForHotkeyButton.Text = previewText;
                }
            }
        }

        private void MainForm_KeyUp(object? sender, KeyEventArgs e)
        {
            // Обрабатываем только если ожидаем настройку горячей клавиши И панель настроек видима
            if (waitingForHotkeyButton != null && waitingForHotkeyId.HasValue && panelSettings != null && panelSettings.Visible)
            {
                e.Handled = true; // Блокируем дальнейшую обработку
                
                // Удаляем клавишу из множества нажатых
                // Используем KeyCode для точного определения клавиши
                Keys keyCode = e.KeyCode;
                Keys releasedModifiers = e.KeyData & Keys.Modifiers;
                
                // Удаляем не-модификаторную клавишу
                if (keyCode != Keys.None && keyCode != Keys.ControlKey && keyCode != Keys.Menu && keyCode != Keys.ShiftKey)
                {
                    pressedKeysSet.Remove(keyCode);
                }
                
                // Удаляем модификаторы
                if ((releasedModifiers & Keys.Control) != 0)
                    pressedKeysSet.Remove(Keys.Control);
                if ((releasedModifiers & Keys.Alt) != 0)
                    pressedKeysSet.Remove(Keys.Alt);
                if ((releasedModifiers & Keys.Shift) != 0)
                    pressedKeysSet.Remove(Keys.Shift);
                
                // Если все клавиши отпущены - сохраняем горячую клавишу
                if (pressedKeysSet.Count == 0 && maxPressedKeys != Keys.None)
                {
                    // Используем максимальную комбинацию всех одновременно нажатых клавиш
                    Keys finalCombination = maxPressedKeys;
                    
                    // Извлекаем модификаторы
                    uint finalModifier = GetModifierFromKeys(finalCombination);
                    
                    // Используем последнюю нажатую не-модификаторную клавишу
                    // НО если это модификатор или None, используем текущий KeyCode из события KeyUp
                    Keys finalKey = lastNonModifierKey;
                    
                    // Если lastNonModifierKey не установлен или это модификатор, используем KeyCode из текущего события
                    Keys excludedModifiers = Keys.Control | Keys.Alt | Keys.Shift | Keys.LWin | Keys.RWin | Keys.ControlKey | Keys.Menu | Keys.ShiftKey | Keys.LMenu | Keys.RMenu | Keys.LShiftKey | Keys.RShiftKey | Keys.LControlKey | Keys.RControlKey;
                    if (finalKey == Keys.None || (finalKey & excludedModifiers) != 0)
                    {
                        // Используем KeyCode из события KeyUp (это реальная клавиша, которая была отпущена)
                        if (keyCode != Keys.None && (keyCode & excludedModifiers) == 0)
                        {
                            finalKey = keyCode;
                        }
                        else
                        {
                            // Если и KeyCode не подходит, извлекаем из комбинации
                            Keys keysWithoutMods = GetKeyWithoutModifiers(finalCombination);
                            finalKey = keysWithoutMods;
                            
                            // Убираем модификаторы из keysWithoutMods
                            finalKey &= ~excludedModifiers;
                            
                            // Если все еще не нашли, ищем вручную
                            if (finalKey == Keys.None)
                            {
                                for (int i = 255; i >= 1; i--)
                                {
                                    Keys testKey = (Keys)i;
                                    if ((keysWithoutMods & testKey) == testKey && (testKey & excludedModifiers) == 0)
                                    {
                                        finalKey = testKey;
                                        break;
                                    }
                                }
                            }
                            
                            // Если все еще None, используем keysWithoutMods как есть
                            if (finalKey == Keys.None)
                            {
                                finalKey = keysWithoutMods;
                            }
                        }
                    }
                    
                    // Отладочная информация
                    Console.WriteLine($"DEBUG: lastNonModifierKey = {lastNonModifierKey}, keyCode = {keyCode}, maxPressedKeys = {maxPressedKeys}, finalKey = {finalKey}, finalCombination = {finalCombination}");
                    
                    // Проверяем, является ли клавиша системной
                    bool isSystemKey = systemKeys.Contains(finalKey);
                    if (isSystemKey)
                    {
                        var result = MessageBox.Show(
                            "Предупреждение: эта клавиша может использоваться системой, в случае, если она не работает, попробуйте другую клавишу или сочетание клавиш.\n\nПродолжить?",
                            "Предупреждение",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);
                        
                        if (result == DialogResult.No)
                        {
                            // Восстанавливаем старое значение
                            string oldText = FormatKeysToString(
                                savedHotkeys.ContainsKey(waitingForHotkeyId.Value) ? savedHotkeys[waitingForHotkeyId.Value] : Keys.None,
                                savedHotkeyModifiers.ContainsKey(waitingForHotkeyId.Value) ? savedHotkeyModifiers[waitingForHotkeyId.Value] : 0);
                            waitingForHotkeyButton.Text = oldText;
                            waitingForHotkeyButton.BackColor = System.Drawing.Color.White;
                            waitingForHotkeyButton = null;
                            waitingForHotkeyId = null;
                            currentPressedKeys = Keys.None;
                            maxPressedKeys = Keys.None;
                            pressedKeysSet.Clear();
                            return;
                        }
                    }
                    
                    // Сохраняем новую горячую клавишу в память ПЕРЕД отменой старой
                    savedHotkeys[waitingForHotkeyId.Value] = finalKey;
                    savedHotkeyModifiers[waitingForHotkeyId.Value] = finalModifier;
                    
                    // Сохраняем в файл
                    SaveHotkeysToFile();
                    
                    // Регистрируем новую горячую клавишу через RegisterSingleHotkey
                    // (он сам отменит старую регистрацию перед регистрацией новой)
                    int hotkeyId = waitingForHotkeyId.Value;
                    
                    AddLog($"🔧 Попытка регистрации горячей клавиши {GetHotkeyActionName(hotkeyId)}: key={finalKey}, mod={finalModifier}");
                    
                    // Используем RegisterSingleHotkey для единообразной логики регистрации
                    RegisterSingleHotkey(hotkeyId, finalKey, finalModifier);
                    
                    // Преобразуем Keys в строку
                    string keyString = FormatKeysToString(finalCombination, finalModifier);
                    
                    // Обновляем текст кнопки
                    waitingForHotkeyButton.Text = keyString;
                    waitingForHotkeyButton.BackColor = System.Drawing.Color.White;
                    
                    // Обновляем Tag кнопки
                    waitingForHotkeyButton.Tag = new { HotkeyId = waitingForHotkeyId.Value, Key = finalKey, Mod = finalModifier };
                    
                    AddLog($"⌨️ Установлена горячая клавиша для {GetHotkeyActionName(waitingForHotkeyId.Value)}: {keyString}");
                    
                    // ВАЖНО: Сбрасываем состояние ПОСЛЕ сохранения, чтобы горячие клавиши не активировались на главном экране
                    waitingForHotkeyButton = null;
                    waitingForHotkeyId = null;
                    currentPressedKeys = Keys.None;
                    maxPressedKeys = Keys.None;
                    lastNonModifierKey = Keys.None;
                    pressedKeysSet.Clear();
                    
                    // Убеждаемся, что KeyPreview выключен после настройки
                    this.KeyPreview = false;
                }
                else if (pressedKeysSet.Count > 0)
                {
                    // Обновляем текущие нажатые клавиши (после удаления одной)
                    currentPressedKeys = Keys.None;
                    foreach (var k in pressedKeysSet)
                    {
                        currentPressedKeys |= k;
                    }
                    // Добавляем модификаторы из текущего состояния
                    if (e.KeyData != Keys.None)
                    {
                        currentPressedKeys |= (e.KeyData & Keys.Modifiers);
                    }
                }
            }
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
            
            // Используем modifier если он указан, иначе извлекаем из keyData
            uint mods = modifier;
            if (mods == 0)
            {
                mods = GetModifierFromKeys(keyData);
            }
            
            // Проверяем модификаторы
            if ((mods & 0x0002) != 0) // MOD_CONTROL
                result += "Ctrl+";
            if ((mods & 0x0001) != 0) // MOD_ALT
                result += "Alt+";
            if ((mods & 0x0004) != 0) // MOD_SHIFT
                result += "Shift+";
            
            Keys key = keyData & ~Keys.Modifiers;
            
            // Преобразуем клавишу в строку
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
                // Убираем префикс "D" для цифровых клавиш
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
            // Поддерживаем комбинации модификаторов
            if ((keyData & Keys.Control) == Keys.Control)
                mod |= 0x0002; // MOD_CONTROL
            if ((keyData & Keys.Alt) == Keys.Alt)
                mod |= 0x0001; // MOD_ALT
            if ((keyData & Keys.Shift) == Keys.Shift)
                mod |= 0x0004; // MOD_SHIFT
            return mod;
        }
        
        private Keys GetKeyWithoutModifiers(Keys keyData)
        {
            // Keys.Modifiers включает Control, Alt, Shift, но может не включать все модификаторы
            // Убираем все возможные модификаторы, чтобы получить чистую клавишу
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
                
                // Загружаем клавиши
                if (hotkeyData.TryGetProperty("Keys", out var keysElement))
                {
                    foreach (var prop in keysElement.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out int hotkeyId) && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            int keyValue = prop.Value.GetInt32();
                            Keys loadedKey = (Keys)keyValue;
                            
                            // Проверяем, что это не пробел (0x20 или Keys.Space) для HOTKEY_START_RECORD
                            if (hotkeyId == HOTKEY_START_RECORD && (loadedKey == Keys.Space || keyValue == 0x20))
                            {
                                // Игнорируем пробел, используем значение по умолчанию F9
                                AddLog($"⚠️ Обнаружен пробел для начала записи, заменяем на F9");
                                continue;
                            }
                            
                            // Проверяем, что это не пробел (0x20 или Keys.Space) для HOTKEY_PLAY
                            if (hotkeyId == HOTKEY_PLAY && (loadedKey == Keys.Space || keyValue == 0x20))
                            {
                                // Игнорируем пробел, используем значение по умолчанию F11
                                AddLog($"⚠️ Обнаружен пробел для воспроизведения, заменяем на F11");
                                continue;
                            }
                            
                            // Логируем загруженное значение для отладки
                            Console.WriteLine($"DEBUG LoadHotkeysFromFile: hotkeyId={hotkeyId}, keyValue={keyValue} (0x{keyValue:X}), loadedKey={loadedKey}");
                            savedHotkeys[hotkeyId] = loadedKey;
                        }
                    }
                }
                
                // Загружаем модификаторы
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
                // Очищаем сохраненные горячие клавиши
                savedHotkeys.Clear();
                savedHotkeyModifiers.Clear();
                
                UnregisterGlobalHotkeys();
                RegisterGlobalHotkeys();
                AddLog("🔄 Горячие клавиши сброшены к настройкам по умолчанию");
                ShowHotkeysSettings(); // Обновляем UI
            }
        }
    }
}

