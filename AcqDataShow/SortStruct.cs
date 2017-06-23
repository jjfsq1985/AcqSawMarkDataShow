using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Data;

namespace AcqDataShow
{
    //多测量数值的结果
    [Serializable]
    public class GropResults
    {
        private StationType stationType;
        private ValidType validResult = ValidType.Normal;  //叠片等不需要进行分选的特殊情况
        private bool[,] classifyResult;   //每个测量项有18个结果
        private CalType[] classifyResultNames;   //classifyResult中每一维对应的测量项名称

        private CalResult[] calResult;  //测量数据+中间数据
        private FlawArea flaw;          //瑕疵位置

        #region 属性

        public ValidType ValidResult
        {
            get
            {
                return validResult;
            }
            set
            {
                validResult = value;
            }
        }


        public bool[,] ClassifyResult
        {
            get
            {
                return classifyResult;
            }
            set
            {
                classifyResult = value;
            }
        }
    

        //测量数据
        [CategoryAttribute("测量结果数据"), DescriptionAttribute("测量结果数据查看"), TypeConverterAttribute(typeof(ArrayConverter))]
        public CalResult[] DataResults
        {
            get
            {
                return calResult;
            }
            set
            {
                calResult = value;
            }
        }


        [CategoryAttribute("缺陷位置"), DescriptionAttribute("缺陷位置查看"), TypeConverterAttribute(typeof(ExpandableObjectConverter))]
        public FlawArea Flaw
        {
            get
            {
                return flaw;
            }
            set
            {
                flaw = value;
            }
        }

        public StationType StationName
        {
            get
            {
                return stationType;
            }
            set
            {
                stationType = value;
            }


        }

        public CalType[] ClassifyResultNames
        {
            get
            {
                return classifyResultNames;
            }
            set
            {
                classifyResultNames = value;
            }


        }

        #endregion
    }

    [Serializable]
    [CategoryAttribute("测量结果"), DescriptionAttribute("测量结果"), TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    public class CalResult
    {
        public CalType calType;

        
        public ResultType[] result;

        [CategoryAttribute("测量结果"), DescriptionAttribute("测量结果"), TypeConverterAttribute(typeof(ArrayConverter))]
        public ResultType[] Result
        {
            get
            {
                return result;
            }
        }

        
        public ResultType[] processData;


        [CategoryAttribute("测量过程结果"), DescriptionAttribute("测量过程结果"), TypeConverterAttribute(typeof(ArrayConverter))]
        public ResultType[] ProcessData
        {
            get
            {
                return processData;
            }
        }

    }

    [Serializable]
    [CategoryAttribute("数据"), DescriptionAttribute("数据"), TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    public struct ResultType
    {
        public string name;
        public double value;
        public string remark;

        public string Name
        {
            get
            {
                return name;
            }
        }

        public double Value
        {
            get
            {
                return value;
            }
        }

        public string Remark
        {
            get
            {
                return remark;
            }
        }


    }


    public enum ValidType
    {
        Normal,
        Timeout,     //超时
        CalError,    //计算异常
        TranError,   //通讯异常
        LightError,  //光源异常
        ChipError,   //碎片
        OverlapError,//叠片
        OutView,//超视野
        MissWafer, //视野内无硅片
    }

    //算法参数
    [Serializable]
    public class Parameter
    {
        private StationType station;

        private ClassifyParameter[] classifyPar;

        private CellParameter[] commonPar;

        //参数属相
        public ClassifyParameter[] ClassifyPar
        {
            get
            {
                return classifyPar;
            }
            set
            {
                classifyPar = value;
            }
        }

        public CellParameter[] CommonPar
        {
            get
            {
                return commonPar;
            }
            set
            {
                commonPar = value;
            }
        }

        public StationType Station
        {
            get
            {
                return station;
            }
            set
            {
                station = value;
            }
        }
    }

    //工位名称
    [Serializable]
    public enum StationType
    {
        Geometry,
        Surface,
        SawMark,
        IR,
        Electrical
    }

    //测量项名称
    [Serializable]
    public enum CalType
    {
        EdgeLengthGeo,       //边长
        DiagonalLengthGeo,   //对角线
        OrthogonalityGeo,    //垂直度
        ChamferWidthGeo,    //倒角宽度
        ChordLengthGeo,  //弦长（倒角长度）
        ChamferArcLengthGeo,  //弧长       
        ChamferAngleGeo, //倒角角度
        VShapeatEdgeGeo,    //缺角

        SawMarkSM,
        ThicknessSM,
        TTVSM,
        WarpSM,

        StainSurface,
        ChippingSurface,
        PinHoleSurface,

        MicrocrackIR,
        InclusionIR,
        PinHoleIR,
        LargeCrackIR,
    }



    #region 瑕疵定义
    //瑕疵区域类
    [Serializable]
    public class FlawArea
    {
        private int flawNum; //瑕疵个数
        private ARegion[] flawAreas; //瑕疵位置记录

        #region 属性
        public int FlawNum
        {
            get
            {
                return flawNum;
            }
            set
            {
                flawNum = value;
            }
        }

        [CategoryAttribute("瑕疵位置"), DescriptionAttribute("瑕疵位置查看"), TypeConverterAttribute(typeof(ArrayConverter))]
        public ARegion[] FlawAreas
        {
            get
            {
                return flawAreas;
            }
            set
            {
                flawAreas = value;
            }
        }
        #endregion

    }

    //瑕疵区域表示
    [Serializable]
    [CategoryAttribute("位置"), DescriptionAttribute("位置查看"), TypeConverterAttribute(typeof(ExpandableObjectConverter))]
    public struct ARegion
    {
        public CalType calType;
        public double xPos; //矩形中心点X坐标
        public double yPos; //矩形中心点Y坐标
        public double len1; //矩形halfWidth
        public double len2; //矩形halfHeight
        public double angle; //矩形旋转的角度

        public CalType CalType
        {
            get
            {
                return calType;
            }
        }


        public double XPox
        {
            get
            {
                return xPos;
            }
        }

        public double YPos
        {
            get
            {
                return yPos;
            }
        }

        public double Len1
        {
            get
            {
                return len1;
            }
        }


        public double Len2
        {
            get
            {
                return len2;
            }
        }

        public double Angle
        {
            get
            {
                return angle;
            }
        }
    }
    #endregion


    //参数表示法
    [Serializable]
    public struct CellParameter
    {
        public string remark;
        public string parValue;
        public ParType parType;
    }


    #region 算法参数细分
    public class ClassifyParameter
    {
        public CalType calType;
        public bool isShield;
        public CellParameter[] parDetail;
    }

    public enum ParType
    {
        //通用参数
        ClassifyingName,  //规则名字
        Format,  //0 表示小晶片  1 表示大晶
        Pseudo,  //正方形晶片（值 0）和假正方形晶片(值1)
        Size,   //产品宽度，125或者156
        Materials, //此处提及的是已分拣晶片的属性，例如晶片是否具有单晶体结构或多晶体结构
        SawTechnology,//切割工艺 （0表示砂浆线，1表示金刚线）
        LineSpeed, //线速
        Coord,     //坐标系
        CutLength, //最小切割矩形长
        CutWidth,  //最小切割矩形宽
        LaserSpace,//激光模组偏移量
        DiagonalLengthStd,//对角线标准值
        WaferSpace, //硅片之间的间距



        //几何测量
        EdgeLengthMin, //边长规格
        EdgeLengthMax,

        DiagonalLengthMin,//对角线规格
        DiagonalLengthMax,

         //垂直度规格
        OrthogonalityMin,
        OrthogonalityMax,

        //弧长规格
        ChamferArcMin,
        ChamferArcMax,

        //弦长（倒角长度）规格
        ChordLengthMin,  
        ChordLengthMax,

        //倒角宽度规格
        ChamferWidthMin,    
        ChamferWidthMax,

        //倒角角度
        ChamferAngleMin, 
        ChamferAngleMax,

        VShapeLengthMax,      //缺角规格
        VShapeDepththMax,
        VShapeLengthMin,
        VShapeDepthMin,
        VshapeNum_max,

        //表面测量
        StainSizeMin,      //表面脏污
        StainSizeMax,
        StainNumMax,

        ChipsLengthMin,   //崩边
        ChipsDepthMin,
        ChipsLengthMax,
        ChipsDepthMax,
        ChipsNumMax,
        ChipsLengthSumMax,

        HoleNumMaxSurface,   //气孔


        //线痕
        SawMarkDepthMin,   //线痕
        SawMarkDepthMax,
        SawMarkNumMax,
        SmallSawMarkDepthMin,
        SmallSawMarkDepthMax,
        SmallSawMarkNumMax,

        ThicknessMin,    //厚度
        ThicknessMax,
        //ThicknessMinPt,
        //ThicknessMaxPt,

        TTVMin,         //TTV
        TTVMax,
        //LATFMax,

        WarpMin,        //翘曲
        WarpMax,

        //隐裂
        MicrocrackMax,
        CrackNumMax,
        InclusionNumMax,
        HoleNumMaxIR,

    }
    #endregion
}
