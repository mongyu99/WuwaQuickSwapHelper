using System.Windows;
using System.Windows.Input;
using WuwaQuickSwapHelper.Models;
using WuwaQuickSwapHelper.Services;

namespace WuwaQuickSwapHelper;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        inputService.InputReceived += InputService_InputReceived;

        _ = inputService.StartAsync();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.E:
                CurrentKey.Text = "E";
                break;

            case Key.Q:
                CurrentKey.Text = "Q";
                break;

            case Key.R:
                CurrentKey.Text = "R";
                break;

            default:
                // 다른 키는 무시
                break;
        }
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CurrentKey.Text = "Left Click";
    }

    private readonly GlobalInputService inputService = new();

    private void InputService_InputReceived(InputEvent input)
    {
        Dispatcher.Invoke(() =>
        {
            CurrentKey.Text = input.Code.ToString();
        });
    }
}