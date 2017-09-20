using CsvHelper;
using GraphComponent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AcqDataShow
{
    public partial class AcqDataForm : Form
    {
        private SawMarkStation SawMark = new SawMarkStation();
        private string m_BatchProcessPath = "";
        private string m_ProbeMonitorPath = "";
        private Hashtable testDataFiles;
        private double[,] currentTestData = null;
        private double[][] ProbeGraphData = null;
        private int ProbeDataLen;
        private int ProbeNum = 1;
        private int nGraphType = 0;

        private struct gaussThreadParam
        {
            public ManualResetEvent wait_handle;
            public bool bCalcRoughness;
            public double[] dblDataIn;
            public double[] dblDataOut;
        }

        private double[][] probeDataFiltered = null;
        private int nDataLength = 0;

        public AcqDataForm()
        {
            InitializeComponent();
            testDataFiles = new Hashtable();
            textProbe.Text = ProbeNum.ToString();
            LineType.Checked = true;
        }

        private void OpenFileMenuItem_Click(object sender, EventArgs e)
        {
            SawMark.Init(Application.StartupPath + @"\AlgoConfigFile.xml");
            SetParameter(SawMark);
            OpenFileDialog openFileDlg = new OpenFileDialog();
            openFileDlg.Filter = "CSV(*.csv)|*.csv";
            //openFileDlg.InitialDirectory = Application.StartupPath;
            DialogResult result = openFileDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                CsvReader csvRead = new CsvReader(openFileDlg.FileName, Encoding.Default);
                DataTable table = csvRead.ReadIntoDataTable();
                currentTestData = new double[table.Columns.Count, table.Rows.Count];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    for (int j = 0; j < table.Rows.Count; j++)
                    {
                        currentTestData[i, j] = Convert.ToDouble(table.Rows[j].ItemArray[i].ToString());
                    }
                }
                this.Text = "数据显示" + openFileDlg.FileName.Substring(openFileDlg.FileName.LastIndexOf("\\")+1);
                CalculationAcqData(currentTestData);
            }

        }

        private void SetParameter(SawMarkStation calc)
        {
            Parameter par = new Parameter();
            par.Station = StationType.SawMark;
            par.ClassifyPar = new ClassifyParameter[12];

            // SawMarkDepthMin
            par.ClassifyPar[0] = new ClassifyParameter();
            par.ClassifyPar[0].calType = CalType.SawMarkSM;
            par.ClassifyPar[0].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[0].parDetail[ni].parType = ParType.SawMarkDepthMin;
                par.ClassifyPar[0].parDetail[ni].parValue = "-9999";
            }
            par.ClassifyPar[0].parDetail[0].parValue = "15";
            par.ClassifyPar[0].parDetail[1].parValue = "15";
            par.ClassifyPar[0].parDetail[2].parValue = "30";

            //SawMarkDepthMax
            par.ClassifyPar[1] = new ClassifyParameter();
            par.ClassifyPar[1].calType = CalType.SawMarkSM;
            par.ClassifyPar[1].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[1].parDetail[ni].parType = ParType.SawMarkDepthMax;
                par.ClassifyPar[1].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[1].parDetail[0].parValue = "9999";
            par.ClassifyPar[1].parDetail[1].parValue = "30";
            par.ClassifyPar[1].parDetail[2].parValue = "50";

            //SawMarkNumMax
            par.ClassifyPar[2] = new ClassifyParameter();
            par.ClassifyPar[2].calType = CalType.SawMarkSM;
            par.ClassifyPar[2].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[2].parDetail[ni].parType = ParType.SawMarkNumMax;
                par.ClassifyPar[2].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[2].parDetail[0].parValue = "0";
            par.ClassifyPar[2].parDetail[1].parValue = "9999";
            par.ClassifyPar[2].parDetail[2].parValue = "9999";

            // smallSawMarkDepthMin
            par.ClassifyPar[3] = new ClassifyParameter();
            par.ClassifyPar[3].calType = CalType.SawMarkSM;
            par.ClassifyPar[3].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[3].parDetail[ni].parType = ParType.SmallSawMarkDepthMin;
                par.ClassifyPar[3].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[3].parDetail[0].parValue = "10";

            //SmallSawMarkDepthMax
            par.ClassifyPar[4] = new ClassifyParameter();
            par.ClassifyPar[4].calType = CalType.SawMarkSM;
            par.ClassifyPar[4].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[4].parDetail[ni].parType = ParType.SmallSawMarkDepthMax;
                par.ClassifyPar[4].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[4].parDetail[0].parValue = "15";

            //SmallSawMarkNumMax
            par.ClassifyPar[5] = new ClassifyParameter();
            par.ClassifyPar[5].calType = CalType.SawMarkSM;
            par.ClassifyPar[5].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[5].parDetail[ni].parType = ParType.SmallSawMarkNumMax;
                par.ClassifyPar[5].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[5].parDetail[0].parValue = "9999";

            // ThicknessMin
            par.ClassifyPar[6] = new ClassifyParameter();
            par.ClassifyPar[6].calType = CalType.ThicknessSM;
            par.ClassifyPar[6].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[6].parDetail[ni].parType = ParType.ThicknessMin;
                par.ClassifyPar[6].parDetail[ni].parValue = "-9999";
            }
            par.ClassifyPar[6].parDetail[0].parValue = "180";

            //ThicknessMax
            par.ClassifyPar[7] = new ClassifyParameter();
            par.ClassifyPar[7].calType = CalType.ThicknessSM;
            par.ClassifyPar[7].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[7].parDetail[ni].parType = ParType.ThicknessMax;
                par.ClassifyPar[7].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[7].parDetail[0].parValue = "280";

            // TTVMin
            par.ClassifyPar[8] = new ClassifyParameter();
            par.ClassifyPar[8].calType = CalType.TTVSM;
            par.ClassifyPar[8].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[8].parDetail[ni].parType = ParType.TTVMin;
                par.ClassifyPar[8].parDetail[ni].parValue = "-9999";
            }
            par.ClassifyPar[8].parDetail[0].parValue = "0";

            //TTVMax
            par.ClassifyPar[9] = new ClassifyParameter();
            par.ClassifyPar[9].calType = CalType.TTVSM;
            par.ClassifyPar[9].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[9].parDetail[ni].parType = ParType.TTVMax;
                par.ClassifyPar[9].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[9].parDetail[0].parValue = "30";

            // WarpMin
            par.ClassifyPar[10] = new ClassifyParameter();
            par.ClassifyPar[10].calType = CalType.WarpSM;
            par.ClassifyPar[10].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[10].parDetail[ni].parType = ParType.WarpMin;
                par.ClassifyPar[10].parDetail[ni].parValue = "-9999";
            }
            par.ClassifyPar[10].parDetail[0].parValue = "0";

            //WarpMax
            par.ClassifyPar[11] = new ClassifyParameter();
            par.ClassifyPar[11].calType = CalType.WarpSM;
            par.ClassifyPar[11].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[11].parDetail[ni].parType = ParType.WarpMax;
                par.ClassifyPar[11].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[11].parDetail[0].parValue = "30";


            par.CommonPar = new CellParameter[4];
            par.CommonPar[0] = new CellParameter();
            par.CommonPar[0].parType = ParType.Size;
            par.CommonPar[0].parValue = "156";

            par.CommonPar[1] = new CellParameter();
            par.CommonPar[1].parType = ParType.LineSpeed;
            par.CommonPar[1].parValue = "300";

            par.CommonPar[2] = new CellParameter();
            par.CommonPar[2].parType = ParType.CutLength;
            par.CommonPar[2].parValue = "40";

            par.CommonPar[3] = new CellParameter();
            par.CommonPar[3].parType = ParType.CutWidth;
            par.CommonPar[3].parValue = "30";

            if (!calc.SetParameter(par))
            {
                MessageBox.Show("参数设置失败！");
            }
        }        


        private static bool GaussFilter(double[] dataIn, int dataLen, double lambda, double dx, ref double[] dataOut)
        {
            try
            {
                if ((null == dataIn) || (null == dataOut) || (dataLen <= 0) || (lambda <= 0) || (dx <= 0))
                {
                    return false;
                }

                //****************** 高斯滤波 V1.1 *******************//

                double alpha = 0.4697;
                double coef = 1.0 / (alpha * lambda);
                int lenx = Convert.ToInt32(2 * lambda / dx + 1);
                double[] h = new double[lenx];
                double hsum = 0;
                for (int ni = 0; ni < lenx; ni++)
                {
                    double x = -lambda + dx * ni;
                    h[ni] = coef * Math.Exp(-Math.PI * (x * coef) * (x * coef));
                    hsum += h[ni];
                }

                int k=dataLen + lenx - 1;
                double[] y = new double[k];               

                int i, j;
                for (i = 0; i < k; i++)
                {
                    for (j = Math.Max(0, i + 1 - dataLen); j <= Math.Min(i, lenx - 1); j++)
                    {
                        y[i] += h[j] * dataIn[i - j];
                    }
                }
//                 if (dataLen >= lenx)
//                 {
//                     for (int i = 0; i < lenx; i++)
//                     {
//                         for (int j = 0; j <= i; j++)
//                         {
//                             y[i] = y[i] + dataIn[i - j] * h[j];
//                         }
//                     }
// 
//                     for (int i = lenx; i < dataLen; i++)
//                     {
//                         for (int j = 0; j < lenx; j++)
//                         {
//                             y[i] = y[i] + dataIn[i - j] * h[j];
//                         }
//                     }
// 
//                     for (int i = dataLen; i < dataLen + lenx - 1; i++)
//                     {
//                         for (int j = i - dataLen + 1; j < lenx; j++)
//                         {
//                             y[i] = y[i] + dataIn[i - j] * h[j];
//                         }
//                     }
//                 }
//                 else
//                 {
//                     for (int i = 0; i < dataLen; i++)
//                     {
//                         for (int j = 0; j <= i; j++)
//                         {
//                             y[i] = y[i] + h[i - j] * dataIn[j];
//                         }
//                     }
// 
//                     for (int i = dataLen; i < lenx; i++)
//                     {
//                         for (int j = 0; j < dataLen; j++)
//                         {
//                             y[i] = y[i] + h[i - j] * dataIn[j];
//                         }
//                     }
// 
//                     for (int i = lenx; i < dataLen + lenx - 1; i++)
//                     {
//                         for (int j = i - lenx + 1; j < dataLen; j++)
//                         {
//                             y[i] = y[i] + h[i - j] * dataIn[j];
//                         }
//                     }
//                 }

                int halfLen = Convert.ToInt32(Math.Floor(0.5 * lenx));
                for (int ni = 0; ni < dataLen; ni++)
                {
                    dataOut[ni] = y[ni + halfLen] / hsum;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ResamplingData(ref double[] dbData, ref int nLen, ref double dx)
        {
            const int nRatio = 4;
            int nReSampleLen = nLen / nRatio;
            double dbResamplDx = dx * nRatio;
            double[] dbReSamplingData = new double[nReSampleLen];
            for(int i=0; i< nReSampleLen; i++)
            {
                double dbSum = 0;
                bool bValid = true;
                for (int j = 0; j < nRatio; j++)
                {
                    if (dbData[i * nRatio + j] >= -1.0 && dbData[i * nRatio + j] <= 1.0)
                    {
                        dbSum += dbData[i * nRatio + j];
                    }
                    else
                    {
                        bValid = false;
                    }
                }
                if (bValid)
                    dbReSamplingData[i] = dbSum / nRatio;
                else
                    dbReSamplingData[i] = 100;
            }
            dbData = dbReSamplingData;
            nLen = nReSampleLen;
            dx = dbResamplDx;
        }

        private static void GaussFilterFunc(object obj)
        {
            gaussThreadParam param = (gaussThreadParam)obj;

            int probeLen = param.dblDataIn.Length;
            double dx = 300.0 / (150000 * 1.0);
            double lambdaS = 0.008;
            double lambdaC1 = 2.5;
            double[] probeDataInGauss = new double[probeLen];

            double[] xData = new double[probeLen];
            for (int inA = 0; inA < probeLen; inA++)
                xData[inA] = inA * dx;

            System.DateTime GaussTimeTick = System.DateTime.Now;
           AcqDataForm.GaussFilter(param.dblDataIn, probeLen, lambdaS, dx, ref probeDataInGauss);
           TimeSpan GaussTime = System.DateTime.Now.Subtract(GaussTimeTick);
           System.Diagnostics.Trace.WriteLine("Gauss滤波lambdaS耗时：" + GaussTime.TotalMilliseconds.ToString() + "毫秒");

            if (param.bCalcRoughness)
            {
                double[] probeDataFilt = new double[probeLen];
                System.DateTime GaussTick = System.DateTime.Now;
                AcqDataForm.GaussFilter(param.dblDataIn, probeLen, lambdaC1, dx, ref probeDataFilt);
                TimeSpan GaussTimeC = System.DateTime.Now.Subtract(GaussTick);
                System.Diagnostics.Trace.WriteLine("Gauss滤波lambdaC耗时：" + GaussTimeC.TotalMilliseconds.ToString() + "毫秒");

                for (int inA = 0; inA < probeLen; inA++)
                {
                    param.dblDataOut[inA] = (probeDataInGauss[inA] - probeDataFilt[inA]) * 1000.0;

                }
            }
            else
            {
                for (int inA = 0; inA < probeLen; inA++)
                {
                    param.dblDataOut[inA] = probeDataInGauss[inA] * 1000.0;
                }
            }

            param.wait_handle.Set();
        }

        private void Calc(object obj)
        {
            double[,] probeDataIn = (double[,])obj;

            System.DateTime startTime = System.DateTime.Now;

            gaussThreadParam[] param = new gaussThreadParam[6];
            ManualResetEvent[] hWaitGauss = new ManualResetEvent[6];
            for (int i = 0; i < 6; i++)
            {
                System.DateTime pareDataTime = System.DateTime.Now;
                param[i] = new gaussThreadParam();
                param[i].wait_handle = new ManualResetEvent(false);
                hWaitGauss[i] = param[i].wait_handle;
                param[i].bCalcRoughness = LineType.Checked;

                int nLen = probeDataIn.GetLength(1);
                int probeStart = 0;
                for (int nj = 0; nj < nLen; nj++)
                {
                    if ((probeDataIn[i, nj] >= -3.0) && (probeDataIn[i, nj] <= 3.0))
                    {
                        probeStart = nj;
                        break;
                    }
                }

                int probeEnd = nLen - 1;
                for (int nj = nLen - 1; nj >= 0; nj--)
                {
                    if ((probeDataIn[i, nj] >= -3.0) && (probeDataIn[i, nj] <= 3.0))
                    {
                        probeEnd = nj;
                        break;
                    }
                }

                int probeLen = probeEnd - probeStart + 1;
                int nOffset = probeStart;
                param[i].dblDataIn = new double[probeLen];
                param[i].dblDataOut = new double[probeLen];
                for (int inA = 0; inA < probeLen; inA++)
                {
                    param[i].dblDataIn[inA] = probeDataIn[i, nOffset + inA];
                }
                TimeSpan parepareTime = System.DateTime.Now.Subtract(pareDataTime);
                System.Diagnostics.Trace.WriteLine("数据准备耗时：" + parepareTime.TotalMilliseconds.ToString() + "毫秒");
 
                Thread hThread = new Thread(GaussFilterFunc);
                hThread.Start(param[i]);
            }

            if (!WaitHandle.WaitAll(hWaitGauss, 6000))
            {
                return;
            }
            for (int i = 0; i < 6; i++)
                hWaitGauss[i].Reset();

            probeDataFiltered = new double[6][];
            nDataLength = 0;
            for (int i = 0; i < 6; i++)
            {
                int nDataLen = param[i].dblDataOut.Length;
                if (nDataLength < nDataLen)
                    nDataLength = nDataLen;
            }
            for (int i = 0; i < 6; i++)
            {
                probeDataFiltered[i] = new double[nDataLength];
                Array.Copy(param[i].dblDataOut, probeDataFiltered[i], param[i].dblDataOut.Length);
            }

            TimeSpan fCal = System.DateTime.Now.Subtract(startTime);
            System.Diagnostics.Trace.WriteLine("C# 端多线程 gaussian filter耗时：" + fCal.TotalMilliseconds.ToString() + "毫秒");
        }

        private void CalculationAcqData(double[,] probeDataIn)
        {
            Thread hThread = new Thread(Calc);
            hThread.Start(probeDataIn);
            hThread.Join();

            DateTime CalTm = System.DateTime.Now;
            GropResults result = null;
            object dataShowObj = null;
            SawMark.CalProcess(probeDataIn, out result, out dataShowObj);
            TimeSpan tsCal = System.DateTime.Now.Subtract(CalTm);
            System.Diagnostics.Trace.WriteLine("SawMark.CalProcess耗时：" + tsCal.TotalMilliseconds.ToString() + "毫秒");


            LinePeakValleyPair[] peakDatagroup1 = null;
            LinePeakValleyPair[] valleyDatagroup1 = null;
            LinePeakValleyPair[] peakDatagroup2 = null;
            LinePeakValleyPair[] valleyDatagroup2 = null;
            if (result != null)
            {
                SawMarkDataShow dataShow = (SawMarkDataShow)dataShowObj;
                string dataOut;
                string classifyResult = "";
                int num1 = result.ClassifyResult.GetLength(0);
                int num2 = result.ClassifyResult.GetLength(1);
                PropertyGridData outputGrid = new PropertyGridData();
                outputGrid.QLevelSawMark = new bool[num2];
                outputGrid.QLevelThickness = new bool[num2];
                outputGrid.QLevelTTV = new bool[num2];
                outputGrid.QLevelWarp = new bool[num2];


                peakDatagroup1 = new LinePeakValleyPair[3];
                valleyDatagroup1 = new LinePeakValleyPair[3];
                peakDatagroup2 = new LinePeakValleyPair[3];
                valleyDatagroup2 = new LinePeakValleyPair[3];
                ///
                for (int ni = 0; ni < num1; ni++)
                {
                    for (int nj = 0; nj < num2; nj++)
                    {
                        classifyResult += result.ClassifyResult[ni, nj].ToString();
                        if (ni == 0)
                            outputGrid.QLevelSawMark[nj] = result.ClassifyResult[ni, nj];
                        else if (ni == 1)
                            outputGrid.QLevelThickness[nj] = result.ClassifyResult[ni, nj];
                        else if (ni == 2)
                            outputGrid.QLevelTTV[nj] = result.ClassifyResult[ni, nj];
                        else if (ni == 3)
                            outputGrid.QLevelWarp[nj] = result.ClassifyResult[ni, nj];
                    }
                    classifyResult += "\r\n";
                }
                string calresult = "";
                outputGrid.TypeName = "线痕";
                outputGrid.DataName = new string[43];//线痕共43组数据，统计数据7 +（6个探头*每个探头6中数据）
                outputGrid.DataValue = new double[43];
                int outputGridindex = 0;
                foreach (CalResult calResult in result.DataResults)
                {
                    calresult += "\r\n检测项：" + calResult.calType.ToString() + "\r\n";
                    int nIndex = 0;
                    foreach (ResultType resultcal in calResult.result)
                    {
                        calresult += resultcal.remark + ":" + resultcal.value.ToString() + "\r\n";
                        if (calResult.calType == CalType.SawMarkSM)
                        {
                            outputGrid.DataName[outputGridindex] = resultcal.remark;
                            outputGrid.DataValue[outputGridindex] = resultcal.value;

                            GetPeakValleyPair(ref peakDatagroup1, ref valleyDatagroup1, ref peakDatagroup2, ref valleyDatagroup2, resultcal);
                            outputGridindex++;
                        }
                        nIndex++;
                    }

                    foreach (ResultType processcal in calResult.processData)
                    {
                        calresult += processcal.remark + ":" + processcal.value.ToString() + "\r\n";
                    }
                }
                outputGrid.StationName = result.StationName.ToString();
                dataOut = "StationType:" + result.StationName.ToString() + "\r\n"
                    + "ValidType:" + result.ValidResult.ToString() + "\r\n"
                    + "ClassifyResult:" + classifyResult + calresult;


                System.Diagnostics.Trace.WriteLine(dataOut);

                resultGrid.SelectedObject = outputGrid;
                //             int nCount = dataShow.ThicknessOfProbePair1.Length;
                //             if (dataShow.ThicknessOfProbePair2.Length < nCount)
                //                 nCount = dataShow.ThicknessOfProbePair2.Length;
                //             if (dataShow.ThicknessOfProbePair3.Length < nCount)
                //                 nCount = dataShow.ThicknessOfProbePair3.Length;
                //             DataGraph.SetYValueAxis(150.0, 250,0, true);
                //             DataGraph.setGroupDataCount(1);
                //             DataGraph.SetLineData(0,dataShow.ThicknessOfProbePair1, dataShow.ThicknessOfProbePair2, dataShow.ThicknessOfProbePair3, nCount);
            }

            nGraphType = 0;
            int nGroupCount = 2;
            DataGraph.SetYValueAxis(-100.0, 300.0, true);
            DataGraph.setGroupDataCount(nGroupCount);

            DataGraph.SetProcessedDataUp(probeDataFiltered[0], probeDataFiltered[2], probeDataFiltered[4], nDataLength);
            DataGraph.SetProcessedDataDown(probeDataFiltered[1], probeDataFiltered[3], probeDataFiltered[5], nDataLength);

            if (result != null)
            {
                DataGraph.setGroupPeakValley(0, peakDatagroup1, valleyDatagroup1);
                DataGraph.setGroupPeakValley(1, peakDatagroup2, valleyDatagroup2);
            }
            DataGraph.Invalidate();
        }

        private void CalcPeakValleyMenuItem_Click(object sender, EventArgs e)
        {
            if (!DataGraph.DrawDropGraph)
                return;
            DataGraph.CalcPeakValley();
        }

        private void GetPeakValleyPair(ref LinePeakValleyPair[] peakData1, ref LinePeakValleyPair[] valleyData1, ref LinePeakValleyPair[] peakData2, ref LinePeakValleyPair[] valleyData2, ResultType resultcal)
        {
            //数组大小为3
            switch (resultcal.name)
            {
                case "SawMarkDepthMaxPeakPosForProbe1":
                    peakData1[0].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxPeakPosForProbe2":
                    peakData2[0].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxPeakPosForProbe3":
                    peakData1[1].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxPeakPosForProbe4":
                    peakData2[1].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxPeakPosForProbe5":
                    peakData1[2].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxPeakPosForProbe6":
                    peakData2[2].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxValleryPosForProbe1":
                    valleyData1[0].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxValleryPosForProbe2":
                    valleyData2[0].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxValleryPosForProbe3":
                    valleyData1[1].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxValleryPosForProbe4":
                    valleyData2[1].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxValleryPosForProbe5":
                    valleyData1[2].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxValleryPosForProbe6":
                    valleyData2[2].DataPos = resultcal.value;
                    break;
                case "SawMarkDepthMaxPeakValForProbe1":
                    peakData1[0].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxPeakValForProbe2":
                    peakData2[0].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxPeakValForProbe3":
                    peakData1[1].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxPeakValForProbe4":
                    peakData2[1].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxPeakValForProbe5":
                    peakData1[2].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxPeakValForProbe6":
                    peakData2[2].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxValleryValForProbe1":
                    valleyData1[0].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxValleryValForProbe2":
                    valleyData2[0].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxValleryValForProbe3":
                    valleyData1[1].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxValleryValForProbe4":
                    valleyData2[1].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxValleryValForProbe5":
                    valleyData1[2].DataValue = resultcal.value * 1000.0;
                    break;
                case "SawMarkDepthMaxValleryValForProbe6":
                    valleyData2[2].DataValue = resultcal.value * 1000.0;
                    break;
            }
        }

        private void btnBatchTest_Click(object sender, EventArgs e)
        {
            SawMark.Init(Application.StartupPath + @"\AlgoConfigFile.xml");
            SetParameter(SawMark);

            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = false;
            if (m_BatchProcessPath != "")
                folderDlg.SelectedPath = m_BatchProcessPath;
            else
                folderDlg.RootFolder = Environment.SpecialFolder.MyComputer;


            if (folderDlg.ShowDialog() == DialogResult.OK)
            {
                m_BatchProcessPath = folderDlg.SelectedPath;
                backgroundBatchCalc.RunWorkerAsync();
            }

        }

        private void backgroundBatchCalc_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((BatchProgressBar != null) && BatchProgressBar.IsHandleCreated)
            {
                BatchProgressBar.BeginInvoke(new MethodInvoker(() => BatchProgressBar.Value = BatchProgressBar.Maximum));
            }
            BeginInvoke(new MethodInvoker(delegate { Cursor = Cursors.Default; }));
            MessageBox.Show("批量处理完成。");
            BatchProgressBar.BeginInvoke(new MethodInvoker(() => BatchProgressBar.Value = BatchProgressBar.Minimum));

        }

        private void backgroundBatchCalc_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if ((BatchProgressBar != null) && BatchProgressBar.IsHandleCreated)
            {
                BatchProgressBar.BeginInvoke(new MethodInvoker(delegate { BatchProgressBar.Value = e.ProgressPercentage; }));
            }
        }


        private List<FileInfo> ListFiles(FileSystemInfo info, string extend, string excludeFile, bool cancelFlag)
        {
            List<FileInfo> batchFileList = new List<FileInfo>();
            if (info.Exists)
            {
                DirectoryInfo info2 = info as DirectoryInfo;
                if (info2 != null)
                {
                    FileSystemInfo[] fileSystemInfos = info2.GetFileSystemInfos();
                    for (int i = 0; i < fileSystemInfos.Length; i++)
                    {
                        if (cancelFlag)
                        {
                            break;
                        }
                        FileInfo item = fileSystemInfos[i] as FileInfo;
                        if (((item != null) && item.Extension.Equals(extend)) && !item.Name.Equals(excludeFile))
                        {
                            if (item.FullName.Contains("Original"))
                                batchFileList.Add(item);
                        }
                        else
                        {
                            batchFileList.AddRange(ListFiles(fileSystemInfos[i], extend, excludeFile, cancelFlag));
                        }
                    }
                }
            }
            return batchFileList;
        }

        private bool GetProbeData(string fileFullPath, int nProbeNum, out double[] probeData)
        {
            probeData = null;
            FileInfo info = new FileInfo(fileFullPath);
            string name = info.Name;
            if (testDataFiles.ContainsKey(name))
            {
                return false;
            }
            try
            {
                DataTable table = new CsvReader(fileFullPath, Encoding.Default).ReadIntoDataTable();
                double[] numArray = new double[table.Rows.Count];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    if (i + 1 == nProbeNum)
                    {
                        for (int j = 0; j < table.Rows.Count; j++)
                        {
                            numArray[j] = Convert.ToDouble(table.Rows[j].ItemArray[i].ToString());
                        }
                    }
                }
                probeData = numArray;
                return true;
            }
            catch (Exception exception)
            {
                MessageBox.Show("加载失败待测文件失败！" + exception.Message);
                return false;
            }
        }

        private void backgroundBatchCalc_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                this.BeginInvoke(new MethodInvoker(delegate { Cursor = Cursors.WaitCursor; }));
                List<FileInfo> batchFileList = new List<FileInfo>();
                testDataFiles.Clear();
                string path = Path.Combine(m_BatchProcessPath, "Result");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                DataTable table = new DataTable("SawMarkTest");
                table.Columns.Add("FileName");
                table.Columns.Add("SawMarkType");
                table.Columns.Add("MaxSawMarkDepth");
                table.Columns.Add("MaxProbeNum");
                table.Columns.Add("PeakPos");
                table.Columns.Add("ValleyPos");
                table.Columns.Add("Thickness");
                table.Columns.Add("TTV");
                table.Columns.Add("PeakValue");
                table.Columns.Add("ValleyValue");
                if (backgroundBatchCalc.CancellationPending)
                {
                    e.Cancel = true;
                }

                Thread.Sleep(500);
                backgroundBatchCalc.ReportProgress(10);
                batchFileList = ListFiles(new DirectoryInfo(m_BatchProcessPath), ".csv", "Result.csv", backgroundBatchCalc.CancellationPending);
                backgroundBatchCalc.ReportProgress(20);
                Thread.Sleep(500);

                for (int i = 0; i < batchFileList.Count; i++)
                {
                    DataRow dr = table.NewRow();
                    if (backgroundBatchCalc.CancellationPending)
                    {
                        return;
                    }
                    dr[0] = batchFileList[i].Name;
                    currentTestData = GetTestData(batchFileList[i].FullName);
                    GropResults result = RunCalCulate(currentTestData);
                    GetTestResult(dr, result);
                    table.Rows.Add(dr);
                    backgroundBatchCalc.ReportProgress(20 + Convert.ToInt32(80.0 * (i + 1) / batchFileList.Count));
                    Thread.Sleep(50);
                }
                DataTableToCsv(table, Path.Combine(path, "Result.csv"));
            } 
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void GetTestResult(DataRow dr, GropResults results)
        {
            if (results == null)
                return;
            foreach (CalResult result in results.DataResults)
            {
                foreach (ResultType type3 in result.result)
                {
                    if ((dr != null) && (dr.ItemArray.Length >= 3))
                    {
                        if (type3.name == "SawMarkType")
                        {
                            dr[1] = type3.value;
                        }
                        else if (type3.name == "SawMarkDepthMax")
                        {
                            dr[2] = type3.value;
                        }
                        else if (type3.name == "SawMarkDepthMaxProbeIndex")
                        {
                            dr[3] = type3.value;
                        }
                        else if (type3.name == "SawMarkDepthMaxPeakPos")
                        {
                            dr[4] = type3.value;
                        }
                        else if (type3.name == "SawMarkDepthMaxValleryPos")
                        {
                            dr[5] = type3.value;
                        }
                        else if(type3.name == "AverageThickness")
                        {
                            dr[6] = type3.value;
                        }
                        else if (type3.name == "TTV")
                        {
                            dr[7] = type3.value;
                        }
                        else if (type3.name == "SawMarkDepthMaxPeakVal")
                        {
                            dr[8] = type3.value;
                        }
                        else if (type3.name == "SawMarkDepthMaxValleryVal")
                        {
                            dr[9] = type3.value;
                        }
                    }
                }
            }
        }

        private GropResults RunCalCulate(double[,] probeDataIn)
        {
            GropResults result;
            object dataShowObj;
            SawMark.CalProcess(probeDataIn, out result, out dataShowObj);

            SawMarkDataShow dataShow = (SawMarkDataShow)dataShowObj;
            string dataOut;
            string classifyResult = "";
            int num1 = result.ClassifyResult.GetLength(0);
            int num2 = result.ClassifyResult.GetLength(1);

            ///
            for (int ni = 0; ni < num1; ni++)
            {
                for (int nj = 0; nj < num2; nj++)
                {
                    classifyResult += result.ClassifyResult[ni, nj].ToString();
                }
                classifyResult += "\r\n";
            }
            string calresult = "";
            foreach (CalResult calResult in result.DataResults)
            {
                calresult += "\r\n检测项：" + calResult.calType.ToString() + "\r\n";
                int nIndex = 0;
                foreach (ResultType resultcal in calResult.result)
                {
                    calresult += resultcal.remark + ":" + resultcal.value.ToString() + "\r\n";
                    nIndex++;
                }

                foreach (ResultType processcal in calResult.processData)
                {
                    calresult += processcal.remark + ":" + processcal.value.ToString() + "\r\n";
                }
            }
            dataOut = "StationType:" + result.StationName.ToString() + "\r\n"
                + "ValidType:" + result.ValidResult.ToString() + "\r\n"
                + "ClassifyResult:" + classifyResult + calresult;


            System.Diagnostics.Trace.WriteLine(dataOut);

            return result;
        }

        private double[,] GetTestData(string fileName)
        {
            if (!File.Exists(fileName))
                return null;
            DataTable table = new CsvReader(fileName, Encoding.Default).ReadIntoDataTable();
            double[,] numArray = new double[table.Columns.Count, table.Rows.Count];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                for (int j = 0; j < table.Rows.Count; j++)
                {
                    numArray[i, j] = Convert.ToDouble(table.Rows[j].ItemArray[i].ToString());
                }
            }
            return numArray;
        }

        public void DataTableToCsv(DataTable dt, string filePath)
        {
            if(File.Exists(filePath))
            {
                filePath = filePath.Remove(filePath.Length - 4, 4) + DateTime.Now.ToString("HH-mm-ss") + ".csv";
            }
            StringBuilder builder = new StringBuilder();
            IEnumerable<string> values = from column in dt.Columns.Cast<DataColumn>() select column.ColumnName;
            builder.AppendLine(string.Join(",", values));
            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> enumerable2 = from field in row.ItemArray select "\"" + field.ToString().Replace("\"", "\"\"") + "\"";
                builder.AppendLine(string.Join(",", enumerable2));
            }
            File.WriteAllText(filePath, builder.ToString());
        }

        private void btnMonitor_Click(object sender, EventArgs e)
        {
            int nNum = Convert.ToInt32(textProbe.Text);

            if(nNum < 1 || nNum > 6)
            {
                MessageBox.Show("探头号只能1-6", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ProbeNum = nNum;
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowNewFolderButton = false;
            if (m_BatchProcessPath != "")
                folderDlg.SelectedPath = m_ProbeMonitorPath;
            else
                folderDlg.RootFolder = Environment.SpecialFolder.MyComputer;


            if (folderDlg.ShowDialog() == DialogResult.OK)
            {
                m_ProbeMonitorPath = folderDlg.SelectedPath;
                ProbeMonitorWorker.RunWorkerAsync();               
            }
        }
        private void ProbeMonitorWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((BatchProgressBar != null) && BatchProgressBar.IsHandleCreated)
            {
                BatchProgressBar.BeginInvoke(new MethodInvoker(() => BatchProgressBar.Value = BatchProgressBar.Maximum));
            }
            BatchProgressBar.BeginInvoke(new MethodInvoker(() => BatchProgressBar.Value = BatchProgressBar.Minimum));
        }

        private void ProbeMonitorWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if ((BatchProgressBar != null) && BatchProgressBar.IsHandleCreated)
            {
                BatchProgressBar.BeginInvoke(new MethodInvoker(delegate { BatchProgressBar.Value = e.ProgressPercentage; }));
            }
        }

        private void ProbeMonitorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                List<FileInfo> batchFileList = new List<FileInfo>();
                testDataFiles.Clear();
                string path = Path.Combine(m_ProbeMonitorPath, "Result");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (ProbeMonitorWorker.CancellationPending)
                {
                    e.Cancel = true;
                }

                Thread.Sleep(500);
                backgroundBatchCalc.ReportProgress(10);
                batchFileList = ListFiles(new DirectoryInfo(m_ProbeMonitorPath), ".csv", "Result.csv", ProbeMonitorWorker.CancellationPending);
                backgroundBatchCalc.ReportProgress(20);
                Thread.Sleep(500);
                int num = 0;
                double[][] ProbeDataAll = new double[6][];
                foreach (FileInfo info in batchFileList)
                {
                    if (ProbeMonitorWorker.CancellationPending)
                    {
                        return;
                    }
                    if (num >= 6)
                        break;
                    double[] dbProbeData = null;
                    if(GetProbeData(info.FullName, ProbeNum, out dbProbeData))
                    {
                        int nDataLen = dbProbeData.Length;
                        ProbeDataAll[num] = new double[nDataLen];
                        Array.Copy(dbProbeData, ProbeDataAll[num], nDataLen);
                    }
                    num++;
                    ProbeMonitorWorker.ReportProgress(20 + Convert.ToInt32((30.0 * num) / 6));
                }
                ProbeMonitorWorker.ReportProgress(50);
                Thread.Sleep(500);
                if (num == 6)
                {
                    ConvertToDataGraph(ProbeDataAll);
                    BeginInvoke(new MethodInvoker(delegate { LineGraphUpdate(); }));
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
        }

        private void LineGraphUpdate()
        {
            int nGroupCount = 2;
            DataGraph.SetYValueAxis(-100.0, 100.0, true);
            DataGraph.setGroupDataCount(nGroupCount);
            DataGraph.SetProcessedDataUp(ProbeGraphData[0], ProbeGraphData[2], ProbeGraphData[4], ProbeDataLen);
            DataGraph.SetProcessedDataDown(ProbeGraphData[1], ProbeGraphData[3], ProbeGraphData[5], ProbeDataLen);
            DataGraph.Invalidate();
        }

        private void ConvertToDataGraph(double[][] probeData)
        {
            //数组重新排布
            ProbeGraphData = new double[6][];
            int nDataLength = 65536;
            double lambdaC1 = 2.5;
            for (int a = 0; a < 6; a++)
            {
                int nLen = probeData[a].Length;
                int probeStart = 0;
                for (int nj = 0; nj < nLen; nj++)
                {
                    if ((probeData[a][nj] >= -3.0) && (probeData[a][nj] <= 3.0))
                    {
                        probeStart = nj;
                        break;
                    }
                }

                int probeEnd = nLen - 1;
                for (int nj = nLen - 1; nj >= 0; nj--)
                {
                    if ((probeData[a][nj] >= -3.0) && (probeData[a][nj] <= 3.0))
                    {
                        probeEnd = nj;
                        break;
                    }
                }

                int probeLen = probeEnd - probeStart + 1;
                if (probeLen > nDataLength)
                    probeStart += (probeLen - nDataLength) / 2;
               int nOffset = probeStart;
               ProbeGraphData[a] = new double[nDataLength];

                double dx = 300.0 / (150000 * 1.0);
                double[] dbGaussData = new double[nDataLength];
                if (probeLen >= nDataLength)
                    Array.Copy(probeData[a], probeStart, dbGaussData, 0, nDataLength);
                else
                {
                    Array.Copy(probeData[a], probeStart, dbGaussData, 0, probeLen);
                    for(int i=nDataLength; i>probeLen; i--)
                    {
                        dbGaussData[i - 1] = 0;
                    }
                }

                GaussFilter(dbGaussData, nDataLength, lambdaC1, dx, ref ProbeGraphData[a]);
                for (int i = 0; i < nDataLength; i++)
                {
                    if (a % 2 != 0)
                        ProbeGraphData[a][i] *= -1000.0;
                    else
                        ProbeGraphData[a][i] *= 1000.0;
                }
                ProbeMonitorWorker.ReportProgress(50 + Convert.ToInt32(50.0 * (a + 1) / 6));
            }
            ProbeDataLen = nDataLength;

        }
        public enum fftw_flags : uint
        {
            Measure = 0,
            DestroyInput = 1,
            Unaligned = 2,
            ConserveMemory = 4,
            Exhaustive = 8,
            PreserveInput = 16,
            Patient = 32,
            Estimate = 64
        }

       public enum fftw_direction : int
        {
             Forward = -1,
              Backward = 1
        }

        [DllImport("libfftw3-3.dll",
     EntryPoint = "fftw_malloc",
     ExactSpelling = true,
     CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_malloc(int length);

        [DllImport("libfftw3-3.dll",
     EntryPoint = "fftw_free",
     ExactSpelling = true,
     CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_free(IntPtr mem);

        [DllImport("libfftw3-3.dll",
     EntryPoint = "fftw_destroy_plan",
     ExactSpelling = true,
     CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_destroy_plan(IntPtr plan);

        [DllImport("libfftw3-3.dll",
     EntryPoint = "fftw_cleanup",
     ExactSpelling = true,
     CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_cleanup();

        [DllImport("libfftw3-3.dll",
     EntryPoint = "fftw_execute",
     ExactSpelling = true,
     CallingConvention = CallingConvention.Cdecl)]
        public static extern void fftw_execute(IntPtr plan);

        [DllImport("libfftw3-3.dll",
     EntryPoint = "fftw_plan_dft_1d",
     ExactSpelling = true,
     CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr fftw_plan_dft_1d(int n, IntPtr input, IntPtr output,
            fftw_direction direction, fftw_flags flags);

        private void SpectrumData_Click(object sender, EventArgs e)
        {
            return;
            if (currentTestData == null || nGraphType == 1)
                return;

            Cursor = Cursors.WaitCursor;

            double[][] SpectrumData = new double[6][];
            double dx = 300.0 / (150000 * 1.0);
            int nDataLength = 65536;
            int nFftLen = 25600;
            double lambdaS = 0.008;
            double lambdaC1 = 2.5;
            double dbMax = Double.MinValue;
            double dbMin = Double.MaxValue;
            for (int i = 0; i < 6; i++)
            {
                int nLen = currentTestData.GetLength(1);
                int probeStart = 0;
                for (int nj = 0; nj < nLen; nj++)
                {
                    if ((currentTestData[i, nj] >= -3.0) && (currentTestData[i, nj] <= 3.0))
                    {
                        probeStart = nj;
                        break;
                    }
                }

                int probeEnd = nLen - 1;
                for (int nj = nLen - 1; nj >= 0; nj--)
                {
                    if ((currentTestData[i, nj] >= -3.0) && (currentTestData[i, nj] <= 3.0))
                    {
                        probeEnd = nj;
                        break;
                    }
                }

                int probeLen = probeEnd - probeStart + 1;
                if (probeLen > nDataLength)
                    probeStart += (probeLen - nDataLength) / 2;
                int nOffset = probeStart;
                double[] yData = new double[nDataLength*2];
                double[] yDataInGauss = new double[nDataLength];
                double[] yDataFiltered = new double[nDataLength];
                for (int inA = 0; inA < nDataLength; inA++)
                {
                    if (inA < probeLen)
                        yData[inA] = currentTestData[i, nOffset + inA];
                    else
                        yData[inA] = 0;
                }

                GaussFilter(yData, nDataLength, lambdaS, dx, ref yDataInGauss);
                GaussFilter(yData, nDataLength, lambdaC1, dx, ref yDataFiltered);

                for (int inA = 0; inA < nDataLength; inA++)
                {
                    if (inA < probeLen)
                    {
                        yData[2 * inA + 0] = yDataInGauss[inA] - yDataFiltered[inA];
                        yData[2 * inA + 1] = 0;
                    }
                    else
                    {
                        yData[2 * inA + 0] = 0;
                        yData[2 * inA + 1] = 0;
                    }
                }

                IntPtr pin = fftw_malloc(nDataLength * Marshal.SizeOf(typeof(double)) * 2);
                IntPtr pout = fftw_malloc(nDataLength * Marshal.SizeOf(typeof(double)) * 2);

                Marshal.Copy(yData, 0, pin, nDataLength*2);

                IntPtr fftw_plan1 = fftw_plan_dft_1d(nDataLength, pin, pout, fftw_direction.Forward, fftw_flags.Estimate);

                fftw_execute(fftw_plan1);

                double[] yDataOut = new double[nDataLength * 2];
                Marshal.Copy(pout, yDataOut, 0, nDataLength * 2);
       
                SpectrumData[i] = new double[nFftLen];
                for (int inA = 0; inA < nFftLen; inA++)
                {
                    SpectrumData[i][inA] = Math.Sqrt(yDataOut[inA * 2] * yDataOut[inA * 2] + yDataOut[inA * 2 + 1] * yDataOut[inA * 2 + 1]);
                    if (SpectrumData[i][inA] > dbMax)
                        dbMax = SpectrumData[i][inA];
                    if(SpectrumData[i][inA] < dbMin)
                        dbMin = SpectrumData[i][inA];
                }

                fftw_free(pin);
                fftw_free(pout);
                fftw_destroy_plan(fftw_plan1);
            }
            fftw_cleanup();

            nGraphType = 1;
            int nGroupCount = 2;
            DataGraph.SetYValueAxis(dbMin, dbMax, true);
            DataGraph.setGroupDataCount(nGroupCount);
            DataGraph.SetLineData(0, SpectrumData[0], SpectrumData[1], SpectrumData[2], nFftLen);
            DataGraph.SetLineData(1, SpectrumData[3], SpectrumData[4], SpectrumData[5], nFftLen);
            DataGraph.Invalidate();

            Cursor = Cursors.Default;
        }

        private void WaveLineData_Click(object sender, EventArgs e)
        {
            if (currentTestData == null || nGraphType == 0)
                return;
            Cursor = Cursors.WaitCursor;
            CalculationAcqData(currentTestData);
            Cursor = Cursors.Default;
        }
    }
}
