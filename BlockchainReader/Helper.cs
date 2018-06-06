using Neo.IO;
using System;
using System.IO;
using System.Text;

namespace BlockchainReader
{

    class Helper
    {
        public static string ToHexString(ISerializable ser)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(ser);
            bw.Flush();
            byte[] tx_data = ms.ToArray();
            StringBuilder ret = new StringBuilder();
            foreach (byte b in tx_data)
            {
                //{0:X2} 大写
                ret.AppendFormat("{0:x2} ", b);
            }
            return ret.ToString();
        }
    }
}

