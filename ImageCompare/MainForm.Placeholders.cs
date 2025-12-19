using System;
using System.Drawing;
using System.Windows.Forms;

namespace ImageCompare
{
    public partial class MainForm : Form
    {
        private bool _placeholdersWired;

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && !_placeholdersWired)
            {
                WirePlaceholderPaint();
                _placeholdersWired = true;
                if (pbImage1 != null) pbImage1.Invalidate();
                if (pbImage2 != null) pbImage2.Invalidate();
            }
        }

        private void WirePlaceholderPaint()
        {
            if (pbImage1 != null)
            {
                pbImage1.Paint -= Pbx_PaintPlaceholder;
                pbImage1.Paint += Pbx_PaintPlaceholder;
            }
            if (pbImage2 != null)
            {
                pbImage2.Paint -= Pbx_PaintPlaceholder;
                pbImage2.Paint += Pbx_PaintPlaceholder;
            }
        }

        private void Pbx_PaintPlaceholder(object sender, PaintEventArgs e)
        {
            var pb = sender as PictureBox;
            if (pb == null || pb.Image != null) return;

            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            const string text = "Click to load";
            using (var font = new Font(Font.FontFamily, 24, FontStyle.Bold))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                var rect = new RectangleF(0, 0, pb.ClientSize.Width, pb.ClientSize.Height);
                using (var shadow = new SolidBrush(Color.FromArgb(70, Color.Black)))
                using (var white = new SolidBrush(Color.LightSlateGray))
                {
                    var shadowRect = new RectangleF(rect.X + 3, rect.Y + 3, rect.Width, rect.Height);
                    g.DrawString(text, font, shadow, shadowRect, format);
                    g.DrawString(text, font, white, rect, format);
                }
            }
        }
    }
}
