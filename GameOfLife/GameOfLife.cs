using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GameOfLife
{
    public partial class GameOfLife : Form
    {
        private bool mouseDown;
        private Point lastLocation;
        private bool cellClickIntended;

        private const float maxZoomOut = 5 / 1; //pix / gridlen
        private const float maxZoomIn = 50 / 1; //pix / gridlen

        private int boardWidth = 40;
        private int boardHeight = 30;
        private Point gridOffsetPix = new Point(0,0);
        private float gridScale = 20; //pix/gridlen
        private Color gridLines = Color.Khaki;
        private Color gridBackground = Color.Gray;
        private Color activeSqr = Color.LightGoldenrodYellow;
        private int zoomFactor = 5;
        private Point center;

        private bool[,,] gameBoard;
        private const int ticksStored = 2;
        private int currentTick = 0;

        private bool playMode = false;
        private static System.Windows.Forms.Timer checkTimer;

        public GameOfLife()
        {
            InitializeComponent();
            center = new Point(pctBx_Display.Width / 2, pctBx_Display.Height / 2);
            gameBoard = new bool[ticksStored, boardWidth, boardHeight];
            checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Tick += new EventHandler(continueGame);


            updateDisplay();
        }

        private void updateDisplay()
        {
            int imgWidth = pctBx_Display.Width, imgHeight = pctBx_Display.Height;

            if (pctBx_Display.Image != null)
                pctBx_Display.Image.Dispose();
            pctBx_Display.Image = new Bitmap(imgWidth, imgHeight);
            Random rNum = new Random();
            Point gridPoint = new Point();
            Point currentCell = new Point();
            for (int i = 0; i < imgWidth; i++)
            {
                for (int j = 0; j < imgHeight; j++)
                {
                    gridPoint.X = i - gridOffsetPix.X;
                    gridPoint.Y = j - gridOffsetPix.Y;
                    currentCell.X = (gridPoint.X > 0) ? (int)(gridPoint.X / gridScale) : -1;
                    currentCell.Y = (gridPoint.Y > 0) ? (int)(gridPoint.Y / gridScale) : -1;
                    if ((gridPoint.X == 0 && gridPoint.Y >=0 && gridPoint.Y <= boardHeight * gridScale) || (gridPoint.Y == 0 && gridPoint.X >= 0 && gridPoint.X <= boardWidth * gridScale)
                        || (gridPoint.X == boardWidth*gridScale && gridPoint.Y <= boardHeight * gridScale && gridPoint.Y >= 0) || (gridPoint.Y == boardHeight * gridScale && gridPoint.X <= boardWidth * gridScale && gridPoint.X >= 0))
                        ((Bitmap)pctBx_Display.Image).SetPixel(i, j, Color.Black);
                    else
                    {
                        if (gridPoint.X % gridScale == 0 || gridPoint.Y % gridScale == 0)
                            ((Bitmap)pctBx_Display.Image).SetPixel(i, j, gridLines);
                        else
                        {
                            if (currentCell.X >= 0 && currentCell.Y >= 0 && currentCell.X < boardWidth && currentCell.Y < boardHeight && gameBoard[currentTick, currentCell.X, currentCell.Y])
                                ((Bitmap)pctBx_Display.Image).SetPixel(i, j, activeSqr);
                            else
                                ((Bitmap)pctBx_Display.Image).SetPixel(i, j, gridBackground);
                        }
                    }

                }
            }

            pctBx_Display.Refresh();
        }

        private void incrementTick()
        {
            currentTick = (currentTick + 1) % ticksStored; 
        }
        private int nextTick()
        {
            return (currentTick + 1) % ticksStored;
        }
        private void processBoard()
        {
            int nxtTck = nextTick();
            for(int x = 0; x<boardWidth; x++)
                for(int y = 0; y<boardHeight; y++)
                {
                    bool isActive = gameBoard[currentTick, x, y];

                    int numActive = 0;

                    if (y - 1 >= 0)
                    {
                        numActive += (gameBoard[currentTick, x, y - 1]) ? 1 : 0;
                        if (x - 1 >= 0)
                            numActive += (gameBoard[currentTick, x - 1, y - 1]) ? 1 : 0;
                        if (x + 1 < boardWidth)
                            numActive += (gameBoard[currentTick, x + 1, y - 1]) ? 1 : 0;
                    }
                    if(y+1 < boardHeight)
                    {
                        numActive += (gameBoard[currentTick, x, y + 1]) ? 1 : 0;
                        if (x - 1 >= 0)
                            numActive += (gameBoard[currentTick, x - 1, y + 1]) ? 1 : 0;
                        if (x + 1 < boardWidth)
                            numActive += (gameBoard[currentTick, x + 1, y + 1]) ? 1 : 0;
                    }
                    if (x - 1 >= 0)
                        numActive += (gameBoard[currentTick, x - 1, y]) ? 1 : 0;
                    if (x + 1 < boardWidth)
                        numActive += (gameBoard[currentTick, x + 1, y]) ? 1 : 0;


                    if (isActive)
                    {
                        if (numActive > 3 || numActive < 2)
                            gameBoard[nxtTck, x, y] = false;
                        else
                            gameBoard[nxtTck, x, y] = true;
                    }
                    else if (numActive == 3)
                        gameBoard[nxtTck, x, y] = true;
                    else
                        gameBoard[nxtTck, x, y] = false;

                }
            incrementTick();
        }

        //Event Handlers
        protected override void WndProc(ref Message m)
        {
            const int RESIZE_HANDLE_SIZE = 10;

            switch (m.Msg)
            {
                case 0x0084/*NCHITTEST*/ :
                    base.WndProc(ref m);

                    if ((int)m.Result == 0x01/*HTCLIENT*/)
                    {
                        Point screenPoint = new Point(m.LParam.ToInt32());
                        Point clientPoint = this.PointToClient(screenPoint);
                        if (clientPoint.Y <= RESIZE_HANDLE_SIZE)
                        {
                            /*if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)13;//HTTOPLEFT
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)12;//HTTOP
                            else
                                m.Result = (IntPtr)14;//HTTOPRIGHT*/
                        }
                        else if (clientPoint.Y <= (Size.Height - RESIZE_HANDLE_SIZE))
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)10/*HTLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)2/*HTCAPTION*/ ;
                            else
                                m.Result = (IntPtr)11/*HTRIGHT*/ ;
                        }
                        else
                        {
                            if (clientPoint.X <= RESIZE_HANDLE_SIZE)
                                m.Result = (IntPtr)16/*HTBOTTOMLEFT*/ ;
                            else if (clientPoint.X < (Size.Width - RESIZE_HANDLE_SIZE))
                                m.Result = (IntPtr)15/*HTBOTTOM*/ ;
                            else
                                m.Result = (IntPtr)17/*HTBOTTOMRIGHT*/ ;
                        }
                    }
                    return;
            }
            base.WndProc(ref m);
        }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x20000; // <--- use 0x20000
                return cp;
            }
        }
        private void btn_Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void btn_DragRegion_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }
        private void btn_DragRegion_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }
        private void btn_DragRegion_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }
        private void pctBx_Display_MouseDown(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
            if (e.Button == MouseButtons.Middle)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.NoMove2D;
                cellClickIntended = false;
            }
            else
                cellClickIntended = true;
        }
        private void pctBx_Display_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.NoMove2D;
                gridOffsetPix.X += e.Location.X - lastLocation.X;
                gridOffsetPix.Y += e.Location.Y - lastLocation.Y;

                lastLocation = e.Location;
                cellClickIntended = false;
                updateDisplay();
            }
        }
        private void pctBx_Display_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
            System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            if (cellClickIntended)
            {
                Point currentCell = new Point((int)((lastLocation.X - gridOffsetPix.X) / gridScale), (int)((lastLocation.Y - gridOffsetPix.Y) / gridScale));
                if (currentCell.X >= 0 && currentCell.Y >= 0 && currentCell.X < boardWidth && currentCell.Y < boardHeight)
                    gameBoard[currentTick, currentCell.X, currentCell.Y] = !gameBoard[currentTick, currentCell.X, currentCell.Y];

            }
            updateDisplay();

        }
        private void GameOfLife_ResizeEnd(object sender, EventArgs e)
        {
            resizeForm();
        }
        private void resizeForm()
        {
            this.btn_Close.Location = new Point(this.Size.Width - 20, 0);
            this.pctBx_Display.Size = new Size(this.Size.Width - pctBx_Display.Location.X - 12, this.Size.Height - pctBx_Display.Location.Y - 34);
            center = new Point((int)(pctBx_Display.Size.Width / 2), (int)(pctBx_Display.Size.Height / 2));
            this.btn_DragRegion.Size = new Size(this.Size.Width - 40, 20);
            this.btn_Maximize.Location = new Point(this.Size.Width - 40, 0);
            this.btn_ZoomOut.Location = new Point(12, this.Size.Height - 28);
            this.btn_ZoomIn.Location = new Point(38, this.Size.Height - 28);
            this.btn_Next.Location = new Point(64, this.Size.Height - 28);
            this.btn_Play.Location = new Point(110, this.Size.Height - 28);
            this.btn_ResetImage.Location = new Point(156, this.Size.Height - 28);
            this.btn_Rand.Location = new Point(182, this.Size.Height - 28);
            this.btn_Settings.Location = new Point(this.Size.Width - 32, this.Size.Height - 28);

            updateDisplay();
        }
        private void btn_maximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Maximized)
                this.WindowState = FormWindowState.Maximized;
            else
                this.WindowState = FormWindowState.Normal;

            GameOfLife_ResizeEnd(sender, e);
        }

        private void btn_ZoomOut_Click(object sender, EventArgs e)
        {
            if (gridScale >= maxZoomOut + zoomFactor)
            {
                gridScale -= zoomFactor;
                gridOffsetPix.X = (int)(center.X - (gridScale) / (gridScale + zoomFactor) * (center.X - gridOffsetPix.X));
                gridOffsetPix.Y = (int)(center.Y - (gridScale) / (gridScale + zoomFactor) * (center.Y - gridOffsetPix.Y));
            }
            else
                gridScale = maxZoomOut;


            updateDisplay();
        }

        private void btn_ZoomIn_Click(object sender, EventArgs e)
        {
            if (gridScale <= maxZoomIn - zoomFactor)
            {
                gridScale += zoomFactor;
                gridOffsetPix.X = (int)(center.X - (gridScale) / (gridScale - zoomFactor) * (center.X - gridOffsetPix.X));
                gridOffsetPix.Y = (int)(center.Y - (gridScale) / (gridScale - zoomFactor) * (center.Y - gridOffsetPix.Y));
            }
            else
                gridScale = maxZoomIn;
            updateDisplay();
        }

        private void btn_Next_Click(object sender, EventArgs e)
        {
            processBoard();
            updateDisplay();
        }

        private void btn_Play_Click(object sender, EventArgs e)
        {
            if (!playMode)
            {
                playMode = true;
                setNextAlert();
            }
            else
            {
                playMode = false;
            }
        }
        private void setNextAlert()
        {
            processBoard();
            updateDisplay();

            checkTimer.Interval = 1;
            checkTimer.Start();
        }

        private void continueGame(object source, EventArgs e)
        {
            checkTimer.Stop();

            if (playMode)
            {
                setNextAlert();
            }

        }

        private void btn_Rand_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            for (int x = 0; x < boardWidth; x++)
                for (int y = 0; y < boardHeight; y++) 
                {
                    gameBoard[currentTick,x,y] = (rand.NextDouble() > 0.5) ? true : false;
                }
            updateDisplay();
        }

        private void btn_ResetImage_Click(object sender, EventArgs e)
        {
            for (int x = 0; x < boardWidth; x++)
                for (int y = 0; y < boardHeight; y++)
                {
                    gameBoard[currentTick, x, y] = false;
                }
            updateDisplay();
        }
    }
}
