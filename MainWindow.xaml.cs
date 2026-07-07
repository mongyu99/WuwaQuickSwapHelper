using System.Windows;
using WuwaQuickSwapHelper.Engine;
using WuwaQuickSwapHelper.Models;
using WuwaQuickSwapHelper.Services;

namespace WuwaQuickSwapHelper;

public partial class MainWindow : Window
{
    private readonly GlobalInputService inputService;
    private readonly ComboEngine comboEngine = new();

    public MainWindow()
    {
        InitializeComponent();

        inputService = new GlobalInputService();
        inputService.InputReceived += InputService_InputReceived;

        Loaded += async (_, _) =>
        {
            await inputService.StartAsync();
        };

        NextInputText.Text = comboEngine.CurrentInput.ToString();
        CurrentInputText.Text = "-";
    }

    private void InputService_InputReceived(InputCode input)
    {
        Dispatcher.Invoke(() =>
        {
            // 현재 입력한 키 표시
            CurrentInputText.Text = input.ToString();

            // 맞는 입력이면 다음 키 변경
            if (comboEngine.Push(input))
            {
                NextInputText.Text = comboEngine.CurrentInput.ToString();
            }
        });
    }
}