using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonMaxwellGame
{
    public class WallArc : WallBase
    {
        public Vect2 _Centre;
        public double _fRadius;
        public int _iStartAngle;
        public int _iEndAngle;
        public Vect2 _vStart, _vEnd;

        public RectF _Bounds;
        private PathF _Path;

        public WallArc(Vect2 centre, double radius, int startAngle, int endAngle, bool removable, 
                       bool bounceCorners, double bounciness, bool bVisible, int iLinkedWallIndex) : base()
        {
            _Centre = centre;
            _fRadius = radius;
            _iStartAngle = startAngle;
            _iEndAngle = endAngle;
            _bRemovable = removable;
            _bBounceOffCorners = bounceCorners;
            _fBounciness = bounciness;
            _bVisible = bVisible;
            _iLinkedWallIndex = iLinkedWallIndex;
            SetPoints();
        }

        private void SetPoints()
        {
            int end = (_iEndAngle > _iStartAngle) ? _iEndAngle : (_iEndAngle + 360);
            double left = 10000.0, top = 10000.0, right = -10000.0, bottom = -10000.0;
            double fCircum = 2.0 * Math.PI * _fRadius * (double)(end - _iStartAngle) / 360.0;
            int iLastPnt = Math.Min(Math.Max((int)(fCircum * 30.0), 4), 89);
            _Path = new PathF();
            for (int i = 0; i <= iLastPnt; i++)
            {
                int ang = (_iStartAngle + i * (end - _iStartAngle) / iLastPnt) % 360;
                Vect2 pnt = (DrawFuncs._trigtable[ang] * _fRadius) + _Centre;
                if (i == 0)
                {
                    _Path.MoveTo(pnt);
                    _vStart = pnt;
                }
                else
                {
                    _Path.LineTo(pnt);
                    if (i == iLastPnt)
                        _vEnd = pnt;
                }
                if (left > pnt.X)
                    left = pnt.X;
                if (top > pnt.Y)
                    top = pnt.Y;
                if (right < pnt.X)
                    right = pnt.X;
                if (bottom < pnt.Y)
                    bottom = pnt.Y;
            }
            _Bounds = new RectF((float)left, (float)top, (float)(right - left), (float)(bottom - top));
        }

        public override void Increment()
        {

        }

        public override bool CheckCollision(Ball2 ball)
        {
            bool bHit = _bVisible ? ball.CollisionDetectionArc(this) : false;
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
                    canvas.DrawPath(_Path);
                }
                else
                {
                    canvas.StrokeSize = 4.0f;
                    DrawDashedArc(canvas);
                }
                canvas.StrokeSize = 1.0f;
            }
            //canvas.DrawRectangle(_Bounds);
            //canvas.StrokeColor = Colors.Blue;
            //canvas.DrawRectangle(DemMaxGame._rectBalls);
            //canvas.DrawEllipse(DemMaxGame._rectBalls);
        }

        private void DrawDashedArc(ICanvas canvas)
        {
            for(int i = 0; i < _Path.Count - 1; i += 3)
            {
                canvas.DrawLine(_Path[i], _Path[i + 1]);
            }
        }

        public override bool IntersectsCircle(Vect2 centre, double rad)
        {
            if ((centre.X + rad < _Bounds.Left) ||
                (centre.X - rad > _Bounds.Right) ||
                (centre.Y + rad < _Bounds.Top) ||
                (centre.Y - rad > _Bounds.Bottom))
                return false;

            double r2 = rad * rad;
            foreach(PointF pnt in _Path.Points)
            {
                if ((centre - (Vect2)pnt).LenSq < r2)
                    return true;
            }
            return false;

            /*
            double r = (centre - _Centre).Len;
            if (r > (_fRadius + rad))
                return false;
            if (r < (_fRadius - rad))
                return false;
            // TODO
            return true;
            */
        }
        public override bool IntersectsLine(Vect2 v1, Vect2 v2)
        {
            if (!_bVisible)
                return false;

            Vect2 d = v2 - v1;         // Direction vector of the line
            Vect2 f = v1 - _Centre;    // Vector from circle center to v1

            double A = d.LenSq;
            double B = 2.0 * f.Dot(d);
            double C = f.LenSq - _fRadius * _fRadius;

            double discriminant = B * B - 4 * A * C;
            if (discriminant < 0)
                return false;

            if (_iStartAngle == _iEndAngle)
                return true;

            discriminant = Math.Sqrt(discriminant);
            double[] ts = new double[2];
            ts[0] = (-B - discriminant) / (2.0 * A);
            ts[1] = (-B + discriminant) / (2.0 * A);
            if ((ts[0] < 0 || ts[0] > 1.0) && (ts[1] < 0 || ts[1] > 1.0))
                return false;
            foreach (double t in ts)
            {
                Vect2 v;
                if (t >= 0 && t <= 1.0)
                {
                    v = (v1 + d * t) - _Centre;
                    int angle = (int)(Math.Atan2(v.Y, v.X) * 180.0 / Math.PI);
                    if (angle < 0)
                        angle += 360;
                    if (_iStartAngle < _iEndAngle)
                    {
                        if (angle >= _iStartAngle && angle <= _iEndAngle)
                            return true;
                    }
                    else
                    {
                        if (angle >= _iStartAngle || angle <= _iEndAngle)
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
