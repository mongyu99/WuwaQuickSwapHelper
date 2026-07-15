using SharpHook;
using SharpHook.Data;
using SharpHook.Native;
using System.Diagnostics;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Services;

public class GlobalInputService : IDisposable
{
    private readonly TaskPoolGlobalHook hook;

    private readonly Dictionary<InputCode, DateTime> lastInputTimes = new();

    // 각 키 마다 딜레이를 추가합니다. ( ms )
    private readonly Dictionary<InputCode, int> inputCooldown = new()
    {
        { InputCode.LeftClick, 200 },
        { InputCode.Space, 150 },

        // 추후 필요하면 추가
        // { InputCode.Q, 100 },
        // { InputCode.E, 100 },
    };

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

            if (!IsDuplicateInput(InputCode.LeftClick))
            {
                InputReceived?.Invoke(InputCode.LeftClick);
            }

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
                if (!IsDuplicateInput(InputCode.Space))
                {
                    InputReceived?.Invoke(InputCode.Space);
                }
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

    // 시간 이내에 동일한 키 입력 시 무시합니다.
    private bool IsDuplicateInput(InputCode input)
    {
        if (!inputCooldown.TryGetValue(input, out int cooldown))
            return false;

        var now = DateTime.UtcNow;

        if (lastInputTimes.TryGetValue(input, out var lastTime))
        {
            if ((now - lastTime).TotalMilliseconds < cooldown)
            {
                return true;
            }
        }

        lastInputTimes[input] = now;

        return false;
    }
}