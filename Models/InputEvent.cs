namespace WuwaQuickSwapHelper.Models;
public enum InputEventType
{
    KeyDown,
    KeyUp,
    MouseDown,
    MouseUp
}

public enum InputCode
{
    E,
    Q,
    R,
    LeftClick
}

public class InputEvent
{
    public InputEventType Type { get; set; }

    public InputCode Code { get; set; }

    public DateTime Time { get; set; }
}