//  Copyright (c) 2008, Michael unfried
//  Email:  serbius3@gmail.com
//  All rights reserved.

//  Redistribution and use in source and binary forms, with or without modification, 
//  are permitted provided that the following conditions are met:

//  Redistributions of source code must retain the above copyright notice, 
//  this list of conditions and the following disclaimer. 
//  Redistributions in binary form must reproduce the above copyright notice, 
//  this list of conditions and the following disclaimer in the documentation 
//  and/or other materials provided with the distribution. 

//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using RainstormStudios;

namespace RainstormStudios.Drawing.Spirograph
{
    public class Spirograph : IDisposable
    {
        #region Declarations
        //***************************************************************************
        // Constants
        // 
        private const int
            iMax = 20000,
            bMax = 200, bMin = 5;
        private const double
            P2Offset = 1.4;
        //***************************************************************************
        // Private Fields
        // 
        private IntPtr
            _hwnd;
        Unmanaged.DeviceContext
            _devCtx;
        private Graphics
            _gfx = null;
        Rectangle
            _bounds;
        private RenderState
            _state = RenderState.NoActivity;
        private Thread
            _drawThread = null;
        private bool
            _disposed = false,
            _shuttingDown = false,
            _running = false,
            _paused = false;
        private double
            x1 = 0, y1 = 0,
            x2 = 0, y2 = 0;
        private double
            fx1, fy1,
            fx2, fy2,
            rx1, ry1,
            rx2, ry2;
        private double
            ch, ra, rb;
        private int
            i = 0, _scrnSize;
        private double
            t = 0, ot = 0;
        private PointF
            P1, P2, lP1, lP2;
        private int
            xCenter = 0,
            yCenter = 0,
            fpsLimit = 120;
        private RgbColor
            clr1 = new RgbColor(RgbColor.RandomColor()),
            clr2 = new RgbColor(RgbColor.RandomColor());
        private Color
            _bgColor;
        private bool
            finishedPic = false,
            saveInProgress = false;
        //***************************************************************************
        // Public Events
        // 
        public event EventHandler
            ShuttingDown;
        public event PaintEventHandler
            PaintStart;
        public event PaintEventHandler
            PaintComplete;
        public event ExceptionEventHandler
            Exception;
        #endregion

        #region Properties
        //***************************************************************************
        // Public Properties
        // 
        public RenderState RenderStatus
        { get { return this._state; } }
        public int RenderStatusCode
        { get { return (int)this._state; } }
        public IntPtr Handle
        { get { return this._hwnd; } }
        public bool Paused
        { get { return this._paused; } }
        #endregion

        #region Class Constructors
        //***************************************************************************
        // Class Constructors
        // 
        public Spirograph(IntPtr windowHandle, Color backgroundColor)
        {
            this._hwnd = windowHandle;
            this._bgColor = backgroundColor;
            this.Init();
        }
        ~Spirograph()
        {
            this.Dispose(false);
        }
        #endregion

        #region Public Methods
        //***************************************************************************
        // Public Methods
        // 
        public void Start()
        {
            this._drawThread.Start();
        }
        public void Pause()
        {
            if (this._running)
            {
                // If the draw thread is running, pause it and wait for any current render operation to complete.
                this._paused = true;
                while (this._state == RenderState.RenderInProgress && !this._shuttingDown)
                    Thread.SpinWait(200);
            }
        }
        public void Resume()
        {
            this._paused = false;
        }
        public void Stop()
        {
            this._shuttingDown = true;
            this._state = RenderState.ShuttingDown;
            if (this._drawThread != null)
            {
                this._drawThread.Abort();
                this._drawThread.Join(15000);
            }
        }
        public void Dispose()
        {
            this.Stop();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        public void SaveScreen()
        {
            try
            {
                Bitmap scrCap = new Bitmap((int)this._gfx.ClipBounds.Width, (int)this._gfx.ClipBounds.Height);
                Graphics bg = Graphics.FromImage(scrCap);
                try
                {
                    string basePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Spiralific");
                    if (!System.IO.Directory.Exists(basePath))
                        System.IO.Directory.CreateDirectory(basePath);
                    int flNum = System.IO.Directory.GetFiles(basePath, "spiral*.png", System.IO.SearchOption.TopDirectoryOnly).Length;
                    string fileName = "spiralific" + flNum.ToString().PadLeft(3, '0') + ".png";
                    bg.CopyFromScreen(new Point(0, 0), new Point(0, 0), Size.Truncate(scrCap.PhysicalDimension), CopyPixelOperation.SourceCopy);
                    using (System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(basePath, fileName), System.IO.FileMode.Create, System.IO.FileAccess.Write))
                        scrCap.Save(fs, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                finally
                {
                    bg.Dispose();
                    scrCap.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
            finally
            {
                saveInProgress = false;
            }
        }
        public void Reset()
        {
            this.Pause();
            this.ResetVars();
            this.Resume();
        }
        public void ResetDeviceContext()
        {
            this.Pause();

            this._gfx.Dispose();
            this._devCtx.Dispose();

            this.Init();
            this.ResetVars();
            this.Resume();
        }
        #endregion

        #region Private Methods
        //***************************************************************************
        // Private Methods
        // 
        private void Init()
        {
            this._state = RenderState.InitializingDevice;
            try
            {
                this._devCtx = Unmanaged.DeviceContext.GetWindow(this._hwnd);

                this._gfx = this._devCtx.GetGraphics();
                this._gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                this._gfx.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                this._gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                this._gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                this._gfx.Clear(this._bgColor);

                this._bounds = Unmanaged.Win32.GetWindowRect(this._hwnd);

                this.yCenter = this._bounds.Height / 2;
                this.xCenter = this._bounds.Width / 2;
                this._scrnSize = (int)((System.Math.Min(this._bounds.Height, this._bounds.Width) / 2) * 0.9);

                this._drawThread = new Thread(new ThreadStart(this.DrawingThreadWorker));
                this._drawThread.IsBackground = true;

                // We need to sleep this thread until the preview window is visible.
                DateTime dtWaitStart = DateTime.Now;
                while (Unmanaged.Win32.IsWindowVisible(this._hwnd) == false)
                {
                    Thread.CurrentThread.Join(200);
                    if (DateTime.Now.Subtract(dtWaitStart).TotalSeconds > 15)
                        break;
                }

                if (!Unmanaged.Win32.IsWindowVisible(this._hwnd))
                    throw new Exception("Timeout period reached, waiting for window to become visible.");

                this.ResetVars();
                this._state = RenderState.Ready;
            }
            catch (Exception ex)
            {
                this.OnException(new ExceptionEventArgs(ex));
                this._state = RenderState.RenderError;
            }
        }
        private void DrawingThreadWorker()
        {
            try
            {
                this._running = true;
                DateTime dtThreadStart = DateTime.Now;
                DateTime dtFrameStart = DateTime.Now;
                TimeSpan tsTaken = TimeSpan.MinValue;
                while (!this._shuttingDown && Unmanaged.Win32.IsWindowVisible(this._hwnd) == true)
                {
                    // Reset to "ready" state.
                    this._state = RenderState.Ready;

                    // Create the args object for our events.
                    PaintEventArgs args = new PaintEventArgs(this._hwnd, this.x1, this.y1, this.x2, this.y2, this.i, this.t, this.ra, this.rb, this.ch);

                    // If we're limiting framerate, record "right now".
                    if (this.fpsLimit > 0)
                        dtFrameStart = DateTime.Now;

                    // If the user requested a screen capture, we'll wait for that to finish.
                    // We put the thread to sleep for 200ms at a time here to keep from burning
                    //   processor cycles by looping.
                    while (this.saveInProgress || this._paused)
                    {
                        this._state = RenderState.NoActivity;
                        // We use Thread.SpinWait here to stall for a few ticks without causing
                        //   the thread to transition into kernel mode.
                        Thread.SpinWait(200);
                    }

                    // Trigger the "PaintStart" event
                    this.OnPaintStart(args);

                    // Now, we do the "real" stuff.
                    this.DrawLine();

                    // Done, set "success" state.
                    this._state = RenderState.RenderSuccess;

                    // And then sleep to let the other threads have their turn.
                    tsTaken = DateTime.Now.Subtract(dtFrameStart);
                    int thdDly = (this.fpsLimit > 0)
                            ? (int)System.Math.Max((1000 / fpsLimit) - tsTaken.TotalMilliseconds, 0)
                            : 0;
                    Thread.CurrentThread.Join(thdDly);
                }
            }
            catch (Exception ex)
            {
                this.OnException(new ExceptionEventArgs(ex));
                this._state = RenderState.RenderError;
            }
            this._running = false;
        }
        private void DrawLine()
        {
            this._state = RenderState.RenderInProgress;

            i++;
            if (i > iMax)
                ResetVars();

            this.t = this.i.ToRadians();
            this.ot = (this.i - 1).ToRadians();

            x1 = xCenter + ((ra - rb) * System.Math.Cos(t) + ch * System.Math.Cos(((ra - rb) / rb) * t));
            y1 = yCenter + ((ra - rb) * System.Math.Sin(t) + ch * System.Math.Sin(((ra - rb) / rb) * t));
            x2 = xCenter + ((ra - (rb * P2Offset)) * System.Math.Cos(t) + (ch * P2Offset) * System.Math.Cos(((ra - (rb * P2Offset)) / (rb * P2Offset)) * ot));
            y2 = yCenter + ((ra - (rb * P2Offset)) * System.Math.Sin(t) + (ch * P2Offset) * System.Math.Sin(((ra - (rb * P2Offset)) / (rb * P2Offset)) * ot));

            P1.X = (float)x1; P1.Y = (float)y1;
            P2.X = (float)x2; P2.Y = (float)y2;

            if (i == 1)
            {
                fx1 = x1; fy1 = y1;
                fx2 = x2; fy2 = y2;
            }
            else if (i > 3)
            {
                try
                {
                    using (SolidBrush brush = new SolidBrush(clr1.Color))
                    using (Pen linePen = new Pen(brush))
                        this._gfx.DrawLine(linePen, P1, lP1);
                    using (SolidBrush brush = new SolidBrush(clr2.Color))
                    using (Pen linePen = new Pen(brush))
                        this._gfx.DrawLine(linePen, P2, lP2);

                    if (x1 == fx1 || y1 == fy1 || x2 == fx2 || y2 == fy2)
                    {
                        // If we manage to actually reach the exact spot where we started,
                        //   then wait three seconds and start a new shape.
                        Thread.CurrentThread.Join(5000);
                        ResetVars();
                    }
                }
                catch
#if DEBUG
                    (Exception ex)
#endif
                {
#if DEBUG
                    System.Diagnostics.Debug.Write("Fatal error: " + ex.ToString());
#endif
                    throw;
                }
            }

            lP1.X = P1.X; lP1.Y = P1.Y;
            lP2.X = P2.X; lP2.Y = P2.Y;

            if (i == 400)
                SaveVars();

            clr1.CycleColor();
            clr2.CycleColor();
        }
        private void ResetVars()
        {
            this._state = RenderState.NotReady;
            LoadVars();
            try
            {
                rb = MotionPoint.Rand(bMin, bMax);
                ra = MotionPoint.Rand((int)(this._scrnSize * 0.2), this._scrnSize);
                ch = MotionPoint.Rand(bMin / 2, (int)rb + bMax);
                i = 1;
                if (finishedPic)
                {
                    saveInProgress = true;
                    SaveScreen();
                    finishedPic = false;
                }
                this._gfx.Clear(this._bgColor);

#if DEBUG
                this._gfx.DrawEllipse(System.Drawing.Pens.Green, xCenter - (int)(ra / 2), yCenter - (int)(ra / 2), (int)ra, (int)ra);
                this._gfx.DrawEllipse(System.Drawing.Pens.Blue, xCenter - (int)(rb / 2), yCenter - (int)(rb / 2), (int)rb, (int)rb);
                this._gfx.DrawEllipse(System.Drawing.Pens.Red, xCenter - (int)(ch / 2), yCenter - (int)(ch / 2), (int)ch, (int)ch);
#endif
            }
            catch (Exception ex)
            {
                this.OnException(new ExceptionEventArgs(ex));
                this._state = RenderState.RenderError;
            }
        }
        private void SaveVars()
        {
            this._state = RenderState.NotReady;
            rx1 = x1; ry1 = y1;
            rx2 = x2; ry2 = y2;
        }
        private void LoadVars()
        {
            this._state = RenderState.NotReady;
            x1 = rx1; y1 = ry1;
            x2 = rx2; y2 = ry2;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            if (disposing)
            {
                if (this._shuttingDown)
                    this.Stop();
                this._gfx.Dispose();
                this._devCtx.Dispose();
            }
            this._drawThread = null;
            this._gfx = null;
            this._devCtx = null;
            this._disposed = true;
        }
        //***************************************************************************
        // Event Triggers
        // 
        protected virtual void OnShuttingDown(EventArgs e)
        {
            if (this.ShuttingDown != null)
                this.ShuttingDown.Invoke(this, e);
        }
        protected virtual void OnPaintStart(PaintEventArgs e)
        {
            if (this.PaintStart != null)
                this.PaintStart.Invoke(this, e);
        }
        protected virtual void OnPaintComplete(PaintEventArgs e)
        {
            if (this.PaintComplete != null)
                this.PaintComplete.Invoke(this, e);
        }
        protected virtual void OnException(ExceptionEventArgs e)
        {
            if (this.Exception != null)
                this.Exception.Invoke(this, e);
        }
        #endregion
    }
}
