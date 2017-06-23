using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace AcqDataShow
{
    public class APSerializer
    {
        #region XML Serialize
        /// <summary>
        /// 将对象序列化为xml文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="obj">对象</param>
        /// <param name="nameSpace">命名空间</param>
        public static void XmlSerialize<T>(T obj, string filePath, string nameSpace)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T), nameSpace);
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                StreamWriter writer = new StreamWriter(fs);
                xs.Serialize(writer, obj);
            }
        }
        /// <summary>
        /// xml反序化器
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="nameSpace">命名空间</param>
        /// <returns></returns>
        public static T XmlDeserialize<T>(string filePath, string nameSpace)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                XmlSerializer xs = new XmlSerializer(typeof(T), nameSpace);
                T ret = (T)xs.Deserialize(reader);
                return ret;
            }
        }
        /// <summary>
        /// 将对象序列化为xml文件,默认命名空间为：www.apintec.com
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="obj">对象</param>
        public static void XmlSerialize<T>(T obj, string filePath)
        {
            XmlSerializer xs = new XmlSerializer(typeof(T));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                StreamWriter writer = new StreamWriter(fs);
                xs.Serialize(writer, obj);
            }
        }
        /// <summary>
        /// xml反序化器,默认命名空间为：www.apintec.com
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static T XmlDeserialize<T>(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                XmlSerializer xs = new XmlSerializer(typeof(T));
                T ret = (T)xs.Deserialize(reader);
                return ret;
            }
        }
        #endregion

    }
}
