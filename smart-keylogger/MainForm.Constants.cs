// MainForm.Constants.cs
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace MacroRecorderGUI
{
    public partial class MainForm
    {
        // Константы для регистрации горячих клавиш
        private const int MOD_NONE = 0x0000;
        private const int WM_HOTKEY = 0x0312;
        
        // ID для горячих клавиш
        private const int HOTKEY_START_RECORD = 1;
        private const int HOTKEY_STOP_RECORD = 2;
        private const int HOTKEY_PLAY = 3;
        private const int HOTKEY_PLAY_LOOP = 4;
        private const int HOTKEY_STOP_PLAY = 5;
        private const int HOTKEY_PAUSE_RECORD = 6;
        private const int HOTKEY_PAUSE_PLAYBACK = 7;

        // Код клавиши Backquote ( ` )
        private const uint VK_BACKQUOTE = 0xC0;
        private const uint VK_PAUSE = 0x13; // Код клавиши Pause/Break
        private const uint VK_HOME = 0x24;  // Код клавиши Home

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}

