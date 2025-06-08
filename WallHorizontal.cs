using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonMaxwellGame
{
    public class WallHorizontal : WallBase
    {
        public double Left, Right, Y;

        public WallHorizontal(double left, double right, double y, bool bFinStart, bool bFinEnd, 
                              bool removable, bool bounceCorners, double bounciness, bool bVisible, int iLinkedWallIndex) : base()
        {
            Left = left;
            Right = right;
            Y = y;
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
            bool bHit = _bVisible ? ball.CollisionDetectionHorizontal(this) : false;
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
                if(_bVisible)
                {
                    canvas.StrokeSize = 6.0f;
                }
                else
                {
                    canvas.StrokeSize = 4.0f;
                    canvas.StrokeDashPattern = [3.0f, 6.0f];
                }
                canvas.DrawLine(_bFiniteStart ? (float)Left : Ball2._rectBounds.Left, (float)Y,
                                _bFiniteEnd ? (float)Right : Ball2._rectBounds.Right, (float)Y);
                canvas.StrokeDashPattern = [];
                canvas.StrokeSize = 1.0f;
            }
        }

        public override bool IntersectsCircle(Vect2 centre, double rad)
        {
            return (centre.X + rad >= Left || !_bFiniteStart) &&
                   (centre.X - rad <= Right || !_bFiniteEnd) &&
                   (centre.Y + rad >= Y) &&
                   (centre.Y - rad <= Y);
        }
        public override bool IntersectsLine(Vect2 v1, Vect2 v2)
        {
            if (!_bVisible)
                return false;
            if (v1.Y < Y && v2.Y < Y)
                return false;
            if (v1.Y > Y && v2.Y > Y)
                return false;
            if (!_bFiniteStart && !_bFiniteEnd)
                return true;
            double x = v1.X + (v2.X - v1.X) * (Y - v1.Y) / (v2.Y - v1.Y);
            if (_bFiniteStart && x < Left)
                return false;
            if (_bFiniteEnd && x > Right)
                return false;
            return true;
        }
    }
}
