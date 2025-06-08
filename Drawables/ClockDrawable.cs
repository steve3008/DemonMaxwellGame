using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DemonMaxwellGame;

namespace DemonMaxwellGame.Drawables;

public class ClockDrawable : IDrawable
{
    int _iTheTime = 0;
    int _iLowTimeWarningTime = 6;
    const int _iClockMaxTime = 60;
    Vect2 _vClockFullSize = new Vect2(152.0, 152.0);
    Vect2 _vClockCentreFullSize = new Vect2(76.0, 76.0);
    const double _fHandLenFullSize = 62.0;
    const double _fClockRadiusFullSize = 70.0;
    const double _fClockCentreRadiusFullSize = 10.0;

    public ClockDrawable()
    {
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        Vect2 vScale = new Vect2(dirtyRect.Width, dirtyRect.Height) / _vClockFullSize;
        int angle = (630 - _iTheTime * 360 / _iClockMaxTime) % 360;
        Vect2 vHandStart = _vClockCentreFullSize * vScale;
        Vect2 vHandEnd = vHandStart + ((_fHandLenFullSize * DrawFuncs._trigtable[angle]) * vScale);
        if (_iTheTime <= _iLowTimeWarningTime && _iTheTime % 2 == 0)
        {
            canvas.StrokeSize = 3.0f;// (float)vScale.X / 30.0f;
            canvas.StrokeColor = Colors.Red;
            canvas.DrawCircle(vHandStart, _fClockRadiusFullSize * vScale.X);
            canvas.FillColor = Colors.Red;
            canvas.FillCircle(vHandStart, _fClockCentreRadiusFullSize * vScale.X);
        }
        else
        {
            canvas.StrokeSize = 2.0f;// (float)vScale.X / 30.0f;
            canvas.StrokeColor = Colors.Black;
        }
        canvas.DrawLine(vHandStart, vHandEnd);
    }

    public void OnTouch(Point? pntTouched)
    {
    }

    public int TheTime
    {
        get { return _iTheTime;  }
        set
        {
            _iTheTime = value;
        }
    }
}

