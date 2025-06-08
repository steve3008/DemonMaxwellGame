using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonMaxwellGame
{
    public class WallStraight : WallBase
    {
        private Vect2 _Start, _End, _Unit;//, _StartInfinite, _EndInfinite; TODO
        private double _Left, _Top, _Right, _Bottom, _Length;

        public WallStraight(Vect2 start, Vect2 end, bool removable, bool bounceCorners, double bounciness,
                            bool bVisible, int iLinkedWallIndex) : base()
        {
            _Start = start;
            _End = end;
            _Left = Math.Min(_Start.X, _End.X);
            _Top = Math.Min(_Start.Y, _End.Y);
            _Right = Math.Max(_Start.X, _End.X);
            _Bottom = Math.Max(_Start.Y, _End.Y);
            Vect2 startToEnd = _End - _Start;
            _Length = startToEnd.Len;
            _Unit = startToEnd / _Length;
            _bRemovable = removable;
            _bBounceOffCorners = bounceCorners;
            _fBounciness = bounciness;
            _bVisible = bVisible;
            _iLinkedWallIndex = iLinkedWallIndex;
        }

        public Vect2 Start => _Start;
        public Vect2 End => _End;
        public Vect2 UnitVect => _Unit;
        public double Length => _Length;
        public double Left => _Left;
        public double Top => _Top;
        public double Right => _Right;
        public double Bottom => _Bottom;

        public override void Increment()
        {

        }

        public override bool CheckCollision(Ball2 ball)
        {
            bool bHit = _bVisible ? ball.CollisionDetectionStraightLine(this) : false;
            if (bHit)
            {
                ball.Vel *= _fBounciness;
            }
            return bHit;
        }

        public override void Draw(ICanvas canvas)
        {
            if (_bRemovable)
            {
                canvas.StrokeColor = DrawColour;
                if (_bVisible)
                {
                    canvas.StrokeSize = 6.0f;
                }
                else
                {
                    canvas.StrokeSize = 3.0f;
                    canvas.StrokeDashPattern = [3.0f, 6.0f];
                }
                canvas.DrawLine(_Start, _End);
                canvas.StrokeDashPattern = [];
                canvas.StrokeSize = 1.0f;
            }
        }

        public override bool IntersectsCircle(Vect2 centre, double rad)
        {
            double r = rad * 1.01;
            Vect2 p = centre - _Start;
            double distAlongLine = p.Dot(_Unit);
            if(distAlongLine < 0.0 && _bFiniteStart)
                return p.LenSq < r * r;
            if (distAlongLine > _Length && _bFiniteEnd)
                return (centre - _End).LenSq < r * r;
            double dist2 = (p - (_Unit * distAlongLine)).LenSq;
            return dist2 < r * r;
        }
        public override bool IntersectsLine(Vect2 v1, Vect2 v2)
        {
            if (!_bVisible)
                return false;
            // _Start = A, _End = B, v1 = C, v2 = D
            double denominator = (_Start.X - _End.X) * (v1.Y - v2.Y) - (_Start.Y - _End.Y) * (v1.X - v2.X);
            if (Math.Abs(denominator) < 1e-6)
                return false;
            double t = ((_Start.X - v1.X) * (v1.Y - v2.Y) - (_Start.Y - v1.Y) * (v1.X - v2.X)) / denominator;

            return t >= 0.0 && t <= 1.0;
        }
    }
}
