namespace DemonMaxwellGame
{
    public static class DrawFuncs
    {
        public static Vect2[] _trigtable = new Vect2[360];
        public static int[] _SinesInt = new int[360];
        public const int _iSinesIntAmplitude = 128;

        public static void Initialize()
        {
            for (int i = 0; i < 360; i++)
            {
                double a = (double)i * Math.PI * 2.0 / (double)360;
                _trigtable[i].X = Math.Cos(a);
                _trigtable[i].Y = Math.Sin(a);
                _SinesInt[i] = (int)(_trigtable[i].Y * (double)_iSinesIntAmplitude);
            }
        }

        public static void DrawCircleWedge(ICanvas canvas, Vect2 centre, double radius, int angStart, int angEnd, double radiusExplode = 0.0)
        {
            int angEnd2 = (angEnd > angStart) ? angEnd : (angEnd + 360);
            Vect2 centreEx = centre + (radiusExplode * _trigtable[((angStart + angEnd2) / 2) % 360]);
            PathF path = new PathF();
            path.MoveTo(centreEx);
            int step = Math.Max((int)(300.0 / radius), 1);
            for (int i = angStart; i < angEnd2; i += step)
            {
                path.LineTo(centreEx + (_trigtable[i%360] * radius));
            }
            path.LineTo(centreEx + (_trigtable[angEnd] * radius));
            path.LineTo(centreEx);
            canvas.FillPath(path);
        }

        public static int Power(int x, int y)
        {
            if (y < 0)
                return 0;
            int val = 1;
            for(int i = 0; i < y; i++)
            {
                val *= x;
            }
            return val;
        }

        public static Color ColorInterpolatedLinear(Color c1, Color c2, int pos, int range)
        {
            byte r1, g1, b1, a1, r2, g2, b2, a2;
            c1.ToRgba(out r1, out g1, out b1, out a1);
            c2.ToRgba(out r2, out g2, out b2, out a2);
            int r3 = (int)r1 + (((int)r2 - (int)r1) * pos / range);
            int g3 = (int)g1 + (((int)g2 - (int)g1) * pos / range);
            int b3 = (int)b1 + (((int)b2 - (int)b1) * pos / range);
            int a3 = (int)a1 + (((int)a2 - (int)a1) * pos / range);
            return Color.FromRgba(r3, g3, b3, a3);
        }

        public static Color ColorInterpolatedSinusoidal(Color c1, Color c2, int pos, int range)
        {
            int angle = (pos * 360 / range) % 360;
            int m = _SinesInt[angle] + _iSinesIntAmplitude;
            int d = _iSinesIntAmplitude * 2;
            byte r1, g1, b1, a1, r2, g2, b2, a2;
            c1.ToRgba(out r1, out g1, out b1, out a1);
            c2.ToRgba(out r2, out g2, out b2, out a2);
            int r3 = (int)r1 + (((int)r2 - (int)r1) * m / d);
            int g3 = (int)g1 + (((int)g2 - (int)g1) * m / d);
            int b3 = (int)b1 + (((int)b2 - (int)b1) * m / d);
            int a3 = (int)a1 + (((int)a2 - (int)a1) * m / d);
            return Color.FromRgba(r3, g3, b3, a3);
        }
    }
}
