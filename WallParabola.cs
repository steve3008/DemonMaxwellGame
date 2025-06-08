using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemonMaxwellGame
{
    public class WallParabola : WallBase
    {
        public Vect2 _Focus;


        private RectF _Bounds;
        private PathF _Path;

        public WallParabola(Vect2 focus, bool removable, bool bounceCorners, double bounciness,
                            bool bVisible, int iLinkedWallIndex) : base()
        {
            _Focus = focus;
            _bRemovable = removable;
            _bBounceOffCorners = bounceCorners;
            _fBounciness = bounciness;
            _bVisible = bVisible;
            _iLinkedWallIndex = iLinkedWallIndex;
            SetPoints();
            _Path = new PathF(); // TODO
        }

        private void SetPoints()
        {
            double left = 10000.0, top = 10000.0, right = -10000.0, bottom = -10000.0;
            // TODO
            _Bounds = new RectF((float)left, (float)top, (float)(float)right, (float)bottom);
        }

        public override void Increment()
        {

        }

        public override bool CheckCollision(Ball2 ball)
        {
            bool bHit = _bVisible ? ball.CollisionDetectionParabola(this) : false;
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
                    canvas.StrokeSize = 3.0f;
                    DrawDashedArc(canvas);
                }
                canvas.StrokeSize = 1.0f;
            }
        }
        private void DrawDashedArc(ICanvas canvas)
        {
            for (int i = 0; i < _Path.Count - 1; i += 2)
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

            // TODO
            return true;
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
