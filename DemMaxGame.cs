using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;

namespace DemonMaxwellGame
{
    public class DemMaxGame
    {
        //public static RectF _rectBalls;
        bool _bGameInitialized = false;
        bool _bRunning = false;
        int _iCurrentLevel = 1;
        string _sCurrentLevelName = "";
        public const int _iTopLevel = 23;
        int _iFramesPerAnimationFrame = 2;
        bool _bCollisional = true;
        public static int _iFrameNum = 0, _iAnimatedFrameNum = 0;
        int _iMaxTime = 60, _iCurrTime = 0; // Seconds
        int _iTargetScore = 100;
        int _iTotalScore = 0;
        // Background
        private Microsoft.Maui.Graphics.IImage? _BackgroundImage;
        private Microsoft.Maui.Graphics.IImage[]? _HelpStageImages;
        // Balls
        public static Ball2[]? _Balls = null;
        int _iNumBalls = 400;
        double _fBallRadiusMin = 5.0, _fBallRadiusMax = 15.0;
        int _iBallBeingDragged = -1;
        // Touching balls to kick them
        bool _bKickBalls = true;
        // Walls
        int _iNumWalls = 5;
        public WallBase[]? _Walls = null;
        Color[,]? _colBallPatternFromBmp = null;

        // Timing frames for debug purposes
        static public int _iFrameTime;
        int _iFrameTimeTotal, _iAnimatedFrameTime;
        const int _iAnimatedFrameTimeFrameSpan = 20;
        Vect2 _vTouchDownPos, _vTouchMovePos, _vTouchUpPos, _vTapPos;

        static public double _fTouchRadius = 10.0;

        // The special case of Air Hockey
        const int _iAirHockeyLevel = 21;
        const int _iAirHockeyHitter = 1;
        const int _iAirHockey1stPuck = 2;
        int _iAirHockeyCurrPuck = _iAirHockey1stPuck;
        const int _iAirHockeyLastPuck = 4;

        // The special case of the final level
        const int _iFinalLevelBallsPerImage = 2000;
        const int _iFinaLevelTimeWhenWallsVanish = 11;
        const int _iFinalLevel1stBrick = 4;
        const int _iFinalLevelNumBricks = 110;

        public DemMaxGame()
        {
            _bGameInitialized = false;
            MainPage._Game = this;
            DrawFuncs.Initialize();
        }

        public void Initialize(RectF dirtyRect)
        {
            if (_bGameInitialized)
                return;
            Ball2._rectBounds = dirtyRect;
            _fBallRadiusMin = Ball2._rectBounds.Width * 0.02;
            _fBallRadiusMax = Ball2._rectBounds.Width * 0.022;
            _fTouchRadius = Ball2._rectBounds.Width * 0.08;
            LoadSettingsFromFile();
            StartLevel(_iCurrentLevel);
            _bGameInitialized = true;
        }

        public bool GameInitialized
        {
            get { return _bGameInitialized; }
        }

        public void StartLevel(int iLevel)
        {
            MainPage._iTargetReachedSignOn = -1;
            _iCurrentLevel = iLevel;
            if (_iCurrentLevel > _iTopLevel)
            {
                _iCurrentLevel = 1;
            }
            if (MainPage._iNextLevel == 1)
                _iTotalScore = 0;
            PrepareImages().Wait();
            LoadLevelFromFile().Wait();
            _iCurrTime = _iMaxTime;
            MainPage._This.UpdateParticlesRemainingNextFrame();
            MainPage._This.UpdateTheScoreNextFrame();
            MainPage._This.UpdateTheTargetScoreNextFrame();
            if (IsAirHockeyGame)
                _iAirHockeyCurrPuck = _iAirHockey1stPuck;
            if (_iCurrentLevel == _iTopLevel)
            {
                PrepareFinalLevel();
            }
        }

        private void PrepareFinalLevel()
        {
            if (_Balls == null || _Walls == null)
                return;
            // Make the demon image fly apart so it comes together
            int iStart = 0;
            int iEnd = iStart + _iFinalLevelBallsPerImage;
            Ball2 ball;
            for (int i = iStart; i < iEnd; i++)
            {
                ball = _Balls[i];
                for (int j = 0; j < 40; j++)
                {
                    ball.Increment();
                    foreach (WallBase wall in _Walls)
                    {
                        if (ball.Collidable && wall.CheckCollision(ball))
                            break;
                    }
                }
            }
            for (int i = iStart; i < iEnd; i++)
            {
                _Balls[i].Vel = _Balls[i].Vel * -0.2;
            }
        }

        private async Task PrepareImages(string sAppend = "")
        {
            _BackgroundImage = await LoadImageFromRes("bgd_lvl_" + _iCurrentLevel.ToString("00") + sAppend + ".jpg");
            _HelpStageImages = new Microsoft.Maui.Graphics.IImage[MainPage._iHelpStageNumHelpPages];
            for (int i = 0; i < MainPage._iHelpStageNumHelpPages; i++)
            {
                _HelpStageImages[i] = await LoadImageFromRes("bgd_instructions" + (i+1).ToString() + ".jpg");
            }
        }

        static public async Task<Microsoft.Maui.Graphics.IImage?> LoadImageFromRes(string filename)
        {
            Microsoft.Maui.Graphics.IImage? img = null;
            try
            {
                // Load the image stream from resources
                bool bExists = await FileSystem.Current.AppPackageFileExistsAsync(filename);
                if (bExists)
                {
                    using Stream stream = await FileSystem.Current.OpenAppPackageFileAsync(filename);
                    // Convert stream to IImage
                    img = PlatformImage.FromStream(stream);
                    stream.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
            }
            return img;
        }

        private int IntFromFileStream(StreamReader sr, int iDefaultValue)
        {
            string? s = sr.ReadLine();
            if (s == null)
                return iDefaultValue;
            int iResult;
            if (int.TryParse(s, out iResult))
                return iResult;
            return iDefaultValue;
        }

        private void LoadSettingsFromFile()
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.dat");
            if (!File.Exists(filePath))
                return;
            using (StreamReader sr = new StreamReader(filePath))
            {
                _iCurrentLevel = IntFromFileStream(sr, _iCurrentLevel);
                MainPage._iNextLevel = (_iCurrentLevel == _iTopLevel) ? 1 : (_iCurrentLevel + 1);
                _iTotalScore = IntFromFileStream(sr, _iTotalScore);
                sr.Close();
            }
        }

        private void SaveSettingsToFile()
        {
            string filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.dat");
            using (StreamWriter sw = new StreamWriter(filePath, append: false))
            {
                sw.WriteLine(_iCurrentLevel.ToString());
                sw.WriteLine(_iTotalScore.ToString());
                sw.Flush();
                sw.Close();
            }
        }

        private async Task LoadLevelFromFile()
        {
            string? line = null;
            using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync("level" + _iCurrentLevel.ToString("00") + ".dat");
            using StreamReader sr = new(fileStream);
            int version = int.Parse(sr.ReadLine()); // File version
            _sCurrentLevelName = sr.ReadLine(); // Level name
            _iMaxTime = int.Parse(sr.ReadLine());
            _iTargetScore = int.Parse(sr.ReadLine());
            _iFramesPerAnimationFrame = int.Parse(sr.ReadLine());
            _bCollisional = bool.Parse(sr.ReadLine());
            // Walls
            line = sr.ReadLine(); // "--Walls--"
            line = sr.ReadLine(); // Number of walls
            int.TryParse(line, out _iNumWalls);
            _Walls = new WallBase[_iNumWalls];
            string wallType;
            string[] wParas;
            float w = Ball2._rectBounds.Width;
            float h = Ball2._rectBounds.Height;
            for (int i = 0; i < _iNumWalls; i++)
            {
                wallType = sr.ReadLine(); // Type of wall
                wParas = ReadParameters(sr.ReadLine());
                switch (wallType)
                {
                    case "Arc":
                        _Walls[i] = new WallArc(new Vect2(w * double.Parse(wParas[0]), h * double.Parse(wParas[1])),
                                                w * double.Parse(wParas[2]), int.Parse(wParas[3]), int.Parse(wParas[4]),
                                                bool.Parse(wParas[5]), bool.Parse(wParas[6]), double.Parse(wParas[7]),
                                                bool.Parse(wParas[8]), int.Parse(wParas[9]));
                        break;
                    case "Box":
                        Rect r = new Rect(w * double.Parse(wParas[0]), h * double.Parse(wParas[1]),
                                          w * double.Parse(wParas[2]), h * double.Parse(wParas[3]));
                        _Walls[i] = new WallBox(r, bool.Parse(wParas[4]), bool.Parse(wParas[5]),
                                                   bool.Parse(wParas[6]), bool.Parse(wParas[7]),
                                                   double.Parse(wParas[8]), bool.Parse(wParas[9]), int.Parse(wParas[10]), int.Parse(wParas[11]));
                        break;
                    case "Horizontal":
                        _Walls[i] = new WallHorizontal(w * double.Parse(wParas[0]), w * double.Parse(wParas[1]), h * double.Parse(wParas[2]),
                                                       bool.Parse(wParas[3]), bool.Parse(wParas[4]), bool.Parse(wParas[5]),
                                                       bool.Parse(wParas[6]), double.Parse(wParas[7]),
                                                       bool.Parse(wParas[8]), int.Parse(wParas[9]));
                        break;
                    case "Straight":
                        _Walls[i] = new WallStraight(new Vect2(w * double.Parse(wParas[0]), h * double.Parse(wParas[1])),
                                                     new Vect2(w * double.Parse(wParas[2]), h * double.Parse(wParas[3])),
                                                     bool.Parse(wParas[4]), bool.Parse(wParas[5]), double.Parse(wParas[6]),
                                                     bool.Parse(wParas[7]), int.Parse(wParas[8]));
                        break;
                    case "Vertical":
                        _Walls[i] = new WallVertical(h * double.Parse(wParas[0]), h * double.Parse(wParas[1]), w * double.Parse(wParas[2]),
                                                     bool.Parse(wParas[3]), bool.Parse(wParas[4]), bool.Parse(wParas[5]),
                                                     bool.Parse(wParas[6]), double.Parse(wParas[7]),
                                                     bool.Parse(wParas[8]), int.Parse(wParas[9]));
                        break;
                }
            }

            // Make sure all linked walls are linked both forwards and backwards
            for (int i = 0; i < _iNumWalls; i++)
            {
                int j = _Walls[i]._iLinkedWallIndex;
                if (j >= 0)
                    _Walls[j]._iLinkedPrevWallIndex = i;
            }
            // Make sure each set of linked walls has a unique pair of colours so it can be seen that they're linked
            int iColourPairIndex = 0;
            for (int i = 0; i < _iNumWalls; i++)
            {
                if (_Walls[i].RecSetColourPair(iColourPairIndex))
                    iColourPairIndex++;
            }

            // Balls
            line = sr.ReadLine(); // "Balls"
            _iNumBalls = int.Parse(sr.ReadLine()); // Total number of balls
            _Balls = new Ball2[_iNumBalls];
            int iNumSetsBalls = int.Parse(sr.ReadLine()); // Number of sets of balls
            int iNumBallsInSet, iFirstBallInSet = 0;
            string ballSetType;
            string[] ballSetParas;
            RectF bounds, boundsSink;
            double velrange, radius;
            Vect2 gravity;
            Color colour;
            bool bEllipse;
            string sType, sBallPatternBmp;
            bool bKickable, bMoveable;
            for (int i = 0; i < iNumSetsBalls; i++)
            {
                ballSetType = sr.ReadLine(); // Type of ball set
                iNumBallsInSet = int.Parse(sr.ReadLine()); // Number of balls in this set
                ballSetParas = ReadParameters(sr.ReadLine());
                switch (ballSetType)
                {
                    case "Rnd":
                    case "Spin":
                        bounds = new RectF(w * float.Parse(ballSetParas[0]), h * float.Parse(ballSetParas[1]),
                                           w * float.Parse(ballSetParas[2]), h * float.Parse(ballSetParas[3]));
                        bEllipse = bool.Parse(ballSetParas[4]);
                        velrange = w * double.Parse(ballSetParas[5]);
                        radius = w * double.Parse(ballSetParas[6]);
                        gravity = new Vect2(w * double.Parse(ballSetParas[7]), h * double.Parse(ballSetParas[8]));
                        colour = Color.FromRgb(int.Parse(ballSetParas[9]), int.Parse(ballSetParas[10]), int.Parse(ballSetParas[11]));
                        boundsSink = new RectF(w * float.Parse(ballSetParas[12]), h * float.Parse(ballSetParas[13]),
                                               w * float.Parse(ballSetParas[14]), h * float.Parse(ballSetParas[15]));
                        sType = ballSetParas[16];
                        bKickable = bool.Parse(ballSetParas[17]);
                        if (ballSetType == "Rnd")
                            CreateRandomNonOverlappingBalls(iFirstBallInSet, iNumBallsInSet, bounds, bEllipse,
                                                            velrange, radius, gravity, colour, boundsSink, Ball2.TypeFromString(sType), bKickable);
                        else
                            CreateSpinningBalls(iFirstBallInSet, iNumBallsInSet, bounds,
                                                velrange, radius, gravity, colour, boundsSink, Ball2.TypeFromString(sType), bKickable);
                        break;
                    case "Grid":
                        bounds = new RectF(w * float.Parse(ballSetParas[0]), h * float.Parse(ballSetParas[1]),
                                           w * float.Parse(ballSetParas[2]), h * float.Parse(ballSetParas[3]));
                        Vect2 spa = new Vect2(w * float.Parse(ballSetParas[4]), h * float.Parse(ballSetParas[5]));
                        radius = w * double.Parse(ballSetParas[6]);
                        colour = Color.FromRgb(int.Parse(ballSetParas[7]), int.Parse(ballSetParas[8]), int.Parse(ballSetParas[9]));
                        boundsSink = new RectF(w * float.Parse(ballSetParas[10]), h * float.Parse(ballSetParas[11]),
                                               w * float.Parse(ballSetParas[12]), h * float.Parse(ballSetParas[13]));
                        sType = ballSetParas[14];
                        bKickable = bool.Parse(ballSetParas[15]);
                        bMoveable = bool.Parse(ballSetParas[16]);
                        CreateGridOfBalls(iFirstBallInSet, iNumBallsInSet, bounds,
                                          spa, radius, colour, boundsSink, Ball2.TypeFromString(sType), bKickable, bMoveable);
                        break;
                    case "Single":
                        Vect2 pos = new Vect2(w * float.Parse(ballSetParas[0]), h * float.Parse(ballSetParas[1]));
                        Vect2 vel = new Vect2(w * float.Parse(ballSetParas[2]), h * float.Parse(ballSetParas[3]));
                        radius = w * double.Parse(ballSetParas[4]);
                        gravity = new Vect2(w * double.Parse(ballSetParas[5]), h * double.Parse(ballSetParas[6]));
                        colour = Color.FromRgb(int.Parse(ballSetParas[7]), int.Parse(ballSetParas[8]), int.Parse(ballSetParas[9]));
                        boundsSink = new RectF(w * float.Parse(ballSetParas[10]), h * float.Parse(ballSetParas[11]),
                                               w * float.Parse(ballSetParas[12]), h * float.Parse(ballSetParas[13]));
                        sType = ballSetParas[14];
                        bKickable = bool.Parse(ballSetParas[15]);
                        bMoveable = bool.Parse(ballSetParas[16]);
                        double fMass = radius * radius * radius;
                        _Balls[iFirstBallInSet] = new Ball2(pos, vel, radius, fMass, gravity, colour, boundsSink, Ball2.TypeFromString(sType), false, bKickable, bMoveable);
                        break;
                    case "Bmp":
                        bounds = new RectF(w * float.Parse(ballSetParas[0]), h * float.Parse(ballSetParas[1]),
                                           w * float.Parse(ballSetParas[2]), h * float.Parse(ballSetParas[3]));
                        bEllipse = bool.Parse(ballSetParas[4]);
                        velrange = w * double.Parse(ballSetParas[5]);
                        radius = w * double.Parse(ballSetParas[6]);
                        gravity = new Vect2(w * double.Parse(ballSetParas[7]), h * double.Parse(ballSetParas[8]));
                        boundsSink = new RectF(w * float.Parse(ballSetParas[9]), h * float.Parse(ballSetParas[10]),
                                               w * float.Parse(ballSetParas[11]), h * float.Parse(ballSetParas[12]));
                        sType = ballSetParas[13];
                        bKickable = bool.Parse(ballSetParas[14]);
                        sBallPatternBmp = ballSetParas[15];
                        SetupRandomBallSetFromBallPatternBmp(iFirstBallInSet, iNumBallsInSet, bounds, bEllipse,
                                                        velrange, radius, gravity, boundsSink, Ball2.TypeFromString(sType), bKickable, sBallPatternBmp);
                        break;
                }
                iFirstBallInSet += iNumBallsInSet;
            }
            sr.Close();
            fileStream.Close();
            fileStream.Dispose();
        }

        private string[] ReadParameters(string line)
        {
            string lineCleaned = line.Replace("(", "").Replace(")", "");
            return lineCleaned.Split(',');
        }

        private async Task ReadColoursFromBallPatternBmpFile(string sBallPatternBmp)
        {
            using Stream fileStream = await FileSystem.Current.OpenAppPackageFileAsync(sBallPatternBmp + ".dat");
            using StreamReader sr = new(fileStream);

            int width = int.Parse(sr.ReadLine());
            int height = int.Parse(sr.ReadLine());
            _colBallPatternFromBmp = new Color[width, height];
            string? line = null;
            uint iCol;
            for (int y = 0; y < height; y++)
            {
                line = sr.ReadLine();
                for (int x = 0; x < width; x++)
                {
                    iCol = uint.Parse(line.Substring(x * 8, 8), System.Globalization.NumberStyles.HexNumber);
                    _colBallPatternFromBmp[x, y] = Color.FromUint(iCol);
                }
            }

            sr.Close();
            fileStream.Dispose();
        }


        private void SetupRandomBallSetFromBallPatternBmp(int iFirstBall, int iNumBalls, RectF rectBounds, bool bEllipse,
                                                     double velrange, double fRadius, Vect2 gravity, RectF rectBoundsSink, EBallType eType, bool bKickable, string sBallPatternBmp)
        {
            ReadColoursFromBallPatternBmpFile(sBallPatternBmp).Wait();
            Vect2 pos;
            int j;
            Ball2 ball;
            Vect2 centre = new Vect2(rectBounds.X + rectBounds.Width * 0.5f, rectBounds.Y + rectBounds.Height * 0.5f);
            float rMax2 = Math.Min(rectBounds.Width, rectBounds.Height) * 0.5f - (float)fRadius * 1.2f;
            rMax2 *= rMax2;
            double fMass = fRadius * fRadius * fRadius;
            Color col = Colors.Red;
            int xBmp, yBmp;
            double rBig = fRadius * 1.001;
            int bmpWid = _colBallPatternFromBmp.GetLength(0);
            int bmpHei = _colBallPatternFromBmp.GetLength(1);
            for (int i = iFirstBall; i < iFirstBall + iNumBalls; i++)
            {
                bool bKeepTrying = false;
                Vect2 vel = Vect2.RandomPointWithinSquare(velrange);
                do
                {
                    pos = Vect2.RandomPointWithinRect(rectBounds, rBig);

                    xBmp = (int)((pos.X - rectBounds.X - rBig) * bmpWid / (rectBounds.Width - rBig * 2.0));
                    if (xBmp >= bmpWid) xBmp = bmpWid - 1;
                    yBmp = (int)((pos.Y - rectBounds.Y - rBig) * bmpHei / (rectBounds.Height - rBig * 2.0));
                    if (yBmp >= bmpHei) yBmp = bmpHei - 1;

                    col = _colBallPatternFromBmp[xBmp, yBmp];
                    ball = new Ball2(pos, vel, fRadius, fMass, gravity, col, rectBoundsSink, eType, true, bKickable, true);
                    if (col.Alpha < 0.001f)
                    {
                        bKeepTrying = true;
                        continue;
                    }

                    bKeepTrying = false;
                    if (bEllipse && (pos - centre).LenSq > rMax2)
                    {
                        bKeepTrying = true;
                        continue;
                    }
                    if (_iCurrentLevel != _iTopLevel)
                    {
                        for (j = 0; j < i; j++)
                        {
                            if (ball.Overlaps(_Balls[j]))
                            {
                                bKeepTrying = true;
                                break;
                            }
                        }
                    }
                    if (!bKeepTrying && _Walls != null)
                    {
                        foreach (WallBase wall in _Walls)
                        {
                            if (wall.IsVisible && wall.IntersectsCircle(ball.Pos, ball.Radius))
                            {
                                bKeepTrying = true;
                                break;
                            }
                        }
                    }
                }
                while (bKeepTrying);
                _Balls[i] = ball;
            }
        }

        private void CreateRandomNonOverlappingBalls(int iFirstBall, int iNumBalls, RectF rectBounds, bool bEllipse,
                                                     double velrange, double fRadius, Vect2 gravity, Color col, RectF rectBoundsSink, EBallType eType, bool bKickable)
        {
            Vect2 pos;
            int j;
            Ball2 ball;
            Vect2 centre = new Vect2(rectBounds.X + rectBounds.Width * 0.5f, rectBounds.Y + rectBounds.Height * 0.5f);
            float rMax2 = Math.Min(rectBounds.Width, rectBounds.Height) * 0.5f - (float)fRadius * 1.2f;
            rMax2 *= rMax2;
            double fMass = fRadius * fRadius * fRadius;
            for (int i = iFirstBall; i < iFirstBall + iNumBalls; i++)
            {
                bool bKeepTrying = false;
                Vect2 vel = Vect2.RandomPointWithinSquare(velrange);
                do
                {
                    //double fRadius = _fBallRadiusMin + Vect2._Rnd.NextDouble() * (_fBallRadiusMax - _fBallRadiusMin);
                    pos = Vect2.RandomPointWithinRect(rectBounds, fRadius * 1.1);
                    ball = new Ball2(pos, vel, fRadius, fMass, gravity, col, rectBoundsSink, eType, true, bKickable, true);
                    bKeepTrying = false;
                    if (bEllipse && (pos - centre).LenSq > rMax2)
                    {
                        bKeepTrying = true;
                        continue;
                    }
                    if (_bCollisional)
                    {
                        for (j = 0; j < i; j++)
                        {
                            if (ball.Overlaps(_Balls[j]))
                            {
                                bKeepTrying = true;
                                break;
                            }
                        }
                    }
                    if (!bKeepTrying && _Walls != null)
                    {
                        foreach (WallBase wall in _Walls)
                        {
                            if (wall.IsVisible && wall.IntersectsCircle(ball.Pos, ball.Radius))
                            {
                                bKeepTrying = true;
                                break;
                            }
                        }
                    }
                }
                while (bKeepTrying);
                _Balls[i] = ball;
            }
        }

        private void CreateSpinningBalls(int iFirstBall, int iNumBalls, RectF rectBounds,
                                         double speed, double fRadius, Vect2 gravity, Color col, RectF rectBoundsSink, EBallType eType, bool bKickable)
        {
            Vect2 pos, vel;
            Vect2 centre = new Vect2(rectBounds.X + rectBounds.Width * 0.5f, rectBounds.Y + rectBounds.Height * 0.5f);
            float rMax = Math.Min(rectBounds.Width, rectBounds.Height) * 0.5f - (float)fRadius;
            double fMass = fRadius * fRadius * fRadius;
            for (int i = iFirstBall; i < iFirstBall + iNumBalls; i++)
            {
                int ang = (i - iFirstBall) * 360 / iNumBalls;
                pos = DrawFuncs._trigtable[ang];
                vel = speed * pos.RotatedRight90;
                pos = centre + (pos * rMax);
                _Balls[i] = new Ball2(pos, vel, fRadius, fMass, gravity, col, rectBoundsSink, eType, true, bKickable, true);
            }
        }

        private void CreateGridOfBalls(int iFirstBall, int iNumBalls, RectF rectBounds,
                                       Vect2 vSpacing, double fRadius, Color col, RectF rectBoundsSink, EBallType eType, bool bKickable, bool bMoveable)
        {
            Vect2 pos = new Vect2(rectBounds.Left, rectBounds.Top);
            Vect2 vel = new Vect2(0,0);
            double fMass = fRadius * fRadius * fRadius;
            Vect2 gravity = new Vect2(0, 0);
            for (int i = iFirstBall; i < iFirstBall + iNumBalls; i++)
            {
                _Balls[i] = new Ball2(pos, vel, fRadius, fMass, gravity, col, rectBoundsSink, eType, true, bKickable, bMoveable);
                pos.X += vSpacing.X;
                if(pos.X > rectBounds.Right)
                {
                    pos.X = rectBounds.Left;
                    pos.Y += vSpacing.Y;
                }
            }
        }

        private Color GetRandomColour()
        {
            int[] c = new int[3];
            for (int i = 0; i < 3; i++)
                c[i] = Vect2._Rnd.Next(256);
            if (c[0] + c[1] + c[2] < 255)
                c[Vect2._Rnd.Next(3)] = 255;
            return Color.FromRgb(c[0], c[1], c[2]);
        }

        public int NumVisibleBalls
        {
            get
            {
                int num = 0;
                if (_Balls != null)
                {
                    foreach (Ball2 ball in _Balls)
                    {
                        if (ball.Visible)
                            num++;
                    }
                }
                return num;
            }
        }

        public int NumHomeBalls
        {
            get
            {
                int num = 0;
                if (_Balls != null)
                {
                    foreach (Ball2 ball in _Balls)
                    {
                        if (ball.IsHome)
                            num++;
                    }
                }
                return num;
            }
        }
        public int TheTime
        {
            get
            {
                return _iCurrTime;
            }
            set
            {
                _iCurrTime = value;
            }
        }
        public int TotalScore
        {
            get
            {
                return _iTotalScore;
            }
        }
        public int TargetScore
        {
            get
            {
                return _iTargetScore;
            }
        }

        public int CurrentLevel
        {
            get
            {
                return _iCurrentLevel;
            }
        }
        public int TopLevel
        {
            get
            {
                return _iTopLevel;
            }
        }
        public bool IsRunning
        {
            get { return _bRunning; }
            set
            {
                _bRunning = value;
            }
        }
        public int AnimationFrame
        {
            get
            {
                return _iAnimatedFrameNum;
            }
        }
        public void DrawCurrentFrame(ICanvas canvas, RectF frameRect)
        {
            if (!_bGameInitialized)
                return;
            if (MainPage._iHelpStage > 0 && MainPage._iHelpStage <= MainPage._iHelpStageNumHelpPages)
            {
                canvas.DrawImage(_HelpStageImages[MainPage._iHelpStage - 1], 0, 0, frameRect.Width, frameRect.Height);
                return;
            }

            canvas.DrawImage(_BackgroundImage, 0, 0, frameRect.Width, frameRect.Height);

            if (_iCurrentLevel < _iTopLevel || _iCurrTime > -1)
            {
                if (_Walls != null)
                {
                    foreach (WallBase wall in _Walls)
                    {
                        wall.Draw(canvas);
                    }
                }
                if (_Balls != null)
                {
                    foreach (Ball2 ball in _Balls)
                    {
                        ball.Draw(canvas);
                    }
                }
            }

            if (!_bRunning && _iCurrentLevel != _iTopLevel)
            {
                canvas.FillColor = Color.FromRgba(192, 208, 255, 128);
                canvas.FillRectangle(frameRect);
            }
            else if (MainPage._This.SecretEndStageReached(false))
            {
                canvas.FillColor = Color.FromRgba(255, 0, 0, 192);
                canvas.FillRectangle(frameRect);
                canvas.FontSize = frameRect.Width * 0.1f;
                canvas.DrawString("SECRET SHORTCUT FOUND!", frameRect, HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            if (MainPage._bShowDebugInfo)
            {
                canvas.FontSize = 20;
                canvas.FontColor = Colors.White;
                canvas.DrawString(_iAnimatedFrameTime.ToString(), 0, 20, HorizontalAlignment.Left);
                canvas.DrawString("Down: " + _vTouchDownPos.X.ToString() + "," + _vTouchDownPos.Y.ToString(), 0, 50, HorizontalAlignment.Left);
                canvas.DrawString("Move: " + _vTouchMovePos.X.ToString() + "," + _vTouchMovePos.Y.ToString(), 0, 100, HorizontalAlignment.Left);
                canvas.DrawString("Up: " + _vTouchUpPos.X.ToString() + "," + _vTouchUpPos.Y.ToString(), 0, 150, HorizontalAlignment.Left);
                canvas.DrawString("Tap: " + _vTapPos.X.ToString() + "," + _vTapPos.Y.ToString(), 0, 200, HorizontalAlignment.Left);
            }
        }
        public void IncrementAnimationFrame()
        {
            if (!_bRunning || !_bGameInitialized)
                return;

            for (int i = 0; i < _iFramesPerAnimationFrame; i++)
            {
                IncrementFrame();
            }

            if (MainPage._bShowDebugInfo)
            {
                _iFrameTimeTotal += _iFrameTime;
                if (_iAnimatedFrameNum % _iAnimatedFrameTimeFrameSpan == 0)
                {
                    _iAnimatedFrameTime = _iFrameTimeTotal / _iAnimatedFrameTimeFrameSpan;
                    _iFrameTimeTotal = 0;
                }
            }
            _iAnimatedFrameNum++;
            if (_iAnimatedFrameNum % MainPage._iFramesPerSecond == 0)
            {
                _iCurrTime--;
                if (_iCurrTime < 0)
                {
                    IsRunning = false;
                    int iScore = NumHomeBalls;
                    if (iScore >= _iTargetScore || MainPage._This.SecretEndStageReached(true))
                    {
                        _iTotalScore += iScore;
                        if (_iCurrentLevel == _iTopLevel)
                        {
                            // End of the whole game
                            // Game over banner followed by return to level 1
                            //MainPage._This.GameOver(); // TODO: Special "You completed the game!" banner.
                            // TODO: Something more than just starting again
                            MainPage._iNextLevel = 1;
                        }
                        else
                        {
                            MainPage._This.EndOfLevel();
                            MainPage._iNextLevel = _iCurrentLevel + 1;
                        }
                        SaveSettingsToFile();
                    }
                    else
                    {
                        // TODO: Game over banner followed by return to this level
                        MainPage._This.GameOver();
                        MainPage._iNextLevel = _iCurrentLevel;
                    }
                    return;
                }
                MainPage._This.UpdateTheClockNextFrame();
                MainPage._This.UpdateTheDigitalTimeNextFrame();

                if (_iCurrentLevel == _iTopLevel)
                {
                    if (_iCurrTime == _iFinaLevelTimeWhenWallsVanish)
                    {
                        for(int i = 0; i < 4; i++)
                            _Walls[i].IsVisible = false;
                        for (int i = _iFinalLevel1stBrick; i < _iFinalLevel1stBrick + _iFinalLevelNumBricks; i++)
                            _Walls[i].IsVisible = true;
                        PrepareImages("a").Wait();
                    }
                    else if (_iCurrTime == 0)
                    {
                        for (int i = _iFinalLevel1stBrick; i < _iFinalLevel1stBrick + _iFinalLevelNumBricks; i++)
                            _Walls[i].IsVisible = false;
                        PrepareImages("b").Wait();
                    }
                }
            }
        }

        private void IncrementFrame()
        {
            if (_Balls != null && _Walls != null)
            {
                // Special cases
                // Air Hockey
                if(IsAirHockeyGame)
                {
                    AirHockeyAI();
                }

                // Collisions of balls with walls
                foreach (WallBase wall in _Walls)
                {
                    wall.Increment();
                }
                foreach (Ball2 ball in _Balls)
                {
                    ball.Increment();
                    foreach (WallBase wall in _Walls)
                    {
                        if (ball.Collidable && wall.CheckCollision(ball))
                            break;
                    }
                }

                // Inter-ball collisions
                if (_bCollisional)
                {
                    for (int i = 1; i < _iNumBalls; i++)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            _Balls[i].CollisionDetectionInterBall(_Balls[j]);
                        }
                    }
                }
            }
            _iFrameNum++;
        }

        public bool IsAirHockeyGame => (_iCurrentLevel == _iAirHockeyLevel);

        public void NextAirHockeyPuck()
        {
            if (!IsAirHockeyGame || !_bGameInitialized)
                return;
            if(_iAirHockeyCurrPuck < _iAirHockeyLastPuck)
            {
                _iAirHockeyCurrPuck++;
                _Balls[_iAirHockeyCurrPuck].MoveThisAirHockeyPuckIntoPlay();
            }
        }

        private void AirHockeyAI()
        {
            Ball2 ballHitter = _Balls[_iAirHockeyHitter];
            Ball2 ballPuck = _Balls[_iAirHockeyCurrPuck];
            double yBottom = (Ball2._rectBounds.Height * 776 / 1980) - ballHitter.Radius * 1.5; // Don't go beyond the blue line.
            double yTop = (Ball2._rectBounds.Height * 300 / 1980) - ballHitter.Radius; // Don't go beyond the goal line.
            double xCentre = Ball2._rectBounds.Width * 0.5;
            Vect2 vTarget = ballPuck.Pos;// - new Vect2(0.0, ballPuck.Radius);
            vTarget.X -= (ballPuck.Pos.X - xCentre) * ballPuck.Radius * 0.5 / xCentre;
            vTarget += (ballPuck.Vel * 40.0);

            if (ballHitter.Pos.Y < yTop)
                ballHitter.Vel = new Vect2(ballHitter.Pos.X, yTop + 0.001) - ballHitter.Pos;
            else if (vTarget.Y > ballHitter.Pos.Y && ballHitter.Pos.Y > yBottom)
            {
                ballHitter.Vel *= 0.03;
            }
            else
            {
                double dist = (ballHitter.Pos - ballPuck.Pos).Len;
                if (dist > (ballHitter.Radius + ballPuck.Radius) * 1.2)
                {
                    Vect2 vAim = (vTarget - ballHitter.Pos).Unit * (ballHitter.Radius * 0.008);
                    ballHitter.Vel += vAim;
                }
                else
                {
                    ballHitter.Vel *= 0.01;
                }
            }
        }

        public void OnTapped(PointF pntTouched)
        {
            if (_Balls == null || _Walls == null || !_bGameInitialized)
                return;
            _vTapPos = pntTouched;
            if (!ExplodeTouchSensitiveBallsThatAreTouched(_vTapPos))
            {
                foreach (WallBase wall in _Walls)
                {
                    if (wall.OnTouch(_vTapPos))
                        return;
                }
            }

            //if (_bKickBalls)
            //    KickBallsAwayFromPos(_vTapPos, 0.02, Ball2._rectBounds.Width * 0.25);
        }

        public void OnTouchDown(PointF pntTouched)
        {
            if (_Balls == null || _Walls == null || !_bGameInitialized)
                return;
            _vTouchDownPos = _vTouchMovePos = pntTouched;
            DragDragableBallThatIsTouched(_vTouchMovePos);
            //foreach (WallBase wall in _Walls)
            //{
            //    if (wall.OnTouch(_vTouchDownPos))
            //        return;
            //}

            //ExplodeTouchSensitiveBallsThatAreTouched(_vTouchDownPos);
        }

        public Ball2? BallBeingDragged
        {
            get
            {
                return _Balls == null || _iBallBeingDragged == -1 ? null : _Balls[_iBallBeingDragged];
            }
        }


        public void OnTouchMove(PointF pntTouched)
        {
            if (!_bGameInitialized)
                return;
            Vect2 vTouchMove = (Vect2)pntTouched - _vTouchMovePos;
            if (_bKickBalls)
                GuideBallsInTouchMoveDirection(_vTouchDownPos, vTouchMove, 0.01, Ball2._rectBounds.Width * 0.1);
            _vTouchMovePos = pntTouched;
            if (_iBallBeingDragged >= 0 && _Balls != null)
            {
                Vect2 vDiff = _vTouchMovePos - _Balls[_iBallBeingDragged].Pos;
                double r = _Balls[_iBallBeingDragged].Radius * 0.1;
                if (vDiff.LenSq > r * r)
                    _Balls[_iBallBeingDragged].Vel = vDiff * 0.2;
                else
                    _Balls[_iBallBeingDragged].Vel = new Vect2(0, 0);
            }
        }
        public void OnTouchUp(PointF pntTouched)
        {
            _vTouchUpPos = pntTouched;
            _iBallBeingDragged = -1;
        }

        public void OnTouchClock()
        {

        }

        private bool ExplodeTouchSensitiveBallsThatAreTouched(Vect2 touch)
        {
            int i = 0;
            int iNearestBall = -1;
            double rNearestBall = 1000000000.0;
            foreach (Ball2 ball in _Balls)
            {
                Vect2 vTouchToBall = ball.Pos - touch;
                double len2 = vTouchToBall.LenSq;
                if (ball.BallType == EBallType.TouchSensitive && ball.Visible)
                {
                    double r = Math.Max(ball.Radius, Ball2._rectBounds.Width * 0.07);
                    if (len2 < r * r && len2 < rNearestBall)
                    {
                        iNearestBall = i;
                        rNearestBall = len2;
                    }
                }
                i++;
            }
            if(iNearestBall >= 0)
            {
                Ball2 ball = _Balls[iNearestBall];
                ball.ExplodeMe();
                KickBallsAwayFromPos(touch, 0.04, ball.Radius * 4.0);
                return true;
            }
            return false;
        }

        private void DragDragableBallThatIsTouched(Vect2 touch)
        {
            if (!_bGameInitialized)
                return;
            _iBallBeingDragged = -1;
            int i = 0;
            foreach (Ball2 ball in _Balls)
            {
                if (ball.BallType == EBallType.Dragable)
                {
                    Vect2 vTouchToBall = ball.Pos - touch;
                    double len2 = vTouchToBall.LenSq;
                    if (len2 < ball.Radius * ball.Radius)
                    {
                        _iBallBeingDragged = i;
                        break;
                    }
                }
                i++;
            }
        }

        public void KickBallsAwayFromPos(Vect2 touch, double fKickStrength, double fBallKickRadius)
        {
            if (!_bGameInitialized)
                return;
            double fBallKickRadius2 = fBallKickRadius * fBallKickRadius;
            foreach (Ball2 ball in _Balls)
            {
                if (!ball.Kickable)
                    continue;
                Vect2 vTouchToBall = ball.Pos - touch;
                double len2 = vTouchToBall.LenSq;
                if (len2 > fBallKickRadius2 || len2 < 0.0000001)
                    continue;
                double len = Math.Sqrt(len2);
                Vect2 vTouchToBallUnit = vTouchToBall / len;
                Vect2 vKickBallPos = touch + (vTouchToBallUnit * fBallKickRadius);
                bool bKickTowardsAWall = false;
                foreach (WallBase wall in _Walls)
                {
                    if (wall.IntersectsLine(touch, ball.Pos) || wall.IntersectsLine(touch, vKickBallPos))
                    {
                        bKickTowardsAWall = true;
                        break;
                    }
                }
                if (bKickTowardsAWall)
                    continue;
                Vect2 vKick = vTouchToBallUnit * ((fBallKickRadius - len) * 0.02);
                ball.Vel += vKick;
            }
        }

        private void GuideBallsInTouchMoveDirection(Vect2 touch, Vect2 direction, double fKickStrength, double fBallKickRadius)
        {
            if (!_bGameInitialized)
                return;
            if (direction.LenSq < 1.0)
                return;
            Vect2 vKick = direction.Unit * (Ball2._rectBounds.Width * 0.001f);
            double fBallKickRadius2 = fBallKickRadius * fBallKickRadius;
            foreach (Ball2 ball in _Balls)
            {
                if (!ball.Kickable)
                    continue;
                Vect2 vTouchToBall = ball.Pos - touch;
                double len2 = vTouchToBall.LenSq;
                if (len2 > fBallKickRadius2 || len2 < 0.0000001)
                    continue;
                bool bWallInTheWay = false;
                foreach (WallBase wall in _Walls)
                {
                    if (wall.IntersectsLine(touch, ball.Pos))
                    {
                        bWallInTheWay = true;
                        break;
                    }
                }
                if (bWallInTheWay)
                    continue;
                ball.Vel += vKick;
            }
        }

        public void ExplodeNearbyFragileBalls(Ball2 ballExploding)
        {
            if (!_bGameInitialized)
                return;
            foreach (Ball2 ball in _Balls)
            {
                if (ball.Collidable && ball.BallType == EBallType.Fragile)
                {
                    Vect2 vPosToBall = ball.Pos - ballExploding.Pos;
                    double len2 = vPosToBall.LenSq;
                    double dist = ballExploding.Radius * 2.0 + ball.Radius;
                    if (len2 < dist * dist)
                    {
                        bool bExplode = true;
                        foreach (WallBase wall in _Walls)
                        {
                            if (wall.IntersectsLine(ballExploding.Pos, ball.Pos))
                            {
                                bExplode = false;
                                break;
                            }
                        }
                        if(bExplode)
                            ball.ExplodeMe();
                    }
                }
            }
        }
    }
}
