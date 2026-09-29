using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WuwaQuickSwapHelper.Engine;
using WuwaQuickSwapHelper.Models;
using WuwaQuickSwapHelper.Services;
using System.IO;

namespace WuwaQuickSwapHelper;


public partial class MainWindow : Window
{

    private readonly GlobalInputService inputService;

    private ComboEngine comboEngine;

    private const int GWL_EXSTYLE = -20;

    private const int WS_EX_TRANSPARENT = 0x20;

    private const int WS_EX_LAYERED = 0x80000;

    private readonly OverlayService overlayService = new();

    private bool clickThrough = false;

    private readonly List<NextInputItem> displayItems = new();

    // 사이클 목록 UI 제어용
    private bool isCyclePanelOpen = false;

    // 모든 사이클을 불러오고, 선택된 사이클에 대한 정보를 저장합니다.
    private List<Combo> comboList = new();
    private int currentComboIndex = 0;

    // 캐릭터별 사이클 (1, 2, 3 = 캐릭터 슬롯)
    // TODO: 테스트용 사이클입니다. 추후 캐릭터별 실제 사이클로 교체하세요.
    private readonly Dictionary<InputCode, Combo> characterCycles = new()
    {
        { InputCode.Swap1, new Combo { Name = "Character 1", Steps = new() { InputCode.Q } } },
        { InputCode.Swap2, new Combo { Name = "Character 2", Steps = new() { InputCode.E } } },
        { InputCode.Swap3, new Combo { Name = "Character 3", Steps = new() { InputCode.R } } },
    };

    private InputCode currentCharacter = InputCode.Swap1;

    // 사이클 진행 여부
    private bool isRunning = false;

    // 초기 구동 호출
    private void InitializeDisplay()
    {
        displayItems.Clear();

        foreach (var step in comboEngine.CurrentCombo.Steps)
        {
            displayItems.Add(new NextInputItem
            {
                Text = step.DisplayName(),
                State = StepState.Waiting
            });
        }

        if (displayItems.Count > 0)
        {
            displayItems[0].State = StepState.Current;
        }

        NextInputList.ItemsSource = displayItems;
        NextInputList.Items.Refresh();
    }

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

        var loader = new JsonComboLoader();

        var path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "combos.json");

        comboList = loader.Load(path);

        comboEngine = new ComboEngine(characterCycles[currentCharacter]);

        InitializeDisplay();

        inputService = new GlobalInputService();
        inputService.InputReceived += InputService_InputReceived;

        Loaded += async (_, _) =>
        {
            await inputService.StartAsync();
        };
    }

    private async void InputService_InputReceived(InputCode input)
    {

        System.Diagnostics.Debug.WriteLine($"Input : {input}");

        // F10 : 이동 모드 / 게임 모드 전환
        if (input == InputCode.F10)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                clickThrough = !clickThrough;

                overlayService.SetClickThrough(this, clickThrough);

                CurrentModeText.Text =
                    clickThrough ? "GAME MODE" : "MOVE MODE";

                System.Diagnostics.Debug.WriteLine(CurrentModeText.Text);
            });

            return;
        }

        // F9 : 현재 선택된 사이클 시작
        if (input == InputCode.F9)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StartCycle();
            });

            return;
        }

        // 1, 2, 3 : 캐릭터 스왑 -> 해당 캐릭터의 사이클로 변경
        if (input is InputCode.Swap1 or InputCode.Swap2 or InputCode.Swap3)
        {
            await Dispatcher.InvokeAsync(() => SwapCharacter(input));

            return;
        }

        // 시작 상태가 아닌 경우 입력을 받지 않습니다.
        if (!isRunning)
        {
            return;
        }

        await Dispatcher.InvokeAsync(async () =>
        {
            var result = comboEngine.Push(input);

            UpdateDisplay(result);

            switch (result.State)
            {
                case PushState.Success:
                    break;

                case PushState.Failed:

                    await ShakeWindow();

                    break;

                case PushState.Completed:

                    await Task.Delay(300);

                    comboEngine.Reset();

                    InitializeDisplay();

                    break;
            }
        });
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


            animation.KeyFrames.Add(new System.Windows.Media.Animation.DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150))));

            ShakeTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation );
        });


        await Task.Delay(150);

    }
    
    private void MakeClickThrough()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;

        int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
    }

    private void Window_MouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
    {
        if (!clickThrough && e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    // 창을 움직일지를 정합니다.
    private async void Window_KeyDown(
    object sender,
    KeyEventArgs e)
    {
        if (e.Key == Key.F10)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                clickThrough = !clickThrough;

                overlayService.SetClickThrough(this,clickThrough);
            });

            return;
        }
    }

    private void UpdateDisplay(PushResult result)
    {
        switch (result.State)
        {
            case PushState.Success:

                displayItems[result.Index].State = StepState.Success;

                if (result.Index + 1 < displayItems.Count)
                {
                    displayItems[result.Index + 1].State = StepState.Current;
                }

                break;


            case PushState.Failed:

                displayItems[result.Index].State = StepState.Failed;

                break;


            case PushState.Completed:

                displayItems[result.Index].State = StepState.Success;

                break;
        }

        NextInputList.Items.Refresh();
    }

    private void ToggleCyclePanel() // 사이클 목록 UI 제어
    {
        isCyclePanelOpen = !isCyclePanelOpen;

        if (isCyclePanelOpen)
        {
            CyclePanel.Height = 180;
            CycleButton.Content = "▲ Cycle";
        }
        else
        {
            CyclePanel.Height = 0;
            CycleButton.Content = "▼ Cycle";
        }
    }

    private void CycleButton_Click(object sender, RoutedEventArgs e) // 사이클 버튼 클릭 이벤트
    {
        ToggleCyclePanel();
    }

    private void ChangeCombo(int index)
    {
        currentComboIndex = index;

        comboEngine.SetCombo(comboList[index]);

        InitializeDisplay();
    }

    private void StartCycle() // 현재 선택된 사이클을 초기화합니다.
    {
        if (isRunning)
            return;

        isRunning = true;

        comboEngine.Reset();

        InitializeDisplay();
    }

    private void NextCombo()
    {
        currentComboIndex++;

        if (currentComboIndex >= comboList.Count)
        {
            currentComboIndex = 0;
        }
        ChangeCombo(currentComboIndex);
    }

    // 캐릭터(1, 2, 3)로 스왑하면 해당 캐릭터의 사이클로 변경합니다.
    private void SwapCharacter(InputCode swapKey)
    {
        if (!isRunning || !characterCycles.TryGetValue(swapKey, out var combo))
            return;

        currentCharacter = swapKey;

        comboEngine.SetCombo(combo);

        InitializeDisplay();
    }

    // 시작 메뉴 / 사이클 목록 / 진행 화면 중 하나만 보여줍니다.
    private void ShowView(UIElement view)
    {
        StartMenu.Visibility = view == StartMenu ? Visibility.Visible : Visibility.Collapsed;
        CycleListView.Visibility = view == CycleListView ? Visibility.Visible : Visibility.Collapsed;
        CycleView.Visibility = view == CycleView ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) // 사이클 시작
    {
        currentCharacter = InputCode.Swap1;

        comboEngine.SetCombo(characterCycles[currentCharacter]);

        isRunning = true;

        InitializeDisplay();

        ShowView(CycleView);
    }

    private void ListButton_Click(object sender, RoutedEventArgs e) // 사이클 목록
    {
        CharacterCycleList.ItemsSource = characterCycles.Select(pair =>
            $"{pair.Key.DisplayName()}  {pair.Value.Name} : " +
            string.Join(" → ", pair.Value.Steps.Select(step => step.DisplayName())));

        ShowView(CycleListView);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        ShowView(StartMenu);
    }
}
