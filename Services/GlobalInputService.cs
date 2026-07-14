using SharpHook;
using SharpHook.Data;
using SharpHook.Native;
using System.Diagnostics;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Services;

public class GlobalInputService : IDisposable
{
    private readonly TaskPoolGlobalHook hook;

    public event Action<InputCode>? InputReceived;

    public GlobalInputService()
    {
        hook = new TaskPoolGlobalHook();

        hook.KeyPressed += OnKeyPressed;
        hook.MousePressed += OnMousePressed;
    }

    public async Task StartAsync()
    {
        try
        {
            Debug.WriteLine("Global Hook Started");

            await hook.RunAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    public void Dispose()
    {
        hook.Dispose();
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        Console.WriteLine($"Key : {e.Data.KeyCode}");

        if (e.Data.KeyCode == KeyCode.VcF10)
        {
            Console.WriteLine("GLOBAL F10");

            InputReceived?.Invoke(InputCode.F10);

            return;
        }


        switch (e.Data.KeyCode)
        {
            case KeyCode.VcQ:
                InputReceived?.Invoke(InputCode.Q);
                break;

            case KeyCode.VcE:
                InputReceived?.Invoke(InputCode.E);
                break;

            case KeyCode.VcR:
                InputReceived?.Invoke(InputCode.R);
                break;

            case KeyCode.Vc1:
                InputReceived?.Invoke(InputCode.Swap1);
                break;

            case KeyCode.Vc2:
                InputReceived?.Invoke(InputCode.Swap2);
                break;

            case KeyCode.Vc3:
                InputReceived?.Invoke(InputCode.Swap3);
                break;

            case KeyCode.VcSpace:
                InputReceived?.Invoke(InputCode.Space);
                break;

            case KeyCode.VcF10:
                InputReceived?.Invoke(InputCode.F10);
                break;
        }
    }

    private void OnMousePressed(object? sender, MouseHookEventArgs e)
    {
        Debug.WriteLine($"Mouse : {e.Data.Button}");

        if (e.Data.Button == MouseButton.Button1)
        {
            InputReceived?.Invoke(InputCode.LeftClick);
        }
    }
}