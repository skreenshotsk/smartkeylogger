public class InputEvent
{
    public string EventType { get; set; } = "";
    public long Timestamp { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int KeyCode { get; set; }
    public int MouseButton { get; set; }
    public bool IsPressed { get; set; }
}

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}