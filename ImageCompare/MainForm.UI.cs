using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ImageCompare
{
    public partial class MainForm : Form
    {
        // UI
        private SplitContainer splitContainerMain;
        private SplitContainer splitContainerInputs;
        private PictureBox pbImage1;
        private PictureBox pbImage2;
        private PictureBox pbOutput;
        private ToolStrip toolStrip1;
        private ToolStripButton btnSaveOutput;
        private ToolStripButton btnPickColor;
        private ToolStripLabel lblTolerance;
        private ToolStripTextBox tbTolerance;
        private ToolStripLabel lblColorHex;

        // State
        private Bitmap _img1;
        private Bitmap _img2;
        private Bitmap _diff;
        private float _threshold = 0.003f; // default tolerance
        private Color _markColor = ColorTranslator.FromHtml("#ff6699");
        private float _markAmount = 1.0f; // PinkAmount

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            InitUi();
        }

        private void InitUi()
        {
            if (toolStrip1 != null) return; // already initialized

            toolStrip1 = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
            btnSaveOutput = new ToolStripButton("Save Output");
            btnSaveOutput.Click += btnSaveOutput_Click;
            btnPickColor = new ToolStripButton("Pick Color");
            btnPickColor.Click += btnPickColor_Click;
            lblColorHex = new ToolStripLabel(ColorToHex(_markColor));
            lblTolerance = new ToolStripLabel("Tolerance:");
            tbTolerance = new ToolStripTextBox { AutoSize = false, Width = 60, Text = _threshold.ToString("0.######", CultureInfo.InvariantCulture) };
            tbTolerance.KeyDown += TbTolerance_KeyDown;
            tbTolerance.Leave += TbTolerance_Leave;

            toolStrip1.Items.Add(btnSaveOutput);
            toolStrip1.Items.Add(new ToolStripSeparator());
            toolStrip1.Items.Add(btnPickColor);
            toolStrip1.Items.Add(lblColorHex);
            toolStrip1.Items.Add(new ToolStripSeparator());
            toolStrip1.Items.Add(lblTolerance);
            toolStrip1.Items.Add(tbTolerance);

            splitContainerMain = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            splitContainerInputs = new SplitContainer { Dock = DockStyle.Fill };

            pbImage1 = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, AllowDrop = true };
            pbImage1.Click += pbImage_Click;
            pbImage1.DragEnter += pbImage_DragEnter;
            pbImage1.DragDrop += pbImage_DragDrop;

            pbImage2 = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, AllowDrop = true };
            pbImage2.Click += pbImage_Click;
            pbImage2.DragEnter += pbImage_DragEnter;
            pbImage2.DragDrop += pbImage_DragDrop;

            pbOutput = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom };

            splitContainerInputs.Panel1.Controls.Add(pbImage1);
            splitContainerInputs.Panel2.Controls.Add(pbImage2);
            splitContainerMain.Panel1.Controls.Add(splitContainerInputs);
            splitContainerMain.Panel2.Controls.Add(pbOutput);

            Controls.Add(splitContainerMain);
            Controls.Add(toolStrip1);

            Text = "Image Compare";

            EnsureDebugButton();
        }

        private void TbTolerance_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ApplyToleranceFromText();
            }
        }

        private void TbTolerance_Leave(object sender, EventArgs e)
        {
            ApplyToleranceFromText();
        }

        private void ApplyToleranceFromText()
        {
            if (float.TryParse(tbTolerance.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            {
                if (v < 0) v = 0;
                if (v > 1) v = 1;
                _threshold = v;
                Recompute();
            }
            else
            {
                tbTolerance.Text = _threshold.ToString("0.######", CultureInfo.InvariantCulture);
            }
        }

        private void btnPickColor_Click(object sender, EventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                dlg.Color = _markColor;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _markColor = dlg.Color;
                    lblColorHex.Text = ColorToHex(_markColor);
                    Recompute();
                }
            }
        }

        private void btnSaveOutput_Click(object sender, EventArgs e)
        {
            if (_diff == null)
            {
                MessageBox.Show(this, "No output to save.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PNG Image|*.png|JPEG Image|*.jpg;*.jpeg|Bitmap|*.bmp";
                sfd.DefaultExt = "png";
                sfd.AddExtension = true;
                sfd.FileName = "diff.png";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    ImageFormat fmt = ImageFormat.Png;
                    var ext = Path.GetExtension(sfd.FileName).ToLowerInvariant();
                    if (ext == ".jpg" || ext == ".jpeg") fmt = ImageFormat.Jpeg;
                    else if (ext == ".bmp") fmt = ImageFormat.Bmp;
                    _diff.Save(sfd.FileName, fmt);
                }
            }
        }

        private void pbImage_Click(object sender, EventArgs e)
        {
            var which = sender == pbImage1 ? 1 : 2;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    LoadImage(which, ofd.FileName);
                }
            }
        }

        private void pbImage_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0 && IsImageFile(files[0]))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void pbImage_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0) return;
            var which = sender == pbImage1 ? 1 : 2;
            if (IsImageFile(files[0]))
            {
                LoadImage(which, files[0]);
            }
        }

        private bool IsImageFile(string path)
        {
            var ext = Path.GetExtension(path)?.ToLowerInvariant();
            switch (ext)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":
                case ".gif":
                case ".tif":
                case ".tiff":
                    return true;
                default:
                    return false;
            }
        }

        private void LoadImage(int which, string path)
        {
            try
            {
                var bmp = LoadBitmapUnlocked(path);
                if (which == 1)
                {
                    DisposeAndSet(ref _img1, bmp);
                    pbImage1.Image = _img1;
                }
                else
                {
                    DisposeAndSet(ref _img2, bmp);
                    pbImage2.Image = _img2;
                }
                Recompute();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load image: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Bitmap LoadBitmapUnlocked(string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var img = Image.FromStream(fs))
            {
                return new Bitmap(img);
            }
        }

        private void DisposeAndSet(ref Bitmap target, Bitmap value)
        {
            var old = target;
            target = value;
            if (old != null && !ReferenceEquals(old, value)) old.Dispose();
        }

        private void Recompute()
        {
            if (_img1 == null || _img2 == null)
            {
                pbOutput.Image = null;
                DisposeAndSet(ref _diff, null);
                return;
            }

            var outBmp = ComputeDiffImage(_img2, _img1, _threshold, _markColor, _markAmount);
            DisposeAndSet(ref _diff, outBmp);
            pbOutput.Image = _diff;
        }

        private static Bitmap To32bpp(Bitmap src)
        {
            var bmp = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.DrawImage(src, 0, 0, src.Width, src.Height);
            }
            return bmp;
        }

        private static Bitmap ResizeTo32bpp(Bitmap src, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, width, height));
            }
            return bmp;
        }

        private static string ColorToHex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        private static float Smoothstep(float edge0, float edge1, float x)
        {
            if (Math.Abs(edge1 - edge0) < 1e-6f)
            {
                return x >= edge0 ? 1f : 0f;
            }
            var t = (x - edge0) / (edge1 - edge0);
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return t * t * (3 - 2 * t);
        }

        private static Bitmap ComputeDiffImage(Bitmap img1, Bitmap img2, float diffThreshold, Color markColor, float markAmount)
        {
            int w = img2.Width;
            int h = img2.Height;

            using (var a = ResizeTo32bpp(img1, w, h))
            using (var b = To32bpp(img2))
            {
                var result = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                var rect = new Rectangle(0, 0, w, h);

                var bdA = a.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var bdB = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                var bdR = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                try
                {
                    int strideA = bdA.Stride;
                    int strideB = bdB.Stride;
                    int strideR = bdR.Stride;
                    int bytesPerPixel = 4;
                    int lenA = strideA * h;
                    int lenB = strideB * h;
                    int lenR = strideR * h;

                    byte[] bufA = new byte[lenA];
                    byte[] bufB = new byte[lenB];
                    byte[] bufR = new byte[lenR];

                    Marshal.Copy(bdA.Scan0, bufA, 0, lenA);
                    Marshal.Copy(bdB.Scan0, bufB, 0, lenB);

                    float pr = markColor.R / 255f;
                    float pg = markColor.G / 255f;
                    float pb = markColor.B / 255f;

                    for (int y = 0; y < h; y++)
                    {
                        int rowA = y * strideA;
                        int rowB = y * strideB;
                        int rowR = y * strideR;
                        for (int x = 0; x < w; x++)
                        {
                            int idxA = rowA + x * bytesPerPixel;
                            int idxB = rowB + x * bytesPerPixel;
                            int idxR = rowR + x * bytesPerPixel;

                            float aB = bufA[idxA + 0] / 255f;
                            float aG = bufA[idxA + 1] / 255f;
                            float aR = bufA[idxA + 2] / 255f;
                            float bB = bufB[idxB + 0] / 255f;
                            float bG = bufB[idxB + 1] / 255f;
                            float bR = bufB[idxB + 2] / 255f;
                            byte bA = bufB[idxB + 3];

                            float dr = Math.Abs(aR - bR);
                            float dg = Math.Abs(aG - bG);
                            float db = Math.Abs(aB - bB);
                            float d = Math.Max(dr, Math.Max(dg, db));

                            float m = Smoothstep(diffThreshold, diffThreshold * 2f, d);
                            float t = m * markAmount;
                            if (t > 1f) t = 1f; if (t < 0f) t = 0f;

                            float oR = bR + (pr - bR) * t;
                            float oG = bG + (pg - bG) * t;
                            float oB = bB + (pb - bB) * t;

                            bufR[idxR + 0] = (byte)(oB * 255f + 0.5f);
                            bufR[idxR + 1] = (byte)(oG * 255f + 0.5f);
                            bufR[idxR + 2] = (byte)(oR * 255f + 0.5f);
                            bufR[idxR + 3] = bA;
                        }
                    }

                    Marshal.Copy(bufR, 0, bdR.Scan0, lenR);
                }
                finally
                {
                    a.UnlockBits(bdA);
                    b.UnlockBits(bdB);
                    result.UnlockBits(bdR);
                }

                return result;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            DisposeAndSet(ref _img1, null);
            DisposeAndSet(ref _img2, null);
            DisposeAndSet(ref _diff, null);
        }
    }
}
