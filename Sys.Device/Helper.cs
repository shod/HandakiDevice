using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Sys.Device
{
    internal static class Helper
    {
        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            byte[] array = new byte[bits.Length / 8];
            bits.CopyTo(array, 0);
            return array;
        }
        public static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder stringBuilder = new StringBuilder(data.Length * 3);
            for (int i = 0; i < data.Length; i++)
            {
                byte value = data[i];
                stringBuilder.Append(Convert.ToString(value, 16).PadLeft(2, '0').PadRight(3, ' '));
            }
            return stringBuilder.ToString().ToUpper();
        }
        public static long HexToDouble(string strHex)
        {
            string text = string.Empty;
            int num = strHex.Length / 2;
            for (int i = 0; i < num; i++)
            {
                text = strHex.Substring(i * 2, 2) + text;
            }
            return long.Parse(text, NumberStyles.HexNumber);
        }
        public static string IntToHex(int value)
        {
            return value.ToString("x2");// string.Format("{00:X}", value);
        }
        public static string Data_Hex_Asc(string Data)
        {
            string str = "";
            while (Data.Length > 0)
            {
                string str2 = Convert.ToChar(Convert.ToUInt32(Data.Substring(0, 2), 16)).ToString();
                str += str2;
                Data = Data.Substring(2, Data.Length - 2);
            }
            return Data;
        }
        public static int HexToInt(string value)
        {
            return int.Parse(value, NumberStyles.HexNumber);
        }

        /// <summary>
        /// Перевод hex строка в битовую строку 101010101
        /// </summary>
        /// <param name="hexValue"></param>
        /// <returns></returns>
        public static string HexToBinary(string hexValue)
        {

            string binaryString = string.Empty;

            binaryString = Convert.ToString(Convert.ToInt32(hexValue, 16), 2).PadLeft(hexValue.Length * 4, '0');
            return binaryString;
        }

        public static string HexToBinary32(string hexValue)
        {

            string binaryString = string.Empty;

            binaryString = Convert.ToString(Convert.ToInt32(hexValue, 16), 2).PadLeft(hexValue.Length * 8, '0');
            return binaryString;
        }

        /// <summary>
        /// Создание CRC кода на данные
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static string CreateCRC(string Data)
        {
            int num = 0;
            int num2 = Data.Length / 2;
            for (int i = 0; i < num2; i++)
            {
                string value = Data.Substring(i * 2, 2);
                num = Helper.HexToInt(value) + num;
            }
            string text = num.ToString("X2");
            return text.Substring(text.Length - 2);
        }

    }
}
