using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Justin.Updater.Server
{
    public static class CryptoHelper
    {
        private static RijndaelManaged CryptoService = new RijndaelManaged();
        private static MD5 md5 = new MD5CryptoServiceProvider();

        /// <summary>
        /// 解密过程
        /// </summary>
        /// <param name="source">被加密的数据</param>
        /// <param name="key">解密串</param>
        /// <returns>解密后的数据</returns>
        public static string Decrypting(string source, string key)
        {
            byte[] bytIn = Convert.FromBase64String(source);
            byte[] bytOut = Decrypting(bytIn, key);
            return UnicodeEncoding.Default.GetString(bytOut);
        }

        /// <summary>
        /// 解密过程
        /// </summary>
        /// <param name="bytIn">待解密的字节数组</param>
        /// <param name="key">解密串</param>
        /// <returns>解密后的字节数组</returns>
        public static byte[] Decrypting(byte[] bytIn, string key)
        {
            if (bytIn == null || bytIn.Length == 0)
            {
                return new byte[0];
            }
            // create a MemoryStream with the input
            MemoryStream ms = new MemoryStream(bytIn, 0, bytIn.Length);

            // set the private key
            CryptoService.Key = GetLegalKey(key);
            CryptoService.IV = GetLegalIV(key);

            // create a Decryptor from the Provider Service instance
            ICryptoTransform encrypto = CryptoService.CreateDecryptor();

            // create Crypto Stream that transforms a stream using the decryption
            CryptoStream cs = new CryptoStream(ms, encrypto, CryptoStreamMode.Read);

            //System.IO.StreamReader sr = new System.IO.StreamReader(cs);

            using (MemoryStream source = new MemoryStream())
            {
                //从压缩流中读出所有数据
                byte[] bytes = new byte[4096];
                int n;
                while ((n = cs.Read(bytes, 0, bytes.Length)) != 0)
                {
                    source.Write(bytes, 0, n);
                }

                return source.ToArray();
            }
        }

        /// <summary>
        /// 取得机密密钥(不符合长度的用空格填充)
        /// </summary>
        /// <param name="key">密钥串</param>
        /// <returns>填充后的密钥</returns>
        private static byte[] GetLegalKey(string key)
        {
            byte[] byte_key = UnicodeEncoding.UTF8.GetBytes(key);
            CryptoService.GenerateKey();
            byte[] byte_temp = CryptoService.Key;
            int key_length = byte_temp.Length;

            for (int i = 0; i < key_length; i++)
            {
                if (i >= byte_key.Length)
                {
                    byte_temp[i] = 32;
                }
                else
                {
                    byte_temp[i] = byte_key[i];
                }
            }

            return byte_temp;
        }

        /// <summary>
        /// 取得初始化向量(不符合长度的用空格填充)
        /// </summary>
        /// <param name="key">密钥串</param>
        /// <returns>密钥向量</returns>
        private static byte[] GetLegalIV(string key)
        {
            byte[] byte_key = UnicodeEncoding.UTF8.GetBytes(key);
            CryptoService.GenerateIV();
            byte[] byte_temp = CryptoService.IV;
            int key_length = byte_temp.Length;

            for (int i = 0; i < key_length; i++)
            {
                if (i >= byte_key.Length)
                {
                    byte_temp[i] = 32;
                }
                else
                {
                    byte_temp[i] = byte_key[i];
                }
            }

            return byte_temp;
        }

        /// <summary>
        /// md5
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string MD5(Stream stream)
        {
            using (Stream _stream = stream)
            {
                return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
            }
        }

        /// <summary>
        /// md5
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string MD5(String str)
        {
            return MD5(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(str)));
        }
    }
}
