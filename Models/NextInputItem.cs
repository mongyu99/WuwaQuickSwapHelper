using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WuwaQuickSwapHelper.Models;

public class NextInputItem : INotifyPropertyChanged
{
    private string text = "";

    private StepState state = StepState.Waiting;

    public string Text
    {
        get => text;
        set
        {
            text = value;
            OnPropertyChanged();
        }
    }

    public StepState State
    {
        get => state;
        set
        {
            state = value;
            OnPropertyChanged();
        }
    }

    // 반복 시작 지점 표시
    public bool IsLoopStart { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}