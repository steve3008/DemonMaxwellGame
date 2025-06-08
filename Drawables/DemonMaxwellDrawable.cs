using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DemonMaxwellGame;

namespace DemonMaxwellGame.Drawables;

public class DemonMaxwellDrawable : IDrawable
{
    RectF _Rect = new RectF(0, 0, 0, 0);
    private bool _bAspectRatioFixed = false;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (MainPage._Game == null)
            return;
        if (!_bAspectRatioFixed)
        {
            _Rect = MainPage._This.FixAspectRatio();
            _bAspectRatioFixed = true;
        }
        MainPage._Game.DrawCurrentFrame(canvas, _Rect);
    }

    public int NumVisibleBalls
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.NumVisibleBalls; }
    }

    public int NumHomeBalls
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.NumHomeBalls; }
    }
    public int TheTime
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.TheTime; }
    }
    public int TotalScore
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.TotalScore; }
    }
    public int TargetScore
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.TargetScore; }
    }
    public int AnimationFrame
    {
        get { return MainPage._Game == null ? 0 : MainPage._Game.AnimationFrame; }
    }
    public void StartLevel(int iLevel)
    {
        if (MainPage._Game != null)
            MainPage._Game.StartLevel(iLevel);
    }

    public void IncrementAnimationFrame()
    {
        if (MainPage._Game == null)
            return;
        MainPage._Game.IncrementAnimationFrame();
    }

    public bool GameIsRunning
    {
        get
        {
            return MainPage._Game != null && MainPage._Game.IsRunning;
        }
        set
        {
            if (MainPage._Game != null)
                MainPage._Game.IsRunning = value;
        }
    }

    public void OnTapped(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTapped((PointF)pntTouched);
    }

    public void OnTouchDown(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTouchDown((PointF)pntTouched);
    }

    public void OnTouchMove(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTouchMove((PointF)pntTouched);
    }

    public void OnTouchUp(Point? pntTouched)
    {
        if (MainPage._Game == null || pntTouched == null)
            return;
        MainPage._Game.OnTouchUp((PointF)pntTouched);
    }

    public void OnTouchClock()
    {
        if (MainPage._Game == null)
            return;
        MainPage._Game.OnTouchClock();
    }
}

