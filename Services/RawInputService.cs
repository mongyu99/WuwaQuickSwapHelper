using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WuwaQuickSwapHelper.Models;

namespace WuwaQuickSwapHelper.Services;

// Windows Raw Input으로 키보드 / 마우스 입력을 "읽기만" 합니다.
// 글로벌 훅과 달리 입력 흐름 중간에 끼어들지 않아 입력을 막거나 바꿀 수 없고,
// 게임에는 아무 영향 없이 Windows가 보내주는 입력 사본만 받습니다.
public class RawInputService : IDisposable
{
    public event Action<InputCode>? InputReceived;

    // 왼쪽 클릭 위치 (화면 좌표)
    public event Action<int, int>? MouseLeftPressed;

    private HwndSource? source;

    // 누르고 있는 동안 반복 입력이 들어오므로, 처음 눌린 순간만 처리합니다.
    private readonly HashSet<ushort> pressedKeys = new();

    private readonly Dictionary<InputCode, DateTime> lastInputTimes = new();

    // 각 키 마다 딜레이를 추가합니다. ( ms )
    private readonly Dictionary<InputCode, int> inputCooldown = new()
    {
        { InputCode.LeftClick, 200 },
        { InputCode.Space, 150 },
    };

    // 가상 키 코드 → 입력
    private static readonly Dictionary<ushort, InputCode> KeyMap = new()
    {
        { 0x51, InputCode.Q },
        { 0x45, InputCode.E },
        { 0x52, InputCode.R },
        { 0x31, InputCode.Swap1 },
        { 0x32, InputCode.Swap2 },
        { 0x33, InputCode.Swap3 },
        { 0x20, InputCode.Space },
        { 0x78, InputCode.F9 },
        { 0x79, InputCode.F10 },
    };

    // 창이 만들어진 뒤(Loaded 이후) 호출합니다.
    public void Start(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;

        source = HwndSource.FromHwnd(hwnd);
        source.AddHook(WndProc);

        // INPUTSINK: 창이 활성화되어 있지 않아도(게임 중) 입력 사본을 받습니다.
        var devices = new[]
        {
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x06, dwFlags = RIDEV_INPUTSINK, hwndTarget = hwnd }, // 키보드
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_INPUTSINK, hwndTarget = hwnd }, // 마우스
        };

        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
        {
            System.Diagnostics.Debug.WriteLine($"RegisterRawInputDevices 실패: {Marshal.GetLastWin32Error()}");
        }
    }

    public void Dispose()
    {
        if (source == null)
            return;

        var devices = new[]
        {
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x06, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero },
            new RAWINPUTDEVICE { usUsagePage = 0x01, usUsage = 0x02, dwFlags = RIDEV_REMOVE, hwndTarget = IntPtr.Zero },
        };

        RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>());

        source.RemoveHook(WndProc);
        source = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_INPUT)
        {
            ReadRawInput(lParam);
        }

        // 메시지를 소비하지 않습니다. (handled = false)
        return IntPtr.Zero;
    }

    private void ReadRawInput(IntPtr hRawInput)
    {
        int headerSize = Marshal.SizeOf<RAWINPUTHEADER>();
        uint size = 0;

        GetRawInputData(hRawInput, RID_INPUT, IntPtr.Zero, ref size, (uint)headerSize);

        if (size == 0)
            return;

        var buffer = Marshal.AllocHGlobal((int)size);

        try
        {
            if (GetRawInputData(hRawInput, RID_INPUT, buffer, ref size, (uint)headerSize) != size)
                return;

            var header = Marshal.PtrToStructure<RAWINPUTHEADER>(buffer);
            var data = buffer + headerSize;

            if (header.dwType == RIM_TYPEKEYBOARD)
            {
                OnKeyboard(Marshal.PtrToStructure<RAWKEYBOARD>(data));
            }
            else if (header.dwType == RIM_TYPEMOUSE)
            {
                OnMouse(Marshal.PtrToStructure<RAWMOUSE>(data));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private void OnKeyboard(RAWKEYBOARD keyboard)
    {
        bool isKeyUp = (keyboard.Flags & RI_KEY_BREAK) != 0;

        if (isKeyUp)
        {
            pressedKeys.Remove(keyboard.VKey);
            return;
        }

        // 이미 눌려 있는 키의 반복 입력은 무시합니다.
        if (!pressedKeys.Add(keyboard.VKey))
            return;

        if (KeyMap.TryGetValue(keyboard.VKey, out var input) && !IsDuplicateInput(input))
        {
            InputReceived?.Invoke(input);
        }
    }

    private void OnMouse(RAWMOUSE mouse)
    {
        if ((mouse.usButtonFlags & RI_MOUSE_LEFT_BUTTON_DOWN) == 0)
            return;

        if (!IsDuplicateInput(InputCode.LeftClick))
        {
            InputReceived?.Invoke(InputCode.LeftClick);
        }

        // Raw Input의 마우스 값은 이동량이라, 클릭 위치는 현재 커서 위치로 구합니다.
        if (GetCursorPos(out var point))
        {
            MouseLeftPressed?.Invoke(point.X, point.Y);
        }
    }

    // 시간 이내에 동일한 키 입력 시 무시합니다.
    private bool IsDuplicateInput(InputCode input)
    {
        if (!inputCooldown.TryGetValue(input, out int cooldown))
            return false;

        var now = DateTime.UtcNow;

        if (lastInputTimes.TryGetValue(input, out var lastTime) && (now - lastTime).TotalMilliseconds < cooldown)
            return true;

        lastInputTimes[input] = now;

        return false;
    }

    // ───── Win32 ─────

    private const int WM_INPUT = 0x00FF;
    private const uint RID_INPUT = 0x10000003;
    private const uint RIM_TYPEMOUSE = 0;
    private const uint RIM_TYPEKEYBOARD = 1;
    private const uint RIDEV_REMOVE = 0x00000001;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const ushort RI_KEY_BREAK = 0x01;
    private const ushort RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort usUsagePage;
        public ushort usUsage;
        public uint dwFlags;
        public IntPtr hwndTarget;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint dwType;
        public uint dwSize;
        public IntPtr hDevice;
        public IntPtr wParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct RAWMOUSE
    {
        [FieldOffset(0)] public ushort usFlags;
        [FieldOffset(4)] public ushort usButtonFlags;
        [FieldOffset(6)] public ushort usButtonData;
        [FieldOffset(8)] public uint ulRawButtons;
        [FieldOffset(12)] public int lLastX;
        [FieldOffset(16)] public int lLastY;
        [FieldOffset(20)] public uint ulExtraInformation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

    [DllImport("user32.dll")]
    private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);
}
