using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WuwaQuickSwapHelper.Engine;
using WuwaQuickSwapHelper.Models;
using WuwaQuickSwapHelper.Services;
using System.Windows.Media.Animation;

namespace WuwaQuickSwapHelper;

public partial class MainWindow : Window
{
    private readonly GlobalInputService inputService;
    private ComboEngine comboEngine;

    public MainWindow()
    {
        InitializeComponent();

        Topmost = true;

        var loader = new JsonComboLoader();

        var combos = loader.Load(
            "Data/combos.json"
        );

        comboEngine = new ComboEngine(
            combos[0].Steps
        );


        inputService = new GlobalInputService();

        inputService.InputReceived += InputService_InputReceived;


        Loaded += async (_, _) =>
        {
            RefreshNextInputs();

            await inputService.StartAsync();
        };
    }

    private async void InputService_InputReceived(InputCode input)
    {
        bool success = false;


        await Dispatcher.InvokeAsync(() =>
        {
            CurrentInputText.Text = input.ToString();

            success = comboEngine.Push(input);


            if (success)
            {
                CurrentInputText.Foreground = Brushes.White;

                RefreshNextInputs();
            }
            else
            {
                CurrentInputText.Foreground = Brushes.Red;
            }
        });


        if (!success)
        {
            await ShakeWindow();


            await Dispatcher.InvokeAsync(() =>
            {
                CurrentInputText.Foreground = Brushes.White;
            });
        }
    }

    private void RefreshNextInputs()
    {
        NextInputList.Items.Clear();

        var nextInputs = comboEngine.GetNextInputs(5);

        foreach (var input in nextInputs)
        {
            NextInputList.Items.Add(input.ToString());
        }
    }

    private void  qe(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private async Task ShakeWindow() // 실패 시 화면이 흔들립니다
    {
        await Dispatcher.InvokeAsync(() =>
        {
            var animation = new DoubleAnimationUsingKeyFrames();

            animation.KeyFrames.Add(
                new DiscreteDoubleKeyFrame(
                    -10,
                    KeyTime.FromTimeSpan(
                        TimeSpan.FromMilliseconds(50)
                    )
                )
            );

            animation.KeyFrames.Add(
                new DiscreteDoubleKeyFrame(
                    10,
                    KeyTime.FromTimeSpan(
                        TimeSpan.FromMilliseconds(100)
                    )
                )
            );

            animation.KeyFrames.Add(
                new DiscreteDoubleKeyFrame(
                    0,
                    KeyTime.FromTimeSpan(
                        TimeSpan.FromMilliseconds(150)
                    )
                )
            );


            ShakeTransform.BeginAnimation(
                TranslateTransform.XProperty,
                animation
            );
        });


        await Task.Delay(150);
    }
}