using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DemonMaxwellGame;

namespace DemonMaxwellGame.Drawables;

public class ScoreDrawable : IDrawable
{
    private const float _fDigWid = 72, _fDigHalfHei = 46;
    private int _iNumDigs;
    private int _iMaxValue;
    private bool _bGreyed;
    // Digits to be drawn to the canvas (in 2 halves)
    private Microsoft.Maui.Graphics.IImage[,] _DigitImages;
    // Partially flipped digits to be drawn to the canvas (in 2 halves)
    private Microsoft.Maui.Graphics.IImage?[,] _DigitImages2;
    // Digits from 0 to 9 to be drawn to the canvas (in 2 halves, flat and partially flipped)
    private Microsoft.Maui.Graphics.IImage[,] _DigitPartImages;
    // Half a digit, flipped to 90 degrees, drawn over the bottom half
    private Microsoft.Maui.Graphics.IImage? _DigitPartImageMiddle;

    private int _iValue = 0;
    private int[] _iValueDigits;
    private int[] _iValueDigitsPrev;
    private int[] _iValueDigitsFlipStage;

    public ScoreDrawable(int iNumDigs, bool bGreyed = false)
    {
        _iNumDigs = iNumDigs;
        _bGreyed = bGreyed;
        _iMaxValue = DrawFuncs.Power(10, _iNumDigs) - 1;
        _DigitImages = new Microsoft.Maui.Graphics.IImage[_iNumDigs, 2];
        _DigitImages2 = new Microsoft.Maui.Graphics.IImage[_iNumDigs, 2];
        _DigitPartImages = new Microsoft.Maui.Graphics.IImage[10, 4];
        _DigitPartImageMiddle = null;
        _iValueDigits = new int[_iNumDigs];
        _iValueDigitsPrev = new int[_iNumDigs];
        _iValueDigitsFlipStage = new int[_iNumDigs];
        PrepareImages().Wait();
        _bGreyed = bGreyed;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width / (float)_iNumDigs;
        float h = dirtyRect.Height * 0.5f;
        for (int i = 0; i < _iNumDigs; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                canvas.DrawImage(_DigitImages[i, j], i * w, j * h, w, h);
                if (_DigitImages2[i, j] != null)
                    canvas.DrawImage(_DigitImages2[i, j], i * w, j * h, w, h);
            }
        }
        // TODO: Draw a semi-transparent layer over the digits to indicate read-only if applicable.
        if (_bGreyed)
        {
            canvas.FillColor = Color.FromRgba(192, 192, 255, 128);
            canvas.FillRectangle(dirtyRect);
        }
    }
    private async Task PrepareImages()
    {
        for (int i = 0; i < 10; i++)
        {
            string d = i.ToString();
            _DigitPartImages[i, 0] = await DemMaxGame.LoadImageFromRes("d0top_" + d + ".png");
            _DigitPartImages[i, 1] = await DemMaxGame.LoadImageFromRes("d0bot_" + d + ".png");
            _DigitPartImages[i, 2] = await DemMaxGame.LoadImageFromRes("d1top_" + d + ".png");
            _DigitPartImages[i, 3] = await DemMaxGame.LoadImageFromRes("d1bot_" + d + ".png");
        }
        _DigitPartImageMiddle = await DemMaxGame.LoadImageFromRes("d1bot_middle.png");
        for (int i = 0; i < _iNumDigs; i++)
        {
            _iValueDigits[i] = 0;
        }
        SetDigitsFromValue();
        SetDigitImagesFromValueDigits();
    }

    private void SetDigitsFromValue()
    {
        int n = 1;
        for (int i = _iNumDigs - 1; i >= 0; i--)
        {
            _iValueDigitsFlipStage[i] = 0;
            _iValueDigitsPrev[i] = _iValueDigits[i];
            _iValueDigits[i] = (_iValue % (n * 10)) / n;
            if (_iValueDigitsPrev[i] > _iValueDigits[i])
                _iValueDigitsPrev[i]++;
            n *= 10;
        }
    }

    private void SetDigitImagesFromValueDigits()
    {
        for (int i = 0; i < _iNumDigs; i++)
        {
            if (_iValueDigitsPrev[i] == _iValueDigits[i])
            {
                for (int j = 0; j < 2; j++)
                {
                    _DigitImages[i, j] = _DigitPartImages[_iValueDigits[i], j];
                    _DigitImages2[i, j] = null;
                }
            }
            else
            {
                int iBot, iTop;
                if (_iValueDigitsPrev[i] < _iValueDigits[i])
                {
                    iBot = _iValueDigitsPrev[i] + 1;
                    iTop = _iValueDigitsPrev[i];
                }
                else
                {
                    iBot = _iValueDigitsPrev[i];
                    iTop = _iValueDigitsPrev[i] - 1;
                }
                switch(_iValueDigitsFlipStage[i])
                {
                    case 0:
                        _DigitImages[i, 0] = _DigitPartImages[iTop, 0];
                        _DigitImages[i, 1] = _DigitPartImages[iTop, 1];
                        _DigitImages2[i, 0] = _DigitImages2[i, 1] = null;
                        break;
                    case 1:
                        _DigitImages[i, 0] = _DigitPartImages[iTop, 0];
                        _DigitImages[i, 1] = _DigitPartImages[iBot, 1];
                        _DigitImages2[i, 0] = null;
                        _DigitImages2[i, 1] = _DigitPartImages[iTop, 3];
                        break;
                    case 2:
                        _DigitImages[i, 0] = _DigitPartImages[iTop, 0];
                        _DigitImages[i, 1] = _DigitPartImages[iBot, 1];
                        _DigitImages2[i, 0] = null;
                        _DigitImages2[i, 1] = _DigitPartImageMiddle;
                        break;
                    case 3:
                        _DigitImages[i, 0] = _DigitPartImages[iTop, 0];
                        _DigitImages[i, 1] = _DigitPartImages[iBot, 1];
                        _DigitImages2[i, 0] = _DigitPartImages[iBot, 2];
                        _DigitImages2[i, 1] = null;
                        break;
                }
            }
        }
    }

    public int TheValue
    {
        get
        {
            return _iValue;
        }
        set
        {
            if (_iValue != value)
            {
                _iValue = value;
                if (_iValue < 0)
                    _iValue = 0;
                if (_iValue > _iMaxValue)
                    _iValue = _iMaxValue;
                SetDigitsFromValue();
            }
        }
    }

    public bool Greyed
    {
        get { return _bGreyed;  }
        set { _bGreyed = value; }
    }

    public bool IncrementAnimationFrame()
    {
        bool bRet = false;
        for (int i = 0; i < _iNumDigs; i++)
        {
            if(_iValueDigitsPrev[i] != _iValueDigits[i])
            {
                if (_iValueDigitsPrev[i] < _iValueDigits[i])
                {
                    _iValueDigitsFlipStage[i]++;
                    if(_iValueDigitsFlipStage[i] > 3)
                    {
                        _iValueDigitsFlipStage[i] = 0;
                        _iValueDigitsPrev[i]++;
                    }
                }
                else
                {
                    _iValueDigitsFlipStage[i]--;
                    if (_iValueDigitsFlipStage[i] < 0)
                    {
                        _iValueDigitsFlipStage[i] = 3;
                        _iValueDigitsPrev[i]--;
                    }
                }
                SetDigitImagesFromValueDigits();
                bRet = true;
            }
        }
        return bRet;
    }

    public void OnTouch(Point? pntTouched)
    {
    }
}

