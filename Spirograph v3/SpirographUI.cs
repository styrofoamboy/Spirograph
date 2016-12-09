using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using RainstormStudios.Drawing.Spirograph;

namespace SpirographUI
{
    class SpirographUI : IDisposable
    {
        #region Declarations
        //***************************************************************************
        // Private Fields
        // 
        private bool
            _disposed = false,
            _formsMode = false,
            _shutdown = false;
        private Point
            _mouseXY;
        List<Spirograph>
            _screens;
        ManualResetEvent
            _mre;
        List<System.Windows.Forms.Form>
            _forms;
        #endregion

        #region Properties
        //***************************************************************************
        // Public Properties
        // 
        public int ScreenCount
        { get { return this._screens.Count; } }
        #endregion

        #region Class Constructors
        //***************************************************************************
        // Class Constructors
        // 
        public SpirographUI()
        {
            this._screens = new List<Spirograph>();
            this._forms = new List<System.Windows.Forms.Form>();
            this._mre = new ManualResetEvent(false);
        }
        ~SpirographUI()
        {
            this.Dispose(false);
        }
        #endregion

        #region Public Methods
        //***************************************************************************
        // Public Methods
        // 
        public void RunFullScreen()
        {
            this.Reset();
            this._formsMode = false;

            // We're running full screen, so we're going to initiallize a Spirograph instance for each screen.
            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                System.Windows.Forms.Form frm = new Form();
                frm.MouseMove += delegate { this.Stop(); };
                Rectangle rect = Screen.AllScreens[i].Bounds;
                frm.StartPosition = FormStartPosition.Manual;
                frm.SetDesktopBounds(rect.X, rect.Y, rect.Width, rect.Height);
#if DEBUG
#else
                frm.TopMost = true;
#endif
                frm.ShowInTaskbar = false;
                frm.ControlBox = false;
                frm.KeyPreview = true;
                frm.WindowState = FormWindowState.Maximized;
                frm.FormBorderStyle = FormBorderStyle.None;
                frm.Show();
                this._forms.Add(frm);
                this._screens.Add(new Spirograph(frm.Handle, Color.Black));
            }

            this.StartScreens();
            this.DoMeUntilExit();
        }
        public void RunFormsMode(System.Windows.Forms.Form frm)
        {
            this.Reset();
            this._formsMode = true;
            Spirograph spiro = new Spirograph(frm.Handle, Color.Black);
            frm.FormClosed += delegate
            {
                this.Stop();
            };
            //frm.ResizeEnd += delegate
            //{
            //    spiro.ResetDeviceContext();
            //};
            frm.SizeChanged += delegate
            {
                spiro.ResetDeviceContext();
            };
            frm.KeyPress += delegate
            {
                spiro.Reset();
            };
            frm.VisibleChanged += delegate
            {
                if (RainstormStudios.Unmanaged.Win32.IsWindowVisible(frm.Handle) && spiro.Paused)
                    spiro.Resume();
                else if (!RainstormStudios.Unmanaged.Win32.IsWindowVisible(frm.Handle) && !spiro.Paused)
                    spiro.Pause();
            };
            this._forms.Add(frm);
            this._screens.Add(spiro);
            this.StartScreens();
        }
        public void RunPreview(IntPtr hwnd)
        {
            this.Reset();
            while (RainstormStudios.Unmanaged.Win32.IsWindowVisible(hwnd))
            {
                Application.DoEvents();
                Thread.CurrentThread.Join(200);
            }
            this.Stop();
        }
        public void Stop()
        {
            this._shutdown = true;
            this._mre.Set();
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Private Methods
        //***************************************************************************
        // Private Methods
        // 
        protected virtual void StartScreens()
        {
            for (int i = 0; i < this._screens.Count; i++)
                this._screens[i].Start();
        }
        protected virtual void Reset()
        {
            for (int i = 0; i < this._screens.Count; i++)
                this._screens[0].Dispose();
            this._screens.Clear();
        }
        private void DoMeUntilExit()
        {

            // Record the current mouse pointer position.
            this._mouseXY = System.Windows.Forms.Cursor.Position;

            if (!this._formsMode)
                System.Windows.Forms.Cursor.Hide();

            try
            {
                while (!this._shutdown)
                {
                    this._mre.WaitOne(100);
                    if (this._shutdown)
                        break;

                    if (!this._formsMode)
                    {
                        // If we're not running "forms" mode, and the cursor has moved more than 10 pixels, terminate the thread.
                        if (System.Windows.Forms.Cursor.Position.X > this._mouseXY.X + 10
                            || System.Windows.Forms.Cursor.Position.X < this._mouseXY.X - 10
                            || System.Windows.Forms.Cursor.Position.Y > this._mouseXY.Y + 10
                            || System.Windows.Forms.Cursor.Position.Y < this._mouseXY.Y - 10)
                            this.Stop();
                    }
                    else
                    {
                        // If we are running in "forms" mode, we need to check and make sure that the form is still visible.
                        for (int i = 0; i < this._screens.Count; i++)
                            if (!RainstormStudios.Unmanaged.Win32.IsWindowVisible(this._screens[i].Handle))
                                // If any particular "screen" is not visible, then we're going to terminate the main thread.
                                this.Stop();
                    }
                }
                this.Reset();
            }
            finally
            {
                if (!this._formsMode)
                    System.Windows.Forms.Cursor.Show();
            }
        }
        protected virtual void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            if (disposing)
            {
                for (int i = 0; i < this._screens.Count; i++)
                    this._screens[i].Dispose();
                this._screens.Clear();
                this._mre.Dispose();
            }
            this._screens = null;
            this._mouseXY = Point.Empty;
            this._mre = null;
        }
        #endregion
    }
}
