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

    // 창 투명도 단계 (불투명 / 반투명 / 투명)
    private static readonly (double Opacity, string Label)[] opacityLevels =
    {
        (1.0, "불투명"),
        (0.65, "반투명"),
        (0.35, "투명"),
    };
    private int opacityLevelIndex = 0;

    // 초기 구동 호출
    private void InitializeDisplay()
    {
        displayItems.Clear();

        var combo = comboEngine.CurrentCombo;
        int startIndex = comboEngine.CurrentIndex;

        for (int i = 0; i < combo.Steps.Count; i++)
        {
            displayItems.Add(new NextInputItem
            {
                Text = combo.Steps[i].DisplayName(),
                // 반복으로 건너뛴 앞 단계는 완료 상태로 표시합니다.
                State = i < startIndex ? StepState.Success : StepState.Waiting,
                IsLoopStart = combo.HasLoop && i == combo.LoopStartIndex
            });
        }

        if (startIndex < displayItems.Count)
        {
            displayItems[startIndex].State = StepState.Current;
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

        // 창이 닫히면(작업 표시줄 등) 전역 훅을 정리해 프로세스가 남지 않게 합니다.
        Closed += (_, _) =>
        {
            inputService.Dispose();
            Application.Current.Shutdown();
        };

        LocationChanged += (_, _) => RepositionCycleFlyout();
        SizeChanged += (_, _) => RepositionCycleFlyout();

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
            await Dispatcher.InvokeAsync(ToggleLock);

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

        // 1, 2, 3 이외의 키는 무시합니다.
        if (input is not (InputCode.Swap1 or InputCode.Swap2 or InputCode.Swap3))
        {
            return;
        }

        // 시작 상태가 아닌 경우 입력을 받지 않습니다.
        if (!isRunning)
        {
            return;
        }

        // 1, 2, 3 중 아무 키나 누르면 사이클의 다음 단계로 넘어갑니다.
        await Dispatcher.InvokeAsync(async () =>
        {
            var result = comboEngine.Advance();

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

                    comboEngine.Restart();

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
        // F10은 전역 입력(InputService_InputReceived)에서 처리합니다.
        // 여기서도 처리하면 창에 포커스가 있을 때 두 번 전환되어 상쇄됩니다.
        await Task.CompletedTask;
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

    // 사이클 목록 패널을 오른쪽에서 밀어 넣거나 빼냅니다.
    private void ToggleCyclePanel()
    {
        isCyclePanelOpen = !isCyclePanelOpen;

        if (isCyclePanelOpen)
        {
            // 열 때마다 Data 폴더를 다시 읽어 새로 추가된 파일도 보여줍니다.
            CycleFileList.ItemsSource = new JsonComboLoader().LoadFileInfos(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));

            CycleFlyoutPopup.IsOpen = true;
        }

        // 창 뒤에서 오른쪽으로 슉 나왔다가, 닫을 때 다시 창 뒤로 슉 들어갑니다.
        var slide = new DoubleAnimation
        {
            To = isCyclePanelOpen ? 0 : -(CycleFlyout.Width + 10),
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = new CubicEase { EasingMode = isCyclePanelOpen ? EasingMode.EaseOut : EasingMode.EaseIn }
        };

        if (!isCyclePanelOpen)
        {
            slide.Completed += (_, _) =>
            {
                if (!isCyclePanelOpen)
                    CycleFlyoutPopup.IsOpen = false;
            };
        }

        CycleFlyoutTransform.BeginAnimation(TranslateTransform.XProperty, slide);

        CycleListButton.Content = isCyclePanelOpen ? "✕ 목록" : "☰ 목록";
    }

    // 창을 옮기거나 크기를 바꾸면 목록 팝업도 따라가게 합니다.
    private void RepositionCycleFlyout()
    {
        if (!CycleFlyoutPopup.IsOpen)
            return;

        CycleFlyoutPopup.HorizontalOffset += 0.1;
        CycleFlyoutPopup.HorizontalOffset -= 0.1;
    }

    private void CycleListButton_Click(object sender, RoutedEventArgs e) // 사이클 목록 버튼 클릭 이벤트
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

        SetRunning(true);

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

    // 시작 메뉴 / 진행 화면 중 하나만 보여줍니다.
    private void ShowView(UIElement view)
    {
        StartMenu.Visibility = view == StartMenu ? Visibility.Visible : Visibility.Collapsed;
        CycleView.Visibility = view == CycleView ? Visibility.Visible : Visibility.Collapsed;
        MenuButton.Visibility = view == StartMenu ? Visibility.Collapsed : Visibility.Visible;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) // 사이클 시작
    {
        currentCharacter = InputCode.Swap1;

        comboEngine.SetCombo(characterCycles[currentCharacter]);

        SetRunning(true);

        InitializeDisplay();

        ShowView(CycleView);
    }

    // 고정: 클릭이 창을 통과하게 합니다. 고정 중에는 버튼을 누를 수 없으므로 F10으로 해제합니다.
    private void ToggleLock()
    {
        clickThrough = !clickThrough;

        overlayService.SetClickThrough(this, clickThrough);

        LockButton.Content = clickThrough ? "고정됨 (F10)" : "고정";
    }

    private void LockButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleLock();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) // 프로그램 종료
    {
        Close();
    }

    // 사이클 진행 상태를 바꾸고 멈춤/시작 버튼 표시를 맞춥니다.
    private void SetRunning(bool running)
    {
        isRunning = running;

        PauseButton.Content = running ? "⏸ 멈춤" : "▶ 시작";
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e) // 사이클을 멈추고 시작 메뉴로 돌아갑니다.
    {
        SetRunning(false);

        ShowView(StartMenu);
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e) // 사이클 멈춤 / 다시 시작
    {
        SetRunning(!isRunning);
    }

    private void OpacityButton_Click(object sender, RoutedEventArgs e) // 창 투명도 전환
    {
        opacityLevelIndex = (opacityLevelIndex + 1) % opacityLevels.Length;

        var (opacity, label) = opacityLevels[opacityLevelIndex];

        // 배경과 버튼을 포함한 창 전체에 적용합니다.
        Opacity = opacity;
        OpacityButton.Content = label;
    }

}
