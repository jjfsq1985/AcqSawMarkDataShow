using CsvHelper;
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

namespace AcqDataShow
{
    public partial class AcqDataForm : Form
    {
         public AcqDataForm()
        {
            InitializeComponent();
        }

        private void DataGraph_MouseClick(object sender, MouseEventArgs e)
        {
            //数据缩放
        }

        private void OpenFileMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDlg = new OpenFileDialog();
            openFileDlg.Filter = "CSV(*.csv)|*.csv";
            //openFileDlg.InitialDirectory = Application.StartupPath;
            DialogResult result = openFileDlg.ShowDialog();
            if(result == DialogResult.OK)
            {
                CsvReader csvRead = new CsvReader(openFileDlg.FileName, Encoding.Default);
                DataTable table = csvRead.ReadIntoDataTable();
                double[,] probeData = new double[table.Columns.Count, table.Rows.Count];
                for (int i = 0; i < table.Columns.Count; i++)
                {
                    for (int j = 0; j < table.Rows.Count; j++)
                    {
                        probeData[i, j] = Convert.ToDouble(table.Rows[j].ItemArray[i].ToString());
                    }
                }

                CalculationAcqData(probeData);
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
                par.ClassifyPar[0].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[0].parDetail[0].parValue = "5";

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

            // smallSawMarkDepthMin
            par.ClassifyPar[3] = new ClassifyParameter();
            par.ClassifyPar[3].calType = CalType.SawMarkSM;
            par.ClassifyPar[3].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[3].parDetail[ni].parType = ParType.SmallSawMarkDepthMin;
                par.ClassifyPar[3].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[3].parDetail[0].parValue = "200";//去除密线影响

            //SmallSawMarkDepthMax
            par.ClassifyPar[4] = new ClassifyParameter();
            par.ClassifyPar[4].calType = CalType.SawMarkSM;
            par.ClassifyPar[4].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[4].parDetail[ni].parType = ParType.SmallSawMarkDepthMax;
                par.ClassifyPar[4].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[4].parDetail[0].parValue = "300";//去除密线影响

            //SmallSawMarkNumMax
            par.ClassifyPar[5] = new ClassifyParameter();
            par.ClassifyPar[5].calType = CalType.SawMarkSM;
            par.ClassifyPar[5].parDetail = new CellParameter[18];
            for (int ni = 0; ni < 18; ni++)
            {
                par.ClassifyPar[5].parDetail[ni].parType = ParType.SmallSawMarkNumMax;
                par.ClassifyPar[5].parDetail[ni].parValue = "9999";
            }
            par.ClassifyPar[5].parDetail[0].parValue = "5";

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

bool GaussFilter(double[] dataIn, int dataLen, double lambda, double dx, ref double[] dataOut)
{
	try
	{
		if ((null == dataIn) || (null == dataOut) || (dataLen <= 0) || (lambda <= 0) || (dx <= 0))
		{
			return false;
		}

		//****************** 高斯滤波 V1.1 *******************//

		double alpha = 0.4697;
		double coef = 1.0 / (alpha*lambda);
		int lenx = Convert.ToInt32(2 * lambda / dx + 1);
		double[] h = new double[lenx];
		double hsum = 0;
		for (int ni = 0;ni < lenx;ni++)
		{
			double x = -lambda + dx*ni;
			h[ni] = coef*Math.Exp(-Math.PI*(x*coef)*(x*coef));
			hsum += h[ni];
		}

		double[] y = new double[dataLen + lenx - 1];
		for (int ni = 0;ni < dataLen + lenx - 1;ni++)
		{
			y[ni] = 0;
		}
		if (dataLen >= lenx)
		{
			for (int i = 0;i < lenx;i++)
			{
				for (int j = 0;j <= i;j++)
				{
					y[i] = y[i] + dataIn[i - j] * h[j];
				}
			}

			for (int i = lenx;i < dataLen;i++)
			{
				for (int j = 0;j < lenx;j++)
				{
					y[i] = y[i] + dataIn[i - j] * h[j];
				}
			}

			for (int i = dataLen;i < dataLen + lenx - 1;i++)
			{
				for (int j = i - dataLen + 1;j < lenx;j++)
				{
					y[i] = y[i] + dataIn[i - j] * h[j];
				}
			}
		}
		else
		{
			for (int i = 0;i < dataLen;i++)
			{
				for (int j = 0;j <= i;j++)
				{
					y[i] = y[i] + h[i - j] * dataIn[j];
				}
			}

			for (int i = dataLen;i < lenx;i++)
			{
				for (int j = 0;j < dataLen;j++)
				{
					y[i] = y[i] + h[i - j] * dataIn[j];
				}
			}

			for (int i = lenx;i < dataLen + lenx - 1;i++)
			{
				for (int j = i - lenx + 1;j < dataLen;j++)
				{
					y[i] = y[i] + h[i - j] * dataIn[j];
				}
			}
		}

		int halfLen = Convert.ToInt32(Math.Floor(0.5*lenx));
		for (int ni = 0;ni < dataLen;ni++)
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

        private void CalculationAcqData(double[,] probeDataIn)
        {
            GropResults result;
            object dataShowObj;

            System.DateTime startTime = new System.DateTime();
            startTime = System.DateTime.Now;

            SawMarkStation SawMark = new SawMarkStation();
            SawMark.Init(Application.StartupPath +  @"\AlgoConfigFile.xml");
            SetParameter(SawMark);

            int nPolyFit = 5;
            double[] dbCoef = new double[nPolyFit+1];//5阶6个系数
            int nDataLength = 90000;
            double lambdaC1 = 0.8;
            double[][] probeDataFiltered = new double[6][];
            double[][] probeDataInGauss = new double[6][];
            for (int a = 0; a < 6; a++)
            {
                int nLen = probeDataIn.GetLength(1);
                int probeStart = 0;
                for (int nj = 0; nj < nLen; nj++)
                {
                    if ((probeDataIn[a, nj] >= -3.0) && (probeDataIn[a,nj] <= 3.0))
                    {
                        probeStart = nj;
                        break;
                    }
                }

                int probeEnd = nLen - 1;
                for (int nj = nLen - 1; nj >= 0; nj--)
                {
                    if ((probeDataIn[a, nj] >= -3.0) && (probeDataIn[a, nj] <= 3.0))
                    {
                        probeEnd = nj;
                        break;
                    }
                }

                int probeLen = probeEnd - probeStart + 1;
                if (probeLen < nDataLength)
                    nDataLength = probeLen;
                int nOffset = probeStart;
                probeDataFiltered[a] = new double[probeLen];
                probeDataInGauss[a] = new double[probeLen];


                //double[] xData = new double[probeLen];
                for (int inA = 0; inA < probeLen; inA++)
                {
                    probeDataInGauss[a][inA] = probeDataIn[a, nOffset+inA];
                    //xData[inA] = inA;
                }
                    
                double dx = 300.0 / (150000 * 1.0);

                GaussFilter(probeDataInGauss[a], probeLen, lambdaC1, dx, ref probeDataFiltered[a]);

                //DateTime polyFitStart = System.DateTime.Now;

                //SawMark.SMpolyFit(probeDataInGauss[a], probeLen, nPolyFit, ref dbCoef);		                
                //LeastSquare fit = new LeastSquare();
                //fit.polyfit(xData,probeDataInGauss[a],nPolyFit);

                //TimeSpan tspolyFit = System.DateTime.Now.Subtract(polyFitStart);
                //System.Diagnostics.Trace.WriteLine("拟合曲线" + (a+1).ToString() + "耗时：" + tspolyFit.TotalMilliseconds.ToString() + "毫秒");


                //polyFitStart = System.DateTime.Now;
                //for (int inA = 0; inA < probeLen; inA++)
                //{
                    //probeDataInGauss[a][inA] = SawMark.SMpolyFitGetY(dbCoef, nPolyFit+1, inA);
                //    probeDataInGauss[a][inA] =  fit.getY(inA);
                //}
                //TimeSpan tsGetY = System.DateTime.Now.Subtract(polyFitStart);
                //System.Diagnostics.Trace.WriteLine("输出拟合曲线" + (a + 1).ToString() + "耗时：" + tsGetY.TotalMilliseconds.ToString() + "毫秒");

            }

            SawMark.CalProcess(probeDataIn, out result, out dataShowObj);

            System.DateTime currentTime = new System.DateTime();
            currentTime = System.DateTime.Now;
            TimeSpan ts = currentTime.Subtract(startTime);

            string aaa = ts.TotalMilliseconds.ToString();

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
            
            for (int ni = 0; ni < num1; ni++)
            {
                for (int nj = 0; nj < num2; nj++)
                {
                    classifyResult += result.ClassifyResult[ni, nj].ToString();
                    if(ni==0)
                        outputGrid.QLevelSawMark[nj] = result.ClassifyResult[ni, nj];
                    else if (ni == 1)
                        outputGrid.QLevelThickness[nj] = result.ClassifyResult[ni, nj];
                    else if (ni == 2)
                        outputGrid.QLevelTTV[nj] = result.ClassifyResult[ni, nj];
                    else if (ni ==3)
                        outputGrid.QLevelWarp[nj] = result.ClassifyResult[ni, nj];
                }
                classifyResult += "\r\n";
            }
            string calresult = "";
            outputGrid.TypeName = "线痕";
            outputGrid.DataName = new string[7];
            outputGrid.DataValue = new double[7];
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

                        //0,1,14,15,16,17,18
                        if (nIndex == 0 || nIndex == 1 || (nIndex >= 14 && nIndex <= 18))
                        {
                            outputGrid.DataName[outputGridindex] = resultcal.remark;
                            outputGrid.DataValue[outputGridindex] = resultcal.value;
                            outputGridindex++;
                        }
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
//             DataGraph.SetYValueAxis(150.0, 250,0);
//             DataGraph.setGroupDataCount(1);
//             DataGraph.SetLineData(dataShow.ThicknessOfProbePair1, dataShow.ThicknessOfProbePair2, dataShow.ThicknessOfProbePair3, nCount);

             DataGraph.SetYValueAxis(-100.0, 100.0);
             int nGroupCount = 2;
             DataGraph.setGroupDataCount(nGroupCount);
             DataGraph.SetProcessedDataUp(probeDataFiltered[0], probeDataFiltered[2], probeDataFiltered[4], nDataLength);
             DataGraph.SetProcessedDataDown(probeDataFiltered[1], probeDataFiltered[3], probeDataFiltered[5], nDataLength);
             //DataGraph.SetProcessedDataUp(probeDataInGauss[0], probeDataInGauss[2], probeDataInGauss[4], nDataLength);
             //DataGraph.SetProcessedDataDown(probeDataInGauss[1], probeDataInGauss[3], probeDataInGauss[5], nDataLength);
             DataGraph.Invalidate();            
        }
    }
}
