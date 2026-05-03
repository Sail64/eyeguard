using System.Windows.Threading;

namespace EyeGuard;

public enum TimerState
{
    Working,
    Warning,
    Break
}

public class TimerService
{
    private readonly DispatcherTimer _timer;
    private TimerState _state;
    private int _remainingSeconds;
    private readonly Config _config;

    public event EventHandler<TimerState>? StateChanged;
    public event EventHandler<int>? Tick;
    public event EventHandler? WarningStarted;
    public event EventHandler? BreakStarted;
    public event EventHandler? BreakEnded;

    public TimerState CurrentState => _state;
    public int RemainingSeconds => _remainingSeconds;

    public TimerService(Config config)
    {
        _config = config;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;
        _state = TimerState.Working;
        _remainingSeconds = config.WorkDurationMinutes * 60;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void ResetToWork()
    {
        _state = TimerState.Working;
        _remainingSeconds = _config.WorkDurationMinutes * 60;
        StateChanged?.Invoke(this, _state);
    }

    public void ForceBreak()
    {
        EnterBreak();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        Tick?.Invoke(this, _remainingSeconds);

        switch (_state)
        {
            case TimerState.Working:
                if (_remainingSeconds <= _config.WarningSeconds)
                {
                    EnterWarning();
                }
                break;

            case TimerState.Warning:
                if (_remainingSeconds <= 0)
                {
                    EnterBreak();
                }
                break;

            case TimerState.Break:
                if (_remainingSeconds <= 0)
                {
                    ExitBreak();
                }
                break;
        }
    }

    private void EnterWarning()
    {
        _state = TimerState.Warning;
        _remainingSeconds = _config.WarningSeconds;
        StateChanged?.Invoke(this, _state);
        WarningStarted?.Invoke(this, EventArgs.Empty);
    }

    private void EnterBreak()
    {
        _state = TimerState.Break;
        _remainingSeconds = _config.BreakDurationMinutes * 60;
        StateChanged?.Invoke(this, _state);
        BreakStarted?.Invoke(this, EventArgs.Empty);
    }

    private void ExitBreak()
    {
        BreakEnded?.Invoke(this, EventArgs.Empty);
        ResetToWork();
    }
}
