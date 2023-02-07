using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace ContrastEnhancer
{
    public partial class Form1 : Form {
        public static class RegHK {
            [DllImport("user32.dll")]
            public static extern int RegisterHotKey(IntPtr hwnd, int id, int fsModifiers, int vk);
            [DllImport("user32.dll")]
            public static extern int UnregisterHotKey(IntPtr hwnd, int id);

            public const int MOD_ALT = 0x0001;
            public const int MOD_CONTROL = 0x0002;
            public const int MOD_SHIFT = 0x004;
            public const int MOD_WIN = 0x008;
            public const int MOD_NOREPEAT = 0x400;
            public const int WM_HOTKEY = 0x312;
            public const int DSIX = 0x36;
        }
        private int crossHairs = 1;
        private int crohaBorder = 0;
        private Point crohaPoint;
        private Screen scr;
        private Rectangle ss;
        private Rectangle dsr;
        private Rectangle sir;
        private Bitmap b0, b1, b2;
        private bool lastInverted = true;
        private float lastConMid = 0.5f;
        private float lastConAmp = 1.0f;
        private Point mdp = new Point(0, 0);
        private bool mouseLeftForCon = false;
        private Rectangle zoomRect;
        private bool zrs = false;
        private double zoomRat = 0.4;
        private Point zoomPoint;
        private Point zoomAbsBegin;
        private int zooming = 0;

        private int rk1, rk2;
        private char showHideKey = 'A';
        private int showHideModifier = RegHK.MOD_WIN | RegHK.MOD_SHIFT;
        private char refreshKey = 'A';
        private int refreshModifier = RegHK.MOD_ALT | RegHK.MOD_SHIFT;
        private char crossHairKey = 'C';
        private char crossHairBorderKey = 'B';
        private char invertKey = 'X';
        private string settingsFilePathName = AppDomain.CurrentDomain.BaseDirectory + "ce_settings.ini";

        private bool readSettings() {
            string[] lines = new string[] { };
            try {
                lines = File.ReadAllLines(this.settingsFilePathName);
            } catch {
                return false;
            } //catch
            var dict = new Dictionary<string, string>();
            foreach (string line in lines) {
                string[] subs = line.Split('=');
                if (subs.Count() < 2) {
                    return false;
                } //if
                dict.Add(subs[0], subs[1]);
            } //fe

            string v;
            if (!dict.TryGetValue("showHideKey", out v) || (v.Length < 1)) {
                return false;
            } //gua
            char tempShowHideKey = v[0];
            if (!dict.TryGetValue("showHideModifier", out v)) {
                return false;
            } //gua
            int tempShowHideModifier = 0;
            tempShowHideModifier |= (v.Contains("win")) ? RegHK.MOD_WIN : 0;
            tempShowHideModifier |= (v.Contains("ctrl") || v.Contains("control")) ? RegHK.MOD_CONTROL : 0;
            tempShowHideModifier |= (v.Contains("alt")) ? RegHK.MOD_ALT : 0;
            tempShowHideModifier |= (v.Contains("shift")) ? RegHK.MOD_SHIFT : 0;

            if (!dict.TryGetValue("refreshKey", out v) || (v.Length < 1)) {
                return false;
            } //gua
            char tempRefreshKey = v[0];
            if (!dict.TryGetValue("refreshModifier", out v)) {
                return false;
            } //gua
            int tempRefreshModifier = 0;
            tempRefreshModifier |= (v.Contains("win")) ? RegHK.MOD_WIN : 0;
            tempRefreshModifier |= (v.Contains("ctrl") || v.Contains("control")) ? RegHK.MOD_CONTROL : 0;
            tempRefreshModifier |= (v.Contains("alt")) ? RegHK.MOD_ALT : 0;
            tempRefreshModifier |= (v.Contains("shift")) ? RegHK.MOD_SHIFT : 0;

            if (!dict.TryGetValue("crossHairKey", out v) || (v.Length < 1)) {
                return false;
            } //gua
            char tempCrossHairKey = v[0];

            if (!dict.TryGetValue("crossHairBorderKey", out v) || (v.Length < 1)) {
                return false;
            } //gua
            char tempCrossHairBorderKey = v[0];

            if (!dict.TryGetValue("invertKey", out v) || (v.Length < 1)) {
                return false;
            } //gua
            char tempInvertKey = v[0];

            showHideKey = tempShowHideKey;
            showHideModifier = tempShowHideModifier;
            refreshKey = tempRefreshKey;
            refreshModifier = tempRefreshModifier;
            crossHairKey = tempCrossHairKey;
            crossHairBorderKey = tempCrossHairBorderKey;
            invertKey = tempInvertKey;

            return true;
        } //func
        private void saveDefaultSettings() {
            var data = new List<string>();
            data.Add("showHideKey=" + showHideKey);
            data.Add("showHideModifier=" + modifierToString(showHideModifier));
            data.Add("refreshKey=" + refreshKey);
            data.Add("refreshModifier=" + modifierToString(refreshModifier));
            data.Add("invertKey=" + invertKey);
            data.Add("crossHairKey=" + crossHairKey);
            data.Add("crossHairBorderKey=" + crossHairBorderKey);
            try {
                File.WriteAllLines(this.settingsFilePathName, data);
            } catch {
            }//catch
        } //func
        private string modifierToString(int mod) {

            var mods = new List<string>();
            if ((mod & RegHK.MOD_WIN) != 0) {
                mods.Add("win");
            }
            if ((mod & RegHK.MOD_SHIFT) != 0) {
                mods.Add("shift");
            }
            if ((mod & RegHK.MOD_CONTROL) != 0) {
                mods.Add("ctrl");
            }
            if ((mod & RegHK.MOD_ALT) != 0) {
                mods.Add("alt");
            }
            return string.Join("+", mods);
                } //func;

        public Form1()
        {
            ccanshow = true;
            InitializeComponent();
            if(!readSettings()) {
                saveDefaultSettings();
            }
            rk1 = RegHK.RegisterHotKey(Handle, 1, showHideModifier, showHideKey);
            rk2 = RegHK.RegisterHotKey(Handle, 2, refreshModifier, refreshKey);
            CaptureScreen();
            pictureBox1.Visible = false;
        }
        private void CaptureScreen()
        {
            scr = Screen.PrimaryScreen;
            ss = new Rectangle(scr.Bounds.Left, scr.Bounds.Top, scr.Bounds.Width, scr.Bounds.Height);
            sir = new Rectangle(0, 0, scr.Bounds.Width, scr.Bounds.Height);
            if (!zrs)
                zoomRect = new Rectangle(0, 0, scr.Bounds.Width, scr.Bounds.Height);
            zrs = true;
            dsr = new Rectangle(0, 0, scr.Bounds.Width, scr.Bounds.Height);
            b0 = new Bitmap(sir.Width, sir.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Graphics g0 = Graphics.FromImage(b0);
            g0.CopyFromScreen(ss.Left, ss.Top, 0, 0, new Size(ss.Width, ss.Height));
            b1 = new Bitmap(b0);
            b2 = new Bitmap(b1);
        }
        private unsafe void SetGrayContrast_b0b1(float midpoint, float amplev, bool binv)
        {
            var bd0 = b0.LockBits(sir, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bd1 = b1.LockBits(sir, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte* q0 = (byte*)bd0.Scan0;
            byte* q1 = (byte*)bd1.Scan0;
            int len = bd1.Stride * bd1.Height;
            float v_max = 255 * 3;
            float v_mid = v_max * midpoint;
            float d_amp = amplev;
            float mval, dif, d_ref;
            for (int i = 0; i < len; i += 3)
            {
                mval = (float)q0[i] + (float)q0[i + 1] + (float)q0[i + 2];
                dif = mval - v_mid;
                if (dif < 0)
                    d_ref = 0;
                else
                    d_ref = v_max;
                dif = dif * d_amp;
                mval = v_mid + dif - ((v_mid - d_ref) * (1 - d_amp));
                mval /= 3;
                if (binv)
                    mval = 255 - mval;
                if (mval < 0)
                    mval = 0;
                else
                    if (mval > 255)
                    mval = 255;
                q1[i] = (byte)(mval);
                q1[i + 1] = q1[i];
                q1[i + 2] = q1[i];
            }
            b1.UnlockBits(bd1);
            b0.UnlockBits(bd0);
        }
        private unsafe void SetGrayContrast2_b0b1(float mid_ratio, float amp_ratio, bool binvert)
        {
            var bd0 = b0.LockBits(sir, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bd1 = b1.LockBits(sir, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte* q0 = (byte*)bd0.Scan0;
            byte* q1 = (byte*)bd1.Scan0;
            int len = bd1.Stride * bd1.Height;
            const float v_max = 255 * 3;
            float v_mid = v_max * mid_ratio;
            float v_mrm = v_max - v_max * amp_ratio;
            float x, dx;
            int idx;
            for (int i = 0; i < len; i += 3)
            {               
                x = (float)q0[i] + (float)q0[i + 1] + (float)q0[i + 2];
                dx = amp_ratio * x;
                if (x >= v_mid)
                    dx += v_mrm;
                idx = (int)(dx/3);
                if (idx < 0)
                    idx = 0;
                else
                    if (idx > 255)
                    idx = 255;
                if (binvert)
                    idx = 255 - idx;
                q1[i] = (byte)(idx);
                q1[i + 1] = q1[i];
                q1[i + 2] = q1[i];
            }
            b1.UnlockBits(bd1);
            b0.UnlockBits(bd0);
        }
        private unsafe void SetColorContrast_b0b1(float mid_ratio, float amp_ratio, bool binvert)
        {
            var bd0 = b0.LockBits(sir, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var bd1 = b1.LockBits(sir, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            byte* q0 = (byte*)bd0.Scan0;
            byte* q1 = (byte*)bd1.Scan0;
            int len = bd1.Stride * bd1.Height;
            const int v_max = 255;
            int v_mid = (int)(mid_ratio * v_max);
            int v_mrm = v_max - (int)(amp_ratio * v_max);
            int x, dx;
            for (int i = 0; i < len; i++)
            {
                x = (int)q0[i];
                dx = (int)(amp_ratio * x);
                if (x >= v_mid)
                    dx += v_mrm;
                if (dx < 0)
                    dx = 0;
                else
                    if (dx > 255)
                    dx = 255;
                if (binvert)
                    dx = 255 - dx;
                q1[i] = (byte)(dx);
            }
            b1.UnlockBits(bd1);
            b0.UnlockBits(bd0);
        }
        private void startZoom(int dir, double rat, Point sp)
        {
            zoomPoint = new Point(sp.X, sp.Y);
            zoomAbsBegin = new Point(zoomRect.Left + (zoomRect.Width * zoomPoint.X / dsr.Width),
                zoomRect.Top + (zoomRect.Height * zoomPoint.Y / dsr.Height));
            zoomRat = rat;
            zooming = dir;
            zoomTimer.Enabled = true;

        }
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (zooming == 0)
            {
                if (e.Button == MouseButtons.Left && ModifierKeys.HasFlag(Keys.Shift) && !(ModifierKeys.HasFlag(Keys.Control)))
                    startZoom(1, 0.9, e.Location);
                if (e.Button == MouseButtons.Left && !(ModifierKeys.HasFlag(Keys.Shift)) && !(ModifierKeys.HasFlag(Keys.Control)))
                    startZoom(3, 1.0, e.Location);
                if (e.Button == MouseButtons.Middle)
                    startZoom(1, 0.9, e.Location);
                if (e.Button == MouseButtons.Right)
                    startZoom(2, 1 / 0.9, e.Location);
            }
            if (e.Button == MouseButtons.Left && (ModifierKeys.HasFlag(Keys.Control)))
            {
                mdp = e.Location;
                mouseLeftForCon = true;
            }
        }
        private void zoomIn(double ratio, Point dest)
        {
            int nrw = (int)(zoomRect.Width * ratio);
            if (nrw < 10)
                nrw = 10;
            else
                if (nrw > sir.Width)
                nrw = sir.Width;
            int nrh = nrw * sir.Height / sir.Width;
            if (nrh > sir.Height)
            {
                nrh = sir.Height;
                nrw = nrh * sir.Width / sir.Height;
            }
            int nrx = zoomAbsBegin.X - (dest.X * nrw / sir.Width);
            int nry = zoomAbsBegin.Y - (dest.Y * nrh / sir.Height);
            if (nrx < sir.Left)
                nrx = sir.Left;
            else
                if (nrx + nrw > sir.Right)
                nrx = sir.Right - nrw;
            if (nry < sir.Top)
                nry = sir.Top;
            else
                if (nry + nrh > sir.Bottom)
                nry = sir.Bottom - nrh;
            zoomRect = new Rectangle(nrx, nry, nrw, nrh);
        }
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if ((zooming == 1) && ((e.Button == MouseButtons.Left) || (e.Button == MouseButtons.Middle)))
            {
                zooming = 0;
                zoomTimer.Enabled = false;
            }
            if ((zooming == 2) && (e.Button == MouseButtons.Right))
            {
                zooming = 0;
                zoomTimer.Enabled = false;
            }
            if ((zooming == 3) && (e.Button == MouseButtons.Left))
            {
                zooming = 0;
                zoomTimer.Enabled = false;
            }
            if (e.Button == MouseButtons.Left)
                mouseLeftForCon = false;
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (crossHairs > 0)
            {
                crohaPoint = e.Location;
            }
            if (mouseLeftForCon && (ModifierKeys.HasFlag(Keys.Control)))
            {
                lastConMid = (float)(e.Location.X) / ss.Width;
                lastConAmp = (float)e.Location.Y / mdp.Y;
                ApplyFilters_b0b2();
                Graphics g = Graphics.FromHwndInternal(Handle);
                g.DrawImage(b2, 0, 0);
                //contrast changing mouse move 
                //Invalidate();
                return;
            }
            if (zooming != 0)
            {
                zoomPoint = new Point(e.Location.X, e.Location.Y);
            }
            if(crossHairs>0)
            {
                Invalidate();
            }
        }
        private void fromB2ToPic()
        {
            Invalidate();
            //Graphics g = Graphics.FromHwndInternal(pictureBox1.Handle);
            //g.DrawImage(b2, 0, 0);
            //g.DrawImage(b1,ss, zoomRect, GraphicsUnit.Pixel);
        }
        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'x')
            {
            }
        }
        private void reCapture()
        {
            Visible = false;
            Application.DoEvents();
            CaptureScreen();
            //MessageBox.Show("recapturing");
            ApplyFilters_b0b2();
            PlaceWindow();
            ccanshow = true;
            Visible = true;
            PlaceImage();
            Invalidate();
        }
        private void ApplyFilters_b0b2()
        {
            SetColorContrast_b0b1(lastConMid, lastConAmp, lastInverted);
            Graphics g2 = Graphics.FromImage(b2);
            g2.DrawImage(b1, ss, zoomRect, GraphicsUnit.Pixel);
        }
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 32)
            {
                reCapture();
            }
        }
        private void PlaceWindow()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.SetBounds(0, 0, ss.Width, ss.Height);
        }
        private void PlaceImage()
        {
            pictureBox1.SetBounds(0, 0, ss.Width, ss.Height);
            pictureBox1.Image = b2;
        }
        private void zoomTimer_Tick(object sender, EventArgs e)
        {
            if (zooming == 0)
            {
                zoomTimer.Enabled = false;
                return;
            }
            zoomIn(zoomRat, zoomPoint);
            //ApplyZoom();
            Graphics g2 = Graphics.FromImage(b2);
            g2.DrawImage(b1, ss, zoomRect, GraphicsUnit.Pixel);
            Invalidate();
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (rk1 != 0)
            {
            RegHK.UnregisterHotKey(Handle, 1);
            rk1 = 0;
            }
            if (rk2 != 0)
            {
                RegHK.UnregisterHotKey(Handle, 2);
            rk2=0;
            }
        }
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == (int)Keys.F4 && ModifierKeys.HasFlag(Keys.Alt))
            {
                e.SuppressKeyPress=true;
                e.Handled = true;
            }
            if (e.KeyValue == crossHairKey) {
                crossHairs ++;
                crossHairs %= 2;
                Invalidate();
                return;
            }
            if (e.KeyValue == crossHairBorderKey) {
                crohaBorder++;
                crohaBorder %= 2;
                Invalidate();
                return;
            }
            if (e.KeyValue == 'Q' && ModifierKeys.HasFlag(Keys.Alt))
            {
                Close();
                return;
            }
                if (e.KeyValue == 191)
            {
                zoomRect = new Rectangle(ss.Left, ss.Top, ss.Width, ss.Height);
                //ApplyFilters_b0b2();
                //ApplyZoom();
                Graphics g2 = Graphics.FromImage(b2);
                g2.DrawImage(b1, ss, zoomRect, GraphicsUnit.Pixel);
                Invalidate();
                return;
            }
            if (e.KeyValue==invertKey) {
                lastInverted = !lastInverted;
                ApplyFilters_b0b2();
                Invalidate();
                return;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            ;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            //MessageBox.Show("load");
            this.Text = ss.Width.ToString() + " " + ss.Height;
            PlaceWindow();
            ApplyFilters_b0b2();
            PlaceImage();
        }
        private void toggleVisible()
        {
            ccanshow = true;
            Visible = !Visible;
        }
        protected override void WndProc(ref Message m)
        {
            if((m.Msg==RegHK.WM_HOTKEY) && ((int)m.WParam==1))
            {
                toggleVisible();
            }
            if ((m.Msg == RegHK.WM_HOTKEY) && ((int)m.WParam == 2))
            {
                reCapture();
            }
            base.WndProc(ref m);
        }
        protected bool ccanshow = false;
        protected override void SetVisibleCore(bool value)
        {
            //MessageBox.Show("" + Visible+" "+value);
            base.SetVisibleCore(value && ccanshow);
            if(value && ccanshow)
            {
                //this.BringToFront();
                //this.Focus();
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //base.OnPaintBackground(e);
            e.Graphics.DrawImage(b2, 0, 0);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            //e.Graphics.DrawImage(b2, 0, 0);
            if(crossHairs==1)
            {
                Point[] ps = new Point[3];
                ps[0] = new Point(crohaPoint.X, dsr.Height);
                ps[1] = new Point(crohaPoint.X, crohaPoint.Y);
                ps[2] = new Point(dsr.Width, crohaPoint.Y);
                if (crohaBorder == 1)
                    e.Graphics.DrawLines(new Pen(Color.Black, 40), ps);
                e.Graphics.DrawLines(new Pen(Color.White, 20), ps);
            }
            //base.OnPaint(e);
        }
    }
}
