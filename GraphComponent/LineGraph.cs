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
        private double m_dbMaxY = 100; //um

        private double m_dbMinYSaved = 0;
        private double m_dbMaxYSaved = 100;

        private LineDataGroup[] m_groupdata = null;
        private int m_nGroupCount = 0;
        private bool bDrawOnlyBold = false;

        private bool bMouseDrop = false;
        private bool m_bDrawDropGraph = false;
        public bool DrawDropGraph
        {
            get { return m_bDrawDropGraph; }
        }

        private bool m_bDrawCalcPos = false;//C++计算的波峰波谷位置

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

        public void setGroupPeakValley(int nGroup, LinePeakValleyPair[] PairPeakData,LinePeakValleyPair[] PairValleyData)
        {
            if (nGroup < 0 || nGroup >= m_nGroupCount)
                return;
            m_groupdata[nGroup].m_PairPeakData = PairPeakData;
            m_groupdata[nGroup].m_PairValleyData = PairValleyData;
            m_bDrawCalcPos = true;
            m_bDrawDropGraph = false;
        }

        public bool SetLineData(int nGroup,double[] dbLine1,double[] dbLine2, double[] dbLine3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null || nGroup < 0 || nGroup >= m_nGroupCount)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount >= 1);
            m_nDataCount = nCount;
            m_groupdata[nGroup] = new LineDataGroup();
            m_groupdata[nGroup].m_dataLine1 = new double[nCount];
            m_groupdata[nGroup].m_dataLine2 = new double[nCount];
            m_groupdata[nGroup].m_dataLine3 = new double[nCount];
            Array.Copy(dbLine1, m_groupdata[nGroup].m_dataLine1, m_nDataCount);
            Array.Copy(dbLine2, m_groupdata[nGroup].m_dataLine2, m_nDataCount);
            Array.Copy(dbLine3, m_groupdata[nGroup].m_dataLine3, m_nDataCount);

            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            m_YDelta = 1.0f * (this.Height - 20) / (m_dbMaxY - m_dbMinY);
            m_bDrawCalcPos = false;
            return true;
        }

        public void SetYValueAxis(double dbMin, double dbMax,bool bUpdate)
        {
            m_dbMinY = dbMin;
            m_dbMaxY = dbMax;
            if (bUpdate)
            {
                m_dbMinYSaved = m_dbMinY;
                m_dbMaxYSaved = m_dbMaxY;
            }
            m_YDelta = 1.0f * (this.Height - 20) / (m_dbMaxY - m_dbMinY);
        }

        public bool SetProcessedDataUp(double[] dbLineUp1, double[] dbLineUp2, double[] dbLineUp3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount > 1);
            m_nDataCount = nCount;
            m_groupdata[0] = new LineDataGroup();
            m_groupdata[0].m_dataLine1 = new double[m_nDataCount];
            m_groupdata[0].m_dataLine2 = new double[m_nDataCount];
            m_groupdata[0].m_dataLine3 = new double[m_nDataCount];

            Array.ForEach(m_groupdata[0].m_dataLine1, InitData);
            Array.ForEach(m_groupdata[0].m_dataLine2, InitData);
            Array.ForEach(m_groupdata[0].m_dataLine3, InitData);
            Array.Copy(dbLineUp1, m_groupdata[0].m_dataLine1, dbLineUp1.Length);
            Array.Copy(dbLineUp2, m_groupdata[0].m_dataLine2, dbLineUp2.Length);
            Array.Copy(dbLineUp3, m_groupdata[0].m_dataLine3, dbLineUp3.Length);
            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            return true;
        }

        private static void InitData(double dbValue)
        {
            dbValue = double.NaN;
        }

        public bool SetGaussDataUp(double[] dbLineUp1, double[] dbLineUp2, double[] dbLineUp3, int nCount)
        {
            if (nCount <= 0 || m_groupdata == null)
                return false;
            System.Diagnostics.Trace.Assert(m_nGroupCount > 2);
            m_nDataCount = nCount;
            m_groupdata[2] = new LineDataGroup();
            m_groupdata[2].m_dataLine1 = new double[m_nDataCount];
            m_groupdata[2].m_dataLine2 = new double[m_nDataCount];
            m_groupdata[2].m_dataLine3 = new double[m_nDataCount];
            Array.ForEach(m_groupdata[2].m_dataLine1, InitData);
            Array.ForEach(m_groupdata[2].m_dataLine2, InitData);
            Array.ForEach(m_groupdata[2].m_dataLine3, InitData);
            Array.Copy(dbLineUp1, m_groupdata[2].m_dataLine1, dbLineUp1.Length);
            Array.Copy(dbLineUp2, m_groupdata[2].m_dataLine2, dbLineUp2.Length);
            Array.Copy(dbLineUp3, m_groupdata[2].m_dataLine3, dbLineUp3.Length);
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
            m_groupdata[1].m_dataLine1 = new double[m_nDataCount];
            m_groupdata[1].m_dataLine2 = new double[m_nDataCount];
            m_groupdata[1].m_dataLine3 = new double[m_nDataCount];
            Array.ForEach(m_groupdata[1].m_dataLine1, InitData);
            Array.ForEach(m_groupdata[1].m_dataLine2, InitData);
            Array.ForEach(m_groupdata[1].m_dataLine3, InitData);
            Array.Copy(dbLineDown1, m_groupdata[1].m_dataLine1, dbLineDown1.Length);
            Array.Copy(dbLineDown2, m_groupdata[1].m_dataLine2, dbLineDown2.Length);
            Array.Copy(dbLineDown3, m_groupdata[1].m_dataLine3, dbLineDown3.Length);
            
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
            Array.ForEach(m_groupdata[3].m_dataLine1, InitData);
            Array.ForEach(m_groupdata[3].m_dataLine2, InitData);
            Array.ForEach(m_groupdata[3].m_dataLine3, InitData);
            Array.Copy(dbLineDown1, m_groupdata[3].m_dataLine1, dbLineDown1.Length);
            Array.Copy(dbLineDown2, m_groupdata[3].m_dataLine2, dbLineDown2.Length);
            Array.Copy(dbLineDown3, m_groupdata[3].m_dataLine3, dbLineDown3.Length);

            m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
            return true;
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
                if (m_bDrawDropGraph)
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
                    gc.DrawLines(lineDataPen, data1);
                    int nProbe = nGroup < 2 ? nGroup : nGroup - 2;
                    gc.DrawString("探头" + (nProbe + 1).ToString() + "数据", this.Font, new SolidBrush(Color.White), 20, 5+nGroup*10);
                }
                else if(!bDrawOnlyBold)
                {
                    gc.DrawLines(lineDataPen, data1);
                }
                if (m_groupdata[nGroup].bBold2)
                {
                    gc.DrawLines(lineDataPen, data2);
                    int nProbe = nGroup < 2 ? nGroup : nGroup - 2;
                    gc.DrawString("探头" + (nProbe + 3).ToString() + "数据", this.Font, new SolidBrush(Color.White), 20, 5 + nGroup * 10);
                }
                else if (!bDrawOnlyBold)
                {
                    gc.DrawLines(lineDataPen, data2);
                }
                if (m_groupdata[nGroup].bBold3)
                {
                    gc.DrawLines(lineDataPen, data3);
                    int nProbe = nGroup < 2 ? nGroup : nGroup - 2;
                    gc.DrawString("探头" + (nProbe + 5).ToString() + "数据", this.Font, new SolidBrush(Color.White), 20, 5 + nGroup * 10);
                }
                else if (!bDrawOnlyBold)
                {
                    gc.DrawLines(lineDataPen, data3);
                }

                GetCursorValue(nGroup, m_nCurosrDataIndex, m_XDelta, m_YDelta);
                DrawCursor(nGroup, gc);

                if (m_groupdata[nGroup].bDrawPeakValley)
                {
                    DrawPeakValleyPos(gc, nGroup);
                }
                if(m_bDrawCalcPos)
                {
                    DrawSawMarkPeakValleyPos(gc, nGroup);
                }
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
            if (m_bDrawDropGraph)
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

        private void DrawSawMarkPeakValleyPos(Graphics gc, int nGroup)
        {
            int nWaferSize = 156;
            double ReciprocRealdx = 1.0 * m_nDataCount / nWaferSize;
            Pen flagPen = new Pen(Color.White);
            if (m_groupdata[nGroup].m_PairPeakData != null)
            {
                for (int i = 0; i < m_groupdata[nGroup].m_PairPeakData.Length; i++)
                {
                    if (i == 0)
                    {
                        if (!m_groupdata[nGroup].bBold1)
                            continue;
                        ReciprocRealdx = 1.0 * m_groupdata[nGroup].m_dataLine1.Length / nWaferSize;
                    }
                    else if (i == 1)
                    {
                        if (!m_groupdata[nGroup].bBold2)
                            continue;
                        ReciprocRealdx = 1.0 * m_groupdata[nGroup].m_dataLine2.Length / nWaferSize;
                    }
                    else if (i == 2)
                    {
                        if (!m_groupdata[nGroup].bBold3)
                            continue;
                        ReciprocRealdx = 1.0 * m_groupdata[nGroup].m_dataLine3.Length / nWaferSize;
                    }

                    float fPosX = Convert.ToSingle(10 + (m_groupdata[nGroup].m_PairPeakData[i].DataPos * ReciprocRealdx) * m_XDelta);
                    float fPosY = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_PairPeakData[i].DataValue - m_dbMinY) * m_YDelta);
                    gc.DrawLine(flagPen, new PointF(fPosX, fPosY - 5), new PointF(fPosX, fPosY + 5));
                    gc.DrawLine(flagPen, new PointF(fPosX - 5, fPosY), new PointF(fPosX + 5, fPosY));
                    string strVal = m_groupdata[nGroup].m_PairPeakData[i].DataValue.ToString("F3");
                    gc.DrawString(strVal, this.Font, new SolidBrush(Color.White), this.Width / 3 - 250 + 100 * nGroup, 2 + 10 * i);

                }
            }
            Pen flagPenV = new Pen(Color.Yellow);
            if (m_groupdata[nGroup].m_PairValleyData != null)
            {
                for (int i = 0; i < m_groupdata[nGroup].m_PairValleyData.Length; i++)
                {
                    if (i == 0)
                    {
                        if (!m_groupdata[nGroup].bBold1)
                            continue;
                        ReciprocRealdx = 1.0 * m_groupdata[nGroup].m_dataLine1.Length / nWaferSize;
                    }
                    else if (i == 1)
                    {
                        if (!m_groupdata[nGroup].bBold2)
                            continue;
                        ReciprocRealdx = 1.0 * m_groupdata[nGroup].m_dataLine2.Length / nWaferSize;
                    }
                    else if (i == 2)
                    {
                        if (!m_groupdata[nGroup].bBold3)
                            continue;
                        ReciprocRealdx = 1.0 * m_groupdata[nGroup].m_dataLine3.Length / nWaferSize;
                    }

                    float fPosX = Convert.ToSingle(10 + (m_groupdata[nGroup].m_PairValleyData[i].DataPos * ReciprocRealdx) * m_XDelta);
                    float fPosY = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_PairValleyData[i].DataValue - m_dbMinY) * m_YDelta);
                    gc.DrawRectangle(flagPenV, fPosX - 2, fPosY - 2, 4, 4);
                    string strVal = m_groupdata[nGroup].m_PairValleyData[i].DataValue.ToString("F3");
                    gc.DrawString(strVal, this.Font, new SolidBrush(Color.Yellow), this.Width / 3 - 200 + 100 * nGroup, 2 + 10 * i);
                }
            }
        }

        private void DrawPeakValleyPos(Graphics gc, int nGroup)
        {
            Pen flagPen = new Pen(Color.White);
            Pen AvgPen = new Pen(Color.Red);
            PointF[] CenterLinePts = new PointF[m_nMouseZoomDataCount];
            for (int i = 0; i < m_nMouseZoomDataCount; i++)
            {
                CenterLinePts[i].X = Convert.ToSingle(10 + i * m_XDelta);
                double dbY = m_groupdata[nGroup].m_MedianCoef[0] + m_groupdata[nGroup].m_MedianCoef[1] * i;
                CenterLinePts[i].Y = this.Height - 10 - Convert.ToSingle((dbY - m_dbMinY) * m_YDelta);
            }
            gc.DrawLines(AvgPen, CenterLinePts);
            Color[] colorLine = new Color[] { Color.Yellow, Color.Blue, Color.YellowGreen, Color.BlueViolet };
            for (int i = 0; i < m_groupdata[nGroup].m_PairPeakData.Length; i++)
            {
                float fPosX = Convert.ToSingle(10 + (m_groupdata[nGroup].m_PairPeakData[i].DataPos - m_nMouseZoomStart) * m_XDelta);
                float fPosY = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_PairPeakData[i].DataValue - m_dbMinY) * m_YDelta);
                gc.DrawLine(flagPen, new PointF(fPosX, fPosY - 5), new PointF(fPosX, fPosY + 5));
                gc.DrawLine(flagPen, new PointF(fPosX - 5, fPosY), new PointF(fPosX + 5, fPosY));
                string strVal = m_groupdata[nGroup].m_PairPeakData[i].DataValue.ToString("F3");
                gc.DrawString(strVal, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 3 - 250 + 100 * nGroup, 2 + 10 * i);
            }

            Pen flagPenV = new Pen(Color.Yellow);
            for (int i = 0; i < m_groupdata[nGroup].m_PairValleyData.Length; i++)
            {
                float fPosX = Convert.ToSingle(10 + (m_groupdata[nGroup].m_PairValleyData[i].DataPos - m_nMouseZoomStart) * m_XDelta);
                float fPosY = this.Height - 10 - Convert.ToSingle((m_groupdata[nGroup].m_PairValleyData[i].DataValue - m_dbMinY) * m_YDelta);
                gc.DrawRectangle(flagPenV, fPosX - 2, fPosY - 2, 4, 4);
                string strVal = m_groupdata[nGroup].m_PairValleyData[i].DataValue.ToString("F3");
                gc.DrawString(strVal, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 3 - 200 + 100 * nGroup, 2 + 10 * i);
            }
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
                    gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2 + 10 * nGroup);
                    gc.DrawString(strVal1, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 2 - 100, 2 + 10 * nGroup);
                }
                if (m_groupdata[nGroup].bBold2)
                {
                    double realdx = 1.0 * nWaferSize / m_groupdata[nGroup].m_dataLine2.Length;
                    string strXVal = "X:" + m_nCurosrDataIndex.ToString() + "硅片位置（mm）：" + (m_nCurosrDataIndex * realdx).ToString();
                    gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2 + 10 * nGroup);
                    gc.DrawString(strVal2, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 2 - 100, 2 + 10 * nGroup);
                }
                if (m_groupdata[nGroup].bBold3)
                {
                    double realdx = 1.0 * nWaferSize / m_groupdata[nGroup].m_dataLine3.Length;
                    string strXVal = "X:" + m_nCurosrDataIndex.ToString() + "硅片位置（mm）：" + (m_nCurosrDataIndex * realdx).ToString();
                    gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2 + 10 * nGroup);
                    gc.DrawString(strVal3, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 2 - 100, 2 + 10 * nGroup);
                }
            }
            else
            {
                double realdx = 1.0 * nWaferSize / m_nDataCount;
                string strXVal = "X:" + m_nCurosrDataIndex.ToString() + "硅片位置（mm）：" + (m_nCurosrDataIndex * realdx).ToString();
                gc.DrawString(strXVal, this.Font, new SolidBrush(Color.Gold), this.Width / 2 - 20, 2 + 10 * nGroup);
                gc.DrawString(strVal1, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 2 - 100, 2 + 10 * nGroup);
                gc.DrawString(strVal2, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 2 - 100, 2 + 10 * nGroup);
                gc.DrawString(strVal3, this.Font, new SolidBrush(colorLine[nGroup]), this.Width / 2 - 100, 2 + 10 * nGroup);
            }
        }

        private void LineGraph_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || m_groupdata == null)
                return;
            if (e.X < 10 || e.X > this.Width - 10)
                return;
            m_bCursor = true;
            int nDataIndex = Convert.ToInt32((e.X - 10) / m_XDelta);
            if (nDataIndex < 0)
                nDataIndex = 0;
            else if (nDataIndex >= m_nDataCount)
                nDataIndex = m_nDataCount - 1;
            if (m_bDrawDropGraph)
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
            if (e.X < 10 || e.X > this.Width - 10)
                return;
            int nDataIndex = Convert.ToInt32((e.X - 10) / m_XDelta);
            if (nDataIndex < 0)
                nDataIndex = 0;
            else if (nDataIndex >= m_nDataCount)
                nDataIndex = m_nDataCount - 1;
            if (m_bDrawDropGraph)
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
            if (e.X < 10 || e.X > this.Width - 10)
                return;
            if (bMouseDrop && bCtrl)
            {
                int nDataIndex = Convert.ToInt32((e.X - 10) / m_XDelta);
                if (nDataIndex < 0)
                    nDataIndex = 0;
                else if (nDataIndex >= m_nDataCount)
                    nDataIndex = m_nDataCount - 1;
                if (m_bDrawDropGraph)
                    nDataIndex += m_nMouseZoomStart;
                m_nSeekEnd = nDataIndex;
                int nSeekCount = Math.Abs(m_nSeekEnd - m_nSeekStart) + 1;
                if (nSeekCount >= 6)
                 {
                     m_nMouseZoomDataCount = nSeekCount;
                     m_XDelta = 1.0f * (this.Width - 20) / m_nMouseZoomDataCount;
                     m_bDrawDropGraph = true;
                     m_bDrawCalcPos = false;
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
            if (m_groupdata == null || m_nGroupCount < 2)
                return;

            SetYValueAxis(m_dbMinYSaved, m_dbMaxYSaved, false);

            if(e.KeyChar == (Char)Keys.D1)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bDrawPeakValley = false;
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[0].bBold1 = true;
                if (m_nGroupCount == 4)
                {
                    m_groupdata[2].bBold1 = true;
                }
               Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D2)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bDrawPeakValley = false;
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[1].bBold1 = true;
                if (m_nGroupCount == 4)
                {
                    m_groupdata[3].bBold1 = true;
                }
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D3)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bDrawPeakValley = false;
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[0].bBold2 = true;
                if (m_nGroupCount == 4)
                {
                    m_groupdata[2].bBold2 = true;
                }
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D4)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bDrawPeakValley = false;
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[1].bBold2 = true;
                if (m_nGroupCount == 4)
                {
                    m_groupdata[3].bBold2 = true;
                }
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D5)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bDrawPeakValley = false;
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[0].bBold3 = true;
                if (m_nGroupCount == 4)
                {
                    m_groupdata[2].bBold3 = true;
                }
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D6)
            {
                for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                {
                    m_groupdata[nGroup].bDrawPeakValley = false;
                    m_groupdata[nGroup].bBold1 = false;
                    m_groupdata[nGroup].bBold2 = false;
                    m_groupdata[nGroup].bBold3 = false;
                }
                m_groupdata[1].bBold3 = true;
                if (m_nGroupCount == 4)
                {
                    m_groupdata[3].bBold3 = true;
                }
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else if (e.KeyChar == (Char)Keys.D0)
            {
                bDrawOnlyBold = !bDrawOnlyBold;
                if (!bDrawOnlyBold)
                {
                    for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    {
                        m_groupdata[nGroup].bDrawPeakValley = false;
                    }
                }
                Invalidate(new Rectangle(0, 0, this.Width, this.Height));
            }
            else
            {
                if (e.KeyChar == 26 && bCtrl)
                {
                    for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    {
                        m_groupdata[nGroup].bDrawPeakValley = false;
                    }
                    m_bDrawDropGraph = false;
                    m_bDrawCalcPos = false;
                    bMouseDrop = false;
                    m_nMouseZoomStart = 0;
                    m_nMouseZoomEnd = 0;
                    m_XDelta = 1.0f * (this.Width - 20) / m_nDataCount;
                }
                else
                {
                    for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
                    {
                        m_groupdata[nGroup].bDrawPeakValley = false;
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

        public void CalcPeakValley()
        {
            double dbMax = 100;
            double dbMin = -100;
            for (int nGroup = 0; nGroup < m_nGroupCount; nGroup++)
            {
                double[] dbData = new double[m_nMouseZoomDataCount];
                if (m_groupdata[nGroup].bBold1)
                {
                    m_groupdata[nGroup].bDrawPeakValley = true;
                    for (int i = 0; i < m_nMouseZoomDataCount; i++)
                    {
                        dbData[i] = m_groupdata[nGroup].m_dataLine1[m_nMouseZoomStart + i];
                        if (dbData[i] > dbMax)
                            dbMax = dbData[i];
                        if (dbData[i] < dbMin)
                            dbMin = dbData[i];
                    }
                }
                else if (m_groupdata[nGroup].bBold2)
                {
                    m_groupdata[nGroup].bDrawPeakValley = true;
                    for (int i = 0; i < m_nMouseZoomDataCount; i++)
                    {
                        dbData[i] = m_groupdata[nGroup].m_dataLine2[m_nMouseZoomStart + i];
                        if (dbData[i] > dbMax)
                            dbMax = dbData[i];
                        if (dbData[i] < dbMin)
                            dbMin = dbData[nGroup];
                    }
                }
                else if (m_groupdata[nGroup].bBold3)
                {
                    m_groupdata[nGroup].bDrawPeakValley = true;
                    for (int i = 0; i < m_nMouseZoomDataCount; i++)
                    {
                        dbData[i] = m_groupdata[nGroup].m_dataLine3[m_nMouseZoomStart + i];
                        if (dbData[i] > dbMax)
                            dbMax = dbData[i];
                        if (dbData[i] < dbMin)
                            dbMin = dbData[i];
                    }
                }

                if(m_groupdata[nGroup].bDrawPeakValley)
                {
                    int nCalcCount = 8;
                    double[] dbPeak = new double[nCalcCount];
                    int[] PeakIndex = new int[nCalcCount];
                    double[] dbValley = new double[nCalcCount];
                    int[] ValleyIndex = new int[nCalcCount];
                    int nPeakCount = 0;
                    int nValleyCount = 0;

                    double[] dbCoef = new double[2];
                    CalcPeakValleyMethod(dbData, m_nMouseZoomDataCount, nCalcCount, ref dbPeak, ref PeakIndex, ref nPeakCount, ref dbValley, ref ValleyIndex, ref nValleyCount, ref dbCoef);
                    if (nPeakCount > 0)
                    {
                        m_groupdata[nGroup].m_PairPeakData = new LinePeakValleyPair[nPeakCount];
                        for (int n = 0; n < nPeakCount; n++)
                        {
                            m_groupdata[nGroup].m_PairPeakData[n].DataValue = dbPeak[n];
                            m_groupdata[nGroup].m_PairPeakData[n].DataPos = PeakIndex[n] + m_nMouseZoomStart;
                        }
                    }
                    if (nValleyCount > 0)
                    {
                        m_groupdata[nGroup].m_PairValleyData = new LinePeakValleyPair[nValleyCount];
                        for (int n = 0; n < nValleyCount; n++)
                        {
                            m_groupdata[nGroup].m_PairValleyData[n].DataValue = dbValley[n];
                            m_groupdata[nGroup].m_PairValleyData[n].DataPos = ValleyIndex[n] + m_nMouseZoomStart;
                        }
                    }
                    m_groupdata[nGroup].m_MedianCoef = dbCoef;
                }
                //
            }
            SetYValueAxis(dbMin, dbMax,false);
            Invalidate(new Rectangle(0, 0, this.Width, this.Height));
 
        }

        private void CalcPeakValleyMethod(double[] dbData, int nDataLen, int nCalcCount, ref double[] PeakOut, ref int[] PeakIndex, ref int PeakCount, ref double[] ValleyOut, ref int[] ValleyIndex, ref int ValleyCount, ref double[] dbCoef)
        {
            if (nCalcCount <= 0 || nCalcCount >= 10)
                return;
            if (nDataLen < 10)
                return;
            bool bDataUp = false;
            bool bDataDown = false;

            double[] xData = new double[nDataLen];
            for (int i = 0; i < nDataLen; i++)
                xData[i] = i;
            LeastSquare fit = new LeastSquare();
            fit.linearFit(xData, dbData);
            fit.getFactor(ref dbCoef);

            double[] pDataPeak = new double[nDataLen];
            int[] pPeakIndex = new int[nDataLen];
            double[] pDataValley = new double[nDataLen];
            int[] pValleyIndex = new int[nDataLen];
            int nFindPeakCount = 0;
            int nFindValleyCount = 0;
            double dbPeak = 0;
            bool bPeakValueInit = false;
            double dbValley = 0;
            bool bValleyValueInit = false;
            int nPeakIndex = 0;
            int nValleyIndex = 0;
            bool bMaybePeak = false;
            bool bMaybeValley = false;
            double dbHistory = dbData[0] - fit.getY(0);
            for (int i = 0; i < nDataLen - 1; i++)
            {
                //中线值
                double dbCenterVal = fit.getY(i);
                double dbNextCenterVal = fit.getY(i + 1);
                if (!bPeakValueInit && dbData[i] > dbCenterVal)
                {
                    dbPeak = dbData[i];
                    nPeakIndex = i;
                    bPeakValueInit = true;
                }
                if (!bValleyValueInit && dbData[i] < dbCenterVal)
                {
                    dbValley = dbData[i];
                    nValleyIndex = i;
                    bValleyValueInit = true;
                }
                if (dbData[i] - dbCenterVal > dbHistory)
                {
                    bDataUp = true;
                    bDataDown = false;
                    bMaybePeak = false;
                    if (bPeakValueInit && dbData[i] - dbCenterVal > dbPeak - fit.getY(nPeakIndex))
                    {
                        dbPeak = dbData[i];
                        nPeakIndex = i;
                    }
                }
                if (dbData[i] - dbCenterVal < dbHistory)
                {
                    bDataDown = true;
                    bDataUp = false;
                    bMaybeValley = false;
                    if (bValleyValueInit && dbData[i] - dbCenterVal < dbValley - fit.getY(nValleyIndex))
                    {
                        dbValley = dbData[i];
                        nValleyIndex = i;
                    }
                }
                //数据变小且穿过中线时可能是峰值
                if (bDataUp && dbData[i] > dbCenterVal && dbData[i + 1] - dbNextCenterVal < dbData[i] - dbCenterVal)
                    bMaybePeak = true;
                if (bMaybePeak && dbData[i] < dbCenterVal)
                {
                    pDataPeak[nFindPeakCount] = dbPeak;
                    pPeakIndex[nFindPeakCount] = nPeakIndex;
                    nFindPeakCount++;
                    bMaybePeak = false;
                    bPeakValueInit = false;
                }
                //数据变大且穿过中线时可能是谷值
                if (bDataDown && dbData[i] < dbCenterVal && dbData[i + 1] - dbNextCenterVal > dbData[i] - dbCenterVal)
                    bMaybeValley = true;
                if (bMaybeValley && dbData[i] > dbCenterVal)
                {
                    pDataValley[nFindValleyCount] = dbValley;
                    pValleyIndex[nFindValleyCount] = nValleyIndex;
                    nFindValleyCount++;
                    bMaybeValley = false;
                    bValleyValueInit = false;
                }
                dbHistory = dbData[i] - dbCenterVal;
            }

            if (bPeakValueInit)
            {
                pDataPeak[nFindPeakCount] = dbPeak;
                pPeakIndex[nFindPeakCount] = nPeakIndex;
                nFindPeakCount++;
            }

            if (bValleyValueInit)
            {
                pDataValley[nFindValleyCount] = dbValley;
                pValleyIndex[nFindValleyCount] = nValleyIndex;
                nFindValleyCount++;
            }
            //峰从大到小排序（去掉中线值后的数据应是正的）
            for (int i = nFindPeakCount - 1; i > 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    double dbCenterVal1 = fit.getY(pPeakIndex[j]);
                    double dbCenterVal2 = fit.getY(pPeakIndex[j + 1]);
                    if (pDataPeak[j] - dbCenterVal1 < pDataPeak[j + 1] - dbCenterVal2)
                    {
                        double dbTemp = pDataPeak[j];
                        pDataPeak[j] = pDataPeak[j + 1];
                        pDataPeak[j + 1] = dbTemp;
                        //位置也对应
                        int nPos = pPeakIndex[j];
                        pPeakIndex[j] = pPeakIndex[j + 1];
                        pPeakIndex[j + 1] = nPos;

                    }
                }
            }

            PeakCount = nFindPeakCount > nCalcCount ? nCalcCount : nFindPeakCount;
            for (int nj = 0; nj < PeakCount; nj++)
            {
                PeakOut[nj] = pDataPeak[nj];
                PeakIndex[nj] = pPeakIndex[nj];
            }

            //谷从小到大排序(去掉中线值后的数据应是负的)
            for (int i = nFindValleyCount - 1; i > 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    double dbCenterVal1 = fit.getY(pValleyIndex[j]);
                    double dbCenterVal2 = fit.getY(pValleyIndex[j + 1]);
                    if (pDataValley[j] - dbCenterVal1 > pDataValley[j + 1] - dbCenterVal2)
                    {
                        double dbTemp = pDataValley[j];
                        pDataValley[j] = pDataValley[j + 1];
                        pDataValley[j + 1] = dbTemp;
                        //位置也对应
                        int nPos = pValleyIndex[j];
                        pValleyIndex[j] = pValleyIndex[j + 1];
                        pValleyIndex[j + 1] = nPos;

                    }
                }
            }


            ValleyCount = nFindValleyCount > nCalcCount ? nCalcCount : nFindValleyCount;
            for (int nj = 0; nj < ValleyCount; nj++)
            {
                ValleyOut[nj] = pDataValley[nj];
                ValleyIndex[nj] = pValleyIndex[nj];
            }
        }

    }

    public struct  LinePeakValleyPair
    {
        public double DataValue;
        public double DataPos;
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

        public bool bDrawPeakValley = false;

        public LinePeakValleyPair[] m_PairPeakData = null;
        public LinePeakValleyPair[] m_PairValleyData = null;
        public double[] m_MedianCoef = null;
    }
}
