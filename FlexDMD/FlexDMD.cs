﻿using Glide;
using FlexDMD.Actors;
using FlexDMD.Scenes;
using MediaFoundation;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace FlexDMD
{

    #region Interfaces

    /// <summary>
    /// DMDOjbect represents the DMD display.  It consists of a set of predefined, but very flexible scenes.  Some of these scenes are
    /// displayed immediately and replace whatever is currently displayed on the DMD.  Other scenes are placed into a queue, and displayed
    /// in the order in which they were queued.  This allows for more complex looking animations.
    /// 
    /// This early DMD interface attempts to expose a simplified object model for creating scenes with transitions, animations, video and text.
    /// The starting point is a set of 'scenes' required by many pinball machines.  Most scenes/animations that are not directly supported here,
    /// can be achieved by creating a video or animated GIF.
    /// 
    /// There is no direct access to the scene queue; but there is limited control through the ability to clear the queue, clear the
    /// currently rendering scene, or clear the currently rendering scene only if it matches a specific scene identifier.
    /// </summary>
    [Guid("6de5b6e5-717b-48d3-a890-d1d2320ddc43"), ComVisible(true)]
    public interface IDMDObject
    {
        #region Properties

        object RawDmdColoredPixels
        {
            [return: MarshalAs(UnmanagedType.Struct, SafeArraySubType = VarEnum.VT_ARRAY)]
            get;
        }

        object RawDmdPixels
        {
            [return: MarshalAs(UnmanagedType.Struct, SafeArraySubType = VarEnum.VT_ARRAY)]
            get;
        }

        #endregion

        #region Methods

        int RawDmdWidth();

        int RawDmdHeight();

        /// <summary>
        /// Init must be called before any other method.  It initializes the scene queue and internal state.
        /// </summary>
        void Init();

        /// <summary>
        /// Uninit currently does nothing; but in the future it is possible that some resources may need to be released.  This could be the
        /// best place to do that.
        /// </summary>
        void Uninit();

        /// <summary>
        /// The DMDObject interface will change over time.  Any time a breaking change is introduced, the major version will be incremented.
        /// Your table should be hard coded to an exact match to this major version.
        /// </summary>
        /// <returns>Integer major version</returns>
        Int32 GetMajorVersion();

        /// <summary>
        /// The DMDObject interface minor version will be rev'd on every non-breaking change.  The minor version returned should be greater
        /// than or equal to the version for which your table was authored.
        /// </summary>
        /// <returns>Integer minor version</returns>
        Int32 GetMinorVersion();

        /// <summary>
        /// The Build Number is an always increasing number.  It is a six digit base 10 number made up of Year Month Day YYMMDD.  It is
        /// provided for reference only.
        /// </summary>
        /// <returns>Integer build number</returns>
        Int32 GetBuildNumber();

        /// <summary>
        /// Some tables may want to allow the user to select whether or not to show the VirtualDMD.
        /// </summary>
        /// <returns>boolean previous visible state</returns>
        bool SetVisibleVirtualDMD(bool bHide);

        /// <summary>
        /// Some DMDs are displayed as a mirror image (Pin2K)
        /// </summary>
        /// <returns>boolean previous flipY state</returns>
        bool SetFlipY(bool flipY);

        /// <summary>
        /// Check to see if the DMD is currently rendering a video or animation.  If multiple scenes are queued, IsRendering will return
        /// true, even if the DMD is not currently animating.
        /// </summary>
        /// <returns></returns>
        bool IsRendering();

        /// <summary>
        /// Cancels all rendering and empties the scene queue.
        /// </summary>
        void CancelRendering();

        /// <summary>
        /// Cancels the specified scene if it is currently rendering.  The scene identifier is user defined.
        /// </summary>
        /// <param name="sceneId">User defined sceneId</param>
        void CancelRenderingWithId(string sceneId);

        /// <summary>
        /// This Clears the DMD display.  It does NOT clear the scene queue or stop any rendering.  It simply
        /// does a single wipe of the DMD and darkens all LEDs / pixels.
        /// </summary>
        void Clear();

        /// <summary>
        /// Your table's DMD assets should all go into a single folder, but this is no requirement.  The basePath is prepended
        /// to any filename supplied to any method.  It allows for specifying only filenames.  Set the project folder base
        /// path immediately after calling Init.  There is nothing wrong with setting a project folder, queueing a few scenes,
        /// then setting a different project folder.
        /// </summary>
        /// <param name="basePath">Asset base path</param>
        void SetProjectFolder(string basePath);

        /// <summary>
        /// SetVideoStretchMode sets the default display mode for how videos are displayed.  Videos do not need to be
        /// edited to have their aspect ratio matched to the DMD aspect ratio.  Videos can be 640x460, or 640x360, or
        /// any other resolution or aspect ratio.  The video stretch mode determines what horizontal section of the
        /// video will be displayed: top, middle, or bottom.  Alternately, it is possible to display the video distorted
        /// and stretch to fit the DMD aspect ratio.
        /// </summary>
        /// <param name="mode">mode - stretch: 0, crop to top: 1, crop to center: 2, crop to bottom: 3</param>
        void SetVideoStretchMode(Int32 mode);

        /// <summary>
        /// The scoreboard "scene" can be customized with a background image, and custom brightness for the current
        /// player, and other text.  The background is limited to a static image: video and animated gif are not
        /// yet supported.
        /// </summary>
        /// <param name="filename">background image must provide appropriate contrast for the scores and other text</param>
        /// <param name="selectedBrightness">current player brightness 0-15</param>
        /// <param name="unselectedBrightness">not selected players and other text 0-15</param>
        void SetScoreboardBackgroundImage(string filename, Int32 selectedBrightness, Int32 unselectedBrightness);

        /// <summary>
        /// The simplest form of animation is a series of still images.  This creates an animation which can loop, or not,
        /// over a comma separated list of image files.  The identifier returned, cast to a string, can be passed in as a background
        /// for supported scenes.
        /// </summary>
        /// <param name="fps">The speed at which the animation occurs, roughly in frames per second</param>
        /// <param name="loop">An animation can loop indefinitely</param>
        /// <param name="imagelist">Comma separated list of image file names</param>
        /// <returns>The returned animation identifier may be converted to a string, then passed in as a background</returns>
        Int32 CreateAnimationFromImages(Int32 fps, bool loop, string imagelist);//returns an identifier which may be cast to a string and passed as a background.  The imageList is a space separated list of png (or other) files

        /// <summary>
        /// Register a video into the DMD cache.
        /// </summary>
        /// <param name="videoStretchMode">See 'SetVideoStretchMode' for information on the videoStretchMode.</param>
        /// <param name="loop">A video can loop the video indefinitely</param>
        /// <param name="videoFilename">The video filename.</param>
        /// <returns>The returned identifier may be converted to a string, then passed in as a background</returns>
        Int32 RegisterVideo(Int32 videoStretchMode, bool loop, string videoFilename);

        /// <summary>
        /// Displays the UltraDMD animation sequence
        /// </summary>
        void DisplayVersionInfo();

        /// <summary>
        /// The basic scoreboard supports up to 4 players, with a field at the lower left and another at the lower right.  These lower
        /// text fields can be used to display the number of "credits", and "ball" number or "game over".  The scoreboard is one of the
        /// only scenes which displays on the DMD immediately before returning to the caller.  Most other scenes are queued.  If any
        /// other animation is queued, DisplayScoreboard will return immediately, without changing the DMD display.
        /// </summary>
        /// <param name="cPlayers">Count of players 0-4</param>
        /// <param name="highlightedPlayer">Highlighted player 0-4</param>
        /// <param name="score1">Score player 1 displayed in the upper left</param>
        /// <param name="score2">Score player 2 displayed in the upper right</param>
        /// <param name="score3">Score player 3 displayed beneath player 1</param>
        /// <param name="score4">Score player 4 displayed beneath player 2</param>
        /// <param name="lowerLeft">Text will appear left aligned at the bottom left</param>
        /// <param name="lowerRight">Text will appear right aligned at the bottom right</param>
        void DisplayScoreboard(Int32 cPlayers, Int32 highlightedPlayer, Int32 score1, Int32 score2, Int32 score3, Int32 score4, string lowerLeft, string lowerRight);

        /// <summary>
        /// Other than the scoreboard scene, Scene00 may be the only scene required for a pinball table.  It can
        /// display a background image, animated GIF, video, or animation image list returned from CreateAnimationFromImages.
        /// Over the background can be either a single line of text, or two lines of text (top line smaller than the 
        /// bottom line).  Each line of text can be a different brightness.  The whole scene can be transitioned
        /// onto the DMD display using any of the predefined animation types.  The scene will pause then optionally
        /// transition off the DMD display.
        /// </summary>
        /// <param name="background">Background image, animation image list, animated GIF or video</param>
        /// <param name="toptext">Top line of text, may be empty string</param>
        /// <param name="topBrightness">Top line brightness 0-15</param>
        /// <param name="bottomtext">Bottom line of text, may be empty string</param>
        /// <param name="bottomBrightness">Bottom line brightness 0-15</param>
        /// <param name="animateIn">Any of the predefined animation types</param>
        /// <param name="pauseTime">Pause before out animation, roughly in milliseconds</param>
        /// <param name="animateOut">Any of the predefined animation types, will execute after the specified pause time</param>
        void DisplayScene00(string background, string toptext, Int32 topBrightness, string bottomtext, Int32 bottomBrightness, Int32 animateIn, Int32 pauseTime, Int32 animateOut);

        /// <summary>
        /// Scene 00 with more options.  Specifically, the ability to display outline and/or fill font for either
        /// the top and/or bottom text.
        /// </summary>
        /// <param name="background">Background image, animation image list, animated GIF or video</param>
        /// <param name="toptext">Top line of text, may be empty string</param>
        /// <param name="topBrightness">Font fill brightness 0-15</param>
        /// <param name="topOutlineBrightness">Outline font brightness 0-15</param>
        /// <param name="bottomtext">Bottom line of text, may be empty string</param>
        /// <param name="bottomBrightness">Font fill brightness 0-15</param>
        /// <param name="bottomOutlineBrightness">Outline font brightness 0-15</param>
        /// <param name="animateIn">Any of the predefined animation types</param>
        /// <param name="pauseTime">Pause before out animation, roughly in milliseconds</param>
        /// <param name="animateOut">Any of the predefined animation types, will execute after the specified pause time</param>
        void DisplayScene00Ex(string background, string toptext, Int32 topBrightness, Int32 topOutlineBrightness, string bottomtext, Int32 bottomBrightness, Int32 bottomOutlineBrightness, Int32 animateIn, Int32 pauseTime, Int32 animateOut);

        /// <summary>
        /// Scene 00 with still more options.  By specifying a sceneId, it is possible to make updates to the text without
        /// queueing a different scene.  This allows for a continuously running animation with a changing text overlay.  This
        /// prevents flicker when making changes in quick succession.
        /// </summary>
        /// <param name="sceneId">User defined scene identifier</param>
        /// <param name="cancelPrevious">Allows replacing an existing scene</param>
        /// <param name="background">Background image, animation image list, animated GIF or video</param>
        /// <param name="toptext">Top line of text, may be empty string</param>
        /// <param name="topBrightness">Font fill brightness 0-15</param>
        /// <param name="topOutlineBrightness">Outline font brightness 0-15</param>
        /// <param name="bottomtext">Bottom line of text, may be empty string</param>
        /// <param name="bottomBrightness">Font fill brightness 0-15</param>
        /// <param name="bottomOutlineBrightness">Outline font brightness 0-15</param>
        /// <param name="animateIn">Any of the predefined animation types</param>
        /// <param name="pauseTime">Pause before out animation, roughly in milliseconds</param>
        /// <param name="animateOut">Any of the predefined animation types, will execute after the specified pause time</param>
        void DisplayScene00ExWithId(string sceneId, bool cancelPrevious, string background, string toptext, Int32 topBrightness, Int32 topOutlineBrightness, string bottomtext, Int32 bottomBrightness, Int32 bottomOutlineBrightness, Int32 animateIn, Int32 pauseTime, Int32 animateOut);

        /// <summary>
        /// Modifies the scene identified by sceneId, if the scene is currently being rendered.  If the
        /// currently rendering scene doesn't match sceneId, modify returns immediately without doing
        /// anything.
        /// </summary>
        /// <param name="id">Identifier for the scene to modify</param>
        /// <param name="toptext">Top text to modify</param>
        /// <param name="bottomtext">Bottom text to modify</param>
        void ModifyScene00(string id, string toptext, string bottomtext);

        /// <summary>
        /// Modifies the scene identified by sceneId, if the scene is currently being rendered.  If the
        /// currently rendering scene doesn't match sceneId, modify returns immediately without doing
        /// anything.  The scene will be extended to the new pauseTime.
        /// </summary>
        /// <param name="id">Identifier for the scene to modify</param>
        /// <param name="toptext">Top text to modify</param>
        /// <param name="bottomtext">Bottom text to modify</param>
        /// <param name="pauseTime">Set the PauseTime from now</param>
        void ModifyScene00Ex(string id, string toptext, string bottomtext, int pauseTime);

        /// <summary>
        /// Scene 01 is similar to Scene 00, except that only a single line of text is supported; and the
        /// text is displayed in a horizontally scrolling animated overlay.
        /// </summary>
        /// <param name="sceneId">User defined scene identifier</param>
        /// <param name="background">Background image, animation image list, animated GIF or video</param>
        /// <param name="text">Text to display</param>
        /// <param name="textBrightness">Text fill brightness 0-15</param>
        /// <param name="textOutlineBrightness">Text outline brightness 0-15</param>
        /// <param name="animateIn">Any of the predefined animation types</param>
        /// <param name="pauseTime">Pause before out animation, roughly in milliseconds</param>
        /// <param name="animateOut">Any of the predefined animation types, will execute after the specified pause time</param>
        void DisplayScene01(string sceneId, string background, string text, Int32 textBrightness, Int32 textOutlineBrightness, Int32 animateIn, Int32 pauseTime, Int32 animateOut);

        /// <summary>
        /// Simple text display.  This text scene is not queued.  It is displayed immediately.
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="textBrightness">Font fill brightness</param>
        /// <param name="textOutlineBrightness">Font outline brightness</param>
        void DisplayText(string text, Int32 textBrightness, Int32 textOutlineBrightness);

        /// <summary>
        /// Scrolling credits is a useful way of displaying multiple lines of text which scroll vertically.  This can be used to
        /// display high scores, credits, game instructions, etc.
        /// </summary>
        /// <param name="background">Background image, animation image list, animated GIF or video</param>
        /// <param name="text">Multiple lines of text.  Each line of text is separated by a '|' character.</param>
        /// <param name="textBrightness">Text brightness 0-15</param>
        /// <param name="animateIn">Any of the predefined animation types</param>
        /// <param name="pauseTime">Pause before out animation, roughly in milliseconds</param>
        /// <param name="animateOut">Any of the predefined animation types, will execute after the specified pause time</param>
        void ScrollingCredits(string background, string text, Int32 textBrightness, Int32 animateIn, Int32 pauseTime, Int32 animateOut);

        #endregion
    }

    [Guid("83fbf3e4-b4f4-415a-9a5b-7c2f635ff83b"), ComVisible(true), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    public interface IDMDObjectEvents
    {
        #region Events

        [DispId(1)]
        void onDMDChanged();

        #endregion
    }

    #endregion

    [Guid("766e10d3-dfe3-4e1b-ac99-c4d2be16e91f"), ComVisible(true), ClassInterface(ClassInterfaceType.None), ComSourceInterfaces(typeof(IDMDObjectEvents))]
    public class DMDObject : IDMDObject
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private ushort _width = 128;
        private ushort _height = 32;
        private string _basePath = "";
        private bool _visible = false;
        private bool _running = false;
        private Thread _processThread;
        private readonly Tweener _tweener = new Tweener();
        private readonly int _frameRate = 60;
        private int _stretchMode = 0;
        private Bitmap _frame = null;
        private Graphics _graphics = null;
        private readonly Group _stage = new Group();
        private readonly Dictionary<String, Actor> _preloads = new Dictionary<String, Actor>();
        private readonly Dictionary<String, Scene> _scenes = new Dictionary<String, Scene>();
        private int _nextId = 1;
        private ScoreBoard _scoreBoard;
        private SceneQueue _queue = new SceneQueue();
        private List<Action> _runnables = new List<Action>();
        private DMDDevice _dmd = new DMDDevice();
        private Actors.Font _font5, _font7, _font12;
        public delegate void OnDMDChangedDelegate();
        private event OnDMDChangedDelegate OnDMDChanged;
        private object[] _pixels, _coloredPixels;

        object IDMDObject.RawDmdColoredPixels
        {
            get
            {
                if (_coloredPixels == null) _coloredPixels = new object[_width * _height];
                return _coloredPixels;
            }
        }

        object IDMDObject.RawDmdPixels
        {
            get
            {
                if (_pixels == null) _pixels = new object[_width * _height];
                return _pixels;
            }
        }

        public void Init()
        {
            HResult hr = MFExtern.MFStartup(0x10070, MFStartup.Full);
            if (hr < 0) log.Error("Failed to initialize Microsoft Media Foundation: {0}", hr);
            _running = true;
            _font5 = Actors.Font.LoadFromRessource("FlexDMD.Resources.font-5.fnt");
            _font7 = Actors.Font.LoadFromRessource("FlexDMD.Resources.font-7.fnt");
            _font12 = Actors.Font.LoadFromRessource("FlexDMD.Resources.font-12.fnt");
            _frame = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);
            // _pixels = Marshal.AllocHGlobal(_width * _height * 3 * 16);
            // _pixels = SafeArrayCreateVector(VarEnum.VT_VARIANT, 0, _width * _height);
            _graphics = Graphics.FromImage(_frame);
            _scoreBoard = new ScoreBoard(_font7, _font12, _font5);
            _scoreBoard.SetSize(_width, _height);
            _stage.AddActor(_scoreBoard);
            _stage.AddActor(_queue);
            SetVisibleVirtualDMD(true);
            Clear();
            _processThread = new Thread(new ThreadStart(RenderLoop));
            _processThread.IsBackground = true;
            _processThread.Start();

        }

        public void Uninit()
        {
            log.Info("Uninit");
            _running = false;
            _processThread.Join();
            _processThread = null;
            // Marshal.FreeHGlobal(_pixels);
            SetVisibleVirtualDMD(false);
            _graphics.Dispose();
            _graphics = null;
            _frame.Dispose();
            _frame = null;
            HResult hr = MFExtern.MFShutdown();
            if (hr < 0) log.Error("Failed to dispose Microsoft Media Foundation: {0}", hr);
        }

        public void RenderLoop()
        {
            Stopwatch stopWatch = new Stopwatch();
            double elapsedMs = 0.0;
            while (_running)
            {
                stopWatch.Restart();
                float elapsedS = (float)(elapsedMs / 1000.0);
                _stage.SetSize(_width, _height);
                lock (_runnables)
                {
                    _runnables.ForEach(item => item());
                    _runnables.Clear();
                }
                _tweener.Update(elapsedS);
                _stage.Update(elapsedS);
                _scoreBoard._visible &= !_queue.IsRendering();
                if (_visible)
                {
                    _stage.Draw(_graphics);
                    Rectangle rect = new Rectangle(0, 0, _frame.Width, _frame.Height);
                    BitmapData data = _frame.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, _frame.PixelFormat);
                    _dmd.RenderRgb24(_width, _height, data.Scan0);
                    if (_pixels != null)
                    {
                        unsafe
                        {
                            byte* ptr = ((byte*)data.Scan0.ToPointer());
                            int pos = 0;
                            for (int y = 0; y < _height; y++)
                            {
                                for (int x = 0; x < _width; x++)
                                {
                                    byte r = *ptr;
                                    ptr++;
                                    byte g = *ptr;
                                    ptr++;
                                    byte b = *ptr;
                                    ptr++;
                                    // _pixels[y * _width + x] = (byte)((r + g + b) / 3);
                                    float v = 0.2126f * r + 0.7152f * g + 0.0722f * b;
                                    if (v > 0.99f) v = 0.99f;
                                    _pixels[pos] = (byte)(v);
                                    pos++;
                                }
                            }
                        }
                    }
                    if (_coloredPixels != null)
                    {
                        unsafe
                        {
                            byte* ptr = ((byte*)data.Scan0.ToPointer());
                            int pos = 0;
                            for (int y = 0; y < _height; y++)
                            {
                                for (int x = 0; x < _width; x++)
                                {
                                    byte r = *ptr;
                                    ptr++;
                                    byte g = *ptr;
                                    ptr++;
                                    byte b = *ptr;
                                    ptr++;
                                    _coloredPixels[pos] = (uint)((b << 16) + (g << 8) + r);
                                    pos++;
                                }
                            }
                        }
                    }
                    _frame.UnlockBits(data);
                    OnDMDChanged?.Invoke();
                }
                double renderingDuration = stopWatch.Elapsed.TotalMilliseconds;

                int sleepMs = (1000 / _frameRate) - (int)renderingDuration;
                if (sleepMs > 0) Thread.Sleep(sleepMs);
                elapsedMs = stopWatch.Elapsed.TotalMilliseconds;
                // log.Info("Elapsed: {0}ms", elapsedMs);
            }
        }

        public int GetMajorVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileMajorPart;
        }

        public int GetMinorVersion()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileMinorPart;
        }

        public int GetBuildNumber()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileBuildPart * 10000 + fvi.FilePrivatePart;
        }

        public int RawDmdWidth()
        {
            return _width;
        }

        public int RawDmdHeight()
        {
            return _height;
        }

        public bool SetVisibleVirtualDMD(bool bVisible)
        {
            log.Info("SetVisibleVirtualDMD({0})", bVisible);
            bool wasVisible = _visible;
            _visible = bVisible;
            if (!wasVisible && _visible)
            {
                _dmd.Open();
            }
            else if (wasVisible && !_visible)
            {
                _dmd.Close();
            }
            return wasVisible;
        }

        public bool SetFlipY(bool flipY)
        {
            log.Error("SetFlipY is not yet supported in FlexDMD");
            return false;
        }

        public bool IsRendering()
        {
            return _queue.IsRendering();
        }

        public void CancelRendering()
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    log.Info("CancelRendering");
                    _queue.CancelRendering();
                });
            }
        }

        public void CancelRenderingWithId(string sceneId)
        {
            var scene = _scenes[sceneId];
            if (scene != null)
            {
                log.Info("CancelRenderingWithId {0}", sceneId);
                lock (_runnables)
                {
                    _runnables.Add(() =>
                    {
                        // _queue.CancelRendering(scene);
                    });
                }
            }
        }

        public void Clear()
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    log.Info("Clear");
                    _scoreBoard._visible = false;
                    _graphics.Clear(Color.Black);
                });
            }
        }

        public void SetProjectFolder(string basePath)
        {
            log.Info("SetProjectFolder {0}", basePath);
            _basePath = basePath;
        }

        public void SetVideoStretchMode(int mode)
        {
            log.Info("SetVideoStretchMode {0}", mode);
            _stretchMode = mode;
        }

        public int CreateAnimationFromImages(int fps, bool loop, string imagelist)
        {
            var id = _nextId;
            _nextId++;
            var video = new AnimatedImage(_basePath, imagelist, fps, loop);
            _preloads.Add(id.ToString(), video);
            return id;
        }

        public int RegisterVideo(int videoStretchMode, bool loop, string videoFilename)
        {
            var id = _nextId;
            _nextId++;
            var video = new Video(videoFilename, loop, videoStretchMode);
            _preloads.Add(id.ToString(), video);
            return id;
        }

        private Actor ResolveImage(string filename)
        {
            // filename can be a preloaded id, or a comma separated image list, or a filename to an image, gif or video file.
            if (_preloads.ContainsKey(filename)) return _preloads[filename];
            var fullPath = System.IO.Path.Combine(_basePath, filename);
            if (File.Exists(fullPath))
            {
                string extension = Path.GetExtension(filename).ToLowerInvariant().Remove(0, 1);
                if (extension.Equals("png") || extension.Equals("jpg") || extension.Equals("jpeg") || extension.Equals("bmp"))
                {
                    return new Image(fullPath);
                }
                if (extension.Equals("wmv") || extension.Equals("avi") || extension.Equals("mp4"))
                {
                    return new Video(fullPath, false, _stretchMode);
                }
            }
            return new AnimatedImage(_basePath, filename, 25, false);
        }

        public void DisplayVersionInfo()
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    _scoreBoard._visible = false;
                    // scene.SetSize(_width, _height);
                    // _queue.Enqueue(scene);
                });
            }
        }

        public void DisplayScene00(string background, string toptext, int topBrightness, string bottomtext, int bottomBrightness, int animateIn, int pauseTime, int animateOut)
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    _scoreBoard._visible = false;
                    log.Info("DisplayScene00 '{0}', '{1}', {2}, '{3}', {4}, {5}, {6}, {7}", background, toptext, topBrightness, bottomtext, bottomBrightness, animateIn, pauseTime, animateOut);
                    var scene = new Scene00(ResolveImage(background), toptext, _font7, topBrightness, bottomtext, _font12, bottomBrightness, AnimationType.None, pauseTime / 1000f, AnimationType.None, "");
                    _queue.Enqueue(scene);
                });
            }
        }

        public void DisplayScene00Ex(string background, string toptext, int topBrightness, int topOutlineBrightness, string bottomtext, int bottomBrightness, int bottomOutlineBrightness, int animateIn, int pauseTime, int animateOut)
        {
            DisplayScene00ExWithId("", false, background, toptext, topBrightness, topOutlineBrightness, bottomtext, bottomBrightness, bottomOutlineBrightness, animateIn, pauseTime, animateOut);
        }

        public void DisplayScene00ExWithId(string sceneId, bool cancelPrevious, string background, string toptext, int topBrightness, int topOutlineBrightness, string bottomtext, int bottomBrightness, int bottomOutlineBrightness, int animateIn, int pauseTime, int animateOut)
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    _scoreBoard._visible = false;
                    // scene.SetSize(_width, _height);
                    // _queue.Enqueue(scene);
                });
            }
        }

        public void DisplayScene01(string sceneId, string background, string text, int textBrightness, int textOutlineBrightness, int animateIn, int pauseTime, int animateOut)
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    _scoreBoard._visible = false;
                    // scene.SetSize(_width, _height);
                    // _queue.Enqueue(scene);
                });
            }
        }

        public void SetScoreboardBackgroundImage(string filename, int selectedBrightness, int unselectedBrightness)
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    _scoreBoard.SetBackground(ResolveImage(filename));
                    _scoreBoard._selectedBrightness = selectedBrightness;
                    _scoreBoard._unselectedBrightness = unselectedBrightness;
                });
            }
        }

        public void DisplayScoreboard(int cPlayers, int highlightedPlayer, int score1, int score2, int score3, int score4, string lowerLeft, string lowerRight)
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    // Direct rendering: render only if the scene queue is empty, and no direct rendering has happened
                    log.Info("Scoreboard for {0} players, {1} is playing", cPlayers, highlightedPlayer);
                    _scoreBoard._visible = true;
                    _scoreBoard.SetNPlayers(cPlayers);
                    _scoreBoard.SetHighlightedPlayer(highlightedPlayer);
                    _scoreBoard.SetScore(score1, score2, score3, score4);
                    _scoreBoard._lowerLeft.Text = lowerLeft;
                    _scoreBoard._lowerRight.Text = lowerRight;
                });
            }
        }

        public void ModifyScene00(string id, string toptext, string bottomtext)
        {
        }

        public void ModifyScene00Ex(string id, string toptext, string bottomtext, int pauseTime)
        {
        }

        public void DisplayText(string text, int textBrightness, int textOutlineBrightness)
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    _scoreBoard._visible = false;
                    // Direct rendering
                });
            }
        }

        public void ScrollingCredits(string background, string text, int textBrightness, int animateIn, int pauseTime, int animateOut)
        {
            lock (_runnables)
            {
                _runnables.Add(() =>
                {
                    _scoreBoard._visible = false;
                    // scene.SetSize(_width, _height);
                    // _queue.Enqueue(scene);
                });
            }
        }
    }
}