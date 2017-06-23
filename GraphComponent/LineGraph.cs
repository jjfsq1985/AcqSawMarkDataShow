using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

namespace GraphComponent
{
    public partial class LineGraph: UserControl
    {
        
        public int m_nLineHorz = 5;
        public int m_nLineVert = 10;

        public int m_nDataCount = 0;
        
        public int m_nCurosrDataIndex = 0;
        public float m_CursorPosX;

        public bool m_bCursor = false;

        private double m_XDelta = 1.0;
        private double m_YDelta = 1.0;

        private double m_dbMinY = 0; //um
        private double m_dbMaxY = 500; //um

        private LineDataGroup[] m_groupdata = null;
        private int m_nGroupCount = 0;
        private bool bDrawOnlyBold = false;

        private bool bMouseDrop = false;
        private bool bDrawDropGraph = false;
        private bool bCtrl = false;
        private int m_nMouseZoomStart = 0;
        private int m_nMouseZoomEnd = 0;
        private int m_nMouseZoomDataCount = 0;
        private int m_nSeekStart = 0;
        private int m_nSeekEnd = 0;
        public LineGraph()
        {
            InitializeComponent();
        }

        public void setGroupDataCount(int nCount)
        {
            m_nGroupCount = nCount;
            m_groupdata = new LineDataGroup[nCount];
        }

        public bool SetLineData(double[] dbLine1,double[] dbLine2, double[] dbLine3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount == 1);
            m_nDataCount = nCount;
            m_groupdata[0] = new LineDataGroup();
            m_groupdata[0].m_dataLine1 = new double[nCount];
            m_groupdata[0].m_dataLine2 = new double[nCount];
            m_groupdata[0].m_dataLine3 = new double[nCount];
            Array.Copy(dbLine1, m_groupdata[0].m_dataLine1, m_nDataCount);
            Array.Copy(dbLine2, m_groupdata[0].m_dataLine2, m_nDataCount);
            Array.Copy(dbLine3, m_groupdata[0].m_dataLine3, m_nDataCount);

            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            m_YDelta = 1.0f * (this.Height - 20) / 100;
            return true;
        }

        public void SetYValueAxis(double dbMin, double dbMax)
        {
            m_dbMinY = dbMin;
            m_dbMaxY = dbMax;
            m_YDelta = 1.0f * (this.Height - 20) / (m_dbMaxY - m_dbMinY);
        }

        public bool SetProcessedDataUp(double[] dbLineUp1, double[] dbLineUp2, double[] dbLineUp3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount > 1);
            m_nDataCount = nCount;
            m_groupdata[0] = new LineDataGroup();
            m_groupdata[0].m_dataLine1 = new double[dbLineUp1.Length];
            m_groupdata[0].m_dataLine2 = new double[dbLineUp2.Length];
            m_groupdata[0].m_dataLine3 = new double[dbLineUp3.Length];
            for (int i = 0; i < nCount; i++)
            {
                m_groupdata[0].m_dataLine1[i] = dbLineUp1[i] * 1000;
                m_groupdata[0].m_dataLine2[i] = dbLineUp2[i] * 1000;
                m_groupdata[0].m_dataLine3[i] = dbLineUp3[i] * 1000;
            }

            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            return true;
        }

        public bool SetGaussDataUp(double[] dbLineUp1, double[] dbLineUp2, double[] dbLineUp3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount > 2);
            m_nDataCount = nCount;
            m_groupdata[2] = new LineDataGroup();
            m_groupdata[2].m_dataLine1 = new double[nCount];
            m_groupdata[2].m_dataLine2 = new double[nCount];
            m_groupdata[2].m_dataLine3 = new double[nCount];
            for (int i = 0; i < nCount; i++)
            {
                m_groupdata[2].m_dataLine1[i] = dbLineUp1[i] * 1000;
                m_groupdata[2].m_dataLine2[i] = dbLineUp2[i] * 1000;
                m_groupdata[2].m_dataLine3[i] = dbLineUp3[i] * 1000;
            }

            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            return true;
        }

        public bool SetProcessedDataDown(double[] dbLineDown1, double[] dbLineDown2, double[] dbLineDown3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount > 1);
            System.Diagnostics.Trace.Assert(m_nDataCount == nCount);
            m_nDataCount = nCount;
            m_groupdata[1] = new LineDataGroup();
            m_groupdata[1].m_dataLine1 = new double[dbLineDown1.Length];
            m_groupdata[1].m_dataLine2 = new double[dbLineDown2.Length];
            m_groupdata[1].m_dataLine3 = new double[dbLineDown3.Length];
            for (int i = 0; i < m_nDataCount; i++)
            {
                m_groupdata[1].m_dataLine1[i] = dbLineDown1[i] * 1000;
                m_groupdata[1].m_dataLine2[i] = dbLineDown2[i] * 1000;
                m_groupdata[1].m_dataLine3[i] = dbLineDown3[i] * 1000;
            }
            
            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            return true;
        }

        public bool SetGaussDataDown(double[] dbLineDown1, double[] dbLineDown2, double[] dbLineDown3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount > 2);
            System.Diagnostics.Trace.Assert(m_nDataCount == nCount);
            m_nDataCount = nCount;
            m_groupdata[3] = new LineDataGroup();
            m_groupdata[3].m_dataLine1 = new double[m_nDataCount];
            m_groupdata[3].m_dataLine2 = new double[m_nDataCount];
            m_groupdata[3].m_dataLine3 = new double[m_nDataCount];
            for (int i = 0; i < m_nDataCount; i++)
            {
                m_groupdata[3].m_dataLine1[i] = dbLineDown1[i] * 1000;
                m_groupdata[3].m_dataLine2[i] = dbLineDown2[i] * 1000;
                m_groupdata[3].m_dataLine3[i] = dbLineDown3[i] * 1000;
            }

            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            return true;
        }

        private void LineGraph_Load(object sender, EventArgs e)
        {

        }

        private void LineGraph_Paint(object sender, PaintEventArgs e)
        {
            Graphics gc = e.Graphics;
            Pen linePen = new Pen(Color.White);
            Rectangle rc = new Rectangle(0, 0, this.Width, this.Height);
            gc.FillRectangle(new SolidBrush(Color.Black), rc);
            gc.DrawLine(linePen, new Point(10, 10), new Point(10, this.Height - 10));
            float fTempHeight = 1.0f * (this.Height - 20) / m_nLineHorz;
            float fTempWidth = 1.0f * (this.Width - 20) / m_nLineVert;
            Pen dotPen = new Pen(Color.White);
            dotPen.DashStyle = DashStyle.DashDot;
            for(int i=0; i<= m_nLineHorz; i++)
            {
                gc.DrawLine(dotPen, new PointF(10, 10 + i * fTempHeight), new PointF(this.Width - 10, 10 + i * fTempHeight));
            }
            for (int i = 1; i <= m_nLineVert; i++)
            {
                gc.DrawLine(dotPen, new PointF(10 + i * fTempWidth, 10), new PointF(10 + i * fTempWidth, this.Height - 10));
            }
            Color[] colorLine = new Color[] { Color.Yellow, Color.Blue, Color.YellowGreen, Color.BlueViolet };
            System.Diagnostics.Trace.Assert(m_nGroupCount <= 4);
            for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
            {
                //Y轴范围;
                Pen lineDataPen = new Pen(colorLine[nGroup]);
                gc.DrawString(m_dbMinY.ToString("F0"), this.Font, new SolidBrush(Color.White), 0, this.Height - 10);
                gc.DrawString(m_dbMaxY.ToString("F0"), this.Font, new SolidBrush(Color.White), 0, 10);
                //0轴
                float fZeroPos = this.Height - 10 - Convert.ToSingle((0 - m_dbMinY) * m_YDelta);
                gc.DrawLine(linePen, new PointF(10, fZeroPos), new PointF(this.Width - 10, fZeroPos));
                gc.DrawString("0.0", this.Font, new SolidBrush(Color.White), 0, fZeroPos);

                int nDataCount = m_nDataCount;
                int nDataOffset = 0;
                if (bDrawDropGraph)
                {
                    nDataCount = m_nMouseZoomDataCount;
                    nDataOffset = m_nMouseZoomStart;
                }

                PointF[] data1 = new PointF[nDataCount];
                PointF[] data2 = new PointF[nDataCount];
                PointF[] data3 = new PointF[nDataCount];
                for (int i = 0; i < nDataCount; i++)
                {
                    data1[i].X = Convert.ToSingle(10 + i * m_XDelta);
                    data2[i].X = Convert.ToSingle(10 + i * m_XDelta);
                    data3[i].X = Convert.ToSingle(10 + i * m_XDelta);
                    if (double.IsNaN(m_groupdata[nGroup].m_dataLine1[i + nDataOffset]))
                        data1[i].Y = this.Height - 10;
                    else
                        data1[i].Y = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_dataLine1[i + nDataOffset] - m_dbMinY) * m_YDelta);

                    if (double.IsNaN(m_groupdata[nGroup].m_dataLine2[i + nDataOffset]))
                        data2[i].Y = this.Height - 10;
                    else
                        data2[i].Y = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_dataLine2[i + nDataOffset] - m_dbMinY) * m_YDelta);

                    if (double.IsNaN(m_groupdata[nGroup].m_dataLine3[i + nDataOffset]))
                        data3[i].Y = this.Height - 10;
                    else
                        data3[i].Y = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_dataLine3[i + nDataOffset] - m_dbMinY) * m_YDelta);
                }

                if (m_groupdata[nGroup].bBold1)
                {
                    gc.DrawLines(new Pen(lineDataPen.Color, 3.0f), data1);
                    int nProbe = nGroup < 2 ? nGroup : nGroup - 2;
                    gc.DrawString("探头" + (nProbe + 1).ToString() + "数据", this.Font, new SolidBrush(Color.White), 20, 5);
                }
                else if(!bDrawOnlyBold)
                {
                    gc.DrawLines(lineDataPen, data1);
                }
                if (m_groupdata[nGroup].bBold2)
                {
                    gc.DrawLines(new Pen(lineDataPen.Color, 3.0f), data2);
                    int nProbe = nGroup < 2 ? nGroup : nGroup - 2;
                    gc.DrawString("探头" + (nProbe + 3).ToString() + "数据", this.Font, new SolidBrush(Color.White), 20, 5);
                }
                else if (!bDrawOnlyBold)
                {
                    gc.DrawLines(lineDataPen, data2);
                }
                if (m_groupdata[nGroup].bBold3)
                {
                    gc.DrawLines(new Pen(lineDataPen.Color, 3.0f), data3);
                    int nProbe = nGroup < 2 ? nGroup : nGroup - 2;
                    gc.DrawString("探头" + (nProbe + 5).ToString() + "数据", this.Font, new SolidBrush(Color.White), 20, 5);
                }
                else if (!bDrawOnlyBold)
                {
                    gc.DrawLines(lineDataPen, data3);
                }

                GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
                DrawCursor(nGroup, gc);
            }
            if (bMouseDrop && Math.Abs(m_nSeekEnd - m_nSeekStart) >= 5)
            {
                Pen redPen = new Pen(Color.Red,2.0f);
                float fRangeStartPos = Convert.ToSingle(10 + (m_nSeekStart-m_nMouseZoomStart) * m_XDelta);
                float fRangeEndPos = Convert.ToSingle(10 + (m_nSeekEnd-m_nMouseZoomStart) * m_XDelta);
                float fXValue = fRangeStartPos > fRangeEndPos ? fRangeEndPos : fRangeStartPos;
                gc.DrawRectangle(redPen, fXValue, 12.0f, Math.Abs(fRangeEndPos - fRangeStartPos), this.Height - 22.0f);
            }
        }


        private void GetCursorValue(int nGroup, int nDataIndex, double XDelta, double YDelta)
        {
            if (bDrawDropGraph)
                m_CursorPosX = Convert.ToSingle(10 + (nDataIndex-m_nMouseZoomStart) * XDelta);
            else
                m_CursorPosX = Convert.ToSingle(10 + nDataIndex * XDelta);
            m_groupdata[nGroup].m_CursorVal1 = m_groupdata[nGroup].m_dataLine1[nDataIndex];
            m_groupdata[nGroup].m_CursorVal2 = m_groupdata[nGroup].m_dataLine2[nDataIndex];
            m_groupdata[nGroup].m_CursorVal3 = m_groupdata[nGroup].m_dataLine3[nDataIndex];
            if (double.IsNaN(m_groupdata[nGroup].m_CursorVal1))
                m_groupdata[nGroup].m_CursorPosY1 = this.Height - 10;
            else
                m_groupdata[nGroup].m_CursorPosY1 = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_CursorVal1 - m_dbMinY) * YDelta);

            if (double.IsNaN(m_groupdata[nGroup].m_CursorVal2))
                m_groupdata[nGroup].m_CursorPosY2 = this.Height - 10;
            else
                m_groupdata[nGroup].m_CursorPosY2 = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_CursorVal2 - m_dbMinY) * YDelta);

            if (double.IsNaN(m_groupdata[nGroup].m_CursorVal3))
                m_groupdata[nGroup].m_CursorPosY3 = this.Height - 10;
            else
                m_groupdata[nGroup].m_CursorPosY3 = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_CursorVal3 - m_dbMinY) * YDelta);
        }
        private void DrawCursor(int nGroup, Graphics gc)
        {
            if (m_nCurosrDataIndex < 0 || m_nCurosrDataIndex >= m_nDataCount)
                return;
            Color[] colorLine = new Color[] { Color.Yellow, Color.Blue, Color.YellowGreen, Color.BlueViolet };
            System.Diagnostics.Trace.Assert(m_nGroupCount <= 4);

            Pen cursorPen = new Pen(colorLine[nGroup]);
            gc.DrawLine(cursorPen, new PointF(m_CursorPosX, 10), new PointF(m_CursorPosX, this.Height - 10));
            string strVal1 = m_groupdata[nGroup].m_CursorVal1.ToString("F3");
            string strVal2 = m_groupdata[nGroup].m_CursorVal2.ToString("F3");
            string strVal3 = m_groupdata[nGroup].m_CursorVal3.ToString("F3");
            int nWaferSize = 156;
            if(bDrawOnlyBold)
            {
                if (m_groupdata[nGroup].bBold1)
                {
                    double realdx = 1.0 * nWaferSize / m_groupdata[nGroup].m_dataLine1.Length;
                    string strXVal = "X:" + m_nCurosrDataIndex.ToString() + "硅片位置（mm）：" + (m_nCurosrDataIndex * realdx).ToString();
                    gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2);
                    gc.DrawString(strVal1, this.Font, new SolidBrush(colorLine[nGroup]), m_CursorPosX, m_groupdata[nGroup].m_CursorPosY1);
                }
                if (m_groupdata[nGroup].bBold2)
                {
                    double realdx = 1.0 * nWaferSize / m_groupdata[nGroup].m_dataLine2.Length;
                    string strXVal = "X:" + m_nCurosrDataIndex.ToString() + "硅片位置（mm）：" + (m_nCurosrDataIndex * realdx).ToString();
                    gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2);
                    gc.DrawString(strVal2, this.Font, new SolidBrush(colorLine[nGroup]), m_CursorPosX, m_groupdata[nGroup].m_CursorPosY2);
                }
                if (m_groupdata[nGroup].bBold3)
                {
                    double realdx = 1.0 * nWaferSize / m_groupdata[nGroup].m_dataLine3.Length;
                    string strXVal = "X:" + m_nCurosrDataIndex.ToString() + "硅片位置（mm）：" + (m_nCurosrDataIndex * realdx).ToString();
                    gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2);
                    gc.DrawString(strVal3, this.Font, new SolidBrush(colorLine[nGroup]), m_CursorPosX, m_groupdata[nGroup].m_CursorPosY3);
                }
            }
            else
            {
                double realdx = 1.0 * nWaferSize / m_nDataCount;
                string strXVal = "X:" + m_nCurosrDataIndex.ToString() + "硅片位置（mm）：" + (m_nCurosrDataIndex * realdx).ToString();
                gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2);
                gc.DrawString(strVal1, this.Font, new SolidBrush(colorLine[nGroup]), m_CursorPosX, m_groupdata[nGroup].m_CursorPosY1);
                gc.DrawString(strVal2, this.Font, new SolidBrush(colorLine[nGroup]), m_CursorPosX, m_groupdata[nGroup].m_CursorPosY2);
                gc.DrawString(strVal3, this.Font, new SolidBrush(colorLine[nGroup]), m_CursorPosX, m_groupdata[nGroup].m_CursorPosY3);
            }
        }

        private void LineGraph_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || m_groupdata == null)
                return;
            m_bCursor = true;                

            int nDataIndex = Convert.ToInt32((e.X - 10) / m_XDelta);
            if (nDataIndex < 0)
                nDataIndex = 0;
            else if (nDataIndex >= m_nDataCount)
                nDataIndex = m_nDataCount - 1;
            if (bDrawDropGraph)
                nDataIndex += m_nMouseZoomStart;
            if (bCtrl)
            {
                bMouseDrop = true;
                m_nSeekStart = nDataIndex;
                m_nSeekEnd = nDataIndex;
            }
            else
            {
                m_nCurosrDataIndex = nDataIndex;
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
            }
            Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            
        }

        private void LineGraph_MouseMove(object sender, MouseEventArgs e)
        {
            if (!m_bCursor || e.Button != MouseButtons.Left || m_groupdata == null)
                return;
            int nDataIndex = Convert.ToInt32((e.X - 10) / m_XDelta);
            if (nDataIndex < 0)
                nDataIndex = 0;
            else if (nDataIndex >= m_nDataCount)
                nDataIndex = m_nDataCount - 1;
            if (bDrawDropGraph)
                nDataIndex += m_nMouseZoomStart;
            if (bMouseDrop && bCtrl)
            {
                m_nSeekEnd = nDataIndex;
            }
            else
            {
                m_nCurosrDataIndex = nDataIndex;
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
 
            }
            Invalidate(new Rectangle(0, 0, this.Width, this.Height));
        }

        private void LineGraph_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || m_groupdata == null)
                return;
            if (bMouseDrop && bCtrl)
            {
                int nDataIndex = Convert.ToInt32((e.X - 10) / m_XDelta);
                if (nDataIndex < 0)
                    nDataIndex = 0;
                else if (nDataIndex >= m_nDataCount)
                    nDataIndex = m_nDataCount - 1;
                if (bDrawDropGraph)
                    nDataIndex += m_nMouseZoomStart;
                m_nSeekEnd = nDataIndex;
                int nSeekCount = Math.Abs(m_nSeekEnd - m_nSeekStart) + 1;
                if (nSeekCount >= 6)
                 {
                     m_nMouseZoomDataCount = nSeekCount;
                     m_XDelta = 1.0f * (this.Width - 20) / m_nMouseZoomDataCount;
                     bDrawDropGraph = true;
                     m_nMouseZoomStart = m_nSeekStart < m_nSeekEnd ? m_nSeekStart : m_nSeekEnd;
                     m_nMouseZoomEnd = m_nSeekStart < m_nSeekEnd ? m_nSeekEnd : m_nSeekStart;
                 }
            }
            m_bCursor = false;
            bMouseDrop = false;
            Invalidate(new Rectangle(0, 0, this.Width, this.Height));
        }

        private void LineGraph_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (m_groupdata == null)
                return;

            if(e.KeyChar == (Char)Keys.D1)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[0].bBold1 = true;
                if(m_nGroupCount > 2)
                    m_groupdata[2].bBold1 = true;
               Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D2)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[1].bBold1 = true;
                if (m_nGroupCount > 2)
                    m_groupdata[3].bBold1 = true;
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D3)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[0].bBold2 = true;
                if (m_nGroupCount > 2)
                    m_groupdata[2].bBold2 = true;
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D4)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[1].bBold2 = true;
                if (m_nGroupCount > 2)
                    m_groupdata[3].bBold2 = true;
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D5)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[0].bBold3 = true;
                if (m_nGroupCount > 2)
                    m_groupdata[2].bBold3 = true;
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D6)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[1].bBold3 = true;
                if (m_nGroupCount > 2)
                    m_groupdata[3].bBold3 = true;
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D0)
            {
                bDrawOnlyBold = !bDrawOnlyBold;
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else
            {
                if (e.KeyChar == 26 && bCtrl)
                {

                    bDrawDropGraph = false;
                    bMouseDrop = false;
                    m_nMouseZoomStart = 0;
                    m_nMouseZoomEnd = 0;
                    m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
                }
                else
                {
                    for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    {
                        m_groupdata[nGroup].bBold1 = false;
                        m_groupdata[nGroup].bBold2 = false;
                        m_groupdata[nGroup].bBold3 = false;
                    }
                }
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }

            e.Handled = false;
        }

        private void LineGraph_KeyDown(object sender, KeyEventArgs e)
        {
            if (m_groupdata == null)
                return;
            if(e.Control)
            {
                bCtrl = true;
            }
        }

        private void LineGraph_KeyUp(object sender, KeyEventArgs e)
        {
            if (m_groupdata == null)
                return;
            bCtrl = false;
            if (e.KeyCode == Keys.Down)
            {
                m_nCurosrDataIndex += 10;
                if (m_nCurosrDataIndex >= m_nDataCount)
                    m_nCurosrDataIndex = m_nDataCount - 1;
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyCode == Keys.Up)
            {
                m_nCurosrDataIndex -= 10;
                if (m_nCurosrDataIndex <= 0)
                    m_nCurosrDataIndex = 0;
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyCode == Keys.Right)
            {
                m_nCurosrDataIndex++;
                if (m_nCurosrDataIndex >= m_nDataCount)
                    m_nCurosrDataIndex = m_nDataCount - 1;
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
            Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyCode == Keys.Left)
            {
                m_nCurosrDataIndex--;
                if (m_nCurosrDataIndex <= 0)
                    m_nCurosrDataIndex = 0;
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            System.Diagnostics.Trace.WriteLine("e.KeyCode"+e.KeyCode.ToString(),"e.KeyData"+e.KeyData.ToString()+"e.KeyValue"+e.KeyValue.ToString());
        }
    }

    public class LineDataGroup
    {
        public LineDataGroup()
        {

        }

        public double m_CursorVal1;
        public double m_CursorVal2;
        public double m_CursorVal3;
        public float m_CursorPosY1;
        public float m_CursorPosY2;
        public float m_CursorPosY3;

        public double[] m_dataLine1 = null;
        public bool bBold1 = false;
        public double[] m_dataLine2 = null;
        public bool bBold2 = false;
        public double[] m_dataLine3 = null;
        public bool bBold3 = false;
    }
}
