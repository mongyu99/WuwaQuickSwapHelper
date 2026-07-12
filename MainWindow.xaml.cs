using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WuwaQuickSwapHelper.Engine;
using WuwaQuickSwapHelper.Models;
using WuwaQuickSwapHelper.Services;
using System.Runtime.InteropServices;

namespace WuwaQuickSwapHelper;


public partial class MainWindow : Window
{

    private readonly GlobalInputService inputService;

    private ComboEngine comboEngine;

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

    public MainWindow()
    {
        InitializeComponent();


        // JSON 콤보 로딩
        var loader = new JsonComboLoader();


        var path = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "combos.json"
        );


        var combos = loader.Load(path);


        comboEngine = new ComboEngine(
            combos[0].Steps
        );



        // 전역 입력 감지
        inputService = new GlobalInputService();


        inputService.InputReceived += InputService_InputReceived;



        Loaded += async (_, _) =>
        {
            MakeClickThrough();

            RefreshNextInputs();

            await inputService.StartAsync();
        };

    }

    private async void InputService_InputReceived(InputCode input)
    {
        bool success = false;


        await Dispatcher.InvokeAsync(() =>
        {
            success = comboEngine.Push(input);


            if (success)
            {
                RefreshNextInputs();
            }

        });


        if (!success)
        {
            await ShakeWindow();
        }
    }

    private void RefreshNextInputs()
    {

        NextInputList.Items.Clear();


        var inputs = comboEngine.GetNextInputs(5);



        for (int i = 0; i < inputs.Count; i++)
        {

            NextInputList.Items.Add(
                new NextInputItem
                {
                    Text = inputs[i].ToString(),

                    IsCurrent = i == 0
                }
            );

        }

    }

    private async Task ShakeWindow()
    {

        await Dispatcher.InvokeAsync(() =>
        {

            var animation =
                new System.Windows.Media.Animation.DoubleAnimationUsingKeyFrames();


            animation.KeyFrames.Add(
                new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(
                    -10,
                    KeyTime.FromTimeSpan(
                        TimeSpan.FromMilliseconds(50)
                    )
                )
            );


            animation.KeyFrames.Add(
                new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(
                    10,
                    KeyTime.FromTimeSpan(
                        TimeSpan.FromMilliseconds(100)
                    )
                )
            );


            animation.KeyFrames.Add(
                new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(
                    0,
                    KeyTime.FromTimeSpan(
                        TimeSpan.FromMilliseconds(150)
                    )
                )
            );


            ShakeTransform.BeginAnimation(
                System.Windows.Media.TranslateTransform.XProperty,
                animation
            );


        });


        await Task.Delay(150);

    }
    e
    private void MakeClickThrough()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this)
            .Handle;


        int extendedStyle = GetWindowLong(
            hwnd,
            GWL_EXSTYLE);


        SetWindowLong(
            hwnd,
            GWL_EXSTYLE,
            extendedStyle
            | WS_EX_TRANSPARENT
            | WS_EX_LAYERED);
    }

}