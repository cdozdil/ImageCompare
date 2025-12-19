using System;
using System.Windows.Forms;

namespace ImageCompare
{
    public partial class MainForm : Form
    {
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // Ensure the first row (inputs) starts at 50/50 width
            if (splitContainerInputs != null && splitContainerInputs.Width > 0)
            {
                splitContainerInputs.SplitterDistance = splitContainerInputs.Width / 2;
            }

            // Avoid fully solid marking for large diffs by default; keep blending per shader (lerp)
            _markAmount = 0.5f; // PinkAmount default
            Recompute();
        }
    }
}
