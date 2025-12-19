using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ImageCompare
{
    public partial class MainForm
    {
        private ToolStripButton btnLoadDebugView;

        private void EnsureDebugButton()
        {
            if (toolStrip1 == null) return;
            if (btnLoadDebugView != null) return;
            btnLoadDebugView = new ToolStripButton("Load Debug View");
            btnLoadDebugView.Click += btnLoadDebugView_Click;
            toolStrip1.Items.Add(new ToolStripSeparator());
            toolStrip1.Items.Add(btnLoadDebugView);
        }

        private void btnLoadDebugView_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (var fs = new FileStream(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var img = Image.FromStream(fs))
                        using (var bmp = new Bitmap(img))
                        {
                            int thirdW = bmp.Width / 3;
                            int thirdH = bmp.Height / 3;
                            if (thirdW <= 0 || thirdH <= 0)
                            {
                                MessageBox.Show(this, "Image is too small for 3x3 split.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            var rectBottomRight = new Rectangle(thirdW * 2, thirdH * 2, thirdW, thirdH);
                            var rectBottomMiddle = new Rectangle(thirdW * 1, thirdH * 2, thirdW, thirdH);

                            var crop1 = CropBitmap(bmp, rectBottomRight);
                            var crop2 = CropBitmap(bmp, rectBottomMiddle);

                            DisposeAndSet(ref _img1, crop1);
                            DisposeAndSet(ref _img2, crop2);
                            pbImage1.Image = _img1;
                            pbImage2.Image = _img2;
                            Recompute();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "Failed to load debug view: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private static Bitmap CropBitmap(Bitmap source, Rectangle rect)
        {
            var result = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(source, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }
            return result;
        }
    }
}
