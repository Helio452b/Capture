using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Capture
{
    public partial class BmpForm : Form
    {
        #region Variable
        Point linebeginpoint = Point.Empty; // 绘制直线时时候的起点和终点
        Point lineendpoint = Point.Empty;

        Point BeginPoint = Point.Empty;
        Point RecBeginPoint = Point.Empty;          
        Point DownPoint = Point.Empty;              // 移动截图区域时时记录下鼠标按下的初始点
        Point CurrentPoint = Point.Empty;
        Point RecRightBottom = Point.Empty;
        Point RecRightTop = Point.Empty;
        Point RecLeftTop = Point.Empty;
        Point RecLeftBottom = Point.Empty;
        bool IsStart = false;                                  // 标识截取屏幕是否开始
        bool IsMove = false;                                // 标识移动已截取屏幕是否开始
        bool IsLBtnPress = false;                         // 表示左键已经被按下且没有被释放
        bool IsResize = false;                              // 标识截取屏幕是否在改变大小
        Bitmap OriginBmp;
        Bitmap BackBmp;
        Bitmap CopyBmp;
        Pen DrawPen;        
        SolidBrush BackSolidBr;
        SolidBrush BlueSolidBr = new SolidBrush(Color.CadetBlue);
        Rectangle CapRec;
        Rectangle LeftTop, LeftMid, LeftBottom, MidTop, MidBottom, RightTop, RightMid, RightBottom;    // 矩形边框上面的八个小矩形
        Graphics CopyGraphics; 
        Graphics ScreenGraphics;
        Graphics BackGraphics;
        int RecWidth = 0;
        int RecHeigth = 0;
        int offsetX = 0;                                        // 移动截屏范围时的偏移量
        int offsetY = 0;
        int ScreenWidth;
        int ScreenHeight;
         enum  Resize_Type                                // 判断如何改变截取区域                    
        {
            LeftTop, 
            LeftMid,
            LeftBottom,
            MidTop,
            MidBottom,
            RightTop,
            RightMid,
            RightBottom
        };
         Resize_Type resize_type;

        enum PaintMode
        {
            EditMode,
            none
        };
        PaintMode paintmode;

        enum PaintShape
        {
            line,
            rectangle,
            circle
        };
        PaintShape paintshape;

        private void toolStripDropDownButton1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void toolStripMenuItem2_Paint(object sender, PaintEventArgs e)
        {
            toolStripMenuItem2.Size = new Size(10, 10);
            e.Graphics.FillRectangle(new SolidBrush(Color.Yellow), new Rectangle(new Point(1, 1), new Size(20, 20)));
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            this.paintmode = PaintMode.EditMode;
        }

        private void BmpForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics grah = this.CreateGraphics();
            grah.DrawLine(new Pen(new SolidBrush(Color.Red)), new Point(0, 0), new Point(500, 500));
        }
        #endregion

        #region Event
        public BmpForm()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
        }
       
        private void BmpForm_Load(object sender, EventArgs e)
        {
            toolStrip1.Hide();
            OriginBmp = new Bitmap(this.BackgroundImage);
            BackBmp = (Bitmap)OriginBmp.Clone();
            BackGraphics = Graphics.FromImage(BackBmp);
            BackGraphics.FillRectangle(new SolidBrush(Color.FromArgb(130, Color.Gray)), Screen.PrimaryScreen.Bounds); // 加上灰色透明覆盖
            this.BackgroundImage = BackBmp;
            ScreenHeight = BackBmp.Height;
            ScreenWidth = BackBmp.Width;
            BackGraphics.Dispose();
            this.paintmode = PaintMode.none;
        }

        private void BmpForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.paintmode == PaintMode.EditMode)
            {
                linebeginpoint = e.Location;
                IsLBtnPress = true;
                Graphics grah = this.CreateGraphics();
                grah.DrawLine(new Pen(new SolidBrush(Color.Red)), new Point(0, 0), new Point(500, 500));
            }
            else if (this.paintmode == PaintMode.none)
            {
                if (e.Button == MouseButtons.Left)                   // 左键被按下时
                {
                    this.toolStrip1.Hide();
                    IsLBtnPress = true;
                    CurrentPoint = e.Location;
                    Rectangle NewCapRec = new Rectangle(new Point(CapRec.X + 5, CapRec.Y + 5), new Size(CapRec.Width - 10, CapRec.Height - 10));
                    RecRightBottom = new Point(RecBeginPoint.X + RecWidth, RecBeginPoint.Y + RecHeigth);
                    RecRightTop = new Point(RecBeginPoint.X + RecWidth, RecBeginPoint.Y);
                    RecLeftTop = RecBeginPoint;
                    RecLeftBottom = new Point(RecBeginPoint.X, RecBeginPoint.Y + RecHeigth);
                    if (NewCapRec.Contains(CurrentPoint))                // 左键按下时，如果落在矩形区域内，则表示要移动已经选择了的矩形
                    {
                        this.Cursor = Cursors.SizeAll;
                        IsMove = true;
                        DownPoint = e.Location;
                    }
                    else if (LeftTop.Contains(CurrentPoint))                      // 左键按下时，如果落在八个小矩形对应的范围内，则表示择了的矩形
                    {
                        this.Cursor = Cursors.SizeNWSE;
                        IsResize = true;
                        resize_type = Resize_Type.LeftTop;
                    }
                    else if (LeftMid.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.SizeWE;
                        IsResize = true;
                        resize_type = Resize_Type.LeftMid;
                    }
                    else if (LeftBottom.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.SizeNESW;
                        IsResize = true;
                        resize_type = Resize_Type.LeftBottom;
                    }
                    else if (MidTop.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.SizeNS;
                        IsResize = true;
                        resize_type = Resize_Type.MidTop;
                    }
                    else if (MidBottom.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.SizeNS;
                        IsResize = true;
                        resize_type = Resize_Type.MidBottom;
                    }
                    else if (RightTop.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.SizeNESW;
                        IsResize = true;
                        resize_type = Resize_Type.RightTop;
                    }
                    else if (RightMid.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.SizeWE;
                        IsResize = true;
                        resize_type = Resize_Type.RightMid;
                    }
                    else if (RightBottom.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.SizeNWSE;
                        IsResize = true;
                        resize_type = Resize_Type.RightBottom;
                    }
                    else if (!CapRec.Contains(CurrentPoint))
                    {
                        this.Cursor = Cursors.Cross;
                        IsStart = true;
                        BeginPoint = e.Location;
                    }
                }
            }
         }

        private void BmpForm_MouseMove(object sender, MouseEventArgs e)
        {
             if (IsLBtnPress == true)                                                                    // 鼠标左键按下时发生的事件
            {
                CopyBmp = (Bitmap)OriginBmp.Clone();
                CopyBmp.SetResolution(96, 96);
                CopyGraphics = Graphics.FromImage(CopyBmp);                          // 把屏幕原始图像复制到CopyBmp上
                DrawPen = new Pen(Color.DeepSkyBlue, 2);                                  // 新建画笔DrawPen
                BackSolidBr = new SolidBrush(Color.FromArgb(130, Color.Gray));

                if (IsStart)                                                                                      // 开始截屏
                {
                    RecBeginPoint = e.Location;
                    RecWidth = Math.Abs(BeginPoint.X - RecBeginPoint.X);            // 截屏区域矩形宽度
                    RecHeigth = Math.Abs(BeginPoint.Y - RecBeginPoint.Y);           // 截屏区域矩形高度
 
                    if (BeginPoint.X < RecBeginPoint.X)
                        RecBeginPoint.X = BeginPoint.X;

                    if (BeginPoint.Y < RecBeginPoint.Y)
                        RecBeginPoint.Y = BeginPoint.Y;
                 }
                else if (IsMove)                                                                              // 移动截屏区域
                {
                    offsetX = e.X - DownPoint.X;                                                     // X方向平移的偏移量
                    offsetY = e.Y - DownPoint.Y;                                                     // Y方向平移的偏移量

                    RecBeginPoint.Offset(offsetX, offsetY);

                    if (RecBeginPoint.Y <= 0)                                                          // 防止移动出屏幕外
                        RecBeginPoint.Y = 0;
                    else if (RecBeginPoint.Y + RecHeigth >= ScreenHeight)
                        RecBeginPoint.Y = ScreenHeight - RecHeigth;

                    if (RecBeginPoint.X <= 0)
                        RecBeginPoint.X = 0;
                    else if (RecBeginPoint.X + RecWidth >= ScreenWidth)
                        RecBeginPoint.X = ScreenWidth - RecWidth;
                    
                    DownPoint = e.Location;
                }
                else if (IsResize)                                                                             // 改变截屏区域大小   
                {
                     switch (resize_type)
                    {
                        case Resize_Type.LeftTop:                                                     // 从左上方拖动
                            RecWidth = Math.Abs(e.X - RecRightBottom.X);
                            RecHeigth = Math.Abs(e.Y - RecRightBottom.Y);
                             if (e.X > RecRightBottom.X)
                                 RecBeginPoint.X = RecRightBottom.X;
                             else
                                 RecBeginPoint.X = e .X;
                             if (e.Y > RecRightBottom.Y)
                                 RecBeginPoint.Y = RecRightBottom.Y;
                             else
                                 RecBeginPoint.Y = e.Y;
                               break;
                        case Resize_Type.LeftMid:                                                     // 从左中拖动
                            RecWidth = Math.Abs(e.X - RecRightTop.X);
                             if (e.X > RecRightTop.X)
                                 RecBeginPoint.X = RecRightTop.X;
                             else
                                 RecBeginPoint.X = e.X;
                              break;
                        case Resize_Type.LeftBottom:                                               // 从左下方拖动
                            RecWidth = Math.Abs(e.X - RecRightTop.X);
                            RecHeigth = Math.Abs(e.Y - RecRightTop.Y);
                             if (e.X < RecRightTop.X)
                                RecBeginPoint.X = e.X;
                            else
                                RecBeginPoint.X = RecRightTop.X;
                             if (e.Y < RecRightTop.Y)
                                RecBeginPoint.Y = e.Y;
                            else
                                RecBeginPoint.Y = RecRightTop.Y;
                            break;
                        case Resize_Type.MidTop:
                            RecHeigth = Math.Abs(e.Y - RecLeftBottom.Y);
                             if (e.Y < RecLeftBottom.Y)
                                RecBeginPoint.Y = e.Y;
                            else
                                RecBeginPoint.Y = RecLeftBottom.Y;
                              break;
                        case Resize_Type.MidBottom:
                            RecHeigth = Math.Abs(e.Y - RecLeftTop.Y);
                             if (e.Y > RecLeftTop.Y)
                                 RecBeginPoint = RecLeftTop;
                             else
                                 RecBeginPoint.Y = e.Y;
                              break;
                        case Resize_Type.RightTop:
                            RecWidth = Math.Abs(e.X - RecLeftBottom.X);
                            RecHeigth = Math.Abs(e.Y - RecLeftBottom.Y);
                             if (e.X < RecLeftBottom.X)
                                RecBeginPoint.X = e.X;
                            else
                                RecBeginPoint.X = RecLeftBottom.X;
                             if (e.Y < RecLeftBottom.Y)
                                RecBeginPoint.Y = e.Y;
                            else
                                RecBeginPoint.Y = RecLeftBottom.Y;
                             break;
                        case Resize_Type.RightMid:
                            RecWidth = Math.Abs(e.X - RecLeftTop.X);
                             if (e.X < RecLeftTop.X)
                                RecBeginPoint.X = e.X;
                            else
                                RecBeginPoint.X = RecLeftTop.X;
                             break;
                        case Resize_Type.RightBottom:
                             RecWidth = Math.Abs(e.X - RecLeftTop.X);
                            RecHeigth = Math.Abs(e.Y - RecLeftTop.Y);
                             if (e.X < RecLeftTop.X)
                                RecBeginPoint.X = e.X;
                            else
                                RecBeginPoint.X = RecLeftTop.X;
                             if (e.Y < RecLeftTop.Y)
                                RecBeginPoint.Y = e.Y;
                            else
                                RecBeginPoint.Y = RecLeftTop.Y;
                             break;
                     }
                }            
                RefreshRec();
                return;
            }
             else if (IsLBtnPress == true && this.paintmode == PaintMode.EditMode)
            {
                SolidBrush solidBrush = new SolidBrush(Color.Red);
                
                lineendpoint = e.Location;
                RefreshPaintRegion();
            }
            // 鼠标移动时没有任何键按下时发生的事件
             CurrentPoint = e.Location;
            if (LeftTop.Contains(CurrentPoint) || RightBottom.Contains(CurrentPoint))
                this.Cursor = Cursors.SizeNWSE;
            else if (RightTop.Contains(CurrentPoint) || LeftBottom.Contains(CurrentPoint))
                this.Cursor = Cursors.SizeNESW;
            else if (MidTop.Contains(CurrentPoint) || MidBottom.Contains(CurrentPoint))
                this.Cursor = Cursors.SizeNS;
            else if (LeftMid.Contains(CurrentPoint) || RightMid.Contains(CurrentPoint))
                this.Cursor = Cursors.SizeWE;
            else if (CapRec.Contains(CurrentPoint))
                this.Cursor = Cursors.SizeAll;
            else
                this.Cursor = Cursors.Arrow;
          }
        private void RefreshPaintRegion()
        {
            CopyGraphics.DrawLine(DrawPen, linebeginpoint, linebeginpoint);
            ScreenGraphics = this.CreateGraphics();
            ScreenGraphics.DrawImage(CopyBmp, new Point(0, 0));

            ScreenGraphics.Dispose();
            CopyGraphics.Dispose();
            DrawPen.Dispose();
            CopyBmp.Dispose();
        }
        private void RefreshRec()                                                                      // 更新截屏范围
        {
            CapRec = new Rectangle(RecBeginPoint, new Size(RecWidth, RecHeigth));
            CopyGraphics.DrawRectangle(DrawPen, CapRec);                              // 绘制边框
                                                                                                                     // 绘制四角的小角
            LeftTop = new Rectangle(new Point(RecBeginPoint.X - 5, RecBeginPoint.Y - 5), new Size(10, 10));
            RightTop = new Rectangle(new Point(RecBeginPoint.X + CapRec.Width - 5, RecBeginPoint.Y - 5), new Size(10, 10));
            LeftBottom = new Rectangle(new Point(RecBeginPoint.X - 5, RecBeginPoint.Y + CapRec.Height - 5), new Size(10, 10));
            RightBottom = new Rectangle(new Point(RecBeginPoint.X + CapRec.Width - 5, RecBeginPoint.Y + CapRec.Height - 5), new Size(10, 10));
                                                                                                                     // 绘制边框上面的小角
            MidTop = new Rectangle(new Point(RecBeginPoint.X + (CapRec.Width / 2), RecBeginPoint.Y - 5), new Size(10, 10));
            LeftMid = new Rectangle(new Point(RecBeginPoint.X - 5, RecBeginPoint.Y + (CapRec.Height / 2)), new Size(10, 10));
            MidBottom = new Rectangle(new Point(RecBeginPoint.X + (CapRec.Width / 2), RecBeginPoint.Y + CapRec.Height - 5), new Size(10, 10));
            RightMid = new Rectangle(new Point(RecBeginPoint.X + CapRec.Width - 5, RecBeginPoint.Y + (CapRec.Height / 2)), new Size(10, 10));

            CopyGraphics.FillRectangle(BlueSolidBr, LeftTop);
            CopyGraphics.FillRectangle(BlueSolidBr, RightTop);
            CopyGraphics.FillRectangle(BlueSolidBr, LeftBottom);
            CopyGraphics.FillRectangle(BlueSolidBr, RightBottom);
            
            CopyGraphics.FillRectangle(BlueSolidBr, MidTop);
            CopyGraphics.FillRectangle(BlueSolidBr, LeftMid);
            CopyGraphics.FillRectangle(BlueSolidBr, MidBottom);
            CopyGraphics.FillRectangle(BlueSolidBr, RightMid);
            
                                                                                                                     // 绘制阴影
            CopyGraphics.FillRectangle(BackSolidBr, new Rectangle(0, 0, RecBeginPoint.X, Screen.AllScreens[0].Bounds.Height));
            CopyGraphics.FillRectangle(BackSolidBr, new Rectangle(RecBeginPoint.X, 0, Screen.AllScreens[0].Bounds.Width - RecBeginPoint.X, RecBeginPoint.Y));
            CopyGraphics.FillRectangle(BackSolidBr, new Rectangle(RecBeginPoint.X + RecWidth, RecBeginPoint.Y, Screen.AllScreens[0].Bounds.Width - RecWidth - RecBeginPoint.X, Screen.AllScreens[0].Bounds.Height - RecBeginPoint.Y));
            CopyGraphics.FillRectangle(BackSolidBr, new Rectangle(RecBeginPoint.X, RecBeginPoint.Y + RecHeigth, RecWidth, Screen.AllScreens[0].Bounds.Height - RecBeginPoint.Y - RecHeigth));

            ScreenGraphics = this.CreateGraphics();
            ScreenGraphics.DrawImage(CopyBmp, new Point(0, 0));

            ScreenGraphics.Dispose();
            CopyGraphics.Dispose();
            DrawPen.Dispose();
            CopyBmp.Dispose();
        }

        private void BmpForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (paintmode == PaintMode.none)
                {
                    IsLBtnPress = false;
                    if (IsStart)
                        IsStart = false;
                    else if (IsMove)
                        IsMove = false;
                    else if (IsResize)
                        IsResize = false;
                    // 设置工具栏的位置
                    this.toolStrip1.Location = new Point(RecBeginPoint.X + CapRec.Width - toolStrip1.Size.Width, RecBeginPoint.Y + CapRec.Height + 10);
                    this.toolStrip1.Show();
                }
                else if(paintmode == PaintMode.EditMode)
                {
                    IsLBtnPress = false;
                    linebeginpoint = e.Location;
                    Graphics grah = this.CreateGraphics();
                    grah.DrawLine(new Pen(new SolidBrush(Color.Red)), linebeginpoint, linebeginpoint);
                
                }
            }
        }

        private void toolStripButton10_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
