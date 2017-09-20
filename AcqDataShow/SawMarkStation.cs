using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Collections;
using System.Reflection;

namespace AcqDataShow
{
    public class SawMarkStation
    {
        #region DLL Import

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkCalProcess(double[,] probeDatasIn, int probeDataLenIn, int probeNumIn, ref int errorFlagOut,
            StringBuilder errorInfoOut, int errorInfoSizeMax, ref bool warnFlagOut, StringBuilder warnInfoOut, int warnInfoSizeMax,
            double[,] sawMarkInfosOut, int sawMarkInfoMaxLenIn, int sawMarkInfoSizeIn, ref int sawMarkNumOut, double[,] showDataOut,
            int showDataLenIn);

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkCalProcessForOneProbe(double[,] probeDatasIn, int probeDataLenIn, int probeNumIn, int probeIndexIn, ref int errorFlagOut,
            StringBuilder errorInfoOut, int errorInfoSizeMax, ref bool warnFlagOut, StringBuilder warnInfoOut, int warnInfoSizeMax,
            double[,] sawMarkInfosOut, int sawMarkInfoMaxLenIn, int sawMarkInfoSizeIn, ref int sawMarkNumOut, double[,] showDataOut,
            int showDataLenIn);


        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool ThicknessCalProcess(double[,] probeDatasIn, int probeDataLenIn, int probeNumIn, ref int errorFlagOut,
            StringBuilder errorInfoOut, int errorInfoSizeMax, ref bool warnFlagOut, StringBuilder warnInfoOut, int warnInfoSizeMax,
            double[,] thicknessDataOut, int numProbePairsIn, int thicknessMeasurePosIn, int[,] thicknessDataValidFlag);

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool WarpCalProcess(double[,] probeDatasIn, int probeDataLenIn, int probeNumIn, ref int errorFlagOut,
            StringBuilder errorInfoOut, int errorInfoSizeMax, ref bool warnFlagOut, StringBuilder warnInfoOut, int warnInfoSizeMax,
            double[] warpDataOut, int numProbePairsIn);

        #endregion


        #region my Private

        private SawMarkAlgParams algoParams = new SawMarkAlgParams();
        private SawMarkSpecParams specParams = new SawMarkSpecParams();
        private SawMarkCommParams commParams = new SawMarkCommParams();

        private AlgoConfigFileContent algoConfigFile = new AlgoConfigFileContent();

        private string algoConfigFilePath = null;
        private string algoParamFileFolder = null;

        private bool initialised;

        public const int CONTINUOUS_BROKEN_PIECES = 2500;
        public const int CONTINUOUS_OVERLAP_PIECES = 2501;
        public const int CALCULATION_TIMEOUT = 2502;

        private int brokenPiecesCount;
        private int overlapPiecesCount;

        private Hashtable calResultTable = new Hashtable();
        private Mutex calResultMutex = new Mutex();
        private List<Thread> currentThread = new List<Thread>();

        private Hashtable calResultTableSawMark = new Hashtable();
        private Mutex calResultMutexSawMark = new Mutex();
        private List<Thread> currentThreadSawMark = new List<Thread>();

        #endregion


        public SawMarkStation()
        {
            brokenPiecesCount = 0;
            overlapPiecesCount = 0;
            initialised = false;
        }
        public bool Init(string configFile)
        {
            #region  step1: 读入外部配置文件

            ConfigFileContent tempConfigFile;
            if (File.Exists(configFile))
            {
                tempConfigFile = APSerializer.XmlDeserialize<ConfigFileContent>(configFile);
                if (null == tempConfigFile)
                {
                    return false;
                }
            }
            else
            {
                tempConfigFile = new ConfigFileContent();
                APSerializer.XmlSerialize(tempConfigFile, configFile);
            }

            if (!Directory.Exists(tempConfigFile.AlgoParamsFileFolder))
            {
                Directory.CreateDirectory(tempConfigFile.AlgoParamsFileFolder);
            }
            if (null == tempConfigFile.AlgoConfigFilePath)
            {
                return false;
            }

            #endregion

            #region step2: 读入算法配置文件

            if (File.Exists(tempConfigFile.AlgoConfigFilePath))
            {
                algoConfigFile = APSerializer.XmlDeserialize<AlgoConfigFileContent>(tempConfigFile.AlgoConfigFilePath);
                if (null == algoConfigFile)
                {
                    return false;
                }
            }
            else
            {
                algoConfigFile = new AlgoConfigFileContent();
                APSerializer.XmlSerialize(algoConfigFile, tempConfigFile.AlgoConfigFilePath);
            }

            #endregion

            #region step3: 根据算法配置文件中的规则名对应的算法文件名 读入算法参数

            string currentParamFileName;
            algoConfigFile.GetParamFileName(algoConfigFile.CurrentSpecName, out currentParamFileName);
            if ((null == currentParamFileName) || ("" == currentParamFileName))
            {
                return false;
            }
            if (!(currentParamFileName.EndsWith(".xml")))
            {
                return false;
            }
            string fileName = tempConfigFile.AlgoParamsFileFolder + currentParamFileName;
            string errorInfo;
            if (!algoParams.SetParams(fileName, out errorInfo))
            {
                return false;
            }
            #endregion

            algoConfigFilePath = tempConfigFile.AlgoConfigFilePath;
            algoParamFileFolder = tempConfigFile.AlgoParamsFileFolder;

            initialised = true;

            Thread thread = new Thread(new ThreadStart(CalMonitoring));
            thread.IsBackground = true;
            thread.Start();

            return true;
        }
        public bool SetParameter(Parameter par)
        {
            if (!initialised)
            {
                return false;
            }
            string currentParamFileName;
            algoConfigFile.GetParamFileName(algoConfigFile.CurrentSpecName, out currentParamFileName);
            bool updated = false;
            if (specParams.SetParams(par) && commParams.SetParams(par, ref algoConfigFile, ref updated))
            {
                if (updated)
                {
                    APSerializer.XmlSerialize(algoConfigFile, algoConfigFilePath);
                    string newParamFileName;
                    algoConfigFile.GetParamFileName(algoConfigFile.CurrentSpecName, out newParamFileName);
                    if (currentParamFileName != newParamFileName)
                    {
                        string fileName = algoParamFileFolder + newParamFileName;
                        string errorInfo;
                        if (!algoParams.SetParams(fileName, out errorInfo))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
            return true;
        }
        public bool CalProcess(object source, out GropResults results, out object dataShow)
        {
            System.DateTime aaStart = new System.DateTime();
            aaStart = System.DateTime.Now;

            #region step1: result 赋初值

            results = new GropResults();
            results.StationName = StationType.SawMark;
            results.Flaw = new FlawArea();
            results.Flaw.FlawNum = 0;
            results.Flaw.FlawAreas = new ARegion[0];
            results.ValidResult = ValidType.CalError;
            results.ClassifyResult = new bool[4, 18];
            results.ClassifyResultNames = new CalType[4];
            results.ClassifyResultNames[0] = CalType.SawMarkSM;
            results.ClassifyResultNames[1] = CalType.ThicknessSM;
            results.ClassifyResultNames[2] = CalType.TTVSM;
            results.ClassifyResultNames[3] = CalType.WarpSM;
            results.DataResults = new CalResult[4];

            results.DataResults[0] = new CalResult();
            results.DataResults[0].calType = CalType.SawMarkSM;
            results.DataResults[0].processData = new ResultType[0];
            results.DataResults[0].result = new ResultType[43];
            results.DataResults[0].result[0].name = "SawMarkType";
            results.DataResults[0].result[0].remark = "线痕类型";
            results.DataResults[0].result[0].value = -1;
            results.DataResults[0].result[1].name = "SawMarkDepthMax";
            results.DataResults[0].result[1].remark = "最大线痕深度";
            results.DataResults[0].result[1].value = -1;
            results.DataResults[0].result[2].name = "SawMarkDepthMaxPeakPos";
            results.DataResults[0].result[2].remark = "最大线痕波峰位置";
            results.DataResults[0].result[2].value = -1;
            results.DataResults[0].result[3].name = "SawMarkDepthMaxValleryPos";
            results.DataResults[0].result[3].remark = "最大线痕波谷位置";
            results.DataResults[0].result[3].value = -1;
            results.DataResults[0].result[4].name = "SawMarkDepthMaxPeakVal";
            results.DataResults[0].result[4].remark = "最大线痕波峰值";
            results.DataResults[0].result[4].value = -1;
            results.DataResults[0].result[5].name = "SawMarkDepthMaxValleryVal";
            results.DataResults[0].result[5].remark = "最大线痕波谷值";
            results.DataResults[0].result[5].value = -1;
            results.DataResults[0].result[6].name = "SawMarkDepthMaxProbeIndex";
            results.DataResults[0].result[6].remark = "最大线痕探头号";
            results.DataResults[0].result[6].value = -1;
            int nProbeCount = 6;
            int nDataPerProbe = 6; //每个探头6种数据
            for (int ni = 0; ni < nProbeCount; ni++)
            {
                results.DataResults[0].result[nDataPerProbe * ni + 7].name = "SawMarkNumForProbe" + (ni + 1).ToString();
                results.DataResults[0].result[nDataPerProbe * ni + 7].remark = "探头" + (ni + 1).ToString() + "线痕个数";
                results.DataResults[0].result[nDataPerProbe * ni + 7].value = -1;
                results.DataResults[0].result[nDataPerProbe * ni + 8].name = "SawMarkDepthMaxForProbe" + (ni + 1).ToString();
                results.DataResults[0].result[nDataPerProbe * ni + 8].remark = "探头" + (ni + 1).ToString() + "线痕深度";
                results.DataResults[0].result[nDataPerProbe * ni + 8].value = -1;

                results.DataResults[0].result[nDataPerProbe * ni + 9].name = "SawMarkDepthMaxPeakPosForProbe" + (ni + 1).ToString();
                results.DataResults[0].result[nDataPerProbe * ni + 9].remark = "探头" + (ni + 1).ToString() + "最大波峰位置";
                results.DataResults[0].result[nDataPerProbe * ni + 9].value = -1;
                results.DataResults[0].result[nDataPerProbe * ni + 10].name = "SawMarkDepthMaxValleryPosForProbe" + (ni + 1).ToString();
                results.DataResults[0].result[nDataPerProbe * ni + 10].remark = "探头" + (ni + 1).ToString() + "最大波谷位置";
                results.DataResults[0].result[nDataPerProbe * ni + 10].value = -1;

                results.DataResults[0].result[nDataPerProbe * ni + 11].name = "SawMarkDepthMaxPeakValForProbe" + (ni + 1).ToString();
                results.DataResults[0].result[nDataPerProbe * ni + 11].remark = "探头" + (ni + 1).ToString() + "最大波峰值";
                results.DataResults[0].result[nDataPerProbe * ni + 11].value = -1;
                results.DataResults[0].result[nDataPerProbe * ni + 12].name = "SawMarkDepthMaxValleryValForProbe" + (ni + 1).ToString();
                results.DataResults[0].result[nDataPerProbe * ni + 12].remark = "探头" + (ni + 1).ToString() + "最大波谷值";
                results.DataResults[0].result[nDataPerProbe * ni + 12].value = -1;

            }

            results.DataResults[1] = new CalResult();
            results.DataResults[1].calType = CalType.ThicknessSM;
            results.DataResults[1].processData = new ResultType[0];
            results.DataResults[1].result = new ResultType[4];
            results.DataResults[1].result[0].name = "AverageThickness";
            results.DataResults[1].result[0].remark = "总平均厚度";
            results.DataResults[1].result[0].value = -1;
            for (int ni = 0; ni < 3; ni++)
            {
                results.DataResults[1].result[ni + 1].name = "AverageThicknessForProbePair" + (ni + 1).ToString();
                results.DataResults[1].result[ni + 1].remark = "第" + (ni + 1).ToString() + "对探头的平均厚度";
                results.DataResults[1].result[ni + 1].value = -1;
            }

            results.DataResults[2] = new CalResult();
            results.DataResults[2].calType = CalType.TTVSM;
            results.DataResults[2].processData = new ResultType[0];
            results.DataResults[2].result = new ResultType[10];
            results.DataResults[2].result[0].name = "TTV";
            results.DataResults[2].result[0].remark = "总厚度变化";
            results.DataResults[2].result[0].value = -1;
            for (int ni = 0; ni < 3; ni++)
            {
                results.DataResults[2].result[3 * ni + 1].name = "TTVForProbePair" + (ni + 1).ToString();
                results.DataResults[2].result[3 * ni + 1].remark = "第" + (ni + 1).ToString() + "对探头的TTV";
                results.DataResults[2].result[3 * ni + 1].value = -1;
                results.DataResults[2].result[ni * 3 + 1 + 1].name = "MaxThicknessForProbePair" + (ni + 1).ToString();
                results.DataResults[2].result[ni * 3 + 1 + 1].remark = "第" + (ni + 1).ToString() + "对探头的最大厚度";
                results.DataResults[2].result[ni * 3 + 1 + 1].value = -1;
                results.DataResults[2].result[ni * 3 + 2 + 1].name = "MinThicknessForProbePair" + (ni + 1).ToString();
                results.DataResults[2].result[ni * 3 + 2 + 1].remark = "第" + (ni + 1).ToString() + "对探头的最小厚度";
                results.DataResults[2].result[ni * 3 + 2 + 1].value = -1;
            }

            results.DataResults[3] = new CalResult();
            results.DataResults[3].calType = CalType.WarpSM;
            results.DataResults[3].processData = new ResultType[0];
            results.DataResults[3].result = new ResultType[4];
            results.DataResults[3].result[0].name = "Warp";
            results.DataResults[3].result[0].remark = "翘曲";
            results.DataResults[3].result[0].value = -1;
            for (int ni = 0; ni < 3; ni++)
            {
                results.DataResults[3].result[ni + 1].name = "WarpOfProbePair" + (ni + 1).ToString();
                results.DataResults[3].result[ni + 1].remark = "第" + (ni + 1).ToString() + "对探头的翘曲";
                results.DataResults[3].result[ni + 1].value = -1;
            }

            #endregion

            #region step2: dataShow 赋初值

            dataShow = null;

            #endregion

            #region step3: 判断source是否合理

            int gradeNumAll;
            if (!specParams.GetGradeNumAll(out gradeNumAll))
            {
                //LogHelper.ErrorLog("SawMarkAlgError: 获取等级数出错; error at GetGradeNumAll", null);
                return false;
            }
            if (gradeNumAll <= 0)
            {
                //LogHelper.ErrorLog("SawMarkAlgError: 等级数小于0; error gradeNum:" + gradeNumAll, null);
                return false;
            }
            results.ClassifyResult = new bool[4, gradeNumAll];

            double[,] probeDataIn = (double[,])source;
            int probeNum = probeDataIn.GetLength(0);
            int probeDataLen = probeDataIn.GetLength(1);

            if ((probeNum <= 0) || (probeDataLen <= 0))
            {
                //LogHelper.ErrorLog("SawMarkAlgError: CalProcess输入不合法; error input for CalProcess", null);
                return false;
            }

            int numSawMarkMax = 600;
            int numShowDataLen = 8000;
            int numThicknessMeasurePos = Convert.ToInt32(algoParams.MeasurePosNumber);
            int probePairsNum = probeNum / 2;

            #endregion

            try
            {
                System.DateTime startTime = new System.DateTime();
                startTime = System.DateTime.Now;

                #region step4: 判断是否需要检线痕 创建线程开始计算

                string nameThreadSawMark = "SawMark_" + startTime.ToString("HH_mm_ss_fff");
                ThreadSawMarkResult resultSawMark = new ThreadSawMarkResult(numSawMarkMax, probeNum, numShowDataLen);
                Thread threadCalSawMark = null;

                if (specParams.IsInTestItems(CalType.SawMarkSM))        // 判断是否需检线痕
                {
                    if (calResultTable.ContainsKey(nameThreadSawMark))
                    {
                        results.ValidResult = ValidType.CalError;
                        //LogHelper.ErrorLog("SawMarkAlgError: 重复创建SawMark计算线程;thread name:" + nameThreadSawMark, null);
                        return false;
                    }
                    calResultMutex.WaitOne();
                    calResultTable.Add(nameThreadSawMark, resultSawMark);
                    calResultMutex.ReleaseMutex();

                    threadCalSawMark = new Thread(new ParameterizedThreadStart(ThreadSawMarkCalCoreMultiThread));
                    ThreadCalDataIn dataInSawMark = new ThreadCalDataIn();
                    dataInSawMark.dataIn = probeDataIn;
                    dataInSawMark.name = nameThreadSawMark;

                    threadCalSawMark.Name = nameThreadSawMark;
                    threadCalSawMark.Start((object)dataInSawMark);

                    currentThread.Add(threadCalSawMark);
                }

                #endregion

                #region step5: 判断是否需要检厚度或TTV 创建线程开始计算

                string nameThreadThickness = "Thickness_" + startTime.ToString("HH_mm_ss_fff");
                ThreadThicknessResult resultThickness = new ThreadThicknessResult(probePairsNum, numThicknessMeasurePos);
                Thread threadCalThickness = null;

                if (specParams.IsInTestItems(CalType.ThicknessSM) || specParams.IsInTestItems(CalType.TTVSM))        // 判断是否需检厚度或TTV
                {
                    if (calResultTable.ContainsKey(nameThreadThickness))
                    {
                        results.ValidResult = ValidType.CalError;
                        //LogHelper.ErrorLog("SawMarkAlgError: 重复创建Thickness计算线程;thread name:" + nameThreadThickness, null);
                        return false;
                    }
                    calResultMutex.WaitOne();
                    calResultTable.Add(nameThreadThickness, resultThickness);
                    calResultMutex.ReleaseMutex();

                    threadCalThickness = new Thread(new ParameterizedThreadStart(ThreadThicknessCalCore));
                    ThreadCalDataIn dataInThickness = new ThreadCalDataIn();
                    dataInThickness.dataIn = probeDataIn;
                    dataInThickness.name = nameThreadThickness;

                    threadCalThickness.Name = nameThreadThickness;
                    threadCalThickness.Start((object)dataInThickness);

                    currentThread.Add(threadCalThickness);
                }

                #endregion

                #region step6: 判断是否需要翘曲 创建线程开始计算

                string nameThreadWarp = "Warp_" + startTime.ToString("HH_mm_ss_fff");
                ThreadWarpResult resultWarp = new ThreadWarpResult(probePairsNum);
                Thread threadCalWarp = null;

                if (specParams.IsInTestItems(CalType.WarpSM))        // 判断是否需翘曲
                {
                    if (calResultTable.ContainsKey(nameThreadWarp))
                    {
                        results.ValidResult = ValidType.CalError;
                        //LogHelper.ErrorLog("SawMarkAlgError: 重复创建Warp计算线程;thread name:" + nameThreadWarp, null);
                        return false;
                    }
                    calResultMutex.WaitOne();
                    calResultTable.Add(nameThreadWarp, resultWarp);
                    calResultMutex.ReleaseMutex();

                    threadCalWarp = new Thread(new ParameterizedThreadStart(ThreadWarpCalCore));
                    ThreadCalDataIn dataInWarp = new ThreadCalDataIn();
                    dataInWarp.dataIn = probeDataIn;
                    dataInWarp.name = nameThreadWarp;

                    threadCalWarp.Name = nameThreadWarp;
                    threadCalWarp.Start((object)dataInWarp);

                    currentThread.Add(threadCalWarp);
                }

                #endregion

                #region step7: while循环

                System.DateTime currentTime = new System.DateTime();
                TimeSpan ts;

                while (true)
                {
                    bool threadSawMarkRunning = true;
                    bool threadThicknessRunning = true;
                    bool threadWarpRunning = true;

                    Thread temp;
                    temp = currentThread.Find(b => b.Name == nameThreadSawMark);
                    if (temp == null)
                    {
                        threadSawMarkRunning = false;
                        threadCalSawMark = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadSawMarkRunning = false;
                        threadCalSawMark = null;
                        currentThread.Remove(temp);
                    }

                    temp = currentThread.Find(b => b.Name == nameThreadThickness);
                    if (temp == null)
                    {
                        threadThicknessRunning = false;
                        threadCalThickness = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadThicknessRunning = false;
                        threadCalThickness = null;
                        currentThread.Remove(temp);
                    }

                    temp = currentThread.Find(b => b.Name == nameThreadWarp);
                    if (temp == null)
                    {
                        threadWarpRunning = false;
                        threadCalWarp = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadWarpRunning = false;
                        threadCalWarp = null;
                        currentThread.Remove(temp);
                    }

                    if (threadSawMarkRunning || threadThicknessRunning || threadWarpRunning)
                    {
                        currentTime = System.DateTime.Now;
                        ts = currentTime.Subtract(startTime);
                        if (ts.TotalMilliseconds > 5000)
                        {
                            if (threadSawMarkRunning)
                            {
                                threadCalSawMark.Abort();
                                //LogHelper.ErrorLog("SawMarkAlgError: 线痕计算超时; time out,startTime:" + startTime.ToShortTimeString() + ",currentTime:" + currentTime.ToShortTimeString(), null);
                            }
                            if (threadThicknessRunning)
                            {
                                threadCalThickness.Abort();
                                //LogHelper.ErrorLog("SawMarkAlgError: 厚度计算超时; time out,startTime:" + startTime.ToShortTimeString() + ",currentTime:" + currentTime.ToShortTimeString(), null);
                            }
                            if (threadWarpRunning)
                            {
                                threadCalWarp.Abort();
                                //LogHelper.ErrorLog("SawMarkAlgError: 翘曲计算超时; time out,startTime:" + startTime.ToShortTimeString() + ",currentTime:" + currentTime.ToShortTimeString(), null);
                            }
                            results.ValidResult = ValidType.Timeout;
                            return false;

                            Thread.Sleep(80);
                        }
                        else
                        {
                            Thread.Sleep(80);
                        }
                    }
                    else
                    {
                        break;
                    }
                }


                #endregion

                #region step8: 读出结果

                calResultMutex.WaitOne();

                try
                {
                    if (!calResultTable.ContainsKey(nameThreadSawMark))
                    {
                        results.ValidResult = ValidType.CalError;
                        //LogHelper.ErrorLog("SawMarkAlgError: 取线痕计算结果时线程不存在;thread name:" + nameThreadSawMark, null);
                        return false;
                    }
                    if (!calResultTable.ContainsKey(nameThreadThickness))
                    {
                        results.ValidResult = ValidType.CalError;
                        //LogHelper.ErrorLog("SawMarkAlgError: 取厚度计算结果时线程不存在;thread name:" + nameThreadThickness, null);
                        return false;
                    }
                    if (!calResultTable.ContainsKey(nameThreadWarp))
                    {
                        results.ValidResult = ValidType.CalError;
                        //LogHelper.ErrorLog("SawMarkAlgError: 取翘曲计算结果时线程不存在;thread name:" + nameThreadWarp, null);
                        return false;
                    }

                    resultSawMark = (ThreadSawMarkResult)calResultTable[nameThreadSawMark];
                    resultThickness = (ThreadThicknessResult)calResultTable[nameThreadThickness];
                    resultWarp = (ThreadWarpResult)calResultTable[nameThreadWarp];

                    if (resultSawMark.warnFlag)
                    {
                        //LogHelper.InfoLog("SawMarkAlgWarn: 线痕计算报警; warnInfo for SawMark Calculation：" + resultSawMark.warnInfo);
                    }
                    if (resultThickness.warnFlag)
                    {
                        //LogHelper.InfoLog("SawMarkAlgWarn: 厚度计算报警; warnInfo for Thickness Calculation：" + resultThickness.warnInfo);
                    }
                    if (resultWarp.warnFlag)
                    {
                        //LogHelper.InfoLog("SawMarkAlgWarn: 翘曲计算报警; warnInfo for Warp Calculation：" + resultWarp.warnInfo);
                    }

                    #region  step8.1: ValidResult 赋值

                    if ((0 != resultSawMark.errorFlag) || (0 != resultThickness.errorFlag) || (0 != resultWarp.errorFlag))
                    {
                        results.ValidResult = ValidType.CalError;

                        if (0 != resultSawMark.errorFlag)
                        {
                            //LogHelper.ErrorLog("SawMarkAlgError: 线痕计算出错; error at SawMark Calculation,errofInfo:" + resultSawMark.errorInfo, null);
                        }
                        if (0 != resultThickness.errorFlag)
                        {
                            //LogHelper.ErrorLog("SawMarkAlgError: 厚度计算出错; error at Thickness Calculation,errofInfo:" + resultThickness.errorInfo, null);
                        }
                        if (0 != resultWarp.errorFlag)
                        {
                            //LogHelper.ErrorLog("SawMarkAlgError: 翘曲计算出错; error at Warp Calculation,errofInfo:" + resultWarp.errorInfo, null);
                        }

                        if ((resultSawMark.errorFlag == 0x02) || (resultThickness.errorFlag == 0x02) || (resultWarp.errorFlag == 0x02))
                        {
                            overlapPiecesCount++;
                            results.ValidResult = ValidType.OverlapError;
                        }
                        else
                        {
                            overlapPiecesCount = 0;
                        }

                        if ((resultSawMark.errorFlag == 0x04) || (resultThickness.errorFlag == 0x04) || (resultWarp.errorFlag == 0x04))
                        {
                            brokenPiecesCount++;
                            results.ValidResult = ValidType.ChipError;
                        }
                        else
                        {
                            brokenPiecesCount = 0;
                        }
                    }
                    else
                    {
                        results.ValidResult = ValidType.Normal;
                    }

                    #endregion

                    #region step8.2: ClassifyResult 赋值

                    #region 判断是否要检平均厚度

                    if (specParams.IsInTestItems(CalType.ThicknessSM))
                    {
                        double sumValidThickness = 0;
                        int numValidThickness = 0;
                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            for (int nk = 0; nk < numThicknessMeasurePos; nk++)
                            {
                                if (double.NaN.ToString() != resultThickness.thicknessData[ni, nk].ToString())
                                {
                                    numValidThickness++;
                                    sumValidThickness = sumValidThickness + resultThickness.thicknessData[ni, nk];
                                }
                            }
                        }

                        if (numValidThickness > 0)
                        {
                            double avgThickness = sumValidThickness / (1.0 * numValidThickness);
                            for (int ni = 0; ni < gradeNumAll; ni++)
                            {
                                if ((avgThickness >= specParams.ThicknessMin[ni]) && (avgThickness <= specParams.ThicknessMax[ni]))
                                {
                                    results.ClassifyResult[1, ni] = true;
                                }
                                else
                                {
                                    results.ClassifyResult[1, ni] = false;
                                }
                            }
                        }
                        else
                        {
                            for (int ni = 0; ni < gradeNumAll; ni++)
                            {
                                results.ClassifyResult[1, ni] = false;
                            }
                        }
                    }
                    else
                    {
                        for (int ni = 0; ni < gradeNumAll; ni++)
                        {
                            results.ClassifyResult[1, ni] = true;
                        }
                    }

                    #endregion

                    #region 判断是否要检TTV

                    if (specParams.IsInTestItems(CalType.TTVSM))
                    {
                        double maxOfAllValidThickness = 0;
                        double minOfAllValidThickness = 0;
                        int numValidThickness = 0;

                        bool initFlag = true;
                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            for (int nk = 0; nk < numThicknessMeasurePos; nk++)
                            {
                                if (double.NaN.ToString() != resultThickness.thicknessData[ni, nk].ToString())
                                {
                                    numValidThickness++;
                                    if (initFlag)
                                    {
                                        maxOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        minOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        initFlag = false;
                                    }
                                    else
                                    {
                                        if (resultThickness.thicknessData[ni, nk] > maxOfAllValidThickness)
                                        {
                                            maxOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        }
                                        if (resultThickness.thicknessData[ni, nk] < minOfAllValidThickness)
                                        {
                                            minOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        }
                                    }
                                }
                            }
                        }

                        if (numValidThickness > 0)
                        {
                            double ttvOfAllValidThickness = maxOfAllValidThickness - minOfAllValidThickness;
                            for (int ni = 0; ni < gradeNumAll; ni++)
                            {
                                if ((ttvOfAllValidThickness >= specParams.TtvMin[ni]) && (ttvOfAllValidThickness <= specParams.TtvMax[ni]))
                                {
                                    results.ClassifyResult[2, ni] = true;
                                }
                                else
                                {
                                    results.ClassifyResult[2, ni] = false;
                                }
                            }
                        }
                        else
                        {
                            for (int ni = 0; ni < gradeNumAll; ni++)
                            {
                                results.ClassifyResult[2, ni] = false;
                            }
                        }
                    }
                    else
                    {
                        for (int ni = 0; ni < gradeNumAll; ni++)
                        {
                            results.ClassifyResult[2, ni] = true;
                        }
                    }

                    #endregion

                    #region 判断是否要检翘曲

                    if (specParams.IsInTestItems(CalType.WarpSM))
                    {
                        double maxOfAllWarp = resultWarp.warpData[0];
                        double minOfAllWarp = resultWarp.warpData[0];

                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            if (resultWarp.warpData[ni] > maxOfAllWarp)
                            {
                                maxOfAllWarp = resultWarp.warpData[ni];
                            }
                            if (resultWarp.warpData[ni] < minOfAllWarp)
                            {
                                minOfAllWarp = resultWarp.warpData[ni];
                            }
                        }

                        for (int ni = 0; ni < gradeNumAll; ni++)
                        {
                            if ((minOfAllWarp >= specParams.WarpMin[ni]) && (maxOfAllWarp <= specParams.WarpMax[ni]))
                            {
                                results.ClassifyResult[3, ni] = true;
                            }
                            else
                            {
                                results.ClassifyResult[3, ni] = false;
                            }
                        }
                    }
                    else
                    {
                        for (int ni = 0; ni < gradeNumAll; ni++)
                        {
                            results.ClassifyResult[3, ni] = true;
                        }
                    }

                    #endregion

                    #endregion

                    #region step8.3: DataResult.result 和 DataResult.processData  赋值

                    #region 判断是否要检平均厚度

                    if (specParams.IsInTestItems(CalType.ThicknessSM))
                    {
                        double sumValidThickness = 0;
                        int numValidThickness = 0;
                        double avgThickness = 0;
                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            for (int nk = 0; nk < numThicknessMeasurePos; nk++)
                            {
                                if (double.NaN.ToString() != resultThickness.thicknessData[ni, nk].ToString())
                                {
                                    numValidThickness++;
                                    sumValidThickness = sumValidThickness + resultThickness.thicknessData[ni, nk];
                                }
                            }
                        }

                        if (numValidThickness > 0)
                        {
                            avgThickness = sumValidThickness / (1.0 * numValidThickness);
                            results.DataResults[1].result[0].value = avgThickness;                              // 总平均厚度
                        }

                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            double sumThicknessOfProbeNi = 0;
                            int numValidThicknessOfProbeNi = 0;
                            double avgThicknessOfProbePairNi = 0;
                            for (int nk = 0; nk < numThicknessMeasurePos; nk++)
                            {
                                if (double.NaN.ToString() != resultThickness.thicknessData[ni, nk].ToString())
                                {
                                    numValidThicknessOfProbeNi++;
                                    sumThicknessOfProbeNi = sumThicknessOfProbeNi + resultThickness.thicknessData[ni, nk];
                                }
                            }
                            if (numValidThicknessOfProbeNi > 0)
                            {
                                avgThicknessOfProbePairNi = sumThicknessOfProbeNi / (1.0 * numValidThicknessOfProbeNi);
                                results.DataResults[1].result[ni + 1].value = avgThicknessOfProbePairNi;         // 每探头的平均厚度
                            }
                        }
                        // processData为空
                    }

                    #endregion

                    #region 判断是否要检TTV
                    double dblTotalTTV = 0;
                    if (specParams.IsInTestItems(CalType.TTVSM))
                    {
                        double maxOfAllValidThickness = 0;
                        double minOfAllValidThickness = 0;

                        bool initFlag = true;
                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            for (int nk = 0; nk < numThicknessMeasurePos; nk++)
                            {
                                if (double.NaN.ToString() != resultThickness.thicknessData[ni, nk].ToString())
                                {
                                    if (initFlag)
                                    {
                                        maxOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        minOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        initFlag = false;
                                    }
                                    else
                                    {
                                        if (resultThickness.thicknessData[ni, nk] > maxOfAllValidThickness)
                                        {
                                            maxOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        }
                                        if (resultThickness.thicknessData[ni, nk] < minOfAllValidThickness)
                                        {
                                            minOfAllValidThickness = resultThickness.thicknessData[ni, nk];
                                        }
                                    }
                                }
                            }
                        }

                        results.DataResults[2].result[0].value = maxOfAllValidThickness - minOfAllValidThickness;         // 总TTV

                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            double maxOfProbePairNi = 0;
                            double minOfProbePairNi = 0;

                            bool initFlagNi = true;
                            for (int nk = 0; nk < numThicknessMeasurePos; nk++)
                            {
                                if (double.NaN.ToString() != resultThickness.thicknessData[ni, nk].ToString())
                                {
                                    if (initFlagNi)
                                    {
                                        maxOfProbePairNi = resultThickness.thicknessData[ni, nk];
                                        minOfProbePairNi = resultThickness.thicknessData[ni, nk];
                                        initFlagNi = false;
                                    }
                                    else
                                    {
                                        if (resultThickness.thicknessData[ni, nk] > maxOfProbePairNi)
                                        {
                                            maxOfProbePairNi = resultThickness.thicknessData[ni, nk];
                                        }
                                        if (resultThickness.thicknessData[ni, nk] < minOfProbePairNi)
                                        {
                                            minOfProbePairNi = resultThickness.thicknessData[ni, nk];
                                        }
                                    }
                                }
                            }
                            results.DataResults[2].result[3 * ni + 1].value = maxOfProbePairNi - minOfProbePairNi;
                            results.DataResults[2].result[3 * ni + 1 + 1].value = maxOfProbePairNi;
                            results.DataResults[2].result[3 * ni + 2 + 1].value = minOfProbePairNi;
                        }
                        // processData 为空

                        dblTotalTTV = results.DataResults[2].result[0].value;
                    }

                    #endregion

                    #region 判断是否要检线痕

                    if (specParams.IsInTestItems(CalType.SawMarkSM))
                    {
                        //计算线痕值
                        double dbMaxMin = Math.Abs(resultSawMark.sawMarkInfos[0].peakValue - resultSawMark.sawMarkInfos[0].valleyValue) * 1000.0;
                        double maxDepthOfAllProbes = GetRegressSawmark(resultSawMark.sawMarkInfos[0].depthMax, dblTotalTTV, dbMaxMin, resultSawMark.sawMarkInfos[0].type);
                        int nType = resultSawMark.sawMarkInfos[0].type;
                        double dbPeakPos = resultSawMark.sawMarkInfos[0].peakPos;
                        double dbValleyPos = resultSawMark.sawMarkInfos[0].valleyPos;
                        double dbPeakVal = resultSawMark.sawMarkInfos[0].peakValue;
                        double dbValleyVal = resultSawMark.sawMarkInfos[0].valleyValue;
                        int nProbeIndex = resultSawMark.sawMarkInfos[0].probeIndex;
                        for (int nk = 0; nk < resultSawMark.sawMarkNum; nk++)
                        {
                            dbMaxMin = Math.Abs(resultSawMark.sawMarkInfos[nk].peakValue - resultSawMark.sawMarkInfos[nk].valleyValue) * 1000.0;
                            double dbRealSawMark = GetRegressSawmark(resultSawMark.sawMarkInfos[nk].depthMax, dblTotalTTV, dbMaxMin, resultSawMark.sawMarkInfos[nk].type);
                            resultSawMark.sawMarkInfos[nk].depthMax = dbRealSawMark;//修改线痕值
                            if (dbRealSawMark > maxDepthOfAllProbes)
                            {
                                maxDepthOfAllProbes = dbRealSawMark;
                                nType = resultSawMark.sawMarkInfos[nk].type;
                                dbPeakPos = resultSawMark.sawMarkInfos[nk].peakPos;
                                dbValleyPos = resultSawMark.sawMarkInfos[nk].valleyPos;
                                dbPeakVal = resultSawMark.sawMarkInfos[nk].peakValue;
                                dbValleyVal = resultSawMark.sawMarkInfos[nk].valleyValue;
                                nProbeIndex = resultSawMark.sawMarkInfos[nk].probeIndex;
                            }
                        }
                        results.DataResults[0].result[0].value = nType;            // 线痕种类
                        results.DataResults[0].result[1].value = maxDepthOfAllProbes;                 // 最大线痕深度
                        results.DataResults[0].result[2].value = dbPeakPos;
                        results.DataResults[0].result[3].value = dbValleyPos;
                        results.DataResults[0].result[4].value = dbPeakVal;
                        results.DataResults[0].result[5].value = dbValleyVal;
                        results.DataResults[0].result[6].value = nProbeIndex + 1;

                        for (int ni = 0; ni < probeNum; ni++)
                        {
                            bool initFlag = true;
                            double maxDepthOfProbeNi = 0;
                            int numSawMarkOfProbeNi = 0;
                            double maxPeakDepthPos = 0;
                            double maxValleyDepthPos = 0;
                            double maxPeakDepthVal = 0;
                            double maxValleyDepthVal = 0;

                            for (int nk = 0; nk < resultSawMark.sawMarkNum; nk++)
                            {
                                if (ni == resultSawMark.sawMarkInfos[nk].probeIndex)
                                {
                                    numSawMarkOfProbeNi++;
                                    if (initFlag)
                                    {
                                        maxDepthOfProbeNi = resultSawMark.sawMarkInfos[nk].depthMax;
                                        maxPeakDepthPos = resultSawMark.sawMarkInfos[nk].peakPos;
                                        maxValleyDepthPos = resultSawMark.sawMarkInfos[nk].valleyPos;
                                        maxPeakDepthVal = resultSawMark.sawMarkInfos[nk].peakValue;
                                        maxValleyDepthVal = resultSawMark.sawMarkInfos[nk].valleyValue;
                                        initFlag = false;
                                    }
                                    else
                                    {
                                        if (resultSawMark.sawMarkInfos[nk].depthMax > maxDepthOfProbeNi)
                                        {
                                            maxDepthOfProbeNi = resultSawMark.sawMarkInfos[nk].depthMax;
                                            maxPeakDepthPos = resultSawMark.sawMarkInfos[nk].peakPos;
                                            maxValleyDepthPos = resultSawMark.sawMarkInfos[nk].valleyPos;
                                            maxPeakDepthVal = resultSawMark.sawMarkInfos[nk].peakValue;
                                            maxValleyDepthVal = resultSawMark.sawMarkInfos[nk].valleyValue;
                                        }
                                    }
                                }
                            }
                            results.DataResults[0].result[6 * ni + 7].value = numSawMarkOfProbeNi;         // 每探头线痕数
                            results.DataResults[0].result[6 * ni + 8].value = maxDepthOfProbeNi;       // 每探头最大线痕深度
                            results.DataResults[0].result[6 * ni + 9].value = maxPeakDepthPos;         // 峰位置
                            results.DataResults[0].result[6 * ni + 10].value = maxValleyDepthPos;       // 谷位置
                            results.DataResults[0].result[6 * ni + 11].value = maxPeakDepthVal;         // 峰值
                            results.DataResults[0].result[6 * ni + 12].value = maxValleyDepthVal;       // 谷值
                        }

                        // processData 为空
                    }
                    #endregion

                    #region 判断线痕分类等级

                    if (specParams.IsInTestItems(CalType.SawMarkSM))
                    {
                        //最大线痕
                        double dbSawMarkMax = 0;
                        double dbsmallSawMarkMax = 0;
                        for (int nk = 0; nk < resultSawMark.sawMarkNum; nk++)
                        {
                            switch (resultSawMark.sawMarkInfos[nk].type)
                            {
                                case 0:
                                    {
                                        if (resultSawMark.sawMarkInfos[nk].depthMax > dbSawMarkMax)
                                        {
                                            dbSawMarkMax = resultSawMark.sawMarkInfos[nk].depthMax;
                                        }
                                    }
                                    break;
                                case 1:
                                    {
                                        if (resultSawMark.sawMarkInfos[nk].depthMax > dbsmallSawMarkMax)
                                        {
                                            dbsmallSawMarkMax = resultSawMark.sawMarkInfos[nk].depthMax;
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        //判断是否符合各个等级的规则
                        for (int ni = 0; ni < gradeNumAll; ni++)
                        {
                            int sawMarkNumGradeNi = 0;
                            int smallSawMarkNumGradeNi = 0;
                            if ((dbSawMarkMax > specParams.SawMarkDepthMin[ni]) && (dbSawMarkMax < specParams.SawMarkDepthMax[ni]))
                            {
                                sawMarkNumGradeNi++;
                            }
                            if ((dbsmallSawMarkMax > specParams.SmallSawMarkDepthMin[ni]) && (dbsmallSawMarkMax < specParams.SmallSawMarkDepthMax[ni]))
                            {
                                smallSawMarkNumGradeNi++;
                            }
                            //由数目判断等级（只有0和1的判断）
                            if ((sawMarkNumGradeNi <= specParams.SawMarkNumMax[ni]) && (smallSawMarkNumGradeNi <= specParams.SmallSawMarkNumMax[ni]))
                            {
                                results.ClassifyResult[0, ni] = true;
                            }
                            else
                            {
                                results.ClassifyResult[0, ni] = false;
                            }
                        }
                    }
                    else
                    {
                        for (int ni = 0; ni < gradeNumAll; ni++)
                        {
                            results.ClassifyResult[0, ni] = true;
                        }
                    }


                    #endregion

                    #region 判断是否要检翘曲

                    if (specParams.IsInTestItems(CalType.WarpSM))
                    {
                        double maxOfAllWarp = resultWarp.warpData[0];

                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            if (resultWarp.warpData[ni] > maxOfAllWarp)
                            {
                                maxOfAllWarp = resultWarp.warpData[ni];
                            }
                        }
                        results.DataResults[3].result[0].value = maxOfAllWarp;                     // 总翘曲

                        for (int ni = 0; ni < probePairsNum; ni++)
                        {
                            results.DataResults[3].result[ni + 1].value = resultWarp.warpData[ni];    // 每个探头的翘曲
                        }
                    }

                    #endregion

                    #endregion

                    #region  step8.4: DataShow 赋值

                    SawMarkDataShow dataShowOut = new SawMarkDataShow();

                    if (specParams.IsInTestItems(CalType.SawMarkSM))
                    {
                        if (!dataShowOut.SetSurfaceDataOfAllProbes(resultSawMark.sawMarkShowDatas, commParams.Size))
                        {
                            //LogHelper.ErrorLog("SawMarkAlgError: 线痕获取dataShow出错; error at SetSurfaceDataOfAllProbes", null);
                        }
                        if (!dataShowOut.SetSawMarkPositionOfAllProbes(resultSawMark.sawMarkInfos, resultSawMark.sawMarkNum))
                        {
                            //LogHelper.ErrorLog("SawMarkAlgError: 线痕获取dataShow出错; error at SetSawMarkPositionOfAllProbes", null);
                        }
                    }
                    if ((specParams.IsInTestItems(CalType.ThicknessSM)) || (specParams.IsInTestItems(CalType.TTVSM)))
                    {
                        if (!dataShowOut.SetThicknessOfAllProbePairs(resultThickness.thicknessData))
                        {
                            //LogHelper.ErrorLog("SawMarkAlgError: 厚度获取dataShow出错; error at SetThicknessOfAllProbePairs", null);
                        }
                    }

                    dataShow = dataShowOut;

                    #endregion

                }
                catch (Exception ex)
                {
                    //LogHelper.ErrorLog("SawMarkAlgError: 计算发生未知错误; error at CalProcess", ex);
                    if (results.ValidResult == ValidType.Normal)
                    {
                        results.ValidResult = ValidType.CalError;
                    }
                }

                #region  step8.5: remove

                if (!calResultTable.ContainsKey(nameThreadSawMark))
                {
                    results.ValidResult = ValidType.CalError;
                    //LogHelper.ErrorLog("SawMarkAlgError: 移除线痕计算结果时线程不存在;thread name:" + nameThreadSawMark, null);
                    return false;
                }
                if (!calResultTable.ContainsKey(nameThreadThickness))
                {
                    results.ValidResult = ValidType.CalError;
                    //LogHelper.ErrorLog("SawMarkAlgError: 移除厚度计算结果时线程不存在;thread name:" + nameThreadThickness, null);
                    return false;
                }
                if (!calResultTable.ContainsKey(nameThreadWarp))
                {
                    results.ValidResult = ValidType.CalError;
                    //LogHelper.ErrorLog("SawMarkAlgError: 移除翘曲计算结果时线程不存在;thread name:" + nameThreadWarp, null);
                    return false;
                }

                resultSawMark.errorInfo = null;
                resultSawMark.warnInfo = null;
                resultSawMark.sawMarkInfos = null;
                resultSawMark.sawMarkShowDatas = null;
                resultSawMark = null;

                resultThickness.errorInfo = null;
                resultThickness.warnInfo = null;
                resultThickness.thicknessData = null;
                resultThickness = null;

                resultWarp.errorInfo = null;
                resultWarp.warnInfo = null;
                resultWarp.warpData = null;
                resultWarp = null;

                calResultTable.Remove(nameThreadSawMark);
                calResultTable.Remove(nameThreadThickness);
                calResultTable.Remove(nameThreadWarp);

                #endregion

                calResultMutex.ReleaseMutex();


                #endregion

                System.DateTime aaEnd = new System.DateTime();
                aaEnd = System.DateTime.Now;

                TimeSpan aaSpan;
                aaSpan = aaEnd.Subtract(aaStart);
                double aaMiniS = aaSpan.TotalMilliseconds;

                return true;
            }
            catch (Exception excep)
            {
                results.ValidResult = ValidType.CalError;
                //LogHelper.ErrorLog("SawMarkAlgError: 计算发生未知错误; error at CalProcess", excep);
                return false;
            }
        }

        private double GetRegressSawmark(double dblSawMark, double dblTTV, double dbMaxMin, int nType)
        {
            return dblSawMark;
            //if (dblSawMark < 1.0 || dblTTV < 1.0 || dbMaxMin < 1.0)
            //    return dblSawMark;
            //if (nType == 1)
            //{
            //    //密线的系数
            //    const double dbCoef1 = 3.922892461;
            //    const double dbCoef2 = 1.111257907;
            //    const double dbCoef3 = -0.026012897;
            //    const double dbCoef4 = 0.031345993;


            //    double dbRegressSM = dbCoef1 + dbCoef2 * dblSawMark + dbCoef3 * dblTTV + dbCoef4 * dbMaxMin;
            //    if (dbRegressSM <= 1.0 || dbRegressSM >= 300.0)
            //        return dblSawMark;
            //    else
            //        return dbRegressSM > dbMaxMin ? dbMaxMin : dbRegressSM;
            //}
            //else
            //{
            //    //单一线痕的系数
            //    const double dbCoef1 = 1.289359832;
            //    const double dbCoef2 = 1.047318196;
            //    const double dbCoef3 = 0.008967796;
            //    const double dbCoef4 = 0.085059931;
            //    double dbRegressSM = dbCoef1 + dbCoef2 * dblSawMark + dbCoef3 * dblTTV + dbCoef4 * dbMaxMin;
            //    //特殊处理
            //    if (dblSawMark > 6.5 && dblTTV < 20.0 && dbMaxMin / dblSawMark > 3.5)
            //        dbRegressSM += 4.0;

            //    if (dbRegressSM <= 1.0 || dbRegressSM >= 300.0)
            //        return dblSawMark;
            //    else
            //        return dbRegressSM > dbMaxMin ? dbMaxMin : dbRegressSM;
            //}

         }

        public void CalMonitoring()
        {
            while (true)
            {
                if (brokenPiecesCount > algoParams.MonitorLimitForBrokenPieces)
                {
                    brokenPiecesCount = 0;
                }
                if (overlapPiecesCount > algoParams.MonitorLimitForOverlapPieces)
                {
                    brokenPiecesCount = 0;
                }

                Thread.Sleep(1000);
            }
        }
        public string GetAbout()
        {
            return "V" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }



        #region my private 函数
        private void ThreadSawMarkCalCore(object caldataIn)
        {
            ThreadSawMarkResult resultTemp;

            ThreadCalDataIn calDatasSawMark = (ThreadCalDataIn)caldataIn;
            string threadName = calDatasSawMark.name;
            double[,] probeDatas = calDatasSawMark.dataIn;

            try
            {
                #region step1:获取输入

                calResultMutex.WaitOne();
                if ((null == calResultTable) || (null == threadName))
                {
                    calResultMutex.ReleaseMutex();
                    return;
                }

                if (!calResultTable.ContainsKey(threadName))
                {
                    calResultMutex.ReleaseMutex();
                    return;
                }

                resultTemp = (ThreadSawMarkResult)calResultTable[threadName];
                int numSawMarkMax = resultTemp.sawMarkInfos.GetLength(0);
                int numShowDataLen = resultTemp.sawMarkShowDatas.GetLength(1);

                calResultMutex.ReleaseMutex();

                #endregion

                #region step2:调用计算核函数

                int dataLen = probeDatas.GetLength(1);
                int probeNum = probeDatas.GetLength(0);

                int errorFlag = 0;
                StringBuilder errorInfo = new StringBuilder(1024);
                bool warnFlag = false;
                StringBuilder warnInfo = new StringBuilder(2048);

                double[,] sawMarkInfos = new double[numSawMarkMax, 7];
                int sawMarkNumOut = 0;

                double[,] sawMarkShowDatas = new double[probeNum, numShowDataLen];

                System.DateTime startTime = new System.DateTime();
                startTime = System.DateTime.Now;

                if (!SawMarkCalProcess(probeDatas, dataLen, probeNum, ref errorFlag, errorInfo, errorInfo.Capacity, ref warnFlag, warnInfo, warnInfo.Capacity,
                    sawMarkInfos, sawMarkInfos.GetLength(0), sawMarkInfos.GetLength(1), ref sawMarkNumOut, sawMarkShowDatas, sawMarkShowDatas.GetLength(1)))
                {
                    calResultMutex.WaitOne();
                    resultTemp = (ThreadSawMarkResult)calResultTable[threadName];
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 线痕计算发生错误; error at ThreadSawMarkCalCore" + ",threadName:" + threadName + "errorInfo:" + errorInfo.ToString();
                    calResultTable[threadName] = resultTemp;
                    calResultMutex.ReleaseMutex();

                    return;
                }

                System.DateTime currentTime = new System.DateTime();
                currentTime = System.DateTime.Now;
                TimeSpan ts = currentTime.Subtract(startTime);


                #endregion

                #region step3: 复制结果

                calResultMutex.WaitOne();

                resultTemp.errorFlag = errorFlag;
                resultTemp.errorInfo = errorInfo.ToString();
                resultTemp.warnFlag = warnFlag;
                resultTemp.warnInfo = warnInfo.ToString();
                resultTemp.time = ts.TotalMilliseconds;

                if (sawMarkNumOut <= 0)
                {
                    resultTemp.sawMarkNum = 0;
                }
                else
                {
                    resultTemp.sawMarkNum = sawMarkNumOut;
                    for (int nk = 0; nk < sawMarkNumOut; nk++)
                    {
                        resultTemp.sawMarkInfos[nk].probeIndex = Convert.ToInt32(sawMarkInfos[nk, 0]);
                        resultTemp.sawMarkInfos[nk].type = Convert.ToInt32(sawMarkInfos[nk, 1]);
                        resultTemp.sawMarkInfos[nk].peakPos = sawMarkInfos[nk, 2];
                        resultTemp.sawMarkInfos[nk].peakValue = sawMarkInfos[nk, 3];
                        resultTemp.sawMarkInfos[nk].valleyPos = sawMarkInfos[nk, 4];
                        resultTemp.sawMarkInfos[nk].valleyValue = sawMarkInfos[nk, 5];
                        resultTemp.sawMarkInfos[nk].depthMax = sawMarkInfos[nk, 6];
                    }
                }
                for (int nk = 0; nk < probeNum; nk++)
                {
                    for (int nj = 0; nj < numShowDataLen; nj++)
                    {
                        resultTemp.sawMarkShowDatas[nk, nj] = sawMarkShowDatas[nk, nj];
                    }
                }

                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();

                #endregion
            }
            catch (Exception excep)
            {
                calResultMutex.WaitOne();
                resultTemp = (ThreadSawMarkResult)calResultTable[threadName];
                resultTemp.errorFlag = 1;
                resultTemp.errorInfo = "SawMarkAlgError: 线痕计算发生未知错误; error at ThreadSawMarkCalCore" + ",threadName:" + threadName;
                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();

                return;
            }
        }
        private void ThreadSawMarkCalCoreMultiThread(object caldataIn)
        {
            ThreadSawMarkResult resultTemp;

            ThreadCalDataIn calDatasSawMark = (ThreadCalDataIn)caldataIn;
            string threadName = calDatasSawMark.name;
            double[,] probeDatas = calDatasSawMark.dataIn;

            try
            {
                #region step1:获取输入

                calResultMutex.WaitOne();
                if ((null == calResultTable) || (null == threadName))
                {
                    calResultMutex.ReleaseMutex();
                    return;
                }

                if (!calResultTable.ContainsKey(threadName))
                {
                    calResultMutex.ReleaseMutex();
                    return;
                }

                resultTemp = (ThreadSawMarkResult)calResultTable[threadName];
                int numSawMarkMax = resultTemp.sawMarkInfos.GetLength(0);
                int numShowDataLen = resultTemp.sawMarkShowDatas.GetLength(1);

                calResultMutex.ReleaseMutex();

                #endregion

                #region step2: 根据探头数分多个线程分别调计算核函数

                int dataLen = probeDatas.GetLength(1);
                int probeNum = probeDatas.GetLength(0);

                System.DateTime startTime = new System.DateTime();
                startTime = System.DateTime.Now;

                #region step2.1 探头0 开线程

                string nameThreadSawMark_Probe_0 = "SawMark_Probe_0" + startTime.ToString("HH_mm_ss_fff");
                ThreadSawMarkResult resultSawMark_Probe_0 = new ThreadSawMarkResult(numSawMarkMax / 6, probeNum, numShowDataLen);
                Thread threadCalSawMark_Probe_0 = null;

                if (calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_0))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: ThreadSawMarkCalCore内部出错,重复开线程：" + threadName;
                    return;
                }
                calResultMutexSawMark.WaitOne();
                calResultTableSawMark.Add(nameThreadSawMark_Probe_0, resultSawMark_Probe_0);
                calResultMutexSawMark.ReleaseMutex();

                threadCalSawMark_Probe_0 = new Thread(new ParameterizedThreadStart(ThreadSawMarkCalCoreForOneProbe));
                ThreadCalDataSawMarkIn dataInSawMark_Probe_0 = new ThreadCalDataSawMarkIn();
                dataInSawMark_Probe_0.dataIn = probeDatas;
                dataInSawMark_Probe_0.name = nameThreadSawMark_Probe_0;
                dataInSawMark_Probe_0.probeIndex = 0;

                threadCalSawMark_Probe_0.Name = nameThreadSawMark_Probe_0;
                threadCalSawMark_Probe_0.Start((object)dataInSawMark_Probe_0);

                currentThreadSawMark.Add(threadCalSawMark_Probe_0);

                #endregion

                #region step2.2 探头1 开线程

                string nameThreadSawMark_Probe_1 = "SawMark_Probe_1" + startTime.ToString("HH_mm_ss_fff");
                ThreadSawMarkResult resultSawMark_Probe_1 = new ThreadSawMarkResult(numSawMarkMax / 6, probeNum, numShowDataLen);
                Thread threadCalSawMark_Probe_1 = null;

                if (calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_1))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: ThreadSawMarkCalCore内部出错,重复开线程：" + threadName;
                    return;
                }
                calResultMutexSawMark.WaitOne();
                calResultTableSawMark.Add(nameThreadSawMark_Probe_1, resultSawMark_Probe_1);
                calResultMutexSawMark.ReleaseMutex();

                threadCalSawMark_Probe_1 = new Thread(new ParameterizedThreadStart(ThreadSawMarkCalCoreForOneProbe));
                ThreadCalDataSawMarkIn dataInSawMark_Probe_1 = new ThreadCalDataSawMarkIn();
                dataInSawMark_Probe_1.dataIn = probeDatas;
                dataInSawMark_Probe_1.name = nameThreadSawMark_Probe_1;
                dataInSawMark_Probe_1.probeIndex = 1;

                threadCalSawMark_Probe_1.Name = nameThreadSawMark_Probe_1;
                threadCalSawMark_Probe_1.Start((object)dataInSawMark_Probe_1);

                currentThreadSawMark.Add(threadCalSawMark_Probe_1);

                #endregion

                #region step2.3 探头2 开线程

                string nameThreadSawMark_Probe_2 = "SawMark_Probe_2" + startTime.ToString("HH_mm_ss_fff");
                ThreadSawMarkResult resultSawMark_Probe_2 = new ThreadSawMarkResult(numSawMarkMax / 6, probeNum, numShowDataLen);
                Thread threadCalSawMark_Probe_2 = null;

                if (calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_2))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: ThreadSawMarkCalCore内部出错,重复开线程：" + threadName;
                    return;
                }
                calResultMutexSawMark.WaitOne();
                calResultTableSawMark.Add(nameThreadSawMark_Probe_2, resultSawMark_Probe_2);
                calResultMutexSawMark.ReleaseMutex();

                threadCalSawMark_Probe_2 = new Thread(new ParameterizedThreadStart(ThreadSawMarkCalCoreForOneProbe));
                ThreadCalDataSawMarkIn dataInSawMark_Probe_2 = new ThreadCalDataSawMarkIn();
                dataInSawMark_Probe_2.dataIn = probeDatas;
                dataInSawMark_Probe_2.name = nameThreadSawMark_Probe_2;
                dataInSawMark_Probe_2.probeIndex = 2;

                threadCalSawMark_Probe_2.Name = nameThreadSawMark_Probe_2;
                threadCalSawMark_Probe_2.Start((object)dataInSawMark_Probe_2);

                currentThreadSawMark.Add(threadCalSawMark_Probe_2);

                #endregion

                #region step2.4 探头3 开线程

                string nameThreadSawMark_Probe_3 = "SawMark_Probe_3" + startTime.ToString("HH_mm_ss_fff");
                ThreadSawMarkResult resultSawMark_Probe_3 = new ThreadSawMarkResult(numSawMarkMax / 6, probeNum, numShowDataLen);
                Thread threadCalSawMark_Probe_3 = null;

                if (calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_3))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: ThreadSawMarkCalCore内部出错,重复开线程：" + threadName;
                    return;
                }
                calResultMutexSawMark.WaitOne();
                calResultTableSawMark.Add(nameThreadSawMark_Probe_3, resultSawMark_Probe_3);
                calResultMutexSawMark.ReleaseMutex();

                threadCalSawMark_Probe_3 = new Thread(new ParameterizedThreadStart(ThreadSawMarkCalCoreForOneProbe));
                ThreadCalDataSawMarkIn dataInSawMark_Probe_3 = new ThreadCalDataSawMarkIn();
                dataInSawMark_Probe_3.dataIn = probeDatas;
                dataInSawMark_Probe_3.name = nameThreadSawMark_Probe_3;
                dataInSawMark_Probe_3.probeIndex = 3;

                threadCalSawMark_Probe_3.Name = nameThreadSawMark_Probe_3;
                threadCalSawMark_Probe_3.Start((object)dataInSawMark_Probe_3);

                currentThreadSawMark.Add(threadCalSawMark_Probe_3);

                #endregion

                #region step2.5 探头4 开线程

                string nameThreadSawMark_Probe_4 = "SawMark_Probe_4" + startTime.ToString("HH_mm_ss_fff");
                ThreadSawMarkResult resultSawMark_Probe_4 = new ThreadSawMarkResult(numSawMarkMax / 6, probeNum, numShowDataLen);
                Thread threadCalSawMark_Probe_4 = null;

                if (calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_4))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: ThreadSawMarkCalCore内部出错,重复开线程：" + threadName;
                    return;
                }
                calResultMutexSawMark.WaitOne();
                calResultTableSawMark.Add(nameThreadSawMark_Probe_4, resultSawMark_Probe_4);
                calResultMutexSawMark.ReleaseMutex();

                threadCalSawMark_Probe_4 = new Thread(new ParameterizedThreadStart(ThreadSawMarkCalCoreForOneProbe));
                ThreadCalDataSawMarkIn dataInSawMark_Probe_4 = new ThreadCalDataSawMarkIn();
                dataInSawMark_Probe_4.dataIn = probeDatas;
                dataInSawMark_Probe_4.name = nameThreadSawMark_Probe_4;
                dataInSawMark_Probe_4.probeIndex = 4;

                threadCalSawMark_Probe_4.Name = nameThreadSawMark_Probe_4;
                threadCalSawMark_Probe_4.Start((object)dataInSawMark_Probe_4);

                currentThreadSawMark.Add(threadCalSawMark_Probe_4);

                #endregion

                #region step2.6 探头5 开线程

                string nameThreadSawMark_Probe_5 = "SawMark_Probe_5" + startTime.ToString("HH_mm_ss_fff");
                ThreadSawMarkResult resultSawMark_Probe_5 = new ThreadSawMarkResult(numSawMarkMax / 6, probeNum, numShowDataLen);
                Thread threadCalSawMark_Probe_5 = null;

                if (calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_5))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: ThreadSawMarkCalCore内部出错,重复开线程：" + threadName;
                    return;
                }
                calResultMutexSawMark.WaitOne();
                calResultTableSawMark.Add(nameThreadSawMark_Probe_5, resultSawMark_Probe_5);
                calResultMutexSawMark.ReleaseMutex();

                threadCalSawMark_Probe_5 = new Thread(new ParameterizedThreadStart(ThreadSawMarkCalCoreForOneProbe));
                ThreadCalDataSawMarkIn dataInSawMark_Probe_5 = new ThreadCalDataSawMarkIn();
                dataInSawMark_Probe_5.dataIn = probeDatas;
                dataInSawMark_Probe_5.name = nameThreadSawMark_Probe_5;
                dataInSawMark_Probe_5.probeIndex = 5;

                threadCalSawMark_Probe_5.Name = nameThreadSawMark_Probe_5;
                threadCalSawMark_Probe_5.Start((object)dataInSawMark_Probe_5);

                currentThreadSawMark.Add(threadCalSawMark_Probe_5);

                #endregion

                #region step2.7 while循环

                System.DateTime currentTime = new System.DateTime();
                TimeSpan ts;

                while (true)
                {
                    bool threadSawMark_Probe_0_Running = true;
                    bool threadSawMark_Probe_1_Running = true;
                    bool threadSawMark_Probe_2_Running = true;
                    bool threadSawMark_Probe_3_Running = true;
                    bool threadSawMark_Probe_4_Running = true;
                    bool threadSawMark_Probe_5_Running = true;

                    Thread temp;
                    temp = currentThreadSawMark.Find(b => b.Name == nameThreadSawMark_Probe_0);
                    if (temp == null)
                    {
                        threadSawMark_Probe_0_Running = false;
                        threadCalSawMark_Probe_0 = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadSawMark_Probe_0_Running = false;
                        threadCalSawMark_Probe_0 = null;
                        currentThreadSawMark.Remove(temp);
                    }

                    temp = currentThreadSawMark.Find(b => b.Name == nameThreadSawMark_Probe_1);
                    if (temp == null)
                    {
                        threadSawMark_Probe_1_Running = false;
                        threadCalSawMark_Probe_1 = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadSawMark_Probe_1_Running = false;
                        threadCalSawMark_Probe_1 = null;
                        currentThreadSawMark.Remove(temp);
                    }

                    temp = currentThreadSawMark.Find(b => b.Name == nameThreadSawMark_Probe_2);
                    if (temp == null)
                    {
                        threadSawMark_Probe_2_Running = false;
                        threadCalSawMark_Probe_2 = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadSawMark_Probe_2_Running = false;
                        threadCalSawMark_Probe_2 = null;
                        currentThreadSawMark.Remove(temp);
                    }

                    temp = currentThreadSawMark.Find(b => b.Name == nameThreadSawMark_Probe_3);
                    if (temp == null)
                    {
                        threadSawMark_Probe_3_Running = false;
                        threadCalSawMark_Probe_3 = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadSawMark_Probe_3_Running = false;
                        threadCalSawMark_Probe_3 = null;
                        currentThreadSawMark.Remove(temp);
                    }

                    temp = currentThreadSawMark.Find(b => b.Name == nameThreadSawMark_Probe_4);
                    if (temp == null)
                    {
                        threadSawMark_Probe_4_Running = false;
                        threadCalSawMark_Probe_4 = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadSawMark_Probe_4_Running = false;
                        threadCalSawMark_Probe_4 = null;
                        currentThreadSawMark.Remove(temp);
                    }

                    temp = currentThreadSawMark.Find(b => b.Name == nameThreadSawMark_Probe_5);
                    if (temp == null)
                    {
                        threadSawMark_Probe_5_Running = false;
                        threadCalSawMark_Probe_5 = null;
                    }
                    else if (!temp.IsAlive)
                    {
                        threadSawMark_Probe_5_Running = false;
                        threadCalSawMark_Probe_5 = null;
                        currentThreadSawMark.Remove(temp);
                    }

                    if (threadSawMark_Probe_0_Running || threadSawMark_Probe_1_Running || threadSawMark_Probe_2_Running || threadSawMark_Probe_3_Running || threadSawMark_Probe_4_Running || threadSawMark_Probe_5_Running)
                    {
                        currentTime = System.DateTime.Now;
                        ts = currentTime.Subtract(startTime);
                        if (ts.TotalMilliseconds > 10000)
                        {
                            //if (threadSawMark_Probe_0_Running)
                            //{
                            //    threadCalSawMark_Probe_0.Abort();
                            //}
                            //if (threadSawMark_Probe_1_Running)
                            //{
                            //    threadCalSawMark_Probe_1.Abort();
                            //}
                            //if (threadSawMark_Probe_2_Running)
                            //{
                            //    threadCalSawMark_Probe_2.Abort();
                            //}
                            //if (threadSawMark_Probe_3_Running)
                            //{
                            //    threadCalSawMark_Probe_3.Abort();
                            //}
                            //if (threadSawMark_Probe_4_Running)
                            //{
                            //    threadCalSawMark_Probe_4.Abort();
                            //}
                            //if (threadSawMark_Probe_5_Running)
                            //{
                            //    threadCalSawMark_Probe_5.Abort();
                            //}

                            //calResultMutex.WaitOne();
                            //resultTemp = (ThreadSawMarkResult)calResultTable[threadName];
                            //resultTemp.errorFlag = 1;
                            //resultTemp.errorInfo = "SawMarkAlgError: 线痕计算超时; time out at ThreadSawMarkCalCoreMultiThread" + ",threadName:" + threadName;
                            //calResultTable[threadName] = resultTemp;
                            //calResultMutex.ReleaseMutex();

                            //return;

                            Thread.Sleep(50);
                        }
                        else
                        {
                            Thread.Sleep(50);
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                #endregion

                #region step2.8 读出结果

                calResultMutex.WaitOne();

                calResultMutexSawMark.WaitOne();

                try
                {
                    if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_0))
                    {
                        resultTemp.errorFlag = 1;
                        resultTemp.errorInfo = "SawMarkAlgError: 取线痕计算结果时线程不存在,thread name：" + threadName;
                        return;
                    }
                    if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_1))
                    {
                        resultTemp.errorFlag = 1;
                        resultTemp.errorInfo = "SawMarkAlgError: 取线痕计算结果时线程不存在,thread name：" + threadName;
                        return;
                    }
                    if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_2))
                    {
                        resultTemp.errorFlag = 1;
                        resultTemp.errorInfo = "SawMarkAlgError: 取线痕计算结果时线程不存在,thread name：" + threadName;
                        return;
                    }
                    if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_3))
                    {
                        resultTemp.errorFlag = 1;
                        resultTemp.errorInfo = "SawMarkAlgError: 取线痕计算结果时线程不存在,thread name：" + threadName;
                        return;
                    }
                    if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_4))
                    {
                        resultTemp.errorFlag = 1;
                        resultTemp.errorInfo = "SawMarkAlgError: 取线痕计算结果时线程不存在,thread name：" + threadName;
                        return;
                    }
                    if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_5))
                    {
                        resultTemp.errorFlag = 1;
                        resultTemp.errorInfo = "SawMarkAlgError: 取线痕计算结果时线程不存在,thread name：" + threadName;
                        return;
                    }
                    resultSawMark_Probe_0 = (ThreadSawMarkResult)calResultTableSawMark[nameThreadSawMark_Probe_0];
                    resultSawMark_Probe_1 = (ThreadSawMarkResult)calResultTableSawMark[nameThreadSawMark_Probe_1];
                    resultSawMark_Probe_2 = (ThreadSawMarkResult)calResultTableSawMark[nameThreadSawMark_Probe_2];
                    resultSawMark_Probe_3 = (ThreadSawMarkResult)calResultTableSawMark[nameThreadSawMark_Probe_3];
                    resultSawMark_Probe_4 = (ThreadSawMarkResult)calResultTableSawMark[nameThreadSawMark_Probe_4];
                    resultSawMark_Probe_5 = (ThreadSawMarkResult)calResultTableSawMark[nameThreadSawMark_Probe_5];

                    #region step2.8.1 warnInfo warnFlag 

                    if (resultSawMark_Probe_0.warnFlag)
                    {
                        resultTemp.warnFlag = resultSawMark_Probe_0.warnFlag;
                        resultTemp.warnInfo = resultTemp.warnInfo + "warnInfo of probe1:" + resultSawMark_Probe_0.warnInfo;
                    }
                    if (resultSawMark_Probe_1.warnFlag)
                    {
                        resultTemp.warnFlag = resultSawMark_Probe_1.warnFlag;
                        resultTemp.warnInfo = resultTemp.warnInfo + "warnInfo of probe2:" + resultSawMark_Probe_1.warnInfo;
                    }
                    if (resultSawMark_Probe_2.warnFlag)
                    {
                        resultTemp.warnFlag = resultSawMark_Probe_2.warnFlag;
                        resultTemp.warnInfo = resultTemp.warnInfo + "warnInfo of probe3:" + resultSawMark_Probe_2.warnInfo;
                    }
                    if (resultSawMark_Probe_3.warnFlag)
                    {
                        resultTemp.warnFlag = resultSawMark_Probe_3.warnFlag;
                        resultTemp.warnInfo = resultTemp.warnInfo + "warnInfo of probe4:" + resultSawMark_Probe_3.warnInfo;
                    }
                    if (resultSawMark_Probe_4.warnFlag)
                    {
                        resultTemp.warnFlag = resultSawMark_Probe_4.warnFlag;
                        resultTemp.warnInfo = resultTemp.warnInfo + "warnInfo of probe4:" + resultSawMark_Probe_4.warnInfo;
                    }
                    if (resultSawMark_Probe_5.warnFlag)
                    {
                        resultTemp.warnFlag = resultSawMark_Probe_5.warnFlag;
                        resultTemp.warnInfo = resultTemp.warnInfo + "warnInfo of probe6:" + resultSawMark_Probe_5.warnInfo;
                    }

                    #endregion

                    #region step2.8.2 errorInfo errorFlag 

                    if (0 != resultSawMark_Probe_0.errorFlag)
                    {
                        resultTemp.errorFlag = resultSawMark_Probe_0.errorFlag;
                        resultTemp.errorInfo = resultTemp.errorInfo + "errorInfo of probe1:" + resultSawMark_Probe_0.errorInfo;
                    }
                    if (0 != resultSawMark_Probe_1.errorFlag)
                    {
                        resultTemp.errorFlag = resultSawMark_Probe_1.errorFlag;
                        resultTemp.errorInfo = resultTemp.errorInfo + "errorInfo of probe2:" + resultSawMark_Probe_1.errorInfo;
                    }
                    if (0 != resultSawMark_Probe_2.errorFlag)
                    {
                        resultTemp.errorFlag = resultSawMark_Probe_2.errorFlag;
                        resultTemp.errorInfo = resultTemp.errorInfo + "errorInfo of probe3:" + resultSawMark_Probe_2.errorInfo;
                    }
                    if (0 != resultSawMark_Probe_3.errorFlag)
                    {
                        resultTemp.errorFlag = resultSawMark_Probe_3.errorFlag;
                        resultTemp.errorInfo = resultTemp.errorInfo + "errorInfo of probe4:" + resultSawMark_Probe_3.errorInfo;
                    }
                    if (0 != resultSawMark_Probe_4.errorFlag)
                    {
                        resultTemp.errorFlag = resultSawMark_Probe_4.errorFlag;
                        resultTemp.errorInfo = resultTemp.errorInfo + "errorInfo of probe5:" + resultSawMark_Probe_4.errorInfo;
                    }
                    if (0 != resultSawMark_Probe_5.errorFlag)
                    {
                        resultTemp.errorFlag = resultSawMark_Probe_5.errorFlag;
                        resultTemp.errorInfo = resultTemp.errorInfo + "errorInfo of probe6:" + resultSawMark_Probe_5.errorInfo;
                    }

                    #endregion

                    #region step2.8.3 time

                    currentTime = System.DateTime.Now;
                    ts = currentTime.Subtract(startTime);
                    resultTemp.time = ts.TotalMilliseconds;

                    #endregion

                    #region step2.8.4 sawMarkNum sawMarkInfo

                    if (resultSawMark_Probe_0.sawMarkNum > 0)
                    {
                        for (int nk = 0; nk < resultSawMark_Probe_0.sawMarkNum; nk++)
                        {
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].probeIndex = resultSawMark_Probe_0.sawMarkInfos[nk].probeIndex;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].type = resultSawMark_Probe_0.sawMarkInfos[nk].type;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakPos = resultSawMark_Probe_0.sawMarkInfos[nk].peakPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakValue = resultSawMark_Probe_0.sawMarkInfos[nk].peakValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyPos = resultSawMark_Probe_0.sawMarkInfos[nk].valleyPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyValue = resultSawMark_Probe_0.sawMarkInfos[nk].valleyValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].depthMax = resultSawMark_Probe_0.sawMarkInfos[nk].depthMax;
                        }
                        resultTemp.sawMarkNum = resultTemp.sawMarkNum + resultSawMark_Probe_0.sawMarkNum;
                    }
                    if (resultSawMark_Probe_1.sawMarkNum > 0)
                    {
                        for (int nk = 0; nk < resultSawMark_Probe_1.sawMarkNum; nk++)
                        {
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].probeIndex = resultSawMark_Probe_1.sawMarkInfos[nk].probeIndex;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].type = resultSawMark_Probe_1.sawMarkInfos[nk].type;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakPos = resultSawMark_Probe_1.sawMarkInfos[nk].peakPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakValue = resultSawMark_Probe_1.sawMarkInfos[nk].peakValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyPos = resultSawMark_Probe_1.sawMarkInfos[nk].valleyPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyValue = resultSawMark_Probe_1.sawMarkInfos[nk].valleyValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].depthMax = resultSawMark_Probe_1.sawMarkInfos[nk].depthMax;
                        }
                        resultTemp.sawMarkNum = resultTemp.sawMarkNum + resultSawMark_Probe_1.sawMarkNum;
                    }
                    if (resultSawMark_Probe_2.sawMarkNum > 0)
                    {
                        for (int nk = 0; nk < resultSawMark_Probe_2.sawMarkNum; nk++)
                        {
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].probeIndex = resultSawMark_Probe_2.sawMarkInfos[nk].probeIndex;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].type = resultSawMark_Probe_2.sawMarkInfos[nk].type;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakPos = resultSawMark_Probe_2.sawMarkInfos[nk].peakPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakValue = resultSawMark_Probe_2.sawMarkInfos[nk].peakValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyPos = resultSawMark_Probe_2.sawMarkInfos[nk].valleyPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyValue = resultSawMark_Probe_2.sawMarkInfos[nk].valleyValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].depthMax = resultSawMark_Probe_2.sawMarkInfos[nk].depthMax;
                        }
                        resultTemp.sawMarkNum = resultTemp.sawMarkNum + resultSawMark_Probe_2.sawMarkNum;
                    }
                    if (resultSawMark_Probe_3.sawMarkNum > 0)
                    {
                        for (int nk = 0; nk < resultSawMark_Probe_3.sawMarkNum; nk++)
                        {
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].probeIndex = resultSawMark_Probe_3.sawMarkInfos[nk].probeIndex;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].type = resultSawMark_Probe_3.sawMarkInfos[nk].type;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakPos = resultSawMark_Probe_3.sawMarkInfos[nk].peakPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakValue = resultSawMark_Probe_3.sawMarkInfos[nk].peakValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyPos = resultSawMark_Probe_3.sawMarkInfos[nk].valleyPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyValue = resultSawMark_Probe_3.sawMarkInfos[nk].valleyValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].depthMax = resultSawMark_Probe_3.sawMarkInfos[nk].depthMax;
                        }
                        resultTemp.sawMarkNum = resultTemp.sawMarkNum + resultSawMark_Probe_3.sawMarkNum;
                    }
                    if (resultSawMark_Probe_4.sawMarkNum > 0)
                    {
                        for (int nk = 0; nk < resultSawMark_Probe_4.sawMarkNum; nk++)
                        {
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].probeIndex = resultSawMark_Probe_4.sawMarkInfos[nk].probeIndex;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].type = resultSawMark_Probe_4.sawMarkInfos[nk].type;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakPos = resultSawMark_Probe_4.sawMarkInfos[nk].peakPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakValue = resultSawMark_Probe_4.sawMarkInfos[nk].peakValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyPos = resultSawMark_Probe_4.sawMarkInfos[nk].valleyPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyValue = resultSawMark_Probe_4.sawMarkInfos[nk].valleyValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].depthMax = resultSawMark_Probe_4.sawMarkInfos[nk].depthMax;
                        }
                        resultTemp.sawMarkNum = resultTemp.sawMarkNum + resultSawMark_Probe_4.sawMarkNum;
                    }
                    if (resultSawMark_Probe_5.sawMarkNum > 0)
                    {
                        for (int nk = 0; nk < resultSawMark_Probe_5.sawMarkNum; nk++)
                        {
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].probeIndex = resultSawMark_Probe_5.sawMarkInfos[nk].probeIndex;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].type = resultSawMark_Probe_5.sawMarkInfos[nk].type;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakPos = resultSawMark_Probe_5.sawMarkInfos[nk].peakPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].peakValue = resultSawMark_Probe_5.sawMarkInfos[nk].peakValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyPos = resultSawMark_Probe_5.sawMarkInfos[nk].valleyPos;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].valleyValue = resultSawMark_Probe_5.sawMarkInfos[nk].valleyValue;
                            resultTemp.sawMarkInfos[resultTemp.sawMarkNum + nk].depthMax = resultSawMark_Probe_5.sawMarkInfos[nk].depthMax;
                        }
                        resultTemp.sawMarkNum = resultTemp.sawMarkNum + resultSawMark_Probe_5.sawMarkNum;
                    }

                    #endregion

                    #region step2.8.5 showData

                    for (int nj = 0; nj < numShowDataLen; nj++)
                    {
                        resultTemp.sawMarkShowDatas[0, nj] = resultSawMark_Probe_0.sawMarkShowDatas[0, nj];
                        resultTemp.sawMarkShowDatas[1, nj] = resultSawMark_Probe_1.sawMarkShowDatas[1, nj];
                        resultTemp.sawMarkShowDatas[2, nj] = resultSawMark_Probe_2.sawMarkShowDatas[2, nj];
                        resultTemp.sawMarkShowDatas[3, nj] = resultSawMark_Probe_3.sawMarkShowDatas[3, nj];
                        resultTemp.sawMarkShowDatas[4, nj] = resultSawMark_Probe_4.sawMarkShowDatas[4, nj];
                        resultTemp.sawMarkShowDatas[5, nj] = resultSawMark_Probe_5.sawMarkShowDatas[5, nj];
                    }

                    #endregion

                }
                catch (Exception ex)
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 计算发生未知错误; error at ThreadSawMarkCalCoreMultiThread" + ex.ToString();
                }

                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();


                #region step2.8.6 remove

                if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_0))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 移除线痕计算结果时线程不存在,thread name：" + threadName;
                    return;
                }
                if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_1))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 移除线痕计算结果时线程不存在,thread name：" + threadName;
                    return;
                }
                if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_2))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 移除线痕计算结果时线程不存在,thread name：" + threadName;
                    return;
                }
                if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_3))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 移除线痕计算结果时线程不存在,thread name：" + threadName;
                    return;
                }
                if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_4))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 移除线痕计算结果时线程不存在,thread name：" + threadName;
                    return;
                }
                if (!calResultTableSawMark.ContainsKey(nameThreadSawMark_Probe_5))
                {
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 移除线痕计算结果时线程不存在,thread name：" + threadName;
                    return;
                }

                resultSawMark_Probe_0.errorInfo = null;
                resultSawMark_Probe_0.warnInfo = null;
                resultSawMark_Probe_0.sawMarkInfos = null;
                resultSawMark_Probe_0.sawMarkShowDatas = null;
                resultSawMark_Probe_0 = null;

                resultSawMark_Probe_1.errorInfo = null;
                resultSawMark_Probe_1.warnInfo = null;
                resultSawMark_Probe_1.sawMarkInfos = null;
                resultSawMark_Probe_1.sawMarkShowDatas = null;
                resultSawMark_Probe_1 = null;

                resultSawMark_Probe_2.errorInfo = null;
                resultSawMark_Probe_2.warnInfo = null;
                resultSawMark_Probe_2.sawMarkInfos = null;
                resultSawMark_Probe_2.sawMarkShowDatas = null;
                resultSawMark_Probe_2 = null;

                resultSawMark_Probe_3.errorInfo = null;
                resultSawMark_Probe_3.warnInfo = null;
                resultSawMark_Probe_3.sawMarkInfos = null;
                resultSawMark_Probe_3.sawMarkShowDatas = null;
                resultSawMark_Probe_3 = null;

                resultSawMark_Probe_4.errorInfo = null;
                resultSawMark_Probe_4.warnInfo = null;
                resultSawMark_Probe_4.sawMarkInfos = null;
                resultSawMark_Probe_4.sawMarkShowDatas = null;
                resultSawMark_Probe_4 = null;

                resultSawMark_Probe_5.errorInfo = null;
                resultSawMark_Probe_5.warnInfo = null;
                resultSawMark_Probe_5.sawMarkInfos = null;
                resultSawMark_Probe_5.sawMarkShowDatas = null;
                resultSawMark_Probe_5 = null;

                calResultTableSawMark.Remove(nameThreadSawMark_Probe_0);
                calResultTableSawMark.Remove(nameThreadSawMark_Probe_1);
                calResultTableSawMark.Remove(nameThreadSawMark_Probe_2);
                calResultTableSawMark.Remove(nameThreadSawMark_Probe_3);
                calResultTableSawMark.Remove(nameThreadSawMark_Probe_4);
                calResultTableSawMark.Remove(nameThreadSawMark_Probe_5);

                #endregion

                calResultMutexSawMark.ReleaseMutex();

                #endregion

                #endregion

                return;
            }
            catch (Exception excep)
            {
                calResultMutex.WaitOne();
                resultTemp = (ThreadSawMarkResult)calResultTable[threadName];
                resultTemp.errorFlag = 1;
                resultTemp.errorInfo = "SawMarkAlgError: 线痕计算发生未知错误; error at ThreadSawMarkCalCoreMultiThread" + ",threadName:" + threadName;
                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();

                return;
            }
        }
        private void ThreadSawMarkCalCoreForOneProbe(object caldataIn)
        {
            ThreadSawMarkResult resultTemp;

            ThreadCalDataSawMarkIn calDatasSawMark = (ThreadCalDataSawMarkIn)caldataIn;
            string threadName = calDatasSawMark.name;
            double[,] probeDatas = calDatasSawMark.dataIn;
            int probeIndex = calDatasSawMark.probeIndex;

            try
            {
                #region step1:获取输入

                calResultMutexSawMark.WaitOne();
                if ((null == calResultTableSawMark) || (null == threadName))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: ThreadSawMarkCalCoreForOneProbe内部出错,error at ThreadSawMarkCalCoreForOneProbe,illegal calResultTableSawMark or threadName", null);
                    calResultMutexSawMark.ReleaseMutex();
                    return;
                }

                if (!calResultTableSawMark.ContainsKey(threadName))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: ThreadSawMarkCalCoreForOneProbe内部出错,error at ThreadSawMarkCalCoreForOneProbe,threadName not in the list" + threadName, null);
                    calResultMutexSawMark.ReleaseMutex();
                    return;
                }

                resultTemp = (ThreadSawMarkResult)calResultTableSawMark[threadName];
                int numSawMarkMax = resultTemp.sawMarkInfos.GetLength(0);
                int numShowDataLen = resultTemp.sawMarkShowDatas.GetLength(1);

                calResultMutexSawMark.ReleaseMutex();

                #endregion

                #region step2:调用计算核函数

                int dataLen = probeDatas.GetLength(1);
                int probeNum = probeDatas.GetLength(0);

                int errorFlag = 0;
                StringBuilder errorInfo = new StringBuilder(1024);
                bool warnFlag = false;
                StringBuilder warnInfo = new StringBuilder(2048);

                double[,] sawMarkInfos = new double[numSawMarkMax, 7];
                int sawMarkNumOut = 0;

                double[,] sawMarkShowDatas = new double[probeNum, numShowDataLen];

                System.DateTime startTime = new System.DateTime();
                startTime = System.DateTime.Now;

                if (!SawMarkCalProcessForOneProbe(probeDatas, dataLen, probeNum, probeIndex, ref errorFlag, errorInfo, errorInfo.Capacity, ref warnFlag, warnInfo, warnInfo.Capacity,
                    sawMarkInfos, sawMarkInfos.GetLength(0), sawMarkInfos.GetLength(1), ref sawMarkNumOut, sawMarkShowDatas, sawMarkShowDatas.GetLength(1)))
                {
                    calResultMutexSawMark.WaitOne();
                    resultTemp = (ThreadSawMarkResult)calResultTableSawMark[threadName];
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 线痕计算发生错误; error at ThreadSawMarkCalCore" + ",threadName:" + threadName + "errorInfo:" + errorInfo.ToString();
                    calResultTableSawMark[threadName] = resultTemp;
                    calResultMutexSawMark.ReleaseMutex();

                    ////LogHelper.ErrorLog("SawMarkAlgError: 线痕计算发生错误; error at ThreadSawMarkCalCore" + ",threadName:" + threadName + "errorInfo:" + errorInfo.ToString(), null);
                    return;
                }

                System.DateTime currentTime = new System.DateTime();
                currentTime = System.DateTime.Now;
                TimeSpan ts = currentTime.Subtract(startTime);

                ////LogHelper.InfoLog("SawMarkAlgTime: 线痕计算耗时; time for ThreadSawMarkCalCore " + ts.TotalMilliseconds.ToString() + " ms");

                #endregion

                #region step3: 复制结果

                calResultMutexSawMark.WaitOne();

                resultTemp.errorFlag = errorFlag;
                resultTemp.errorInfo = errorInfo.ToString();
                resultTemp.warnFlag = warnFlag;
                resultTemp.warnInfo = warnInfo.ToString();
                resultTemp.time = ts.TotalMilliseconds;

                if (sawMarkNumOut <= 0)
                {
                    resultTemp.sawMarkNum = 0;
                }
                else
                {
                    resultTemp.sawMarkNum = sawMarkNumOut;
                    for (int nk = 0; nk < sawMarkNumOut; nk++)
                    {
                        resultTemp.sawMarkInfos[nk].probeIndex = Convert.ToInt32(sawMarkInfos[nk, 0]);
                        resultTemp.sawMarkInfos[nk].type = Convert.ToInt32(sawMarkInfos[nk, 1]);
                        resultTemp.sawMarkInfos[nk].peakPos = sawMarkInfos[nk, 2];
                        resultTemp.sawMarkInfos[nk].peakValue = sawMarkInfos[nk, 3];
                        resultTemp.sawMarkInfos[nk].valleyPos = sawMarkInfos[nk, 4];
                        resultTemp.sawMarkInfos[nk].valleyValue = sawMarkInfos[nk, 5];
                        resultTemp.sawMarkInfos[nk].depthMax = sawMarkInfos[nk, 6];
                    }
                }
                for (int nk = 0; nk < probeNum; nk++)
                {
                    for (int nj = 0; nj < numShowDataLen; nj++)
                    {
                        resultTemp.sawMarkShowDatas[nk, nj] = sawMarkShowDatas[nk, nj];
                    }
                }

                calResultTableSawMark[threadName] = resultTemp;
                calResultMutexSawMark.ReleaseMutex();

                #endregion

                #region step4: remove

                errorInfo = null;
                warnInfo = null;
                sawMarkInfos = null;
                sawMarkShowDatas = null;
                probeDatas = null;

                #endregion

            }
            catch (Exception excep)
            {
                calResultMutexSawMark.WaitOne();
                resultTemp = (ThreadSawMarkResult)calResultTableSawMark[threadName];
                resultTemp.errorFlag = 1;
                resultTemp.errorInfo = "SawMarkAlgError: 线痕计算发生未知错误; error at ThreadSawMarkCalCore" + ",threadName:" + threadName;
                calResultTableSawMark[threadName] = resultTemp;
                calResultMutexSawMark.ReleaseMutex();

                ////LogHelper.ErrorLog("SawMarkAlgError: 线痕计算发生未知错误; error at ThreadSawMarkCalCore", excep);
                return;
            }

        }
        private void ThreadThicknessCalCore(object caldataIn)
        {
            ThreadThicknessResult resultTemp;

            ThreadCalDataIn calDatasThickness = (ThreadCalDataIn)caldataIn;
            string threadName = calDatasThickness.name;
            double[,] probeDatas = calDatasThickness.dataIn;

            try
            {
                #region step1:获取输入

                calResultMutex.WaitOne();
                if ((null == calResultTable) || (null == threadName))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: ThreadThicknessCalCore内部出错,error at ThreadThicknessCalCore,illegal calResultTable or threadName", null);
                    calResultMutex.ReleaseMutex();
                    return;
                }

                if (!calResultTable.ContainsKey(threadName))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: ThreadThicknessCalCore内部出错,error at ThreadThicknessCalCore,threadName not in the list" + threadName, null);
                    calResultMutex.ReleaseMutex();
                    return;
                }

                resultTemp = (ThreadThicknessResult)calResultTable[threadName];
                int numProbePairs = resultTemp.thicknessData.GetLength(0);
                int numMeasurePos = resultTemp.thicknessData.GetLength(1);

                calResultMutex.ReleaseMutex();

                #endregion

                #region step2:调用计算核函数

                int dataLen = probeDatas.GetLength(1);
                int probeNum = probeDatas.GetLength(0);

                int errorFlag = 0;
                StringBuilder errorInfo = new StringBuilder(1024);
                bool warnFlag = false;
                StringBuilder warnInfo = new StringBuilder(2048);

                double[,] thicknessData = new double[numProbePairs, numMeasurePos];
                int[,] thicknessDataValidFlag = new int[numProbePairs, numMeasurePos];

                System.DateTime startTime = new System.DateTime();
                startTime = System.DateTime.Now;

                if (!ThicknessCalProcess(probeDatas, dataLen, probeNum, ref errorFlag, errorInfo, errorInfo.Capacity, ref warnFlag, warnInfo, warnInfo.Capacity,
                    thicknessData, thicknessData.GetLength(0), thicknessData.GetLength(1), thicknessDataValidFlag))
                {
                    calResultMutex.WaitOne();
                    resultTemp = (ThreadThicknessResult)calResultTable[threadName];
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 厚度计算发生错误; error at ThreadThicknessCalCore" + ",threadName:" + threadName + "errorInfo:" + errorInfo.ToString();
                    calResultTable[threadName] = resultTemp;
                    calResultMutex.ReleaseMutex();

                    ////LogHelper.ErrorLog("SawMarkAlgError: 厚度计算发生错误; error at ThreadThicknessCalCore" + ",threadName:" + threadName + "errorInfo:" + errorInfo.ToString(), null);
                    return;
                }

                System.DateTime currentTime = new System.DateTime();
                currentTime = System.DateTime.Now;
                TimeSpan ts = currentTime.Subtract(startTime);

                ////LogHelper.InfoLog("SawMarkAlgTime: 厚度计算耗时; time for ThreadThicknessCalCore " + ts.TotalMilliseconds.ToString() + " ms");

                #endregion

                #region step3: 复制结果

                calResultMutex.WaitOne();

                resultTemp.errorFlag = errorFlag;
                resultTemp.errorInfo = errorInfo.ToString();
                resultTemp.warnFlag = warnFlag;
                resultTemp.warnInfo = warnInfo.ToString();
                resultTemp.time = ts.TotalMilliseconds;

                for (int ni = 0; ni < numProbePairs; ni++)
                {
                    for (int nj = 0; nj < numMeasurePos; nj++)
                    {
                        if (thicknessDataValidFlag[ni, nj] == 1)
                        {
                            resultTemp.thicknessData[ni, nj] = thicknessData[ni, nj];
                        }
                        else
                        {
                            resultTemp.thicknessData[ni, nj] = Double.NaN;
                        }
                    }
                }
                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();

                #endregion

                #region step4: remove

                errorInfo = null;
                warnInfo = null;
                thicknessData = null;
                thicknessDataValidFlag = null;
                probeDatas = null;

                #endregion

            }
            catch (Exception excep)
            {
                calResultMutex.WaitOne();
                resultTemp = (ThreadThicknessResult)calResultTable[threadName];
                resultTemp.errorFlag = 1;
                resultTemp.errorInfo = "SawMarkAlgError: 厚度计算发生未知错误; error at ThreadThicknessCalCore" + ",threadName:" + threadName;
                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();

                ////LogHelper.ErrorLog("SawMarkAlgError: 厚度计算发生未知错误; error at ThreadThicknessCalCore", excep);
                return;
            }
        }
        private void ThreadWarpCalCore(object caldataIn)
        {
            ThreadWarpResult resultTemp;

            ThreadCalDataIn calDatasWarp = (ThreadCalDataIn)caldataIn;
            string threadName = calDatasWarp.name;
            double[,] probeDatas = calDatasWarp.dataIn;

            try
            {
                #region step1:获取输入

                calResultMutex.WaitOne();
                if ((null == calResultTable) || (null == threadName))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: ThreadWarpCalCore内部出错,error at ThreadWarpCalCore,illegal calResultTable or threadName", null);
                    calResultMutex.ReleaseMutex();
                    return;
                }

                if (!calResultTable.ContainsKey(threadName))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: ThreadWarpCalCore内部出错,error at ThreadWarpCalCore,threadName not in the list" + threadName, null);
                    calResultMutex.ReleaseMutex();
                    return;
                }

                resultTemp = (ThreadWarpResult)calResultTable[threadName];
                int numProbePairs = resultTemp.warpData.GetLength(0);

                calResultMutex.ReleaseMutex();

                #endregion

                #region step2:调用计算核函数

                int dataLen = probeDatas.GetLength(1);
                int probeNum = probeDatas.GetLength(0);

                int errorFlag = 0;
                StringBuilder errorInfo = new StringBuilder(1024);
                bool warnFlag = false;
                StringBuilder warnInfo = new StringBuilder(2048);

                double[] warpData = new double[numProbePairs];

                System.DateTime startTime = new System.DateTime();
                startTime = System.DateTime.Now;

                if (!WarpCalProcess(probeDatas, dataLen, probeNum, ref errorFlag, errorInfo, errorInfo.Capacity, ref warnFlag, warnInfo, warnInfo.Capacity,
                    warpData, warpData.GetLength(0)))
                {
                    calResultMutex.WaitOne();
                    resultTemp = (ThreadWarpResult)calResultTable[threadName];
                    resultTemp.errorFlag = 1;
                    resultTemp.errorInfo = "SawMarkAlgError: 翘曲计算发生错误; error at ThreadWarpCalCore" + ",threadName:" + threadName + "errorInfo:" + errorInfo.ToString();
                    calResultTable[threadName] = resultTemp;
                    calResultMutex.ReleaseMutex();

                    ////LogHelper.ErrorLog("SawMarkAlgError: 翘曲计算发生错误; error at ThreadWarpCalCore" + ",threadName:" + threadName + "errorInfo:" + errorInfo.ToString(), null);
                    return;
                }

                System.DateTime currentTime = new System.DateTime();
                currentTime = System.DateTime.Now;
                TimeSpan ts = currentTime.Subtract(startTime);

                ////LogHelper.InfoLog("SawMarkAlgTime: 翘曲计算耗时; time for ThreadWarpCalCore " + ts.TotalMilliseconds.ToString() + " ms");

                #endregion

                #region step3: 复制结果

                calResultMutex.WaitOne();

                resultTemp.errorFlag = errorFlag;
                resultTemp.errorInfo = errorInfo.ToString();
                resultTemp.warnFlag = warnFlag;
                resultTemp.warnInfo = warnInfo.ToString();
                resultTemp.time = ts.TotalMilliseconds;

                for (int ni = 0; ni < numProbePairs; ni++)
                {
                    resultTemp.warpData[ni] = warpData[ni];
                }
                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();

                #endregion

                #region step4: remove

                errorInfo = null;
                warnInfo = null;
                warpData = null;
                probeDatas = null;

                #endregion

            }
            catch (Exception excep)
            {
                calResultMutex.WaitOne();
                resultTemp = (ThreadWarpResult)calResultTable[threadName];
                resultTemp.errorFlag = 1;
                resultTemp.errorInfo = "SawMarkAlgError: 翘曲计算发生未知错误; error at ThreadWarpCalCore" + ",threadName:" + threadName;
                calResultTable[threadName] = resultTemp;
                calResultMutex.ReleaseMutex();

                ////LogHelper.ErrorLog("SawMarkAlgError: 翘曲计算发生未知错误; error at ThreadWarpCalCore", excep);
                return;
            }
        }

        #endregion

    }


    #region struct ThreadCalDataIn
    public struct ThreadCalDataIn
    {
        public string name;
        public double[,] dataIn;
    }

    #endregion


    #region struct ThreadSawMarkCalDataIn
    public struct ThreadCalDataSawMarkIn
    {
        public string name;
        public int probeIndex;
        public double[,] dataIn;
    }

    #endregion


    #region struct SawMarkPosInfo
    public struct SawMarkPosInfo
    {
        public int probeIndex;        // 0 1 2 3 4 5 
        public int type;              // 0 大线痕    1 密集线痕
        public double peakPos;
        public double peakValue;
        public double valleyPos;
        public double valleyValue;
        public double depthMax;
    }

    #endregion


    #region class ThreadSawMarkResult
    public class ThreadSawMarkResult
    {
        public int errorFlag;
        public string errorInfo;
        public bool warnFlag;
        public string warnInfo;
        public double time;
        public SawMarkPosInfo[] sawMarkInfos;
        public int sawMarkNum;
        public double[,] sawMarkShowDatas;

        public ThreadSawMarkResult(int numSawMarkMax, int probeNum, int numShowDataLen)
        {
            errorFlag = 0;
            errorInfo = null;
            warnFlag = false;
            warnInfo = null;
            time = 0;
            sawMarkInfos = new SawMarkPosInfo[numSawMarkMax];
            sawMarkNum = 0;
            sawMarkShowDatas = new double[probeNum, numShowDataLen];
        }
    }

    #endregion


    #region class ThreadThicknessResult
    public class ThreadThicknessResult
    {
        public int errorFlag;
        public string errorInfo;
        public bool warnFlag;
        public string warnInfo;
        public double time;
        public double[,] thicknessData;
        public ThreadThicknessResult(int probePairsNum, int numThicknessMeasurePos)
        {
            errorFlag = 0;
            errorInfo = null;
            warnFlag = false;
            warnInfo = null;
            time = 0;
            thicknessData = new double[probePairsNum, numThicknessMeasurePos];
        }
    }
    #endregion


    #region class ThreadWarpResult
    public class ThreadWarpResult
    {
        public int errorFlag;
        public string errorInfo;
        public bool warnFlag;
        public string warnInfo;
        public double time;
        public double[] warpData;

        public ThreadWarpResult(int probePairsNum)
        {
            errorFlag = 0;
            errorInfo = null;
            warnFlag = false;
            warnInfo = null;
            time = 0;
            warpData = new double[probePairsNum];
        }
    }
    #endregion


    #region class:SawMarkALgParams
    // 说明：算法参数
    // Init被调用时，读取算法参数文件
    // ShowConfigUI中，生成/更新/保存算法参数文件
    public class SawMarkAlgParams
    {
        #region Dll Import

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkAlgoInit(double[] algoParamIn, int algoParamLen, StringBuilder errorInfo);

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkAlgoSetParam(double algoParamValue, int algoParamIndex, StringBuilder errorInfo);

        #endregion


        #region 算法参数
        public double LambdaC1 { get; set; }                     // 单一线痕，截止波长，单位mm，0.8
        public double LambdaC2 { get; set; }                     // 密集线痕，截止波长，单位mm，0.125
        public double LambdaS { get; set; }                      // 粗糙度（传感器噪声），截止波长，单位mm，0.08
        public double Fs { get; set; }                           // 采样频率，单位Hz，150KHz
        public double SawMarkWindowSize { get; set; }            // 单条线痕滑窗尺寸，单位mm，5
        public double SmallSawMarkWindowSize { get; set; }       // 密集线痕滑窗尺寸，单位mm，1
        public double WindowOverlap { get; set; }                // 滑窗重叠比例，0.85
        public double WorkAreaStart { get; set; }                 // 工作区域起始位置，单位mm，5
        public double WorkAreaEnd { get; set; }                  // 工作区域中止位置，单位mm，5
        public double DataLimitMin { get; set; }                 // 点激光输出范围下限，单位mm，-3
        public double DataLimitMax { get; set; }                 // 点激光输出范围上限，单位mm，3
        public double DisFromProbeToWaferBound { get; set; }      // 两侧点激光与硅片边沿的距离，单位mm，8
        public double ProbeDiameter { get; set; }                 // 点激光光斑直径，单位mm，0.025
        public double Probe1Base { get; set; }                    // 探头1基准值,单位mm，0
        public double Probe2Base { get; set; }                    // 探头2基准值,单位mm，0
        public double Probe3Base { get; set; }                    // 探头3基准值,单位mm，0
        public double Probe4Base { get; set; }                    // 探头4基准值,单位mm，0
        public double Probe5Base { get; set; }                    // 探头5基准值,单位mm，0
        public double Probe6Base { get; set; }                    // 探头6基准值,单位mm，0
        public double ProbePair1ThOffset { get; set; }            // 第1对探头厚度补偿值，单位um，0
        public double ProbePair2ThOffset { get; set; }            // 第2对探头厚度补偿值，单位um，0
        public double ProbePair3ThOffset { get; set; }            // 第3对探头厚度补偿值，单位um，0

        public double MeasurePosNumber { get; set; }              // 每条激光扫描线上厚度测量点数
        public double MeasurePosDiameter { get; set; }            // 厚度测量点直径（平滑尺寸）,单位mm

        public double ResampleRate { get; set; }                  // 采样比例，0~1，值越小数据量越小

        public int MonitorLimitForBrokenPieces { get; set; }      // 连续出现碎片报警
        public int MonitorLimitForOverlapPieces { get; set; }     // 连续出现叠片报警

        #endregion

        public SawMarkAlgParams()
        {
            LambdaC1 = 0.8;
            LambdaC2 = 0.125;
            LambdaS = 0.08;
            Fs = 150 * 1000;
            SawMarkWindowSize = 5;
            SmallSawMarkWindowSize = 1;
            WindowOverlap = 0.85;
            WorkAreaStart = 5;
            WorkAreaEnd = 5;
            DataLimitMin = -3;
            DataLimitMax = 3;
            DisFromProbeToWaferBound = 8;
            ProbeDiameter = 0.025;
            Probe1Base = 0;
            Probe2Base = 0;
            Probe3Base = 0;
            Probe4Base = 0;
            Probe5Base = 0;
            Probe6Base = 0;
            ProbePair1ThOffset = 0;
            ProbePair2ThOffset = 0;
            ProbePair3ThOffset = 0;
            MeasurePosNumber = 25;
            MeasurePosDiameter = 5;
            ResampleRate = 1;

            MonitorLimitForBrokenPieces = 8;
            MonitorLimitForOverlapPieces = 5;


        }                 // 初始化
        public bool SetParams(string configFilePath, out string errorInfo)  // 根据参数文件赋值
        {
            try
            {
                #region 判断输入

                SawMarkAlgParams tempAlgParams;
                if (!File.Exists(configFilePath))
                {
                    tempAlgParams = new SawMarkAlgParams();
                    APSerializer.XmlSerialize(tempAlgParams, configFilePath);
                }
                else
                {
                    tempAlgParams = APSerializer.XmlDeserialize<SawMarkAlgParams>(configFilePath);
                    if (null == tempAlgParams)
                    {
                        errorInfo = "error at xml file reading, possible cause:not a xml file for SawMarkAlgParams";
                        return false;
                    }
                    if (tempAlgParams.LambdaC1 <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.LambdaC1.ToString() + " for LambdaC1 ";
                        return false;
                    }
                    if (tempAlgParams.LambdaC2 <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.LambdaC2.ToString() + " for LambdaC2 ";
                        return false;
                    }
                    if (tempAlgParams.LambdaS <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.LambdaS.ToString() + " for LambdaS ";
                        return false;
                    }
                    if (tempAlgParams.Fs <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.Fs.ToString() + " for Fs ";
                        return false;
                    }
                    if (tempAlgParams.SawMarkWindowSize <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.SawMarkWindowSize.ToString() + " for SawMarkWindowSize ";
                        return false;
                    }
                    if (tempAlgParams.SmallSawMarkWindowSize <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.SmallSawMarkWindowSize.ToString() + " for SmallSawMarkWindowSize ";
                        return false;
                    }
                    if ((tempAlgParams.WindowOverlap <= 0) || (tempAlgParams.WindowOverlap >= 1))
                    {
                        errorInfo = "error input:" + tempAlgParams.WindowOverlap.ToString() + " for WindowOverlap ";
                        return false;
                    }
                    if ((tempAlgParams.WorkAreaStart <= 0) || (tempAlgParams.WorkAreaEnd <= 0))
                    {
                        errorInfo = "error input: for WorkAreaStart or WorkAreaEnd ";
                        return false;
                    }
                    if (tempAlgParams.DataLimitMin > tempAlgParams.DataLimitMax)
                    {
                        errorInfo = "error input: for DataLimitMin or DataLimitMax ";
                        return false;
                    }
                    if (tempAlgParams.DisFromProbeToWaferBound < 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.DisFromProbeToWaferBound.ToString() + " for DisFromProbeToWaferBound ";
                        return false;
                    }
                    if (tempAlgParams.ProbeDiameter <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.ProbeDiameter.ToString() + " for ProbeDiameter ";
                        return false;
                    }
                    if (tempAlgParams.MeasurePosNumber <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.MeasurePosNumber.ToString() + " for MeasurePosNumber ";
                        return false;
                    }
                    if (tempAlgParams.MeasurePosDiameter <= 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.MeasurePosDiameter.ToString() + " for MeasurePosDiameter ";
                        return false;
                    }
                    if ((tempAlgParams.ResampleRate <= 0) || (tempAlgParams.ResampleRate > 1))
                    {
                        errorInfo = "error input:" + tempAlgParams.ResampleRate.ToString() + " for ResampleRate ";
                        return false;
                    }

                    if (tempAlgParams.MonitorLimitForBrokenPieces < 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.MonitorLimitForBrokenPieces.ToString() + " for MonitorLimitForBrokenPieces ";
                        return false;
                    }
                    if (tempAlgParams.MonitorLimitForOverlapPieces < 0)
                    {
                        errorInfo = "error input:" + tempAlgParams.MonitorLimitForOverlapPieces.ToString() + " for MonitorLimitForOverlapPieces ";
                        return false;
                    }
                }

                #endregion

                #region 传给C++

                try
                {
                    double[] algoParamsForCPP = new double[25];
                    algoParamsForCPP[0] = tempAlgParams.LambdaC1;
                    algoParamsForCPP[1] = tempAlgParams.LambdaC2;
                    algoParamsForCPP[2] = tempAlgParams.LambdaS;
                    algoParamsForCPP[3] = tempAlgParams.Fs;
                    algoParamsForCPP[4] = tempAlgParams.SawMarkWindowSize;
                    algoParamsForCPP[5] = tempAlgParams.SmallSawMarkWindowSize;
                    algoParamsForCPP[6] = tempAlgParams.WindowOverlap;
                    algoParamsForCPP[7] = tempAlgParams.WorkAreaStart;
                    algoParamsForCPP[8] = tempAlgParams.WorkAreaEnd;
                    algoParamsForCPP[9] = tempAlgParams.DataLimitMin;
                    algoParamsForCPP[10] = tempAlgParams.DataLimitMax;
                    algoParamsForCPP[11] = tempAlgParams.DisFromProbeToWaferBound;
                    algoParamsForCPP[12] = tempAlgParams.ProbeDiameter;

                    algoParamsForCPP[13] = tempAlgParams.Probe1Base;
                    algoParamsForCPP[14] = tempAlgParams.Probe2Base;
                    algoParamsForCPP[15] = tempAlgParams.Probe3Base;
                    algoParamsForCPP[16] = tempAlgParams.Probe4Base;
                    algoParamsForCPP[17] = tempAlgParams.Probe5Base;
                    algoParamsForCPP[18] = tempAlgParams.Probe6Base;
                    algoParamsForCPP[19] = tempAlgParams.ProbePair1ThOffset;
                    algoParamsForCPP[20] = tempAlgParams.ProbePair2ThOffset;
                    algoParamsForCPP[21] = tempAlgParams.ProbePair3ThOffset;
                    algoParamsForCPP[22] = tempAlgParams.MeasurePosNumber;
                    algoParamsForCPP[23] = tempAlgParams.MeasurePosDiameter;
                    algoParamsForCPP[24] = tempAlgParams.ResampleRate;

                    StringBuilder errorInfoForCPP = new StringBuilder(256);

                    if (!SawMarkAlgoInit(algoParamsForCPP, algoParamsForCPP.Length, errorInfoForCPP))
                    {
                        errorInfo = "error at SetParams:" + errorInfoForCPP.ToString();
                        return false;
                    }
                }
                catch (Exception excep)
                {
                    errorInfo = "error at SetParams:" + excep.ToString();
                    return false;
                }

                #endregion

                #region 更新C#参数 

                LambdaC1 = tempAlgParams.LambdaC1;
                LambdaC2 = tempAlgParams.LambdaC2;
                LambdaS = tempAlgParams.LambdaS;
                Fs = tempAlgParams.Fs;
                SawMarkWindowSize = tempAlgParams.SawMarkWindowSize;
                SmallSawMarkWindowSize = tempAlgParams.SmallSawMarkWindowSize;
                WindowOverlap = tempAlgParams.WindowOverlap;
                WorkAreaStart = tempAlgParams.WorkAreaStart;
                WorkAreaEnd = tempAlgParams.WorkAreaEnd;
                DataLimitMin = tempAlgParams.DataLimitMin;
                DataLimitMax = tempAlgParams.DataLimitMax;
                DisFromProbeToWaferBound = tempAlgParams.DisFromProbeToWaferBound;
                ProbeDiameter = tempAlgParams.ProbeDiameter;
                Probe1Base = tempAlgParams.Probe1Base;
                Probe2Base = tempAlgParams.Probe2Base;
                Probe3Base = tempAlgParams.Probe3Base;
                Probe4Base = tempAlgParams.Probe4Base;
                Probe5Base = tempAlgParams.Probe5Base;
                Probe6Base = tempAlgParams.Probe6Base;
                ProbePair1ThOffset = tempAlgParams.ProbePair1ThOffset;
                ProbePair2ThOffset = tempAlgParams.ProbePair2ThOffset;
                ProbePair3ThOffset = tempAlgParams.ProbePair3ThOffset;
                MeasurePosDiameter = tempAlgParams.MeasurePosDiameter;
                MeasurePosNumber = tempAlgParams.MeasurePosNumber;
                ResampleRate = tempAlgParams.ResampleRate;
                MonitorLimitForBrokenPieces = tempAlgParams.MonitorLimitForBrokenPieces;
                MonitorLimitForOverlapPieces = tempAlgParams.MonitorLimitForOverlapPieces;

                #endregion

                errorInfo = "";
                return true;
            }
            catch (Exception excep)
            {
                errorInfo = "error at SetParams:" + excep.ToString();
                return false;
            }
        }
        public bool GetParRemark(string varName, out string remark)
        {
            switch (varName)
            {
                case "LambdaC1":
                    remark = "单一线痕截止波长,mm";
                    break;
                case "LambdaC2":
                    remark = "密集线痕截止波长,mm";
                    break;
                case "LambdaS":
                    remark = "粗糙度截至波长,mm";
                    break;
                case "Fs":
                    remark = "采样频率";
                    break;
                case "SawMarkWindowSize":
                    remark = "单一线痕窗口尺寸,mm";
                    break;
                case "SmallSawMarkWindowSize":
                    remark = "密集线痕窗口尺寸,mm";
                    break;
                case "WindowOverlap":
                    remark = "窗口重叠比例,0~1";
                    break;
                case "WorkAreaStart":
                    remark = "硅片前端抛除数据长度,mm";
                    break;
                case "WorkAreaEnd":
                    remark = "硅片尾端抛除数据长度,mm";
                    break;
                case "DataLimitMin":
                    remark = "激光数据范围下限,mm";
                    break;
                case "DataLimitMax":
                    remark = "激光数据范围上限,mm";
                    break;
                case "DisFromProbeToWaferBound":
                    remark = "激光到硅片边沿距离,mm";
                    break;
                case "ProbeDiameter":
                    remark = "激光光斑直径,mm";
                    break;
                case "Probe1Base":
                    remark = "探头1基准值,单位mm";
                    break;
                case "Probe2Base":
                    remark = "探头2基准值,单位mm";
                    break;
                case "Probe3Base":
                    remark = "探头3基准值,单位mm";
                    break;
                case "Probe4Base":
                    remark = "探头4基准值,单位mm";
                    break;
                case "Probe5Base":
                    remark = "探头5基准值,单位mm";
                    break;
                case "Probe6Base":
                    remark = "探头6基准值,单位mm";
                    break;
                case "ProbePair1Distance":
                    remark = "第1对探头厚度补偿值,单位um";
                    break;
                case "ProbePair2Distance":
                    remark = "第2对探头厚度补偿值,单位um";
                    break;
                case "ProbePair3Distance":
                    remark = "第3对探头厚度补偿值,单位um";
                    break;
                case "MeasurePosNumber":
                    remark = "每条扫描线上测量点数目";
                    break;
                case "MeasurePosDiameter":
                    remark = "测量点直径,mm";
                    break;
                case "ResampleRate":
                    remark = "采样比例,0~1,值越小则参与计算的数据量越小";
                    break;
                case "MonitorLimitForBrokenPieces":
                    remark = "连续出现几次碎片报警";
                    break;
                case "MonitorLimitForOverlapPieces":
                    remark = "连续出现几次叠片报警";
                    break;
                default:
                    remark = "";
                    return false;
            }
            return true;
        }
        public bool SetParValue(string varName, double value, out string errorInfo)
        {
            StringBuilder errorInfoForCPP = new StringBuilder(256);

            switch (varName)
            {
                case "LambdaC1":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 0, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        LambdaC1 = value;
                        return true;
                    }
                case "LambdaC2":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 1, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        LambdaC2 = value;
                        return true;
                    }
                case "LambdaS":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 2, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        LambdaS = value;
                        return true;
                    }
                case "Fs":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 3, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Fs = value;
                        return true;
                    }
                case "SawMarkWindowSize":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 4, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        SawMarkWindowSize = value;
                        return true;
                    }
                case "SmallSawMarkWindowSize":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 5, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        SmallSawMarkWindowSize = value;
                        return true;
                    }
                case "WindowOverlap":
                    if ((value <= 0) || (value >= 1))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 6, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        WindowOverlap = value;
                        return true;
                    }
                case "WorkAreaStart":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 7, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        WorkAreaStart = value;
                        return true;
                    }
                case "WorkAreaEnd":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 8, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        WorkAreaEnd = value;
                        return true;
                    }
                case "DataLimitMin":
                    if (value > DataLimitMax)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 9, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        DataLimitMin = value;
                        return true;
                    }
                case "DataLimitMax":
                    if (value <= DataLimitMin)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 10, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        DataLimitMax = value;
                        return true;
                    }
                case "DisFromProbeToWaferBound":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 11, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        DisFromProbeToWaferBound = value;
                        return true;
                    }
                case "ProbeDiameter":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 12, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        ProbeDiameter = value;
                        return true;
                    }
                case "Probe1Base":
                    if ((value <= DataLimitMin) || (value >= DataLimitMax))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 13, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Probe1Base = value;
                        return true;
                    }
                case "Probe2Base":
                    if ((value <= DataLimitMin) || (value >= DataLimitMax))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 14, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Probe2Base = value;
                        return true;
                    }
                case "Probe3Base":
                    if ((value <= DataLimitMin) || (value >= DataLimitMax))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 15, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Probe3Base = value;
                        return true;
                    }
                case "Probe4Base":
                    if ((value <= DataLimitMin) || (value >= DataLimitMax))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 16, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Probe4Base = value;
                        return true;
                    }
                case "Probe5Base":
                    if ((value <= DataLimitMin) || (value >= DataLimitMax))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 17, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Probe5Base = value;
                        return true;
                    }
                case "Probe6Base":
                    if ((value <= DataLimitMin) || (value >= DataLimitMax))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 18, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Probe6Base = value;
                        return true;
                    }
                case "ProbePair1ThOffset":
                    ProbePair1ThOffset = value;
                    try
                    {
                        if (!SawMarkAlgoSetParam(value, 19, errorInfoForCPP))
                        {
                            errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                            return false;
                        }
                    }
                    catch (Exception excep)
                    {
                        errorInfo = "error at SetParValue:" + excep.ToString();
                        return false;
                    }
                    errorInfo = "";
                    return true;

                case "ProbePair2ThOffset":
                    ProbePair2ThOffset = value;
                    try
                    {
                        if (!SawMarkAlgoSetParam(value, 20, errorInfoForCPP))
                        {
                            errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                            return false;
                        }
                    }
                    catch (Exception excep)
                    {
                        errorInfo = "error at SetParValue:" + excep.ToString();
                        return false;
                    }
                    errorInfo = "";
                    return true;

                case "ProbePair3ThOffset":
                    ProbePair3ThOffset = value;
                    try
                    {
                        if (!SawMarkAlgoSetParam(value, 21, errorInfoForCPP))
                        {
                            errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                            return false;
                        }
                    }
                    catch (Exception excep)
                    {
                        errorInfo = "error at SetParValue:" + excep.ToString();
                        return false;
                    }
                    errorInfo = "";
                    return true;

                case "MeasurePosNumber":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 22, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        MeasurePosNumber = value;
                        return true;
                    }
                case "MeasurePosDiameter":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 23, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        MeasurePosDiameter = value;
                        return true;
                    }
                case "ResampleRate":
                    if ((value <= 0) || (value > 1))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkAlgoSetParam(value, 24, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        ResampleRate = value;
                        return true;
                    }

                case "MonitorLimitForBrokenPieces":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        errorInfo = "";
                        MonitorLimitForBrokenPieces = (int)value;
                        return true;
                    }
                case "MonitorLimitForOverlapPieces":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        errorInfo = "";
                        MonitorLimitForOverlapPieces = (int)value;
                        return true;
                    }
                default:
                    value = 0;
                    errorInfo = "error input";
                    return false;
            }

            return true;
        }

    }

    #endregion


    #region class:SawMarkSpecParams

    // 说明：规则参数
    // SetParameter 被调用时，为每个检测项设置规格参数
    public class SawMarkSpecParams
    {
        #region Dll Import

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkSpecInit(double[,] specParamIn, int specParamNum, int specGradeNum, StringBuilder errorInfo);

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkSpecSetParam(double specParamValue, int specParamIndex1, int specParamIndex2, StringBuilder errorInfo);

        #endregion

        private CalType[] testItems;

        private double[] sawMarkDepthMin;           // 单条线痕深度下限，单位um
        private double[] sawMarkDepthMax;           // 单条线痕深度上限，单位um
        private double[] sawMarkNumMax;             // 单条线痕个数上限
        private double[] smallSawMarkDepthMin;      // 密集线痕深度下限，单位um
        private double[] smallSawMarkDepthMax;      // 密集线痕深度上限，单位um
        private double[] smallSawMarkNumMax;        // 密集线痕个数上限

        private double[] thicknessMin;              // 厚度下限，单位um
        private double[] thicknessMax;              // 厚度上限，单位um

        private double[] ttvMin;                    // 总厚度差异下限，单位um
        private double[] ttvMax;                    // 总厚度差异上限，单位um

        private double[] warpMin;                   // 翘曲下限，单位um
        private double[] warpMax;                   // 翘曲上限，单位um

        public SawMarkSpecParams()
        {
            testItems = new CalType[4];
            testItems[0] = CalType.SawMarkSM;
            testItems[1] = CalType.ThicknessSM;
            testItems[2] = CalType.TTVSM;
            testItems[3] = CalType.WarpSM;

            int gradeNum = 18;

            sawMarkDepthMin = new double[gradeNum];
            for (int ni = 0; ni < sawMarkDepthMin.Length; ni++)
            {
                sawMarkDepthMin[ni] = -9999;
            }
            sawMarkDepthMax = new double[gradeNum];
            for (int ni = 0; ni < sawMarkDepthMax.Length; ni++)
            {
                sawMarkDepthMax[ni] = 9999;
            }
            sawMarkNumMax = new double[gradeNum];
            for (int ni = 0; ni < sawMarkNumMax.Length; ni++)
            {
                sawMarkNumMax[ni] = 9999;
            }
            smallSawMarkDepthMin = new double[gradeNum];
            for (int ni = 0; ni < smallSawMarkDepthMin.Length; ni++)
            {
                smallSawMarkDepthMin[ni] = -9999;
            }
            smallSawMarkDepthMax = new double[gradeNum];
            for (int ni = 0; ni < smallSawMarkDepthMax.Length; ni++)
            {
                smallSawMarkDepthMax[ni] = 9999;
            }
            smallSawMarkNumMax = new double[gradeNum];
            for (int ni = 0; ni < smallSawMarkNumMax.Length; ni++)
            {
                smallSawMarkNumMax[ni] = 9999;
            }
            thicknessMin = new double[gradeNum];
            for (int ni = 0; ni < thicknessMin.Length; ni++)
            {
                thicknessMin[ni] = -9999;
            }
            thicknessMax = new double[gradeNum];
            for (int ni = 0; ni < thicknessMax.Length; ni++)
            {
                thicknessMax[ni] = 9999;
            }
            ttvMin = new double[gradeNum];
            for (int ni = 0; ni < ttvMin.Length; ni++)
            {
                ttvMin[ni] = -9999;
            }
            ttvMax = new double[gradeNum];
            for (int ni = 0; ni < ttvMax.Length; ni++)
            {
                ttvMax[ni] = 9999;
            }
            warpMin = new double[gradeNum];
            for (int ni = 0; ni < warpMin.Length; ni++)
            {
                warpMin[ni] = -9999;
            }
            warpMax = new double[gradeNum];
            for (int ni = 0; ni < warpMax.Length; ni++)
            {
                warpMax[ni] = 9999;
            }

        }               // 初始化
        public bool SetParams(Parameter par)        // 根据输入参数赋值
        {
            try
            {
                #region 判断输入
                if (par.Station != StationType.SawMark)
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetParams出错,工站名不匹配" + par.Station.ToString(), null);
                    return false;
                }
                #endregion

                #region 更新C#参数

                testItems = new CalType[0];
                foreach (ClassifyParameter parNi in par.ClassifyPar)
                {
                    switch (parNi.calType)
                    {
                        case CalType.SawMarkSM:
                            if ((!IsInTestItems(CalType.SawMarkSM)) && (!parNi.isShield))
                            {
                                Array.Resize(ref testItems, testItems.Length + 1);
                                testItems[testItems.Length - 1] = CalType.SawMarkSM;
                            }
                            break;
                        case CalType.ThicknessSM:
                            if ((!IsInTestItems(CalType.ThicknessSM)) && (!parNi.isShield))
                            {
                                Array.Resize(ref testItems, testItems.Length + 1);
                                testItems[testItems.Length - 1] = CalType.ThicknessSM;
                            }
                            break;
                        case CalType.TTVSM:
                            if ((!IsInTestItems(CalType.TTVSM)) && (!parNi.isShield))
                            {
                                Array.Resize(ref testItems, testItems.Length + 1);
                                testItems[testItems.Length - 1] = CalType.TTVSM;
                            }
                            break;
                        case CalType.WarpSM:
                            if ((!IsInTestItems(CalType.WarpSM)) && (!parNi.isShield))
                            {
                                Array.Resize(ref testItems, testItems.Length + 1);
                                testItems[testItems.Length - 1] = CalType.WarpSM;
                            }
                            break;
                        default:
                            break;
                    }
                    SetParamsDetail(parNi.parDetail);
                }

                #endregion

                #region 传给C++

                int gradeNum;
                if (!GetGradeNumAll(out gradeNum))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetParams出错,获取等级失败", null);
                    return false;
                }


                double[,] specParamsForSawMark = new double[12, gradeNum];
                for (int ni = 0; ni < gradeNum; ni++)
                {
                    specParamsForSawMark[0, ni] = sawMarkDepthMin[ni];
                    specParamsForSawMark[1, ni] = sawMarkDepthMax[ni];
                    specParamsForSawMark[2, ni] = sawMarkNumMax[ni];
                    specParamsForSawMark[3, ni] = smallSawMarkDepthMin[ni];
                    specParamsForSawMark[4, ni] = smallSawMarkDepthMax[ni];
                    specParamsForSawMark[5, ni] = smallSawMarkNumMax[ni];
                    specParamsForSawMark[6, ni] = thicknessMin[ni];
                    specParamsForSawMark[7, ni] = thicknessMax[ni];
                    specParamsForSawMark[8, ni] = ttvMin[ni];
                    specParamsForSawMark[9, ni] = ttvMax[ni];
                    specParamsForSawMark[10, ni] = warpMin[ni];
                    specParamsForSawMark[11, ni] = warpMax[ni];
                }

                StringBuilder errorInfoForCPP = new StringBuilder(256);
                if (!SawMarkSpecInit(specParamsForSawMark, specParamsForSawMark.GetLength(0), specParamsForSawMark.GetLength(1), errorInfoForCPP))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetParams出错,未知错误" + errorInfoForCPP.ToString(), null);
                    return false;
                }

                #endregion

                return true;
            }
            catch (Exception excep)
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: SetParams出错,未知错误", excep);
                return false;
            }
        }
        private bool SetParamsDetail(CellParameter[] parDetail)
        {
            ParType parTypeIn = parDetail[0].parType;
            int ni;
            switch (parTypeIn)
            {
                case ParType.SawMarkDepthMin:
                    sawMarkDepthMin = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        sawMarkDepthMin[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.SawMarkDepthMax:
                    sawMarkDepthMax = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        sawMarkDepthMax[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.SawMarkNumMax:
                    sawMarkNumMax = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        sawMarkNumMax[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.SmallSawMarkDepthMin:
                    smallSawMarkDepthMin = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        smallSawMarkDepthMin[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.SmallSawMarkDepthMax:
                    smallSawMarkDepthMax = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        smallSawMarkDepthMax[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.SmallSawMarkNumMax:
                    smallSawMarkNumMax = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        smallSawMarkNumMax[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.ThicknessMin:
                    thicknessMin = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        thicknessMin[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.ThicknessMax:
                    thicknessMax = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        thicknessMax[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.TTVMin:
                    ttvMin = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        ttvMin[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.TTVMax:
                    ttvMax = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        ttvMax[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.WarpMin:
                    warpMin = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        warpMin[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                case ParType.WarpMax:
                    warpMax = new double[parDetail.Length];
                    ni = 0;
                    foreach (CellParameter cellParam in parDetail)
                    {
                        warpMax[ni] = Convert.ToDouble(cellParam.parValue);
                        ni++;
                    }
                    break;
                default:
                    break;
            }

            return true;
        }
        public bool IsInTestItems(CalType caltype)
        {
            bool inTestItems = false;
            foreach (CalType items in testItems)
            {
                if (items == caltype)
                {
                    inTestItems = true;
                }
            }
            return inTestItems;
        }
        public bool GetGradeNumAll(out int gradeNum)
        {
            int sizeLength = 0;
            if (IsInTestItems(CalType.SawMarkSM))
            {
                sizeLength += 6;
            }
            if (IsInTestItems(CalType.ThicknessSM))
            {
                sizeLength += 2;
            }
            if (IsInTestItems(CalType.TTVSM))
            {
                sizeLength += 2;
            }
            if (IsInTestItems(CalType.WarpSM))
            {
                sizeLength += 2;
            }
            if (sizeLength <= 0)
            {
                gradeNum = 0;
                return false;
            }
            int[] size = new int[sizeLength];
            int index = 0;
            if (IsInTestItems(CalType.SawMarkSM))
            {
                size[index + 0] = sawMarkDepthMin.Length;
                size[index + 1] = sawMarkDepthMax.Length;
                size[index + 2] = sawMarkNumMax.Length;
                size[index + 3] = smallSawMarkDepthMin.Length;
                size[index + 4] = smallSawMarkDepthMax.Length;
                size[index + 5] = smallSawMarkNumMax.Length;
                index += 6;
            }
            if (IsInTestItems(CalType.ThicknessSM))
            {
                size[index + 0] = thicknessMin.Length;
                size[index + 1] = thicknessMax.Length;
                index += 2;
            }
            if (IsInTestItems(CalType.TTVSM))
            {
                size[index + 0] = ttvMin.Length;
                size[index + 1] = ttvMax.Length;
                index += 2;
            }
            if (IsInTestItems(CalType.WarpSM))
            {
                size[index + 0] = warpMin.Length;
                size[index + 1] = warpMax.Length;
                index += 2;
            }
            gradeNum = size[0];
            for (int ni = 0; ni < size.Length; ni++)
            {
                if (gradeNum != size[ni])
                {
                    return false;
                }
            }
            return true;
        }

        public bool GetParValue(string varName, int index, out double value)
        {
            switch (varName)
            {
                case "SawMarkDepthMin":
                    if (index < sawMarkDepthMin.Length)
                    {
                        value = sawMarkDepthMin[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "SawMarkDepthMax":
                    if (index < sawMarkDepthMax.Length)
                    {
                        value = sawMarkDepthMax[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "SawMarkNumMax":
                    if (index < sawMarkNumMax.Length)
                    {
                        value = sawMarkNumMax[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "SmallSawMarkDepthMin":
                    if (index < smallSawMarkDepthMin.Length)
                    {
                        value = smallSawMarkDepthMin[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "SmallSawMarkDepthMax":
                    if (index < smallSawMarkDepthMax.Length)
                    {
                        value = smallSawMarkDepthMax[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "SmallSawMarkNumMax":
                    if (index < smallSawMarkNumMax.Length)
                    {
                        value = smallSawMarkNumMax[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "ThicknessMin":
                    if (index < thicknessMin.Length)
                    {
                        value = thicknessMin[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "ThicknessMax":
                    if (index < thicknessMax.Length)
                    {
                        value = thicknessMax[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "TtvMin":
                    if (index < ttvMin.Length)
                    {
                        value = ttvMin[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "TtvMax":
                    if (index < ttvMax.Length)
                    {
                        value = ttvMax[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "WarpMin":
                    if (index < warpMin.Length)
                    {
                        value = warpMin[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                case "WarpMax":
                    if (index < warpMax.Length)
                    {
                        value = warpMax[index];
                        return true;
                    }
                    else
                    {
                        value = 0;
                        return false;
                    }
                default:
                    value = 0;
                    return false;
            }
            return true;

        }
        public bool SetParValue(string varName, int index, double value, out string errorInfo)
        {
            StringBuilder errorInfoForCPP = new StringBuilder(256);

            switch (varName)
            {
                case "SawMarkDepthMin":
                    if (index < sawMarkDepthMin.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 0, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        sawMarkDepthMin[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "SawMarkDepthMax":
                    if (index < sawMarkDepthMax.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 1, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        sawMarkDepthMax[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "SawMarkNumMax":
                    if (index < sawMarkNumMax.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 2, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        sawMarkNumMax[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "SmallSawMarkDepthMin":
                    if (index < smallSawMarkDepthMin.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 3, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        smallSawMarkDepthMin[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "SmallSawMarkDepthMax":
                    if (index < smallSawMarkDepthMax.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 4, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        smallSawMarkDepthMax[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "smallSawMarkNumMax":
                    if (index < smallSawMarkNumMax.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 5, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        smallSawMarkNumMax[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "ThicknessMin":
                    if (index < thicknessMin.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 6, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        thicknessMin[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "ThicknessMax":
                    if (index < thicknessMax.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 7, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        thicknessMax[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "TtvMin":
                    if (index < ttvMin.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 8, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        ttvMin[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "TtvMax":
                    if (index < ttvMax.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 9, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        ttvMax[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "WarpMin":
                    if (index < warpMin.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 10, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        warpMin[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                case "WarpMax":
                    if (index < warpMax.Length)
                    {
                        try
                        {
                            if (!SawMarkSpecSetParam(value, 11, index, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        warpMax[index] = value;
                        return true;
                    }
                    else
                    {
                        errorInfo = "error at SetParValue:" + "out of range";
                        return false;
                    }
                default:
                    value = 0;
                    errorInfo = "error at SetParValue:" + "error input";
                    return false;
            }

            return true;
        }

        public double[] SawMarkDepthMin
        {
            get
            {
                return sawMarkDepthMin;
            }
            set
            {
                sawMarkDepthMin = value;
            }
        }
        public double[] SawMarkDepthMax
        {
            get
            {
                return sawMarkDepthMax;
            }
            set
            {
                sawMarkDepthMax = value;
            }
        }
        public double[] SawMarkNumMax
        {
            get
            {
                return sawMarkNumMax;
            }
            set
            {
                sawMarkNumMax = value;
            }
        }
        public double[] SmallSawMarkDepthMin
        {
            get
            {
                return smallSawMarkDepthMin;
            }
            set
            {
                smallSawMarkDepthMin = value;
            }
        }
        public double[] SmallSawMarkDepthMax
        {
            get
            {
                return smallSawMarkDepthMax;
            }
            set
            {
                smallSawMarkDepthMax = value;
            }
        }
        public double[] SmallSawMarkNumMax
        {
            get
            {
                return smallSawMarkNumMax;
            }
            set
            {
                smallSawMarkNumMax = value;
            }
        }
        public double[] ThicknessMin
        {
            get
            {
                return thicknessMin;
            }
            set
            {
                thicknessMin = value;
            }
        }
        public double[] ThicknessMax
        {
            get
            {
                return thicknessMax;
            }
            set
            {
                thicknessMax = value;
            }
        }
        public double[] TtvMin
        {
            get
            {
                return ttvMin;
            }
            set
            {
                ttvMin = value;
            }
        }
        public double[] TtvMax
        {
            get
            {
                return ttvMax;
            }
            set
            {
                ttvMax = value;
            }
        }
        public double[] WarpMin
        {
            get
            {
                return warpMin;
            }
            set
            {
                warpMin = value;
            }
        }
        public double[] WarpMax
        {
            get
            {
                return warpMax;
            }
            set
            {
                warpMax = value;
            }
        }

    }

    #endregion


    #region class:SawMarkCommParams
    // 说明：公共参数
    // SetParameter 被调用时，设置公共参数
    public class SawMarkCommParams
    {
        #region DLL Import

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkCommInit(double[] commParamIn, int commParamLen, StringBuilder errorInfo);

        [DllImport("SawMarkStationCoreCPP.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SawMarkCommSetParam(double commParamValue, int commParamIndex, StringBuilder errorInfo);

        #endregion

        public double LineSpeed { get; set; }            // 线速，单位mm/s
        public double Size { get; set; }                 // 晶片尺寸，单位mm，100，125，150，156，210
        public double CutLength { get; set; }            // 最小切割矩形长，单位mm
        public double CutWidth { get; set; }             // 最小切割矩形宽，单位mm

        public SawMarkCommParams()                       // 初始化
        {
            LineSpeed = 293;
            Size = 156;
            CutLength = 40;
            CutWidth = 30;
        }
        public bool SetParams(Parameter par, ref AlgoConfigFileContent algoConfig, ref bool algoConfigUpdated)         // 根据参数文件赋值
        {
            try
            {
                #region 检查工站名

                if (par.Station != StationType.SawMark)
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetParams出错,工站名不匹配" + par.Station.ToString(), null);
                    return false;
                }

                #endregion

                #region 保存C#参数

                foreach (CellParameter param in par.CommonPar)
                {
                    switch (param.parType)
                    {
                        case ParType.ClassifyingName:             //规则名
                            if (("" != param.parValue) && (null != param.parValue))
                            {
                                if (!algoConfig.InListSpecName(param.parValue))
                                {
                                    string currentParamFileName;
                                    algoConfig.GetParamFileName(algoConfig.CurrentSpecName, out currentParamFileName);
                                    algoConfig.AddSpecName(param.parValue, currentParamFileName);
                                    algoConfig.CurrentSpecName = param.parValue;
                                    algoConfigUpdated = true;
                                }
                                else
                                {
                                    if (algoConfig.CurrentSpecName != param.parValue)
                                    {
                                        algoConfig.CurrentSpecName = param.parValue;
                                        algoConfigUpdated = true;
                                    }
                                }
                            }
                            else
                            {
                                ////LogHelper.InfoLog("SawMarkAlgWarn: SetParams报警,error input for" + param.parType.ToString() + ",value:" + param.parValue.ToString());
                            }
                            break;

                        case ParType.LineSpeed:     // 线速
                            double speedIn = Convert.ToDouble(param.parValue);
                            if (speedIn >= 0)
                            {
                                LineSpeed = speedIn;
                            }
                            else
                            {
                                ////LogHelper.InfoLog("SawMarkAlgWarn: SetParams报警,error input for" + param.parType.ToString() + ",value:" + param.parValue.ToString());
                            }
                            break;
                        case ParType.Size:         // 尺寸
                            double sizeIn = Convert.ToDouble(param.parValue);
                            if (sizeIn >= 0)
                            {
                                Size = sizeIn;
                            }
                            else
                            {
                                ////LogHelper.InfoLog("SawMarkAlgWarn: SetParams报警,error input for" + param.parType.ToString() + ",value:" + param.parValue.ToString());
                            }
                            break;
                        case ParType.CutLength:    // 最小切割矩形长
                            double cutLenIn = Convert.ToDouble(param.parValue);
                            if (cutLenIn >= 0)
                            {
                                CutLength = cutLenIn;
                            }
                            else
                            {
                                ////LogHelper.InfoLog("SawMarkAlgWarn: SetParams报警,error input for" + param.parType.ToString() + ",value:" + param.parValue.ToString());
                            }
                            break;
                        case ParType.CutWidth:    // 最小切割矩形宽
                            double cutWidIn = Convert.ToDouble(param.parValue);
                            if (cutWidIn >= 0)
                            {
                                CutWidth = cutWidIn;
                            }
                            else
                            {
                                ////LogHelper.InfoLog("SawMarkAlgWarn: SetParams报警,error input for" + param.parType.ToString() + ",value:" + param.parValue.ToString());
                            }
                            break;
                        default:
                            break;
                    }
                }

                #endregion

                #region 传给CPP

                double[] algoParamsForCPP = new double[4];
                algoParamsForCPP[0] = LineSpeed;
                algoParamsForCPP[1] = Size;
                algoParamsForCPP[2] = CutLength;
                algoParamsForCPP[3] = CutWidth;

                StringBuilder errorInfoForCPP = new StringBuilder(256);

                if (!SawMarkCommInit(algoParamsForCPP, algoParamsForCPP.Length, errorInfoForCPP))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetParams出错,未知错误" + errorInfoForCPP.ToString(), null);
                    return false;
                }

                #endregion

                return true;
            }
            catch (Exception excep)
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: SetParams出错,未知错误", excep);
                return false;
            }
        }
        public bool GetParRemark(string varName, out string remark)
        {
            switch (varName)
            {
                case "ClassifyingName":
                    remark = "规则名";
                    break;
                case "LineSpeed":
                    remark = "线速,mm/s";
                    break;
                case "Size":
                    remark = "硅片尺寸,mm";
                    break;
                case "CutLength":
                    remark = "最小切割矩形长,mm";
                    break;
                case "CutWidth":
                    remark = "最小切割矩形宽,mm";
                    break;
                default:
                    remark = "";
                    return false;
            }
            return true;
        }
        public bool SetParValue(string varName, double value, out string errorInfo)
        {
            StringBuilder errorInfoForCPP = new StringBuilder(256);

            switch (varName)
            {
                case "LineSpeed":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkCommSetParam(value, 0, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        LineSpeed = value;
                        return true;
                    }
                case "Size":
                    if (value <= 0)
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkCommSetParam(value, 1, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        Size = value;
                        return true;
                    }
                case "CutLength":
                    if ((value <= 0) || (value > Size))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkCommSetParam(value, 2, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        CutLength = value;
                        return true;
                    }
                case "CutWidth":
                    if ((value <= 0) || (value > Size))
                    {
                        errorInfo = "error input";
                        return false;
                    }
                    else
                    {
                        try
                        {
                            if (!SawMarkCommSetParam(value, 3, errorInfoForCPP))
                            {
                                errorInfo = "error at SetParValue:" + errorInfoForCPP.ToString();
                                return false;
                            }
                        }
                        catch (Exception excep)
                        {
                            errorInfo = "error at SetParValue:" + excep.ToString();
                            return false;
                        }
                        errorInfo = "";

                        CutWidth = value;
                        return true;
                    }
                default:
                    errorInfo = "error input";
                    return false;
            }
        }


    }

    #endregion


    #region class:SawMarkDataShow
    // 说明：结果显示
    public class SawMarkDataShow
    {
        public double[] ThicknessOfProbePair1 { get; set; }
        public double[] ThicknessOfProbePair2 { get; set; }
        public double[] ThicknessOfProbePair3 { get; set; }

        public double[] ProcessedSurfaceDataOfProbe1_Y { get; set; }
        public double[] ProcessedSurfaceDataOfProbe1_X { get; set; }
        public double[] ProcessedSurfaceDataOfProbe2_Y { get; set; }
        public double[] ProcessedSurfaceDataOfProbe2_X { get; set; }
        public double[] ProcessedSurfaceDataOfProbe3_Y { get; set; }
        public double[] ProcessedSurfaceDataOfProbe3_X { get; set; }
        public double[] ProcessedSurfaceDataOfProbe4_Y { get; set; }
        public double[] ProcessedSurfaceDataOfProbe4_X { get; set; }
        public double[] ProcessedSurfaceDataOfProbe5_Y { get; set; }
        public double[] ProcessedSurfaceDataOfProbe5_X { get; set; }
        public double[] ProcessedSurfaceDataOfProbe6_Y { get; set; }
        public double[] ProcessedSurfaceDataOfProbe6_X { get; set; }


        public double[] LargeSawMarkPositionOfProbe1_X { get; set; }
        public double[] LargeSawMarkPositionOfProbe1_Y { get; set; }
        public double[] LargeSawMarkPositionOfProbe2_X { get; set; }
        public double[] LargeSawMarkPositionOfProbe2_Y { get; set; }
        public double[] LargeSawMarkPositionOfProbe3_X { get; set; }
        public double[] LargeSawMarkPositionOfProbe3_Y { get; set; }
        public double[] LargeSawMarkPositionOfProbe4_X { get; set; }
        public double[] LargeSawMarkPositionOfProbe4_Y { get; set; }
        public double[] LargeSawMarkPositionOfProbe5_X { get; set; }
        public double[] LargeSawMarkPositionOfProbe5_Y { get; set; }
        public double[] LargeSawMarkPositionOfProbe6_X { get; set; }
        public double[] LargeSawMarkPositionOfProbe6_Y { get; set; }


        public double[] SmallSawMarkPositionOfProbe1_X { get; set; }
        public double[] SmallSawMarkPositionOfProbe1_Y { get; set; }
        public double[] SmallSawMarkPositionOfProbe2_X { get; set; }
        public double[] SmallSawMarkPositionOfProbe2_Y { get; set; }
        public double[] SmallSawMarkPositionOfProbe3_X { get; set; }
        public double[] SmallSawMarkPositionOfProbe3_Y { get; set; }
        public double[] SmallSawMarkPositionOfProbe4_X { get; set; }
        public double[] SmallSawMarkPositionOfProbe4_Y { get; set; }
        public double[] SmallSawMarkPositionOfProbe5_X { get; set; }
        public double[] SmallSawMarkPositionOfProbe5_Y { get; set; }
        public double[] SmallSawMarkPositionOfProbe6_X { get; set; }
        public double[] SmallSawMarkPositionOfProbe6_Y { get; set; }



        public SawMarkDataShow()
        {
            ThicknessOfProbePair1 = null;
            ThicknessOfProbePair2 = null;
            ThicknessOfProbePair3 = null;

            ProcessedSurfaceDataOfProbe1_Y = null;
            ProcessedSurfaceDataOfProbe1_X = null;
            ProcessedSurfaceDataOfProbe2_Y = null;
            ProcessedSurfaceDataOfProbe2_X = null;
            ProcessedSurfaceDataOfProbe3_Y = null;
            ProcessedSurfaceDataOfProbe3_X = null;
            ProcessedSurfaceDataOfProbe4_Y = null;
            ProcessedSurfaceDataOfProbe4_X = null;
            ProcessedSurfaceDataOfProbe5_Y = null;
            ProcessedSurfaceDataOfProbe5_X = null;
            ProcessedSurfaceDataOfProbe6_Y = null;
            ProcessedSurfaceDataOfProbe6_X = null;

            LargeSawMarkPositionOfProbe1_X = null;
            LargeSawMarkPositionOfProbe1_Y = null;
            LargeSawMarkPositionOfProbe2_X = null;
            LargeSawMarkPositionOfProbe2_Y = null;
            LargeSawMarkPositionOfProbe3_X = null;
            LargeSawMarkPositionOfProbe3_Y = null;
            LargeSawMarkPositionOfProbe4_X = null;
            LargeSawMarkPositionOfProbe4_Y = null;
            LargeSawMarkPositionOfProbe5_X = null;
            LargeSawMarkPositionOfProbe5_Y = null;
            LargeSawMarkPositionOfProbe6_X = null;
            LargeSawMarkPositionOfProbe6_Y = null;

            SmallSawMarkPositionOfProbe1_X = null;
            SmallSawMarkPositionOfProbe1_Y = null;
            SmallSawMarkPositionOfProbe2_X = null;
            SmallSawMarkPositionOfProbe2_Y = null;
            SmallSawMarkPositionOfProbe3_X = null;
            SmallSawMarkPositionOfProbe3_Y = null;
            SmallSawMarkPositionOfProbe4_X = null;
            SmallSawMarkPositionOfProbe4_Y = null;
            SmallSawMarkPositionOfProbe5_X = null;
            SmallSawMarkPositionOfProbe5_Y = null;
            SmallSawMarkPositionOfProbe6_X = null;
            SmallSawMarkPositionOfProbe6_Y = null;

        }
        public bool SetThicknessOfAllProbePairs(double[,] thicknessDataIn)
        {
            try
            {
                if ((null == thicknessDataIn) || (thicknessDataIn.GetLength(0) < 3) || (thicknessDataIn.GetLength(1) <= 0))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetThicknessOfEachProbePair 出错,error input", null);
                    return false;
                }
                int numMeasurePos = thicknessDataIn.GetLength(1);
                ThicknessOfProbePair1 = new double[numMeasurePos];
                ThicknessOfProbePair2 = new double[numMeasurePos];
                ThicknessOfProbePair3 = new double[numMeasurePos];

                for (int ni = 0; ni < numMeasurePos; ni++)
                {
                    ThicknessOfProbePair1[ni] = thicknessDataIn[0, ni];
                    ThicknessOfProbePair2[ni] = thicknessDataIn[1, ni];
                    ThicknessOfProbePair3[ni] = thicknessDataIn[2, ni];
                }

                return true;
            }
            catch (Exception excep)
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: SetThicknessOfAllProbePairs 出错,未知错误", excep);
                return false;
            }
        }
        public bool SetSurfaceDataOfAllProbes(double[,] dataShowIn, double waferSizeIn)
        {
            try
            {
                if ((null == dataShowIn) || (dataShowIn.GetLength(0) < 6) || (dataShowIn.GetLength(1) <= 0) || (waferSizeIn <= 0))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetSurfaceDataOfAllProbes 出错,error input", null);
                    return false;
                }

                int showDataLen = dataShowIn.GetLength(1);
                double dx = waferSizeIn / (1.0 * showDataLen);
                ProcessedSurfaceDataOfProbe1_Y = new double[showDataLen];
                ProcessedSurfaceDataOfProbe1_X = new double[showDataLen];
                ProcessedSurfaceDataOfProbe2_Y = new double[showDataLen];
                ProcessedSurfaceDataOfProbe2_X = new double[showDataLen];
                ProcessedSurfaceDataOfProbe3_Y = new double[showDataLen];
                ProcessedSurfaceDataOfProbe3_X = new double[showDataLen];
                ProcessedSurfaceDataOfProbe4_Y = new double[showDataLen];
                ProcessedSurfaceDataOfProbe4_X = new double[showDataLen];
                ProcessedSurfaceDataOfProbe5_Y = new double[showDataLen];
                ProcessedSurfaceDataOfProbe5_X = new double[showDataLen];
                ProcessedSurfaceDataOfProbe6_Y = new double[showDataLen];
                ProcessedSurfaceDataOfProbe6_X = new double[showDataLen];
                for (int ni = 0; ni < showDataLen; ni++)
                {
                    ProcessedSurfaceDataOfProbe1_Y[ni] = dataShowIn[0, ni];
                    ProcessedSurfaceDataOfProbe2_Y[ni] = dataShowIn[1, ni];
                    ProcessedSurfaceDataOfProbe3_Y[ni] = dataShowIn[2, ni];
                    ProcessedSurfaceDataOfProbe4_Y[ni] = dataShowIn[3, ni];
                    ProcessedSurfaceDataOfProbe5_Y[ni] = dataShowIn[4, ni];
                    ProcessedSurfaceDataOfProbe6_Y[ni] = dataShowIn[5, ni];

                    ProcessedSurfaceDataOfProbe1_X[ni] = Math.Round(dx * ni, 2);
                    ProcessedSurfaceDataOfProbe2_X[ni] = Math.Round(dx * ni, 2);
                    ProcessedSurfaceDataOfProbe3_X[ni] = Math.Round(dx * ni, 2);
                    ProcessedSurfaceDataOfProbe4_X[ni] = Math.Round(dx * ni, 2);
                    ProcessedSurfaceDataOfProbe5_X[ni] = Math.Round(dx * ni, 2);
                    ProcessedSurfaceDataOfProbe6_X[ni] = Math.Round(dx * ni, 2);
                }
                return true;
            }
            catch (Exception excep)
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: SetSurfaceDataOfEachProbePair 出错,未知错误", excep);
                return false;
            }
        }
        public bool SetSawMarkPositionOfAllProbes(SawMarkPosInfo[] sawMarkDatasIn, int sawMarkNumIn)
        {
            try
            {
                if ((null == sawMarkDatasIn) || (sawMarkDatasIn.Length < sawMarkNumIn) || (sawMarkNumIn < 0))
                {
                    ////LogHelper.ErrorLog("SawMarkAlgError: SetSawMarkPositionOfAllProbes 出错,error input", null);
                    return false;
                }

                #region 计数

                int numLargeSawMark_Probe1 = 0;
                int numLargeSawMark_Probe2 = 0;
                int numLargeSawMark_Probe3 = 0;
                int numLargeSawMark_Probe4 = 0;
                int numLargeSawMark_Probe5 = 0;
                int numLargeSawMark_Probe6 = 0;

                int numSmallSawMark_Probe1 = 0;
                int numSmallSawMark_Probe2 = 0;
                int numSmallSawMark_Probe3 = 0;
                int numSmallSawMark_Probe4 = 0;
                int numSmallSawMark_Probe5 = 0;
                int numSmallSawMark_Probe6 = 0;

                for (int ni = 0; ni < sawMarkNumIn; ni++)
                {
                    switch (sawMarkDatasIn[ni].probeIndex)
                    {
                        case 0:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                numLargeSawMark_Probe1++;
                            }
                            else
                            {
                                numSmallSawMark_Probe1++;
                            }
                            break;
                        case 1:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                numLargeSawMark_Probe2++;
                            }
                            else
                            {
                                numSmallSawMark_Probe2++;
                            }
                            break;
                        case 2:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                numLargeSawMark_Probe3++;
                            }
                            else
                            {
                                numSmallSawMark_Probe3++;
                            }
                            break;
                        case 3:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                numLargeSawMark_Probe4++;
                            }
                            else
                            {
                                numSmallSawMark_Probe4++;
                            }
                            break;
                        case 4:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                numLargeSawMark_Probe5++;
                            }
                            else
                            {
                                numSmallSawMark_Probe5++;
                            }
                            break;
                        case 5:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                numLargeSawMark_Probe6++;
                            }
                            else
                            {
                                numSmallSawMark_Probe6++;
                            }
                            break;
                        default:
                            break;
                    }
                }

                #endregion

                #region 赋值

                LargeSawMarkPositionOfProbe1_X = new double[numLargeSawMark_Probe1];
                LargeSawMarkPositionOfProbe1_Y = new double[numLargeSawMark_Probe1];
                LargeSawMarkPositionOfProbe2_X = new double[numLargeSawMark_Probe2];
                LargeSawMarkPositionOfProbe2_Y = new double[numLargeSawMark_Probe2];
                LargeSawMarkPositionOfProbe3_X = new double[numLargeSawMark_Probe3];
                LargeSawMarkPositionOfProbe3_Y = new double[numLargeSawMark_Probe3];
                LargeSawMarkPositionOfProbe4_X = new double[numLargeSawMark_Probe4];
                LargeSawMarkPositionOfProbe4_Y = new double[numLargeSawMark_Probe4];
                LargeSawMarkPositionOfProbe5_X = new double[numLargeSawMark_Probe5];
                LargeSawMarkPositionOfProbe5_Y = new double[numLargeSawMark_Probe5];
                LargeSawMarkPositionOfProbe6_X = new double[numLargeSawMark_Probe6];
                LargeSawMarkPositionOfProbe6_Y = new double[numLargeSawMark_Probe6];

                SmallSawMarkPositionOfProbe1_X = new double[numSmallSawMark_Probe1];
                SmallSawMarkPositionOfProbe1_Y = new double[numSmallSawMark_Probe1];
                SmallSawMarkPositionOfProbe2_X = new double[numSmallSawMark_Probe2];
                SmallSawMarkPositionOfProbe2_Y = new double[numSmallSawMark_Probe2];
                SmallSawMarkPositionOfProbe3_X = new double[numSmallSawMark_Probe3];
                SmallSawMarkPositionOfProbe3_Y = new double[numSmallSawMark_Probe3];
                SmallSawMarkPositionOfProbe4_X = new double[numSmallSawMark_Probe4];
                SmallSawMarkPositionOfProbe4_Y = new double[numSmallSawMark_Probe4];
                SmallSawMarkPositionOfProbe5_X = new double[numSmallSawMark_Probe5];
                SmallSawMarkPositionOfProbe5_Y = new double[numSmallSawMark_Probe5];
                SmallSawMarkPositionOfProbe6_X = new double[numSmallSawMark_Probe6];
                SmallSawMarkPositionOfProbe6_Y = new double[numSmallSawMark_Probe6];

                int index_LargeSawMark_Probe1 = 0;
                int index_LargeSawMark_Probe2 = 0;
                int index_LargeSawMark_Probe3 = 0;
                int index_LargeSawMark_Probe4 = 0;
                int index_LargeSawMark_Probe5 = 0;
                int index_LargeSawMark_Probe6 = 0;

                int index_SmallSawMark_Probe1 = 0;
                int index_SmallSawMark_Probe2 = 0;
                int index_SmallSawMark_Probe3 = 0;
                int index_SmallSawMark_Probe4 = 0;
                int index_SmallSawMark_Probe5 = 0;
                int index_SmallSawMark_Probe6 = 0;

                for (int ni = 0; ni < sawMarkNumIn; ni++)
                {
                    switch (sawMarkDatasIn[ni].probeIndex)
                    {
                        case 0:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                LargeSawMarkPositionOfProbe1_X[index_LargeSawMark_Probe1] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                LargeSawMarkPositionOfProbe1_Y[index_LargeSawMark_Probe1] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_LargeSawMark_Probe1++;
                            }
                            else
                            {
                                SmallSawMarkPositionOfProbe1_X[index_SmallSawMark_Probe1] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                SmallSawMarkPositionOfProbe1_Y[index_SmallSawMark_Probe1] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_SmallSawMark_Probe1++;
                            }
                            break;
                        case 1:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                LargeSawMarkPositionOfProbe2_X[index_LargeSawMark_Probe2] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                LargeSawMarkPositionOfProbe2_Y[index_LargeSawMark_Probe2] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_LargeSawMark_Probe2++;
                            }
                            else
                            {
                                SmallSawMarkPositionOfProbe2_X[index_SmallSawMark_Probe2] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                SmallSawMarkPositionOfProbe2_Y[index_SmallSawMark_Probe2] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_SmallSawMark_Probe2++;
                            }
                            break;
                        case 2:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                LargeSawMarkPositionOfProbe3_X[index_LargeSawMark_Probe3] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                LargeSawMarkPositionOfProbe3_Y[index_LargeSawMark_Probe3] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_LargeSawMark_Probe3++;
                            }
                            else
                            {
                                SmallSawMarkPositionOfProbe3_X[index_SmallSawMark_Probe3] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                SmallSawMarkPositionOfProbe3_Y[index_SmallSawMark_Probe3] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_SmallSawMark_Probe3++;
                            }
                            break;
                        case 3:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                LargeSawMarkPositionOfProbe4_X[index_LargeSawMark_Probe4] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                LargeSawMarkPositionOfProbe4_Y[index_LargeSawMark_Probe4] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_LargeSawMark_Probe4++;
                            }
                            else
                            {
                                SmallSawMarkPositionOfProbe4_X[index_SmallSawMark_Probe4] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                SmallSawMarkPositionOfProbe4_Y[index_SmallSawMark_Probe4] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_SmallSawMark_Probe4++;
                            }
                            break;
                        case 4:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                LargeSawMarkPositionOfProbe5_X[index_LargeSawMark_Probe5] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                LargeSawMarkPositionOfProbe5_Y[index_LargeSawMark_Probe5] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_LargeSawMark_Probe5++;
                            }
                            else
                            {
                                SmallSawMarkPositionOfProbe5_X[index_SmallSawMark_Probe5] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                SmallSawMarkPositionOfProbe5_Y[index_SmallSawMark_Probe5] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_SmallSawMark_Probe5++;
                            }
                            break;
                        case 5:
                            if (sawMarkDatasIn[ni].type == 0)
                            {
                                LargeSawMarkPositionOfProbe6_X[index_LargeSawMark_Probe6] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                LargeSawMarkPositionOfProbe6_Y[index_LargeSawMark_Probe6] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_LargeSawMark_Probe6++;
                            }
                            else
                            {
                                SmallSawMarkPositionOfProbe6_X[index_SmallSawMark_Probe6] = Math.Round(0.5 * (sawMarkDatasIn[ni].peakPos + sawMarkDatasIn[ni].valleyPos), 2);
                                SmallSawMarkPositionOfProbe6_Y[index_SmallSawMark_Probe6] = sawMarkDatasIn[ni].peakValue + 0.1 + 0.001 * sawMarkDatasIn[ni].depthMax;
                                index_SmallSawMark_Probe6++;
                            }
                            break;
                        default:
                            break;
                    }
                }


                #endregion

                return true;
            }
            catch (Exception excep)
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: SetSurfaceDataOfEachProbePair 出错,未知错误", excep);
                return false;
            }


        }


    }
    #endregion


    #region class:ConfigFileContent
    // 说明：Init时参数文件内容
    public class ConfigFileContent
    {
        public string AlgoConfigFilePath { get; set; }
        public string AlgoParamsFileFolder { get; set; }

        public ConfigFileContent()
        {
            AlgoConfigFilePath = System.Environment.CurrentDirectory + @"\AlgoFiles\AlgoConfig.xml";
            AlgoParamsFileFolder = System.Environment.CurrentDirectory + @"\AlgoFiles\AlgoParamFiles\";
        }

    }


    #endregion


    #region class:AlgoConfigFileContent
    // 对外参数配置文件
    public class AlgoConfigFileContent
    {
        public string CurrentSpecName { get; set; }
        public List<SpecNameToAlgoParamFile> ListSpecNameToParam { get; set; }
        public AlgoConfigFileContent()
        {
            CurrentSpecName = "default";
            ListSpecNameToParam = new List<SpecNameToAlgoParamFile>();
        }

        public bool AddSpecName(string specName, string paramFileName)
        {
            if (ListSpecNameToParam == null)
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: AddSpecName 出错; error at AddSpecName,ListSpecNameToParam is null", null);
                return false;
            }
            if ((null != specName) && ("" != specName) && (null != paramFileName))
            {
                if (InListSpecName(specName))
                {
                    return true;
                }
                else
                {
                    SpecNameToAlgoParamFile tempLink = new SpecNameToAlgoParamFile();
                    tempLink.ParamFileName = paramFileName;
                    tempLink.SpecName = specName;
                    ListSpecNameToParam.Add(tempLink);
                    return true;
                }
            }
            return false;
        }
        public bool InListSpecName(string specName)
        {
            if (ListSpecNameToParam == null)
            {
                return false;
            }
            foreach (SpecNameToAlgoParamFile linkI in ListSpecNameToParam)
            {
                if (linkI.SpecName == specName)
                {
                    return true;
                }
            }
            return true;
        }
        public bool GetParamFileName(string specName, out string paramFileName)
        {
            paramFileName = "default_AlgoParams.xml";
            if ((ListSpecNameToParam == null))
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: GetParamFileName 出错,ListSpecNameToParam is null", null);
                return false;
            }
            if (!InListSpecName(specName))
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: GetParamFileName 出错,specName is not in ListSpecNameToParam", null);
                return false;
            }
            foreach (SpecNameToAlgoParamFile linkI in ListSpecNameToParam)
            {
                if (linkI.SpecName == specName)
                {
                    paramFileName = linkI.ParamFileName;
                    return true;
                }
            }
            return false;
        }
        public bool DeleteSpecName(string specName)
        {
            if (ListSpecNameToParam == null)
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: GetParamFileName 出错,ListSpecNameToParam is null", null);
                return false;
            }
            if ((null == specName) || ("" == specName))
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: GetParamFileName 出错,specNmae is null or empty", null);
                return false;
            }
            if (!InListSpecName(specName))
            {
                ////LogHelper.ErrorLog("SawMarkAlgError: GetParamFileName 出错,specName is not in ListSpecNameToParam", null);
                return false;
            }
            if (CurrentSpecName == specName)
            {
                return false;
            }

            foreach (SpecNameToAlgoParamFile tempLink in ListSpecNameToParam)
            {
                if (tempLink.SpecName == specName)
                {
                    ListSpecNameToParam.Remove(tempLink);
                    return true;
                }
            }
            return false;
        }

    }

    #endregion


    #region class:SpecNameToAlgoParamFile
    public class SpecNameToAlgoParamFile
    {
        private string specName;
        private string paramFileName;
        public string SpecName
        {
            get
            {
                return specName;
            }
            set
            {
                specName = value;
            }
        }
        public string ParamFileName
        {
            get
            {
                return paramFileName;
            }
            set
            {
                paramFileName = value;
            }
        }
    }

    #endregion

}
