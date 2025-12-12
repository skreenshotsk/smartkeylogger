// MainForm.MacroFileManager.cs - Управление файлами макросов
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        private void SaveMacroEvents(string filePath)
        {
            if (currentEditingEvents == null) return;
            
            try
            {
                var macroData = new MacroData
                {
                    Events = currentEditingEvents,
                    KeyboardLayout = currentEditingKeyboardLayout,
                    DurationTicks = currentEditingDurationTicks
                };

                var options = new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                };
                
                string json = System.Text.Json.JsonSerializer.Serialize(macroData, options);
                File.WriteAllText(filePath, json);
                
                AddLog($"💾 Макрос сохранен: {Path.GetFileName(filePath)} ({currentEditingEvents.Count} событий)");
            }
            catch (Exception ex)
            {
                AddLog($"❌ Ошибка при сохранении событий: {ex.Message}");
                throw;
            }
        }
        
        private void LoadMacroMetadata(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var macroData = System.Text.Json.JsonSerializer.Deserialize<MacroData>(json, new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
                
                if (macroData != null)
                {
                    currentEditingKeyboardLayout = macroData.KeyboardLayout;
                    currentEditingDurationTicks = macroData.DurationTicks;
                }
            }
            catch
            {
                currentEditingKeyboardLayout = null;
                currentEditingDurationTicks = null;
            }
        }
    }
}

