using DemonMaxwellGame.Drawables;

namespace DemonMaxwellGame;


public partial class MainPage : ContentPage
{
    public static MainPage _This;
    public static DemMaxGame? _Game = null;

    private const double _fCorrectAspectRatio = 0.545206724216265;
    //private const double _fCorrectAspectRatio = 0.5;


    DemonMaxwellDrawable _DemonMaxwellDrawable;
    ScoreDrawable _particlesRemainingDrawable, _scoreDrawable, _targetScoreDrawable, _digitalTimeDrawable;
    ClockDrawable _clockDrawable;
    ScoreDrawable _totalScoreDrawable, _nextLevelDrawable;
    bool _bUpdateParticlesRemaining = true, _bUpdateScore = true, _bUpdateTargetScore = true, _bUpdateDigitalTime = true, _bUpdateClock = true;
    bool _bUpdateTotalScore = true, _bUpdateNextLevel = true;
    private IDispatcherTimer _timerMain;
    public static bool _bShowDebugInfo = false;
    public const int _iFramesPerSecond = 25;
    public static int _iNextLevel = 1;
    bool _bPausedPanelVisible = false;
    bool _bKillGameNextFrame = false;
    public static int _iTargetReachedSignOn = -1;
    public const int _iHelpStageMax = 50;
    public const int _iHelpStageNumHelpPages = 2;
    public static int _iHelpStage = 0; // >2 = Sign at top over scoreboard. 1 = help page 1. 2 = help page 2. 0 = no help.

    // Secret touch sequence that skips to the next level
    int _iSecretCount = 0;
    int _iSecretStage = 0;
    const int _iSecretTimeLimit = _iFramesPerSecond;
    const int _iSecretEndStage = 5;

    public MainPage()
    {
        InitializeComponent();
        _This = this;
        if (MainPage._Game == null)
            MainPage._Game = new DemMaxGame();

        DemonMaxwellGraphicsView.TouchStart += (sender, e) =>
        {
            OnTouchStartedMainGame(sender, e);
        };
        DemonMaxwellGraphicsView.TouchMove += (sender, e) =>
        {
            OnTouchMovedMainGame(sender, e);
        };
        DemonMaxwellGraphicsView.TouchEnd += (sender, e) =>
        {
            OnTouchEndedMainGame(sender, e);
        };

        // Setup the scoreboard
        _particlesRemainingDrawable = new ScoreDrawable(3);
        ParticlesRemainingGraphicsView.Drawable = _particlesRemainingDrawable;
        _scoreDrawable = new ScoreDrawable(3);
        ScoreGraphicsView.Drawable = _scoreDrawable;
        _targetScoreDrawable = new ScoreDrawable(3, true);
        TargetScoreGraphicsView.Drawable = _targetScoreDrawable;
        _digitalTimeDrawable = new ScoreDrawable(2);
        TimeGraphicsView.Drawable = _digitalTimeDrawable;
        _clockDrawable = new ClockDrawable();
        ClockGraphicsView.Drawable = _clockDrawable;

        // Setup the banner view
        _totalScoreDrawable = new ScoreDrawable(5);
        TotalScoreGraphicsView.Drawable = _totalScoreDrawable;
        _nextLevelDrawable = new ScoreDrawable(2);
        NextLevelGraphicsView.Drawable = _nextLevelDrawable;

        _DemonMaxwellDrawable = new DemonMaxwellDrawable();
        DemonMaxwellGraphicsView.Drawable = _DemonMaxwellDrawable;

        NextLevelImage.Source = "next_level.png";
        NextLevelGrid.IsVisible = true;

        _timerMain = Dispatcher.CreateTimer();
        _timerMain.IsRepeating = true;
        _timerMain.Interval = TimeSpan.FromSeconds(1.0 / (double)_iFramesPerSecond);
        _timerMain.Tick += TimerMain_Tick;
        //_timerMain?.Start();
    }

    public RectF FixAspectRatio()
    {
        // Ensure correct aspect ratio
        double wid = DemonMaxwellGraphicsView.Width;
        double hei = DemonMaxwellGraphicsView.Height;
        double fAspectRatioDiff = wid / hei - _fCorrectAspectRatio;
        if (Math.Abs(fAspectRatioDiff) > 0.0005)
        {
            if(fAspectRatioDiff > 0.0)
                DemonMaxwellGraphicsView.WidthRequest = wid = DemonMaxwellGraphicsView.Height * _fCorrectAspectRatio;
            else
                DemonMaxwellGraphicsView.HeightRequest = hei = DemonMaxwellGraphicsView.Width / _fCorrectAspectRatio;
            //DemonMaxwellGraphicsView.Invalidate();
        }
        RectF rect = new RectF(0.0f, 0.0f, (float)wid, (float)hei);
        if(_Game != null)
            _Game.Initialize(rect);
        _timerMain?.Start();
        return rect;
    }

    private void TimerMain_Tick(object sender, EventArgs e)
    {
        long t1 = 0;
        if (_bShowDebugInfo)
            t1 = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
        _DemonMaxwellDrawable.IncrementAnimationFrame();
        DemonMaxwellGraphicsView.Invalidate();

        // Update the scoreboard
        if (_Game != null && _Game.CurrentLevel == DemMaxGame._iTopLevel)
        {
            if (_bUpdateScore || _bUpdateTargetScore)
            {
                _scoreDrawable.TheValue = _Game.TotalScore / 1000;
                _targetScoreDrawable.TheValue = _Game.TotalScore % 1000;
                _bUpdateScore = false;
                _bUpdateTargetScore = false;
            }
        }
        else
        {
            if (_bUpdateParticlesRemaining)
            {
                _particlesRemainingDrawable.TheValue = _DemonMaxwellDrawable.NumVisibleBalls;
                _bUpdateParticlesRemaining = false;
            }
            if (_bUpdateScore)
            {
                int score = _DemonMaxwellDrawable.NumHomeBalls;
                _scoreDrawable.TheValue = score;
                if (score >= _Game.TargetScore && _Game.CurrentLevel != _Game.TopLevel)
                {
                    TargetReachedSignOn();
                }
                _bUpdateScore = false;
            }
            if (_bUpdateTargetScore)
            {
                _targetScoreDrawable.TheValue = _DemonMaxwellDrawable.TargetScore;
                _bUpdateTargetScore = false;
            }
        }
        if (_bUpdateDigitalTime)
        {
            _digitalTimeDrawable.TheValue = _DemonMaxwellDrawable.TheTime;
            _bUpdateDigitalTime = false;
        }
        if (_bUpdateClock)
        {
            _clockDrawable.TheTime = _DemonMaxwellDrawable.TheTime;
            _bUpdateClock = false;
            ClockGraphicsView.Invalidate();
        }

        // Update the banner
        if (_bUpdateTotalScore)
        {
            _totalScoreDrawable.TheValue = _DemonMaxwellDrawable.TotalScore;
            _bUpdateTotalScore = false;
        }
        if (_bUpdateNextLevel)
        {
            _nextLevelDrawable.TheValue = _iNextLevel;
            _bUpdateNextLevel = false;
        }
        if (_particlesRemainingDrawable.IncrementAnimationFrame())
            ParticlesRemainingGraphicsView.Invalidate();
        if (_scoreDrawable.IncrementAnimationFrame())
            ScoreGraphicsView.Invalidate();
        if (_targetScoreDrawable.IncrementAnimationFrame())
            TargetScoreGraphicsView.Invalidate();
        if (_digitalTimeDrawable.IncrementAnimationFrame())
            TimeGraphicsView.Invalidate();

        if (_totalScoreDrawable.IncrementAnimationFrame())
            TotalScoreGraphicsView.Invalidate();
        if (_nextLevelDrawable.IncrementAnimationFrame())
            NextLevelGraphicsView.Invalidate();

        if(_iTargetReachedSignOn > 0)
        {
            _iTargetReachedSignOn--;
            if (_iTargetReachedSignOn == 0)
            {
                NextLevelGrid.IsVisible = false;
            }
        }

        // Secret code
        if(_iSecretCount > 0)
        {
            if (_iSecretStage == _iSecretEndStage)
            {
                // Trigger next level
                _Game.TheTime = 0;
                _bUpdateDigitalTime = _bUpdateClock = true;
            }
            else
            {
                _iSecretCount--;
                if (_iSecretCount == 0)
                    _iSecretStage = 0;
            }
        }

        // Help stages
        if(_iHelpStage > _iHelpStageNumHelpPages)
        {
            if(_iHelpStage == _iHelpStageMax)
            {
                ScoreBoard.Source = "scoreboard_help.png";
                ParticlesRemainingGraphicsView.IsVisible = false;
                ScoreGraphicsView.IsVisible = false;
                TargetScoreGraphicsView.IsVisible = false;
            }
            _iHelpStage--;
            if (_iHelpStage == _iHelpStageNumHelpPages)
            {
                ScoreBoard.Source = "scoreboard.png";
                ParticlesRemainingGraphicsView.IsVisible = true;
                ScoreGraphicsView.IsVisible = true;
                TargetScoreGraphicsView.IsVisible = true;
                _iHelpStage = 0;
            }
        }

        // Kill game
        if (_bKillGameNextFrame)
        {
            //_Game.SaveSettingsToFile();
            Application.Current.Quit();
        }

        if (_bShowDebugInfo)
        {
            long t2 = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            DemMaxGame._iFrameTime = (int)(t2 - t1);
        }
    }

    public bool SecretEndStageReached(bool bReset)
    {
        if(_iSecretStage == _iSecretEndStage)
        {
            if (bReset)
            {
                _iSecretStage = 0;
                _iSecretCount = 0;
            }
            return true;
        }
        return false;
    }

    public void UpdateParticlesRemainingNextFrame()
    {
        _bUpdateParticlesRemaining = true;
    }
    public void UpdateTheScoreNextFrame()
    {
        _bUpdateScore = true;
    }
    public void UpdateTheTargetScoreNextFrame()
    {
        _bUpdateTargetScore = true;
    }
    public void UpdateTheDigitalTimeNextFrame()
    {
        _bUpdateDigitalTime = true;
    }
    public void UpdateTheClockNextFrame()
    {
        _bUpdateClock = true;
    }

    public void UpdateTheTotalScoreNextFrame()
    {
        _bUpdateTotalScore = true;
    }
    public void UpdateLevelNextFrame()
    {
        _bUpdateNextLevel = true;
    }

    // Helper method to convert Android coordinates to MAUI coordinates
    private Point GetScaledPoint(Vect2 p)
    {
        double density = Microsoft.Maui.Devices.DeviceDisplay.Current.MainDisplayInfo.Density;
        return p / density;
    }

    private void OnTouchStartedMainGame(object sender, TouchableGraphicsView.TouchPointEventArgs e)
    {
        _DemonMaxwellDrawable.OnTouchDown(GetScaledPoint(e.Pos));
    }
    private void OnTouchMovedMainGame(object sender, TouchableGraphicsView.TouchPointEventArgs e)
    {
        _DemonMaxwellDrawable.OnTouchMove(GetScaledPoint(e.Pos));
    }
    private void OnTouchEndedMainGame(object sender, TouchableGraphicsView.TouchPointEventArgs e)
    {
        _DemonMaxwellDrawable.OnTouchUp(GetScaledPoint(e.Pos));
    }
    private void OnTappedMainGame(object sender, TappedEventArgs e)
    {
        Point? pos = e.GetPosition(DemonMaxwellGraphicsView);
        if (_iHelpStage > 0 && _iHelpStage <= _iHelpStageNumHelpPages)
        {
            if (pos.Value.X / DemonMaxwellGraphicsView.Width > 0.7963 && 
                pos.Value.Y / DemonMaxwellGraphicsView.Height > 0.9394)
            {
                _iHelpStage++;
                if (_iHelpStage > _iHelpStageNumHelpPages)
                    _iHelpStage = 1;
            }
            else
            {
                _iHelpStage = 0;
                _DemonMaxwellDrawable.GameIsRunning = true;
            }
            return;
        }

        if (_Game != null && _Game.CurrentLevel == DemMaxGame._iTopLevel && _Game.TheTime < 0)
        {
            ScoreBoard.Source = "scoreboard.png";
            _targetScoreDrawable.Greyed = true;
            _DemonMaxwellDrawable.StartLevel(1);
            _DemonMaxwellDrawable.GameIsRunning = true;
            return;
        }

        _DemonMaxwellDrawable.OnTapped(pos);
    }

    private void OnTouchClock(object sender, TappedEventArgs e)
    {
        if (_iHelpStage > 0)
            return;
        if (_Game != null && _Game.CurrentLevel == DemMaxGame._iTopLevel)
            return;
        _DemonMaxwellDrawable.GameIsRunning = false;
        GamePaused();
        _DemonMaxwellDrawable.OnTouchClock();
    }

    // Secret touch combo: 0:1, 1:3, 2:1, 3:3, 4:2, where:
    // 1 = OnTouchParticlesRemaining
    // 2 = OnTouchCurrentScore
    // 3 = OnTouchTargetScore
    private void OnTouchParticlesRemaining(object sender, TappedEventArgs e)
    {
        IncrementHelpStage();

        if (_iSecretStage == 0)
        {
            _iSecretCount = _iSecretTimeLimit;
            _iSecretStage = 1;
        }
        else if (_iSecretStage == 2)
        {
            _iSecretCount = _iSecretTimeLimit;
            _iSecretStage = 3;
        }
        else
        {
            _iSecretStage = 0;
        }
    }

    private void OnTouchCurrentScore(object sender, TappedEventArgs e)
    {
        IncrementHelpStage();

        if (_iSecretStage == 4)
        {
            _iSecretStage = _iSecretEndStage;
        }
        else
        {
            _iSecretStage = 0;
        }
    }
    private void OnTouchTargetScore(object sender, TappedEventArgs e)
    {
        IncrementHelpStage();

        if (_iSecretStage == 1)
        {
            _iSecretStage = 2;
            _iSecretCount = _iSecretTimeLimit;
        }
        else if (_iSecretStage == 3)
        {
            _iSecretCount = _iSecretTimeLimit;
            _iSecretStage = 4;
        }
        else
        {
            _iSecretStage = 0;
        }
    }

    private void OnTouchScoreboard(object sender, TappedEventArgs e)
    {
        Point? pos = e.GetPosition(ScoreboardGrid);
        if (pos == null)
            return;
        if (pos.Value.X / ScoreboardGrid.Width < 0.6852)
        {
            IncrementHelpStage();
        }
        else
        {
            if (_iHelpStage > 0)
                return;
            if (_Game != null && _Game.CurrentLevel == DemMaxGame._iTopLevel)
                return;
            _DemonMaxwellDrawable.GameIsRunning = false;
            GamePaused();
            _DemonMaxwellDrawable.OnTouchClock();
        }
    }

    private void IncrementHelpStage()
    {
        if (_bPausedPanelVisible)
            return;
        _DemonMaxwellDrawable.GameIsRunning = false;
        if (_iHelpStage > _iHelpStageNumHelpPages)
        {
            ScoreBoard.Source = "scoreboard.png";
            ParticlesRemainingGraphicsView.IsVisible = true;
            ScoreGraphicsView.IsVisible = true;
            TargetScoreGraphicsView.IsVisible = true;
            _iHelpStage = 1;
        }
        else
        {
            _iHelpStage++;
            if (_iHelpStage > _iHelpStageNumHelpPages)
            {
                _iHelpStage = 0;
                _DemonMaxwellDrawable.GameIsRunning = true;
            }
        }
    }

    private void OnTouchNextLevelGrid(object sender, TappedEventArgs e)
    {
        if (_iTargetReachedSignOn > 0)
            return;
        if (_bPausedPanelVisible)
        {
            Point? pnt = e.GetPosition(NextLevelGrid);
            if (pnt != null)
            {
                if(pnt.Value.X > NextLevelGrid.Width / 2)
                {
                    if (pnt.Value.Y > NextLevelGrid.Height / 2)
                    {
                        // Continue Playing button touched
                    }
                    else
                    {
                        if (_Game.NumHomeBalls < _Game.TargetScore) // Only if the button is enabled because the target has been met
                            return;
                        // Finish Level button touched
                        _Game.TheTime = 0;
                        _bUpdateDigitalTime = _bUpdateClock = true;
                    }
                }
                else
                {
                    if (pnt.Value.Y > NextLevelGrid.Height / 2)
                    {
                        // Quit Game button touched
                        _bKillGameNextFrame = true;
                    }
                    else
                    {
                        // Restart Level button touched
                        _DemonMaxwellDrawable.StartLevel(_iNextLevel);
                    }
                }
            }
            _bPausedPanelVisible = false;
        }
        else
        {
            if(_iNextLevel == 1)
            {
                _iHelpStage = _iHelpStageMax;
            }
            ScoreBoard.Source = (_iNextLevel == DemMaxGame._iTopLevel) ? "scoreboard_final.png" : "scoreboard.png";
            _targetScoreDrawable.Greyed = (_iNextLevel != DemMaxGame._iTopLevel);
            _DemonMaxwellDrawable.StartLevel(_iNextLevel);
        }
        NextLevelGrid.IsVisible = false;
        _DemonMaxwellDrawable.GameIsRunning = true;
    }

    public void EndOfLevel()
    {
        _bUpdateNextLevel = true;
        _bUpdateTotalScore = true;
        NextLevelImage.Source = "next_level.png";
        NextLevelGrid.IsVisible = true;
        TotalScoreGraphicsView.IsVisible = true;
        NextLevelGraphicsView.IsVisible = true;
    }

    public void GameOver()
    {
        _bUpdateNextLevel = true;
        _bUpdateTotalScore = true;
        NextLevelImage.Source = "game_over.png";
        NextLevelGrid.IsVisible = true;
        TotalScoreGraphicsView.IsVisible = true;
        NextLevelGraphicsView.IsVisible = false;
    }

    public void GamePaused()
    {
        NextLevelImage.Source = (_Game.NumHomeBalls < _Game.TargetScore) ? "paused_greyed.png" : "paused.png";
        NextLevelGrid.IsVisible = true;
        TotalScoreGraphicsView.IsVisible = false;
        NextLevelGraphicsView.IsVisible = false;
        _bPausedPanelVisible = true;
    }

    public void TargetReachedSignOn()
    {
        if (_iTargetReachedSignOn >= 0)
            return;
        _iTargetReachedSignOn = 25;
        NextLevelImage.Source = "target_reached.png";
        NextLevelGrid.IsVisible = true;
        TotalScoreGraphicsView.IsVisible = false;
        NextLevelGraphicsView.IsVisible = false;
    }
}
