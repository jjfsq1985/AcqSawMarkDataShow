using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcqDataShow
{
    
    public class PropertyGridData
    {
        [CategoryAttribute("计算结果"),
        ReadOnlyAttribute(true) ]
        public string StationName {get;set;}

        [CategoryAttribute("计算结果"),
        ReadOnlyAttribute(true)]
        public bool[] QLevelSawMark { get; set; }
        
        [CategoryAttribute("计算结果"),
        ReadOnlyAttribute(true)]
        public bool[] QLevelThickness { get; set; }
        
        [CategoryAttribute("计算结果"),
        ReadOnlyAttribute(true)]
        public bool[] QLevelTTV { get; set; }
        
        [CategoryAttribute("计算结果"),
        ReadOnlyAttribute(true)]
        public bool[] QLevelWarp { get; set; }

        [CategoryAttribute("计算结果"),
            ReadOnlyAttribute(true)]
        public string TypeName { get; set; }
        
        [CategoryAttribute("计算结果"),
        ReadOnlyAttribute(true)]

        public string[] DataName {get;set;}

        [CategoryAttribute("计算结果"),
            ReadOnlyAttribute(true)]
        public double[] DataValue { get; set; }

        [CategoryAttribute("计算结果"),
           ReadOnlyAttribute(true)]
        public int[] QLevelPeakPos { get; set; }

        [CategoryAttribute("计算结果"),
   ReadOnlyAttribute(true)]
        public int[] QLevelValleyPos { get; set; }
    }
}
