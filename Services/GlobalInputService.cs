using SharpHook;
using SharpHook.Data;
using SharpHook.Native;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Services;

public class GlobalInputService
{
    private readonly EventLoopGlobalHook hook;

    public event Action<InputEvent>? InputReceived;

    public GlobalInputService()
    {
        hook = new EventLoopGlobalHook();

        hook.KeyPressed += OnKeyPressed;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        InputCode? code = e.Data.KeyCode switch
        {
            KeyCode.VcE => InputCode.E,
            KeyCode.VcQ => InputCode.Q,
            KeyCode.VcR => InputCode.R,

            _ => null
        };

        if (code == null)
            return;

        InputReceived?.Invoke(new InputEvent
        {
            Type = InputEventType.KeyDown,
            Code = code.Value,
            Time = DateTime.Now
        });
    }

    public async Task StartAsync()
    {
        await hook.RunAsync();
    }

    public void Stop()
    {
        hook.Stop();
    }
}