using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonMaxwellGame
{
    public class WallBox : WallBase
    {
        public Rect MyRect;
        bool MoveableX = false;
        bool MoveableY = false;
        int _iExplodable = 0;
        int _iExplodableMax = 0;
        bool _bExplodableContact = false;
        Vect2 _Vel;
        double _fMass;

        public WallBox(Rect myRect, bool moveableX, bool moveableY, bool removable, bool bounceCorners, 
                       double bounciness, bool bVisible, int iLinkedWallIndex, int iExplodable ) : base()
        {
            MyRect = myRect;
            MoveableX = moveableX;
            MoveableY = moveableY;
            _Vel = new Vect2(0.0, 0.0);
            _fMass = 100000.0;
            _bRemovable = removable;
            _bBounceOffCorners = bounceCorners;
            _fBounciness = bounciness;
            _bVisible = bVisible;
            _iLinkedWallIndex = iLinkedWallIndex;
            _iExplodableMax = _iExplodable = iExplodable;
        }

        public override void Increment()
        {
            if(MoveableX)
                MyRect.X += _Vel.X;
            if(MoveableY)
                MyRect.Y += _Vel.Y;
        }

        public void Bump(Ball2 ball)
        {
            _Vel += (ball.Vel * ball.Mass / _fMass);
        }

        public override bool CheckCollision(Ball2 ball)
        {
            bool bHit = _bVisible ? ball.CollisionDetectionBox(this) : false;
            if (bHit)
            {
                ball.Vel *= _fBounciness;
                if (_iExplodable > 0 && ball.BallType != EBallType.Dragable)
                {
                    _bExplodableContact = true;
                    _iExplodable--;
                    if (_iExplodable == 0)
                    {
                        _bVisible = false;
                        // TODO: Do an explosion animaton, as with the balls
                    }
                }
            }
            return bHit;
        }

        public override void Draw(ICanvas canvas)
        {
            //if (_bRemovable)
            {
                if (_bVisible)
                {
                    if (_bExplodableContact)
                    {
                        _bExplodableContact = false;
                        canvas.FillColor = Colors.Pink;
                    }
                    else
                    {
                        canvas.FillColor = DrawColour;
                    }
                    canvas.FillRectangle(MyRect);
                    if (_iExplodable > 0)
                    {
                        canvas.StrokeSize = 1.0f;
                        canvas.StrokeColor = DrawFuncs.ColorInterpolatedLinear(Colors.Black, Colors.Red, _iExplodable, _iExplodableMax);
                        Vect2 tl = new Vect2(MyRect.Left, MyRect.Top);
                        Vect2 br = new Vect2(MyRect.Right, MyRect.Bottom);
                        Vect2 mid1 = tl + new Vect2(MyRect.Width * 0.75, MyRect.Height * 0.25);
                        Vect2 mid2 = tl + new Vect2(MyRect.Width * 0.25, MyRect.Height * 0.75);
                        canvas.DrawLine(tl, mid1);
                        canvas.DrawLine(mid1, mid2);
                        canvas.DrawLine(mid2, br);
                    }
                }
                else
                {
                    canvas.StrokeSize = 1.0f;
                    canvas.StrokeDashPattern = [3.0f, 6.0f];
                    canvas.DrawRectangle(MyRect);
                    canvas.StrokeDashPattern = [];
                }
            }
        }

        public override bool IntersectsCircle(Vect2 centre, double rad)
        {
            return (centre.X + rad >= MyRect.Left) &&
                   (centre.X - rad <= MyRect.Right) &&
                   (centre.Y + rad >= MyRect.Top) &&
                   (centre.Y - rad <= MyRect.Bottom);
        }
        public override bool IntersectsLine(Vect2 v1, Vect2 v2)
        {
            if (!_bVisible)
                return false;
            // TODO
            return false;
        }
    }
}

