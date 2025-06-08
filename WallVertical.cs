using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonMaxwellGame
{
    public class WallVertical : WallBase
    {
        public double Top, Bottom, X;

        public WallVertical(double top, double bottom, double x, bool bFinStart, bool bFinEnd, bool removable, bool bounceCorners, 
                            double bounciness, bool bVisible, int iLinkedWallIndex) : base()
        {
            Top = top;
            Bottom = bottom;
            X = x;
            _bFiniteStart = bFinStart;
            _bFiniteEnd = bFinEnd;
            _bRemovable = removable;
            _bBounceOffCorners = bounceCorners;
            _fBounciness = bounciness;
            _bVisible = bVisible;
            _iLinkedWallIndex = iLinkedWallIndex;
        }

        public override void Increment()
        {

        }

        public override bool CheckCollision(Ball2 ball)
        {
            bool bHit = _bVisible ? ball.CollisionDetectionVertical(this) : false;
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
                canvas.DrawLine((float)X, _bFiniteStart ? (float)Top : Ball2._rectBounds.Top,
                                (float)X, _bFiniteEnd ? (float)Bottom : Ball2._rectBounds.Bottom);
                canvas.StrokeDashPattern = [];
                canvas.StrokeSize = 1.0f;
            }
        }

        public override bool IntersectsCircle(Vect2 centre, double rad)
        {
            return (centre.X + rad >= X) &&
                   (centre.X - rad <= X) &&
                   (centre.Y + rad >= Top || !_bFiniteStart) &&
                   (centre.Y - rad <= Bottom || !_bFiniteEnd);
        }

        public override bool IntersectsLine(Vect2 v1, Vect2 v2)
        {
            if (!_bVisible)
                return false;
            if (v1.X < X && v2.X < X)
                return false;
            if (v1.X > X && v2.X > X)
                return false;
            if (!_bFiniteStart && !_bFiniteEnd)
                return true;
            double y = v1.Y + (v2.Y - v1.Y) * (X - v1.X) / (v2.X - v1.X);
            if (_bFiniteStart && y < Top)
                return false;
            if (_bFiniteEnd && y > Bottom)
                return false;
            return true;
        }
    }
}
