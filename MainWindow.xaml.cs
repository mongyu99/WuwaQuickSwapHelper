using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WuwaQuickSwapHelper.Engine;
using WuwaQuickSwapHelper.Models;
using WuwaQuickSwapHelper.Services;
using System.IO;
using System.Net.Http;

namespace WuwaQuickSwapHelper;


public partial class MainWindow : Window
{

    private readonly RawInputService inputService;

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
        { InputCode.Swap1, new Combo { Name = "Character 1", Steps = new() {
            InputCode.Q, InputCode.E, InputCode.Swap1,
            InputCode.Q, InputCode.LeftClick, InputCode.Swap2,
            InputCode.E, InputCode.LeftClick, InputCode.Swap1 } } },
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
    // 한 줄의 칸들을 만듭니다. 줄 끝의 스왑 키(1~3)는 다음 줄로 넘어가는 키라 강조합니다.
    private List<NextInputItem> BuildLineItems(List<InputCode> line, bool isLoopLine)
    {
        return line.Select((step, i) => new NextInputItem
        {
            Text = step.DisplayName(),
            State = step is InputCode.Swap1 or InputCode.Swap2 or InputCode.Swap3 ? StepState.Current : StepState.Waiting,
            IsLoopStart = isLoopLine && i == 0
        }).ToList();
    }

    // 현재 줄을 가로로 표시합니다. 스왑 키(1~3)를 누르면 다음 줄로 넘어갑니다.
    private void InitializeDisplay()
    {
        displayItems.Clear();

        var hasLoop = comboEngine.CurrentCombo.HasLoop;

        displayItems.AddRange(BuildLineItems(comboEngine.GetLine(0), hasLoop && comboEngine.CurrentLine == comboEngine.LoopLine));

        NextInputList.ItemsSource = displayItems;
        NextInputList.Items.Refresh();

        UpdatePreviewLines();
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

        var settings = AppSettings.Load();

        ApplyTheme(settings.Theme);

        // 체크리스트 상태 복원 (InitializeDisplay 전에 설정)
        ShowNextLinesCheckBox.IsChecked = settings.ShowNextLines;

        InitializeDisplay();

        inputService = new RawInputService();
        inputService.InputReceived += InputService_InputReceived;
        inputService.MouseLeftPressed += InputService_MouseLeftPressed;

        // 창이 닫히면(작업 표시줄 등) 입력 수신을 정리하고 프로그램을 종료합니다.
        Closed += (_, _) =>
        {
            inputService.Dispose();
            Application.Current.Shutdown();
        };

        LocationChanged += (_, _) => RepositionCycleFlyout();
        SizeChanged += (_, _) => RepositionCycleFlyout();

        // Raw Input은 창 핸들이 필요하므로 창이 만들어진 뒤 시작합니다.
        Loaded += (_, _) => inputService.Start(this);
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

        // 1, 2, 3 스왑 키를 누르면 다음 줄로 넘어갑니다.
        await Dispatcher.InvokeAsync(() =>
        {
            comboEngine.NextLine();

            InitializeDisplay();
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

    // 사이클 목록 패널을 오른쪽에서 밀어 넣거나 빼냅니다.
    private void ToggleCyclePanel()
    {
        isCyclePanelOpen = !isCyclePanelOpen;

        if (isCyclePanelOpen)
        {
            RefreshCycleFileList();

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

        CycleListButton.Content = isCyclePanelOpen ? "\uE711" : "\uE8FD";
        CycleListButton.ToolTip = isCyclePanelOpen ? "사이클 목록 닫기" : "사이클 목록 열기";
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
        ImportView.Visibility = view == ImportView ? Visibility.Visible : Visibility.Collapsed;
        ReceiveView.Visibility = view == ReceiveView ? Visibility.Visible : Visibility.Collapsed;
        EditView.Visibility = view == EditView ? Visibility.Visible : Visibility.Collapsed;
        EditPickView.Visibility = view == EditPickView ? Visibility.Visible : Visibility.Collapsed;
        MenuButton.Visibility = view == StartMenu ? Visibility.Collapsed : Visibility.Visible;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) // 사이클 시작
    {
        currentCharacter = InputCode.Swap1;

        // 목록에서 고른 사이클이 있으면 그것으로, 없으면 기본 사이클로 시작합니다.
        comboEngine.SetCombo(activeCombo ?? characterCycles[currentCharacter]);

        SetRunning(true);

        InitializeDisplay();

        ShowView(CycleView);
    }

    // 고정: 클릭이 창을 통과하게 합니다. 고정 중에는 버튼을 누를 수 없으므로 F10으로 해제합니다.
    private void ToggleLock()
    {
        clickThrough = !clickThrough;

        overlayService.SetClickThrough(this, clickThrough);

        // 고정 중에는 핀 해제 아이콘 + 강조색
        LockButton.Content = clickThrough ? "\uE77A" : "\uE718";
        LockButton.ToolTip = clickThrough ? "고정됨 (핀을 클릭하거나 F10으로 해제)" : "고정 (클릭이 창을 통과합니다)";
        LockButton.Foreground = clickThrough ? ThemeService.Get("AccentBrush") : ThemeService.Get("SubTextBrush");
    }

    // 고정 중에는 창이 마우스를 받지 못하므로, 전역 마우스 입력으로 핀 버튼 위를 눌렀는지 확인해 해제합니다.
    private void InputService_MouseLeftPressed(int x, int y)
    {
        Dispatcher.InvokeAsync(() =>
        {
            if (!clickThrough || !LockButton.IsVisible)
                return;

            // 핀 버튼의 화면 좌표 (물리 픽셀)
            var topLeft = LockButton.PointToScreen(new Point(0, 0));
            var bottomRight = LockButton.PointToScreen(new Point(LockButton.ActualWidth, LockButton.ActualHeight));

            if (x >= topLeft.X && x <= bottomRight.X && y >= topLeft.Y && y <= bottomRight.Y)
            {
                ToggleLock();
            }
        });
    }

    // ───── 라이트 / 다크 모드 ─────

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyTheme(ThemeService.Current == ThemeService.Dark ? ThemeService.Light : ThemeService.Dark);

        var settings = AppSettings.Load();
        settings.Theme = ThemeService.Current;
        settings.Save();
    }

    private void ApplyTheme(string theme)
    {
        ThemeService.Apply(theme);

        bool dark = ThemeService.Current == ThemeService.Dark;

        ThemeButton.Content = dark ? "\uE708" : "\uE706";
        ThemeButton.ToolTip = dark ? "라이트 모드로" : "다크 모드로";

        // 코드에서 직접 넣은 색도 새 테마로 맞춥니다.
        LockButton.Foreground = clickThrough ? ThemeService.Get("AccentBrush") : ThemeService.Get("SubTextBrush");
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

        PauseButton.Content = running ? "\uE769" : "\uE768";
        PauseButton.ToolTip = running ? "멈춤" : "시작";
    }

    private void ImportMenuButton_Click(object sender, RoutedEventArgs e) // 코드 입력 화면
    {
        ImportTextBox.Clear();
        ImportStatusText.Text = "";

        ShowView(ImportView);
    }

    // 붙여넣은 JSON을 검사하고 통과하면 Data 폴더에 저장합니다.
    private void ImportSaveButton_Click(object sender, RoutedEventArgs e)
    {
        ImportStatusText.Text = SaveValidatedJson(ImportTextBox.Text);
    }

    // 내보내기: Data 폴더의 콤보 전체를 JSON 파일 하나로 저장합니다. (서명하지 않음)
    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var combos = new JsonComboLoader().LoadAll(JsonComboLoader.DataDirectory);

        if (combos.Count == 0)
        {
            MessageBox.Show(this, "내보낼 콤보가 없습니다.", "내보내기");
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"WuwaCombos_{DateTime.Now:yyyyMMdd}.json",
            Filter = "JSON 파일 (*.json)|*.json"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        File.WriteAllText(dialog.FileName, System.Text.Json.JsonSerializer.Serialize(combos, ComboValidator.WriteOptions));

        MessageBox.Show(this, $"콤보 {combos.Count}개를 저장했습니다.\n{dialog.FileName}", "내보내기");
    }

    private void ReceiveMenuButton_Click(object sender, RoutedEventArgs e) // 받기 화면
    {
        ApiKeyBox.Password = AppSettings.Load().ApiKey;
        ReceiveCodeBox.Clear();
        ReceiveStatusText.Text = "";

        ShowView(ReceiveView);
    }

    // 받기: API 키로 웹사이트에서 JSON을 받아 검사 후 저장합니다.
    private async void ReceiveButton_Click(object sender, RoutedEventArgs e)
    {
        var settings = AppSettings.Load();
        settings.ApiKey = ApiKeyBox.Password.Trim();
        settings.Save();

        ReceiveButton.IsEnabled = false;
        ReceiveStatusText.Text = "받는 중...";

        try
        {
            var json = await new ComboApiService().FetchAsync(settings, ReceiveCodeBox.Text);

            ReceiveStatusText.Text = SaveValidatedJson(json);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            ReceiveStatusText.Text = ex is TaskCanceledException ? "응답 시간이 초과되었습니다." : ex.Message;
        }
        finally
        {
            ReceiveButton.IsEnabled = true;
        }
    }

    // 화이트리스트 검사를 통과한 JSON만 Data 폴더에 저장하고 결과 메시지를 돌려줍니다.
    private static string SaveValidatedJson(string json)
    {
        if (!ComboValidator.TryParse(json, out var combos, out var error))
        {
            return $"저장 실패 - {error}";
        }

        var path = new JsonComboLoader().Save(JsonComboLoader.DataDirectory, combos);

        return $"콤보 {combos.Count}개 저장됨 ({Path.GetFileName(path)})";
    }

    // 5줄 한번에 보기: 현재 줄 아래에 이어질 4줄을 같은 크기로 미리 보여줍니다.
    // 마지막 줄 다음에는 반복 구간(없으면 첫 줄)부터 이어집니다.
    private const int PreviewLineCount = 4;

    private void UpdatePreviewLines()
    {
        var showNextLines = ShowNextLinesCheckBox.IsChecked == true;

        PreviewLines.Visibility = showNextLines ? Visibility.Visible : Visibility.Collapsed;

        if (!showNextLines)
            return;

        PreviewLines.ItemsSource = Enumerable.Range(1, PreviewLineCount)
            .Select(offset => BuildLineItems(comboEngine.GetLine(offset), false))
            .ToList();
    }

    private void ShowNextLinesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdatePreviewLines();

        var settings = AppSettings.Load();
        settings.ShowNextLines = ShowNextLinesCheckBox.IsChecked == true;
        settings.Save();
    }

    // 열 때마다 Data 폴더를 다시 읽어 새로 추가된 파일도 보여줍니다.
    private void RefreshCycleFileList()
    {
        var infos = new JsonComboLoader().LoadFileInfos(JsonComboLoader.DataDirectory);

        foreach (var info in infos)
        {
            info.IsActive = string.Equals(info.FilePath, activeCyclePath, StringComparison.OrdinalIgnoreCase);
        }

        CycleFileList.ItemsSource = infos;
    }

    // 목록에서 고른 사이클 (null = 기본 테스트 사이클)
    private Combo? activeCombo;
    private string? activeCyclePath;

    // 사이클 목록 항목 클릭 → 그 사이클로 교체합니다.
    // 진행 중이면 바로 새 사이클로 이어서 진행하고, 메뉴 화면이면 사이클 시작 시 사용됩니다.
    private void CycleFileListItem_Click(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not CycleFileInfo info)
            return;

        var combo = new JsonComboLoader().Load(info.FilePath).FirstOrDefault();

        if (combo == null)
        {
            MessageBox.Show(this, "잘못된 파일이라 불러올 수 없습니다.", "사이클 교체");
            return;
        }

        activeCombo = combo;
        activeCyclePath = info.FilePath;

        comboEngine.SetCombo(combo);

        InitializeDisplay();

        RefreshCycleFileList();
    }

    // ───── 사이클 편집 ─────

    // 편집 중인 파일 (null = 새로 만들기)
    private string? editingPath;

    // 파일에 콤보가 여러 개 있으면 첫 번째만 편집하고 나머지는 그대로 둡니다.
    private List<Combo> editingOtherCombos = new();

    private void EditMenuButton_Click(object sender, RoutedEventArgs e) // 사이클 편집: 파일 고르기 화면
    {
        var infos = new JsonComboLoader().LoadFileInfos(JsonComboLoader.DataDirectory);

        EditPickList.ItemsSource = infos;
        EditPickEmptyText.Visibility = infos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        ShowView(EditPickView);
    }

    private void CycleFileItem_Click(object sender, MouseButtonEventArgs e) // 파일 선택 → 편집
    {
        if ((sender as FrameworkElement)?.DataContext is CycleFileInfo info)
        {
            OpenEditor(info.FilePath);
        }
    }

    private void NewCycleButton_Click(object sender, RoutedEventArgs e)
    {
        OpenEditor(null);
    }

    private void OpenEditor(string? path)
    {
        var combos = path == null ? new List<Combo>() : new JsonComboLoader().Load(path);
        var combo = combos.FirstOrDefault() ?? new Combo();

        editingPath = path;
        editingOtherCombos = combos.Skip(1).ToList();

        EditTitleText.Text = path == null ? "새 사이클" : $"사이클 편집 - {Path.GetFileName(path)}";
        EditNameBox.Text = combo.Name;
        EditCharactersBox.Text = string.Join(", ", combo.Characters);
        EditAuthorBox.Text = combo.Author;
        EditStepsBox.Text = StepsToText(combo.Steps);
        EditLoopLineBox.Text = combo.HasLoop ? (new ComboEngine(combo).LoopLine + 1).ToString() : "0";
        EditStatusText.Text = path != null && combos.Count == 0 ? "잘못된 파일이라 내용을 불러오지 못했습니다." : "";

        SetRunning(false);
        ShowView(EditView);
    }

    // 단계 → 편집용 텍스트 (스왑 키 뒤에서 줄바꿈)
    private static string StepsToText(List<InputCode> steps)
    {
        return string.Join(Environment.NewLine, ComboEngine.SplitLines(steps)
            .Select(line => string.Join(" ", line.Select(StepToToken))));
    }

    private static string StepToToken(InputCode step) => step switch
    {
        InputCode.Swap1 => "1",
        InputCode.Swap2 => "2",
        InputCode.Swap3 => "3",
        InputCode.LeftClick => "평타",
        InputCode.Space => "SPACE",
        _ => step.ToString()
    };

    // 편집용 텍스트 → 단계. 모르는 키가 있으면 null과 오류 메시지를 돌려줍니다.
    private static List<InputCode>? ParseSteps(string text, out string error)
    {
        error = "";
        var steps = new List<InputCode>();

        foreach (var token in text.Split(new[] { ' ', '\t', '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries))
        {
            InputCode? step = token.ToUpperInvariant() switch
            {
                "Q" => InputCode.Q,
                "E" => InputCode.E,
                "R" => InputCode.R,
                "1" => InputCode.Swap1,
                "2" => InputCode.Swap2,
                "3" => InputCode.Swap3,
                "클릭" or "평" or "평타" or "CLICK" or "🖱" => InputCode.LeftClick,
                "SPACE" or "스페이스" or "점프" => InputCode.Space,
                _ => null
            };

            if (step == null)
            {
                error = $"알 수 없는 키: {token}";
                return null;
            }

            steps.Add(step.Value);
        }

        return steps;
    }

    // 편집 화면의 내용으로 콤보를 만들고 화이트리스트 검사까지 통과시킵니다.
    private List<Combo>? BuildEditedCombos()
    {
        var steps = ParseSteps(EditStepsBox.Text, out var error);

        if (steps == null)
        {
            EditStatusText.Text = error;
            return null;
        }

        if (!int.TryParse(EditLoopLineBox.Text.Trim(), out var loopLine) || loopLine < 0)
        {
            EditStatusText.Text = "반복 시작 줄은 0 이상의 숫자여야 합니다.";
            return null;
        }

        // 반복 시작 줄 번호 → 그 줄의 첫 단계 위치
        var lines = ComboEngine.SplitLines(steps);

        if (loopLine > lines.Count)
        {
            EditStatusText.Text = $"반복 시작 줄은 {lines.Count} 이하여야 합니다.";
            return null;
        }

        var combo = new Combo
        {
            Name = EditNameBox.Text,
            Author = EditAuthorBox.Text,
            Characters = EditCharactersBox.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            Steps = steps,
            LoopStartIndex = loopLine == 0 ? -1 : lines.Take(loopLine - 1).Sum(line => line.Count)
        };

        var combos = new List<Combo> { combo };
        combos.AddRange(editingOtherCombos);

        // 붙여넣기 / 받기와 똑같은 검사기를 거칩니다.
        var json = System.Text.Json.JsonSerializer.Serialize(combos, ComboValidator.WriteOptions);

        if (!ComboValidator.TryParse(json, out var validated, out error))
        {
            EditStatusText.Text = error;
            return null;
        }

        return validated;
    }

    private void EditSaveButton_Click(object sender, RoutedEventArgs e)
    {
        var combos = BuildEditedCombos();

        if (combos == null)
            return;

        try
        {
            var loader = new JsonComboLoader();

            if (editingPath == null)
            {
                editingPath = loader.Save(JsonComboLoader.DataDirectory, combos);
            }
            else
            {
                loader.Overwrite(editingPath, combos);
            }

            EditTitleText.Text = $"사이클 편집 - {Path.GetFileName(editingPath)}";
            EditStatusText.Text = "저장됨";

            if (isCyclePanelOpen)
                RefreshCycleFileList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            EditStatusText.Text = $"저장 실패 - {ex.Message}";
        }
    }

    private void EditStartButton_Click(object sender, RoutedEventArgs e) // 편집 중인 내용으로 바로 사이클 시작 (저장은 따로)
    {
        var combos = BuildEditedCombos();

        if (combos == null)
            return;

        activeCombo = combos[0];
        activeCyclePath = editingPath;

        comboEngine.SetCombo(combos[0]);

        SetRunning(true);

        InitializeDisplay();

        ShowView(CycleView);
    }

    private void MenuButton_Click(object sender, RoutedEventArgs e) // 사이클을 멈추고 시작 메뉴로 돌아갑니다.
    {
        SetRunning(false);

        SetCompact(false);

        ShowView(StartMenu);
    }

    // ───── 축소 모드 ─────

    private bool isCompact = false;

    // 축소 전 창 크기 (확대할 때 되돌립니다)
    private Size normalSize;

    private void CompactButton_Click(object sender, RoutedEventArgs e)
    {
        SetCompact(!isCompact);
    }

    private void SetCompact(bool compact)
    {
        if (isCompact == compact)
            return;

        isCompact = compact;

        var hidden = compact ? Visibility.Collapsed : Visibility.Visible;

        // 위쪽 버튼 / 목록 버튼 / 체크리스트를 숨깁니다.
        TopBar.Visibility = hidden;
        MenuButton.Visibility = hidden;
        CycleListButton.Visibility = hidden;
        OptionChecklist.Visibility = hidden;

        if (compact)
        {
            if (isCyclePanelOpen)
                ToggleCyclePanel();

            normalSize = new Size(Width, Height);

            // 내용 크기에 딱 맞춥니다.
            MinWidth = 0;
            MinHeight = 0;
            SizeToContent = SizeToContent.WidthAndHeight;
            ResizeMode = ResizeMode.NoResize;

            CompactButton.Content = "\uE740";
            CompactButton.ToolTip = "확대";
        }
        else
        {
            SizeToContent = SizeToContent.Manual;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            MinWidth = 260;
            MinHeight = 140;
            Width = normalSize.Width;
            Height = normalSize.Height;

            CompactButton.Content = "\uE73F";
            CompactButton.ToolTip = "축소";
        }
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
        OpacityButton.ToolTip = $"투명도: {label} (클릭: 불투명 → 반투명 → 투명)";
    }

}
