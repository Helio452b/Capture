using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Capture
{
    public partial class Form1 : Form
    {
        #region Win32 API and Enumerations       
        [DllImport("shcore.dll")]
        static extern IntPtr SetProcessDpiAwareness(int value);
        int PROCESS_PRE_MONITOE_DPI_AWARE = 2;
        #endregion

        public Form1()
        {
            InitializeComponent();
            SetProcessDpiAwareness(PROCESS_PRE_MONITOE_DPI_AWARE);           
        }

        private void CaptureBtn_Click(object sender, EventArgs e)
        {
            Bitmap ScreenBmp = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);
            Graphics ScreenGraphics = Graphics.FromImage(ScreenBmp);
            ScreenGraphics.CopyFromScreen(new Point(0, 0), new Point(0, 0), new Size(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height));

            BmpForm bmpForm = new BmpForm();
            bmpForm.BackgroundImage = ScreenBmp;
            bmpForm.BackgroundImageLayout = ImageLayout.Stretch;          
            bmpForm.Show();
        }
    }
}
