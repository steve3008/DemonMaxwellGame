using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace DemonMaxwellGame
{
    abstract public class WallBase
    {
        private Color _Colour1 = Colors.Brown, _Colour2 = Colors.Blue;
        private static Color[] _Colour1List = { Colors.Red,  Colors.Green,  Colors.Magenta, Colors.Black, Colors.Yellow, Colors.Green };
        private static Color[] _Colour2List = { Colors.Blue, Colors.Blue,   Colors.Cyan,    Colors.White, Colors.Blue,   Colors.White };
        private int _iColourPairIndex = -1;
        public bool _bFiniteStart = true, _bFiniteEnd = true;
        public bool _bBounceOffCorners = true;
        public double _fBounciness;
        protected bool _bRemovable = false;
        protected bool _bVisible = true;
        public int _iLinkedWallIndex = -1, _iLinkedPrevWallIndex = -1;

        public WallBase()
        {
            _bFiniteStart = true;
            _bFiniteEnd = true;
            _bBounceOffCorners = true;
            _fBounciness = 1.0;
        }

        public bool IsVisible
        {
            get { return _bVisible; }
            set { _bVisible = value; }
        }

        public bool RecSetColourPair(int index, bool bBackward = true, bool bForward = true)
        {
            if (!_bRemovable || _iColourPairIndex >= 0)
                return false;
            _iColourPairIndex = index % _Colour1List.Length;
            _Colour1 = _Colour1List[_iColourPairIndex];
            _Colour2 = _Colour2List[_iColourPairIndex];
            if(bBackward && _iLinkedPrevWallIndex >= 0)
                MainPage._Game._Walls[_iLinkedPrevWallIndex].RecSetColourPair(_iColourPairIndex, true, false);
            if (bForward && _iLinkedWallIndex >= 0)
                MainPage._Game._Walls[_iLinkedWallIndex].RecSetColourPair(_iColourPairIndex, false, true);
            return true;
        }

        protected Color DrawColour
        {
            get
            {
                if (MainPage._Game == null)
                    return _Colour1;
                if (_bRemovable)
                {
                    return DrawFuncs.ColorInterpolatedSinusoidal(_Colour1, _Colour2, MainPage._Game.AnimationFrame % 50, 50);
                }
                return _Colour1;
            }
        }

        public abstract void Increment();
        public abstract bool CheckCollision(Ball2 ball);
        public abstract void Draw(ICanvas canvas);
        public abstract bool IntersectsCircle(Vect2 centre, double rad);
        public abstract bool IntersectsLine(Vect2 v1, Vect2 v2);
        public bool OnTouch(Vect2 touch)
        {
            if (IntersectsCircle(touch, DemMaxGame._fTouchRadius))
            {
                if (_bRemovable && RecGateCanClose())
                {
                    RecToggleVisibility();
                    return true;
                }
            }
            return false;
        }
        private void RecToggleVisibility(bool bBackward = true, bool bForward = true)
        {
            _bVisible = !_bVisible;
            if (_bVisible)
            {
                if (DemMaxGame._Balls != null)
                {
                    foreach (Ball2 ball in DemMaxGame._Balls)
                    {
                        if (ball.Collidable && IntersectsCircle(ball.Pos, ball.Radius))
                        {
                            ball.ExplodeMe();
                        }
                    }
                }
            }
            if(bForward && _iLinkedWallIndex >= 0)
            {
                MainPage._Game._Walls[_iLinkedWallIndex].RecToggleVisibility(false, true);
            }
            if (bBackward && _iLinkedPrevWallIndex >= 0)
            {
                MainPage._Game._Walls[_iLinkedPrevWallIndex].RecToggleVisibility(true, false);
            }
        }

        private bool RecGateCanClose(bool bBackward = true, bool bForward = true)
        {
            if (!_bVisible)
            {
                if (DemMaxGame._Balls != null)
                {
                    foreach (Ball2 ball in DemMaxGame._Balls)
                    {
                        if (ball.Collidable && !ball.DestroyedByClosingGates && IntersectsCircle(ball.Pos, ball.Radius))
                        {
                            return false;
                        }
                    }
                }
            }
            if (bForward && _iLinkedWallIndex >= 0)
            {
                if (!MainPage._Game._Walls[_iLinkedWallIndex].RecGateCanClose(false, true))
                    return false;
            }
            if (bBackward && _iLinkedPrevWallIndex >= 0)
            {
                return MainPage._Game._Walls[_iLinkedPrevWallIndex].RecGateCanClose(true, false);
            }
            return true;
        }
    }
}
