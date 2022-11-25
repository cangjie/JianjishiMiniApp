using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



using System.Web;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

using System.IO;
using System.Net;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MiniApp.Models;
using Microsoft.EntityFrameworkCore;

namespace MiniApp
{
    
    /// <summary>
    /// Summary description for Util
    /// </summary>
    public class Util
    {
        public static string workingPath = $"{Environment.CurrentDirectory}";

        public Util()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public static string GetDbConStr(string fileName)
        {
            string conStr = "";

            string filePath = workingPath + "/" + fileName;

            using (StreamReader sr = new StreamReader(filePath, true))
            {
                conStr = sr.ReadToEnd();
                sr.Close();
            }
            return conStr;
        }

        public static string UrlEncode(string urlStr)
        {
            return HttpUtility.UrlEncode(urlStr.Trim().Replace(" ", "+").Replace("'", "\""));
        }

        public static string UrlDecode(string urlStr)
        {
            return HttpUtility.UrlDecode(urlStr).Replace(" ", "+").Trim();
        }


        public static string GetNonceString(int length)
        {
            string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            char[] charsArr = chars.ToCharArray();
            int charsCount = chars.Length;
            string str = "";
            Random rnd = new Random();
            for (int i = 0; i < length - 1; i++)
            {
                str = str + charsArr[rnd.Next(charsCount)].ToString();
            }
            return str;
        }

        public static string GetMd5Sign(string KeyPairStringWillBeSigned, string key)
        {
            string str = GetSortedArrayString(KeyPairStringWillBeSigned);
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bArr = md5.ComputeHash(Encoding.UTF8.GetBytes(str + "&key=" + key.Trim()));
            string ret = "";
            foreach (byte b in bArr)
            {
                ret = ret + b.ToString("x").PadLeft(2, '0').ToUpper();
            }
            return ret;
        }

        public static string ConverXmlDocumentToStringPair(XmlDocument xmlD)
        {
            XmlNodeList nl = xmlD.ChildNodes[0].ChildNodes;
            string str = "";
            foreach (XmlNode n in nl)
            {
                str = str + "&" + n.Name.Trim() + "=" + n.InnerText.Trim();
            }
            str = str.Remove(0, 1);
            return str;
        }

        public static bool IsCellNumber(string number)
        {
            bool ret = false;
            number = number.Trim();
            string regCmccStr = @"^(134[012345678]\d{7}|1[34578][012356789]\d{8})$";
            string regCuccStr = @"^1[34578][01256]\d{8}$";
            string regCtccStr = @"^1[3578][01379]\d{8}$";
            Regex regCmcc = new Regex(regCmccStr);
            Regex regCucc = new Regex(regCuccStr);
            Regex regCtcc = new Regex(regCtccStr);
            if (regCmcc.IsMatch(number) || regCtcc.IsMatch(number) || regCucc.IsMatch(number))
            {
                ret = true;
            }
            else
            {
                ret = false;
            }
            return ret;
        }

        public static bool IsNumeric(string number)
        {
            Regex reg = new Regex(@"^([1-9]\d*|0)$");
            return reg.IsMatch(number);
        }

        public static string GetSHA1(string str)
        {
            SHA1 sha = SHA1.Create();
            ASCIIEncoding enc = new ASCIIEncoding();
            byte[] bArr = enc.GetBytes(str);
            bArr = sha.ComputeHash(bArr);
            string validResult = "";
            for (int i = 0; i < bArr.Length; i++)
            {
                validResult = validResult + bArr[i].ToString("x").PadLeft(2, '0');
            }
            return validResult.Trim();
        }

        public static string GetMd5(string str)
        {
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bArr = md5.ComputeHash(Encoding.UTF8.GetBytes(str));
            string ret = "";
            foreach (byte b in bArr)
            {
                ret = ret + b.ToString("x").PadLeft(2, '0');
            }
            return ret;
        }

        public static string GetLongTimeStamp(DateTime currentDateTime)
        {
            TimeSpan ts = currentDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

       
        public static string GetWebContent(string url, string method, string content, string contentType, CookieCollection cookieCollection, Encoding enc)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method.Trim();
            req.ContentType = contentType;
            if (!content.Trim().Equals(""))
            {
                StreamWriter sw = new StreamWriter(req.GetRequestStream());
                sw.Write(content);
                sw.Close();
            }
            //CookieContainer cookieContainer = new CookieContainer();
            string cookieString = "";
            foreach (Cookie c in cookieCollection)
            {
                cookieString = cookieString + (cookieString.Trim().Equals("") ? "" : "&") + c.Name.Trim() + "=" + c.Value.Trim();
            }
            req.Headers.Add("Cookie", cookieString);
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            StreamReader sr = new StreamReader(s, enc);
            string str = sr.ReadToEnd();
            sr.Close();
            s.Close();
            res.Close();
            req.Abort();
            return str;
        }

        public static string GetWebContent(string url, string method, string content, string contentType)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = method.Trim();
            req.ContentType = contentType;
            if (!content.Trim().Equals(""))
            {
                StreamWriter sw = new StreamWriter(req.GetRequestStream());
                sw.Write(content);
                sw.Close();
            }
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            StreamReader sr = new StreamReader(s);
            string str = sr.ReadToEnd();
            sr.Close();
            s.Close();
            res.Close();
            req.Abort();
            return str;
        }

        public static string GetWebContent(string url)
        {
            return GetWebContent(url, "GET", "", "html/text");
        }



        public static byte[] GetQrCodeByTicket(string ticket)
        {
            byte[] bArrTmp = new byte[1024 * 1024 * 10];
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://mp.weixin.qq.com/cgi-bin/showqrcode?ticket=" + ticket);
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();
            Stream s = res.GetResponseStream();
            int i = 0;
            int j = s.ReadByte();
            for (; j != -1; i++)
            {
                bArrTmp[i] = (byte)j;
                j = s.ReadByte();
            }
            byte[] bArr = new byte[i];
            for (j = 0; j < i; j++)
            {
                bArr[j] = bArrTmp[j];
            }
            res.Close();
            req.Abort();
            return bArr;

        }

        public static bool SaveBytesToFile(string path, byte[] bArr)
        {
            if (File.Exists(path))
            {
                return false;
            }
            else
            {
                Stream s = File.Create(path);
                s.Write(bArr, 0, bArr.Length);
                s.Close();
                return true;
            }
        }

        public static string ticket = string.Empty;
        public static DateTime ticketTime = DateTime.MinValue;
        
        
        public static string GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        public static string GetTimeStamp(DateTime currentDateTime)
        {
            TimeSpan ts = currentDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

        public static long GetExactTimeStamp(DateTime currentDateTime)
        {
            TimeSpan ts = currentDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }

        public static string GetServerLocalTimeStamp()
        {
            return GetTimeStamp(DateTime.Now);
        }


        public static int GetQrCode(string eventKey)
        {
            int qrCode = 0;
            if (eventKey.ToLower().StartsWith("qrscene_"))
            {
                eventKey = eventKey.Trim().Replace("qrscene_", "");
            }
            try
            {
                qrCode = int.Parse(eventKey);
            }
            catch
            {

            }
            return qrCode;
        }


        public static string GetSimpleJsonStringFromKeyPairArray(KeyValuePair<string, object>[] vArr)
        {
            string r = "";
            for (int i = 0; i < vArr.Length; i++)
            {
                if (i == 0)
                {
                    r = "\"" + vArr[i].Key.Trim() + "\" : \"" + vArr[i].Value.ToString() + "\"";
                }
                else
                {
                    r = r + ", \"" + vArr[i].Key.Trim() + "\" : \"" + vArr[i].Value.ToString() + "\" ";
                }
            }
            return "{ " + r + " }";
        }


        public static byte[] GetBinaryFileContent(string filePathName)
        {
            FileStream fileStream = File.OpenRead(filePathName);
            byte[] bArr = new byte[fileStream.Length];
            for (long i = 0; i < fileStream.Length; i++)
            {
                bArr[i] = (byte)fileStream.ReadByte();
            }
            return bArr;
        }

        public static string GetHaojinMd5Sign(string KeyPairStringWillBeSigned, string key)
        {
            string str = GetSortedArrayString(KeyPairStringWillBeSigned);
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] bArr = md5.ComputeHash(Encoding.UTF8.GetBytes(str + key.Trim()));
            string ret = "";
            foreach (byte b in bArr)
            {
                ret = ret + b.ToString("x").PadLeft(2, '0').ToUpper();
            }
            return ret;
        }

        public static string GetSortedArrayString(string str)
        {
            string[] strArr = str.Split('&');
            Array.Sort(strArr);
            return String.Join("&", strArr);
        }

        public static string ConvertDataFieldsToJson(DataRow dr)
        {
            string ret = "";
            foreach (DataColumn dc in dr.Table.Columns)
            {
                ret = ret + (ret.Trim().Equals("") ? " " : ", ") + "\"" + dc.Caption.Trim() + "\": \""
                    + dr[dc].ToString().Replace("\n", "").Replace("\r", "<BR/>").Replace("\"", "'").Trim() + "\"";
            }
            return "{" + ret.Trim() + "}";
        }

        public static bool InDate(DateTime currentDate, string dateDescription)
        {
            string[] dayDescItems = dateDescription.Trim().Split(',');
            foreach (string day in dayDescItems)
            {
                if (day.Trim().Equals("周六"))
                {
                    if (currentDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        return true;
                    }
                }
                if (day.Trim().Equals("周日"))
                {
                    if (currentDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        return true;
                    }
                }
                if (day.Trim().Equals("周五"))
                {
                    if (currentDate.DayOfWeek == DayOfWeek.Friday)
                    {
                        return true;
                    }
                }
                if (day.IndexOf("--") >= 0)
                {
                    try
                    {
                        DateTime startDate = DateTime.Parse(day.Replace("--", "#").Split('#')[0].Trim());
                        DateTime endDate = DateTime.Parse(day.Replace("--", "#").Split('#')[1].Trim());
                        if (currentDate.Date >= startDate && currentDate.Date <= endDate)
                        {
                            return true;
                        }
                    }
                    catch
                    {

                    }
                }
                try
                {
                    if (currentDate.Date == DateTime.Parse(day.Trim()))
                    {
                        return true;
                    }
                }
                catch
                {

                }
            }
            return false;
        }



        public static string AES_decrypt(string encryptedDataStr, string key, string iv)
        {
            RijndaelManaged rijalg = new RijndaelManaged();
            //-----------------    
            //设置 cipher 格式 AES-128-CBC    

            rijalg.KeySize = 128;

            rijalg.Padding = PaddingMode.PKCS7;
            rijalg.Mode = CipherMode.CBC;

            rijalg.Key = Convert.FromBase64String(key);
            rijalg.IV = Convert.FromBase64String(iv);


            byte[] encryptedData = Convert.FromBase64String(encryptedDataStr);
            //解密    
            ICryptoTransform decryptor = rijalg.CreateDecryptor(rijalg.Key, rijalg.IV);

            string result = "";

            using (MemoryStream msDecrypt = new MemoryStream(encryptedData))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {

                        result += srDecrypt.ReadToEnd();
                    }
                }
            }

            return result;
        }

        



    }
}
