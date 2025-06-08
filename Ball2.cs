using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;
using System.Numerics;

namespace DemonMaxwellGame
{
    public enum EBallType
    {
        Normal,
        Bomb,
        Fragile,
        TouchSensitive,
        Dragable
    }

    public class Ball2
    {
        public static string[] _sTypes = { "Normal", "Bomb", "Fragile", "TouchSensitive", "Dragable" };
        public static RectF _rectBounds;

        Vect2 _Pos;
        Vect2 _Vel;
        Vect2 _Gravity;
        double _Radius, _RadiusHiLite;
        double _fMass;
        double _fMaxVel, _fMaxVel2;
        Vect2 _HiLiteOffset;
        Color _Col, _ColRing, _ColRing2, _ColHilite;
        Microsoft.Maui.Graphics.IImage? _Image = null;
        bool _bDrawViaBmp = false;
        RectF _rectBoundsSink;
        EBallType _eType = EBallType.Normal;

        // Explosion stuff
        int _iExplodingStage = 0;
        const int _iMaxExplodingStage = 8;
        struct ExplosionFragment { public int iAngle; public double fSpeed; public double fCurrPos; }
        ExplosionFragment[]? _ExplosionFrags = null;

        // Images on some types of ball
        private Microsoft.Maui.Graphics.IImage? _bmpExplosion = null;
        private Microsoft.Maui.Graphics.IImage? _bmpFinger = null;

        bool _bMoveable = true;
        bool _bKickable = true;
        bool _bDestroyedByClosingGates = true;
        bool _bVisible = true;
        bool _bHome = false;

        public Ball2(Vect2 pos, Vect2 vel, double radius, double fMass, Vect2 gravity, Color colour, RectF rectBoundsSink, EBallType eType, 
                     bool bDestroyedByClosingGates, bool bKickable, bool bMoveable)
        {
            _Pos = pos;
            _Vel = vel;
            _Radius = radius;
            _fMaxVel = _Radius * 1.9;
            _fMaxVel2 = _fMaxVel * _fMaxVel;
            _RadiusHiLite = Math.Max(_Radius * 0.2, 1.0);
            _HiLiteOffset = new Vect2(-_Radius * 0.33, -_Radius * 0.33);
            _fMass = fMass;
            _Gravity = gravity;
            _rectBoundsSink = rectBoundsSink;
            _eType = eType;
            _bHome = false;
            _bDestroyedByClosingGates = bDestroyedByClosingGates;
            _bKickable = bKickable;
            _bMoveable = bMoveable;
            if (!_bMoveable)
                _fMass = 100000000.0;
            PrepareImages().Wait();

            _Col = colour;
            byte r, g, b;
            _Col.ToRgb(out r, out g, out b);
            //int ringBrighter = 80;
            //_ColRing = Color.FromRgb(Math.Min(r + ringBrighter, 255), Math.Min(g + ringBrighter, 255), Math.Min(b + ringBrighter, 255));
            _ColRing = Color.FromRgb(r * 2 / 3, g * 2 / 3, b * 2 / 3);
            _ColRing2 = Color.FromRgb(r * 3 / 4, g * 3 / 4, b * 3 / 4);
            _ColHilite = Color.FromRgb((r * 2 + 255) / 3, (g * 2 + 255) / 3, (b * 2 + 255) / 3);
            _bDrawViaBmp = false;// _eType != EBallType.Bomb && _Radius > 11.0;

#if ANDROID
            if (_bDrawViaBmp)
                CreateBallImageAndroid();
#endif
            _eType = eType;
            _bDestroyedByClosingGates = bDestroyedByClosingGates;
            _bMoveable = bMoveable;
        }

        private async Task PrepareImages()
        {
            if (_eType == EBallType.Bomb || _eType == EBallType.TouchSensitive || _eType == EBallType.Fragile)
            {
                _bmpExplosion = await DemMaxGame.LoadImageFromRes("ball_explosion.png");
                if (_eType == EBallType.TouchSensitive)
                    _bmpFinger = await DemMaxGame.LoadImageFromRes("ball_finger.png");
                // TODO: Dragable image - maybe 4 arrows
            }
        }

#if ANDROID
        private void CreateBallImageAndroid()
        {
            int size = (int)(_Radius * 2.0 + 1.0);
            var bmp = Android.Graphics.Bitmap.CreateBitmap(size, size, Android.Graphics.Bitmap.Config.Argb8888);
            var androidCanvas = new Android.Graphics.Canvas(bmp);

            DrawBallAndroid(androidCanvas);

            // Convert Android Bitmap to IImage
            using var stream = new MemoryStream();
            bmp.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, stream);
            stream.Position = 0;
            _Image = PlatformImage.FromStream(stream);

            // Clean up
            bmp.Dispose();
        }

        private void DrawBallAndroid(Android.Graphics.Canvas androidCanvas)
        {
            Android.Graphics.Paint paint = new Android.Graphics.Paint();
            float r = (float)_Radius;
            float rMain = r;
            // Dark ring around the ball
            if (r > 3.0f)
            {
                paint.Color = _ColRing.ToPlatform();
                androidCanvas.DrawCircle(r, r, r, paint);
                rMain -= 1.0f;
                if (r > 5.0f)
                {
                    paint.Color = _ColRing2.ToPlatform();
                    androidCanvas.DrawCircle(r, r, r - 1.0f, paint);
                    rMain -= 1.0f;
                }
            }
            // Main colour of the ball
            paint.Color = _Col.ToPlatform();
            androidCanvas.DrawCircle(r, r, rMain, paint);
            // Highlight
            if (_Radius > 6.0)
            {
                if (_Radius > 11.0)
                {
                    paint.Color = _ColHilite.ToPlatform();
                    androidCanvas.DrawCircle(r + (float)_HiLiteOffset.X, r + (float)_HiLiteOffset.Y, (float)_RadiusHiLite * 2.0f, paint);
                }
                paint.Color = Colors.White.ToPlatform();
                androidCanvas.DrawCircle(r + (float)_HiLiteOffset.X, r + (float)_HiLiteOffset.Y, (float)_RadiusHiLite, paint);
            }
            paint.Dispose();
        }
#endif

        public void Draw(ICanvas canvas)
        {
            if (!_bVisible)
                return;
            if(_iExplodingStage > 0)
            {
                DrawExploding(canvas);
                return;
            }

            if (_bDrawViaBmp)
            {
                if (_Image != null)
                    canvas.DrawImage(_Image, (float)(_Pos.X - _Radius), (float)(_Pos.Y - _Radius), (float)_Image.Width, (float)_Image.Height);
            }
            else
            {
                // Main colour of the ball
                canvas.FillColor = _Col;
                canvas.FillCircle(_Pos, (float)_Radius);
                // Highlight
                if (_eType != EBallType.Dragable && !MainPage._Game.IsAirHockeyGame && _Radius > 6.0)
                {
                    if (_Radius > 11.0)
                    {
                        canvas.FillColor = _ColHilite;
                        canvas.FillCircle(_Pos + _HiLiteOffset, (float)_RadiusHiLite * 2.0f);
                    }
                    canvas.FillColor = Colors.White;
                    canvas.FillCircle(_Pos + _HiLiteOffset, (float)_RadiusHiLite);
                }
                // Dark ring around the ball
                if (_Radius > 4.0)
                {
                    canvas.StrokeColor = _ColRing;
                    canvas.DrawCircle(_Pos, (float)_Radius);
                    //if (_Radius > 8.0)
                    //{
                    //    canvas.StrokeColor = _ColRing2;
                    //    canvas.DrawCircle(_Pos, (float)_Radius - 1.0f);
                    //}
                }
                // Spikes and explosions symbol on the bomb
                if (_eType == EBallType.Bomb)
                {
                    Vect2 v;
                    double spikeLen = _Radius * (1.3 + 0.1 * DrawFuncs._trigtable[(DemMaxGame._iAnimatedFrameNum * 12) % 360].Y);
                    canvas.StrokeSize = Math.Max((float)_Radius * 0.2f, 1.0f);
                    canvas.StrokeLineCap = LineCap.Round;
                    for (int a = 8; a < 360; a+= 45)
                    {
                        v = DrawFuncs._trigtable[a];
                        canvas.DrawLine(_Pos + (v * _Radius), _Pos + (v * spikeLen));
                    }
                    canvas.StrokeLineCap = LineCap.Butt;
                    if(_Radius > 4.0)
                        canvas.DrawImage(_bmpExplosion, (float)(_Pos.X - _Radius * 0.5), (float)(_Pos.Y - _Radius * 0.5), (float)_Radius, (float)_Radius);
                }
                // Explosions symbol on touch sensitive
                else if (_eType == EBallType.TouchSensitive)
                {
                    canvas.DrawImage((DemMaxGame._iAnimatedFrameNum % 25 < 15) ? _bmpExplosion : _bmpFinger, 
                                     (float)(_Pos.X - _Radius * 0.5), (float)(_Pos.Y - _Radius * 0.5), (float)_Radius, (float)_Radius);
                }
                // Explosions symbol on fragile
                else if (_eType == EBallType.Fragile)
                {
                    canvas.DrawImage(_bmpExplosion, (float)(_Pos.X - _Radius * 0.5), (float)(_Pos.Y - _Radius * 0.5), (float)_Radius, (float)_Radius);
                }
                else if (_eType == EBallType.Dragable || MainPage._Game.IsAirHockeyGame)
                {
                    bool bBeingDragged = (MainPage._Game.BallBeingDragged == this);
                    if(bBeingDragged)
                    {
                        canvas.StrokeColor = Colors.White;
                        canvas.FillColor = Colors.Red;
                        canvas.FillCircle(_Pos, (float)_Radius * 0.5f);
                    }
                    else
                    {
                        canvas.StrokeColor = Colors.Black;
                    }
                    canvas.DrawCircle(_Pos, (float)_Radius - 1.0f);
                    canvas.DrawCircle(_Pos, (float)_Radius * 0.5f);
                }
            }
        }

        private void DrawExploding(ICanvas canvas)
        {
            // At the start of the explosion process, setup the angular sizes of the explosion fragments, their number and their speeds
            if (_iExplodingStage == 1)
            {
                List<int> angles = new List<int>();
                angles.Add(0);
                do
                {
                    angles.Add(angles.Last() + 20 + Vect2._Rnd.Next(50));
                }
                while (angles.Last() < 360);

                int aMax = angles.Last();
                int aOffset = Vect2._Rnd.Next(40);
                _ExplosionFrags = new ExplosionFragment[angles.Count];
                int i = 0;
                foreach(int angle in angles)
                {
                    _ExplosionFrags[i++] = new ExplosionFragment
                                    { iAngle = (angle * 360 / aMax + aOffset) % 360, 
                                      fSpeed = (float)_Radius * (Vect2._Rnd.NextDouble() * 1.5 + 2.0) / (float)_iMaxExplodingStage,
                                      fCurrPos = 0.0 };
                }
            }

            // Draw the explosion fragments at each stage of the explosion, using the fragment data created at the first explosion stage, above
            if (_ExplosionFrags != null)
            {
                canvas.FillColor = _Col;
                int iNumFrags = _ExplosionFrags.Length - 1;
                ExplosionFragment frag;
                for (int i = 0; i < iNumFrags; i++)
                {
                    frag = _ExplosionFrags[i];
                    _ExplosionFrags[i].fCurrPos += frag.fSpeed;
                    DrawFuncs.DrawCircleWedge(canvas, _Pos, _Radius,
                                frag.iAngle, _ExplosionFrags[i + 1].iAngle, frag.fCurrPos);
                }
            }

            // Increment the explosion stage
            _iExplodingStage++;
            if(_iExplodingStage > _iMaxExplodingStage)
            {
                _iExplodingStage = 0;
                _bVisible = false;
                MainPage._Game.ExplodeNearbyFragileBalls(this);
                MainPage._This.UpdateParticlesRemainingNextFrame();
            }
        }

        public void ExplodeMe()
        {
            // Get to the first explosion stage
            _iExplodingStage = 1;
        }

        public static EBallType TypeFromString(string sType)
        {
            for (int i = 0; i < _sTypes.Length; i++)
            {
                if (sType == _sTypes[i])
                    return (EBallType)i;
            }
            return EBallType.Normal;
        }

        public bool Visible => _bVisible;

        public bool IsHome => _bHome;

        public bool Exploding => (_iExplodingStage > 0);

        public bool Collidable => (_bVisible && !Exploding);

        public EBallType BallType => _eType;

        public bool DestroyedByClosingGates => _bDestroyedByClosingGates;

        public bool Kickable => _bKickable;

        public Vect2 Pos
        {
            get { return _Pos; }
        }
        public Vect2 Vel
        {
            get { return _Vel; }
            set
            {
                if(!_bMoveable)
                {
                    _Vel = new Vect2(0, 0);
                    return;
                }
                _Vel = value;
                if (_Vel.LenSq > _fMaxVel2)
                {
                    _Vel = _Vel.Unit * _fMaxVel;
                }
            }
        }
        public double Radius
        {
            get { return _Radius; }
        }
        public double Mass
        {
            get { return _fMass; }
        }
        public void Increment()
        {
            if (!Collidable || !_bMoveable)
                return;
            Vel += _Gravity;
            //if(_eType == EBallType.Dragable)
            //{
            //    Vel = Vel * 0.8;
            //}
            _Pos += Vel;
            CollisionDetectionOuterBounds();
            CollisionDetectionSink();
        }

        private void CollisionDetectionSink()
        {
            if (_Pos.X > _rectBoundsSink.Left && _Pos.X < _rectBoundsSink.Right &&
                _Pos.Y > _rectBoundsSink.Top && _Pos.Y < _rectBoundsSink.Bottom)
            {
                ExplodeMe();
                _bHome = true;
                MainPage._This.UpdateParticlesRemainingNextFrame();
                MainPage._This.UpdateTheScoreNextFrame();
                MainPage._Game.NextAirHockeyPuck();
            }
        }

        public void MoveThisAirHockeyPuckIntoPlay()
        {
            _Pos = new Vect2(_rectBounds.Width / 2, _rectBounds.Height / 2);
        }

        private void CollisionDetectionOuterBounds()
        {
            if (_Pos.X < _rectBounds.Left - _Radius || _Pos.X > _rectBounds.Right + _Radius ||
                _Pos.Y < _rectBounds.Top - _Radius || _Pos.Y > _rectBounds.Bottom + _Radius)
            {
                _bVisible = false;
                MainPage._This.UpdateParticlesRemainingNextFrame();
            }
        }

        public bool CollisionDetectionHorizontal(WallHorizontal wall)
        {
            if (!_bMoveable || _Pos.Y - _Radius > wall.Y || _Pos.Y + _Radius < wall.Y)
                return false;

            if (wall._bFiniteStart)
            {
                if (_Pos.X + _Radius < wall.Left)
                    return false;
                else if (wall._bBounceOffCorners && _Pos.X < wall.Left)
                    return CollisionDetectionPoint(new Vect2(wall.Left, wall.Y));
            }
            if (wall._bFiniteEnd)
            {
                if (_Pos.X - _Radius > wall.Right)
                    return false;
                else if (wall._bBounceOffCorners && _Pos.X > wall.Right)
                    return CollisionDetectionPoint(new Vect2(wall.Right, wall.Y));
            }

            _Pos -= _Vel;
            _Vel.Y = -_Vel.Y;
            return true;
        }

        public bool CollisionDetectionVertical(WallVertical wall)
        {
            if (!_bMoveable || _Pos.X - _Radius > wall.X || _Pos.X + _Radius < wall.X)
                return false;

            if (wall._bFiniteStart)
            {
                if (_Pos.Y + _Radius < wall.Top)
                    return false;
                else if(wall._bBounceOffCorners && _Pos.Y < wall.Top)
                    return CollisionDetectionPoint(new Vect2(wall.X, wall.Top));
            }
            if (wall._bFiniteEnd)
            {
                if (_Pos.Y - _Radius > wall.Bottom)
                    return false;
                else if (wall._bBounceOffCorners && _Pos.Y > wall.Bottom)
                    return CollisionDetectionPoint(new Vect2(wall.X, wall.Bottom));
            }

            _Pos -= _Vel;
            _Vel.X = -_Vel.X;
            return true;
        }

        public bool CollisionDetectionStraightLine(WallStraight wall)
        {
            if (!_bMoveable || 
                _Pos.X + _Radius < wall.Left || _Pos.X - _Radius > wall.Right ||
                _Pos.Y + _Radius < wall.Top || _Pos.Y - _Radius > wall.Bottom)
                return false;

            Vect2 p = _Pos - wall.Start;
            double distAlongLine = p.Dot(wall.UnitVect);
            double dist2 = (p - (wall.UnitVect * distAlongLine)).LenSq;
            if (dist2 > _Radius * _Radius)
                return false;
            if (wall._bBounceOffCorners)
            {
                if (distAlongLine < 0.0)
                    return CollisionDetectionPoint(wall.Start);
                if (distAlongLine > wall.Length)
                    return CollisionDetectionPoint(wall.End);
            }
            _Pos -= _Vel;
            Vel = ((wall.UnitVect * _Vel.Dot(wall.UnitVect)) * 2.0) - _Vel;
            return true;
        }

        public bool CollisionDetectionBox(WallBox wall)
        {
            if (!_bMoveable || 
                _Pos.X + _Radius < wall.MyRect.Left ||
                _Pos.X - _Radius > wall.MyRect.Right ||
                _Pos.Y + _Radius < wall.MyRect.Top ||
                _Pos.Y - _Radius > wall.MyRect.Bottom)
                return false;

            // Corners
            if (wall._bBounceOffCorners)
            {
                if (_Pos.Y < wall.MyRect.Top)
                {
                    if (_Pos.X < wall.MyRect.Left)
                    {
                        return CollisionDetectionPoint(new Vect2(wall.MyRect.Left, wall.MyRect.Top));
                    }
                    if (_Pos.X > wall.MyRect.Right)
                    {
                        return CollisionDetectionPoint(new Vect2(wall.MyRect.Right, wall.MyRect.Top));
                    }
                    wall.Bump(this);
                    _Pos -= _Vel;
                    _Vel.Y = -Math.Abs(_Vel.Y);
                    return true;
                }
                if (_Pos.Y > wall.MyRect.Bottom)
                {
                    if (_Pos.X < wall.MyRect.Left)
                    {
                        return CollisionDetectionPoint(new Vect2(wall.MyRect.Left, wall.MyRect.Bottom));
                    }
                    if (_Pos.X > wall.MyRect.Right)
                    {
                        return CollisionDetectionPoint(new Vect2(wall.MyRect.Right, wall.MyRect.Bottom));
                    }
                    wall.Bump(this);
                    _Pos -= _Vel;
                    _Vel.Y = Math.Abs(_Vel.Y);
                    return true;
                }
            }

            // Sides
            wall.Bump(this);
            if (_Pos.X - _Radius < wall.MyRect.Left)
            {
                _Pos -= _Vel;
                _Vel.X = -Math.Abs(_Vel.X);
                return true;
            }
            if (_Pos.X + _Radius > wall.MyRect.Right)
            {
                _Pos -= _Vel;
                _Vel.X = Math.Abs(_Vel.X);
                return true;
            }
            _Pos -= _Vel;
            if (_Pos.Y - _Radius < wall.MyRect.Top)
                _Vel.Y = -Math.Abs(_Vel.Y);
            else
                _Vel.Y = Math.Abs(_Vel.Y);
            return true;
        }

        public bool CollisionDetectionArc(WallArc wall)
        {
            if (!_bMoveable || 
                _Pos.X + _Radius < wall._Bounds.Left ||
                _Pos.X - _Radius > wall._Bounds.Right ||
                _Pos.Y + _Radius < wall._Bounds.Top ||
                _Pos.Y - _Radius > wall._Bounds.Bottom)
                return false;
            Vect2 relPosBall = _Pos - wall._Centre;
            double r2 = relPosBall.LenSq;
            double rMin2 = wall._fRadius - _Radius;
            rMin2 *= rMin2;
            if (r2 < rMin2)
                return false;
            double rMax2 = wall._fRadius + _Radius;
            rMax2 *= rMax2;
            if (r2 > rMax2)
                return false;

            if (wall._iStartAngle != wall._iEndAngle)
            {
                int angle = (int)(Math.Atan2(relPosBall.Y, relPosBall.X) * 180.0 / Math.PI);
                if (angle < 0)
                    angle += 360;

                if (wall._iEndAngle > wall._iStartAngle)
                {
                    if (angle < wall._iStartAngle || angle > wall._iEndAngle)
                    {
                        if (wall._bBounceOffCorners)
                        {
                            if (CollisionDetectionPoint(wall._vStart))
                                return true;
                            else
                                return CollisionDetectionPoint(wall._vEnd);
                        }
                        return false;
                    }
                }
                else
                {
                    if (angle < wall._iStartAngle && angle > wall._iEndAngle)
                    {
                        if (wall._bBounceOffCorners)
                        {
                            if (CollisionDetectionPoint(wall._vStart))
                                return true;
                            else
                                return CollisionDetectionPoint(wall._vEnd);
                        }
                        return false;
                    }
                }
            }

            Vect2 vNorm = relPosBall.Unit;
            double dot = vNorm.Dot(_Vel);
            _Pos -= _Vel;
            Vel -= (vNorm * dot * 2.0);
            return true;
        }

        public bool CollisionDetectionParabola(WallParabola wall)
        {
            return false;
            // TODO
        }

        private bool CollisionDetectionPoint(Vect2 pnt)
        {
            if (!_bMoveable)
                return false;
            Vect2 posRel = pnt - _Pos;
            if (posRel.LenSq > _Radius * _Radius)
                return false;

            Vect2 vNorm = posRel.Unit;
            double dot = vNorm.Dot(_Vel);
            if (dot > 0.0)
            {
                _Pos -= _Vel;
                Vel -= (vNorm * dot * 2.0);
                return true;
            }
            return false;
        }

        public bool CollisionDetectionInterBall(Ball2 other)
        {
            if (Overlaps(other))
            {
                if(_eType == EBallType.Bomb || other._eType == EBallType.Bomb)
                {
                    other.ExplodeMe();
                    ExplodeMe();
                    return true;
                }
                //if (_eType != EBallType.Bomb && other._eType == EBallType.Bomb)
                //{
                //    ExplodeMe();
                //    return true;
                //}


                Vect2 vRel = Vel - other.Vel;
                Vect2 vNorm = (other._Pos - _Pos).Unit;
                double dot = vNorm.Dot(vRel);
                if (dot > 0.0)
                {
                    Vect2 vi = vNorm * dot * 2.0;// * ((_frame > _iGravityOnAtFrame) ? _fElasticity : 1.0);
                    double fMasses = _fMass + other._fMass;
                    Vel -= (vi * (other._fMass / fMasses));
                    other.Vel += (vi * (_fMass / fMasses));
                    return true;
                }
            }
            return false;
        }

        public bool Overlaps(Ball2 other)
        {
            if (!Collidable || !other.Collidable)
                return false;
            double r = Radius + other.Radius;
            return (other.Pos - Pos).LenSq < (r * r);
        }
    }
}
