using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WuwaQuickSwapHelper.Services;

public class OverlayService
{
    private const int GWL_EXSTYLE = -20;

    private const int WS_EX_TRANSPARENT = 0x20;

    private const int WS_EX_LAYERED = 0x80000;


    [DllImport("user32.dll")]
    private static extern int GetWindowLong(
        IntPtr hWnd,
        int nIndex);


    [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        int dwNewLong);



    public void SetClickThrough(Window window, bool enable)
    {
        IntPtr hwnd =
            new WindowInteropHelper(window)
            .Handle;


        int style =
            GetWindowLong(
                hwnd,
                GWL_EXSTYLE);



        if (enable)
        {
            style |= WS_EX_TRANSPARENT;
            style |= WS_EX_LAYERED;
        }
        else
        {
            style &= ~WS_EX_TRANSPARENT;
        }


        SetWindowLong(
            hwnd,
            GWL_EXSTYLE,
            style);
    }
}