using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{
    /// <summary>
    /// Класс для переносного терминала медсестры
    /// </summary>
    public class PtmDevice : Device, IDevice
    {

        string[] lstAction = { "E9", "E909", "E900", "E901" };

        public PtmDevice()
        {
            InitAction(lstAction);
            _PingHeader = "E9";
            this.RegPosition = 6;
        }

        /// <summary>
        /// Получение метаинфрмации
        /// </summary>
        /// <returns></returns>        
        public Dictionary<string, ActionMetaInfo> GetMetaInfo()
        {
            var MetaInfo = new Dictionary<string, ActionMetaInfo>();

            ActionMetaInfo AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "ptmdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = false;
            AInfo.isDeleteCommandAfterSend = true;
            AInfo.CountAttSend = 2;
            MetaInfo.Add("E9", AInfo); // Базовый запрос

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "ptmdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = false;
            AInfo.CountAttSend = 2;
            //AInfo.isDeleteCommandAfterSend = true;
            MetaInfo.Add("E909", AInfo);

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "ptmdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = false;
            AInfo.CountAttSend = 2;
            //AInfo.isDeleteCommandAfterSend = true;
            MetaInfo.Add("E900", AInfo);

            // Нажатие на кнопку 1
            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "ptmdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = false;
            AInfo.CountAttSend = 2;            
            MetaInfo.Add("E901", AInfo);

            return MetaInfo;
        }

        public string GetMessagePingToMozek(TypePing TypeMessage, ResponseData ResData)
        {
            string _Res = "";
            if (ResData.strIdDevice != "0000")
            {
                _Res = base.GetMessagePingToMozek(TypeMessage, ResData);
            }
            return _Res;
        }

    }

    /// <summary>
    /// Класс для обработки сообщений общий
    /// </summary>
    class Action_E9 : Action_E909
    {

    }

    /// <summary>
    /// Класс для обработки сообщений на сброс настроек (sleep)
    /// </summary>
    class Action_E900 : Action_E909
    {

        public Action_E900()
        {
            this._MapProtocol = new[] { "HEADER", "DESTADDR", "REG", "NBYTE", "CRC" };
            this._MapProtocolP = new[] { "HEADER:2", "DESTADDR:4", "REG:2", "NBYTE:2", "CRC:2" };
            this.iPckLen = 12;
        }

    }

    /// <summary>
    /// Класс для обработки команды вывода на цифровой индикатор
    /// </summary>
    class Action_E909 : Action, iAction
    {
        public string[] _MapProtocol = { "HEADER", "DESTADDR", "REG", "NBYTE", "TZUM", "TNUM", "NUM", "CRC" };
        public string[] _MapProtocolP = { "HEADER:2", "DESTADDR:4", "REG:2", "NBYTE:2", "TZUM:2", "TNUM:2", "NUM:4", "CRC:2" };
        public int iPckLen = 20;

        public Action_E909()
        {
            this._MapProtocol = new[] { "HEADER", "DESTADDR", "REG", "NBYTE", "TZUM", "TNUM", "NUM", "CRC" };
            this._MapProtocolP = new[] { "HEADER:2", "DESTADDR:4", "REG:2", "NBYTE:2", "TZUM:2", "TNUM:2", "NUM:4", "CRC:2" };
            this.iPckLen = 20;
        }

        public ResponseData ProcessDevice(string txtPackageLine)
        {
            ResponseData _resDta = new ResponseData();
            string[] var_mapProtocol;
            var_mapProtocol = _MapProtocol;

            _resDta.strHead = "E909";
            _resDta.IsError = false;

            string crcPackageData = "";
            txtPackageLine = txtPackageLine.Replace(" ", "");

            if (txtPackageLine.Length < iPckLen)
            {
                _resDta.IsError = true;
                return _resDta;
            }

            string txtPackage = txtPackageLine.Substring(0, iPckLen);

            string[] arrPackage = SplitLine(txtPackage, _MapProtocolP);

            _resDta.strIdDevice = arrPackage[1];
            _resDta.strDataRest = txtPackageLine.Substring(iPckLen);

            if (arrPackage.Length != _MapProtocol.Length)
            {
                _resDta.IsError = true;
            }

            _resDta.IsBeep = true;


            for(int i = 1; i < (arrPackage.Length - 2); i++)
            {
                crcPackageData = crcPackageData + arrPackage[i];
            }

            string strCrc = Helper.CreateCRC(crcPackageData);

            /*Проверка на верность CRC*/
            if (strCrc != arrPackage[7])
            {
                _resDta.IsError = true;
            }
            else
            {
                _resDta.strEcho = FormatEchoMessage(arrPackage);
                arrPackage[6] = arrPackage[6].Substring(4, 4);
                _resDta.strXMLData = ArrayToXML(arrPackage, var_mapProtocol);
            }
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }

        public string ProcessToDevice(XDocument xDoc)
        {
            string sPackage = "";
            string strCrc;
            ResponseData dRes = new ResponseData();
            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string sHeader = xEvent.Element("HEADER").Value;
            string currcomname = "";

            try
            {
                foreach (string lname in this._MapProtocol)
                {
                    currcomname = lname;

                    switch (lname)
                    {
                        case "HEADER":
                        case "CRC":
                            break;
                        case "DESTADDR":
                            sPackage = xEvent.Element("DESTADDR").Value;
                            break;
                        case "NBYTE":
                            sPackage = sPackage + Helper.IntToHex(iPckLen/2);
                            break;
                        default:
                            sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                            break;
                    }
                }

                strCrc = Helper.CreateCRC(sPackage);
            }
            catch (Exception ex)
            {
                throw new Exception("В файле команды E9 отсутствует элемент - " + currcomname);
            }
            return sHeader + sPackage + strCrc;
        }

    }

    /// <summary>
    /// Класс для обработки сообщений нажатия кнопки
    /// </summary>
    class Action_E901 : Action, iAction
    {
        public string[] _MapProtocol = new[] { "HEADER", "SOURCEADDR", "REG", "NBYTE", "BAT", "CRC" };
        public string[] _MapProtocolP = new[] { "HEADER:2", "SOURCEADDR:4", "REG:2", "NBYTE:2", "BAT:2", "CRC:2" };
        public int iPckLen = 14;

        public Action_E901()
        {
            this._MapProtocol = new[] { "HEADER", "SOURCEADDR", "REG", "NBYTE", "BAT", "CRC" };
            this._MapProtocolP = new[] { "HEADER:2", "SOURCEADDR:4", "REG:2", "NBYTE:2", "BAT:2", "CRC:2" };
            this.iPckLen = 14;
        }

        public ResponseData ProcessDevice(string txtPackageLine)
        {
            ResponseData _resDta = new ResponseData();
            string[] var_mapProtocol;
            var_mapProtocol = _MapProtocol;

            _resDta.strHead = "E901";
            _resDta.IsError = false;

            string crcPackageData = "";
            txtPackageLine = txtPackageLine.Replace(" ", "");

            if (txtPackageLine.Length < iPckLen)
            {
                _resDta.IsError = true;
                return _resDta;
            }

            string txtPackage = txtPackageLine.Substring(0, iPckLen);

            string[] arrPackage = SplitLine(txtPackage, _MapProtocolP);

            _resDta.strIdDevice = arrPackage[1];
            _resDta.strDataRest = txtPackageLine.Substring(iPckLen);

            if (arrPackage.Length != _MapProtocol.Length)
            {
                _resDta.IsError = true;
            }

            _resDta.IsBeep = true;


            for (int i = 1; i < (arrPackage.Length - 1); i++)
            {
                crcPackageData = crcPackageData + arrPackage[i];
            }

            string strCrc = Helper.CreateCRC(crcPackageData);

            /*Проверка на верность CRC*/
            if (strCrc != arrPackage[5])
            {
                _resDta.IsError = true;
            }
            else
            {
                _resDta.strEcho = FormatEchoMessage(arrPackage);                
                _resDta.strXMLData = ArrayToXML(arrPackage, var_mapProtocol);
            }
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }

        public override string GetBatValue(string[] arrPackage)
        {
            string res = "00";
            double ibat = Convert.ToDouble(Helper.HexToInt(arrPackage[4]));

            ibat = ibat * 0.03;

            res = ibat.ToString();
            return res;
        }

        public string ProcessToDevice(XDocument xDoc)
        {
            string sPackage = "";
            string strCrc;
            ResponseData dRes = new ResponseData();
            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string sHeader = xEvent.Element("HEADER").Value;
            string currcomname = "";

            try
            {
                foreach (string lname in this._MapProtocol)
                {
                    currcomname = lname;

                    switch (lname)
                    {
                        case "HEADER":
                        case "CRC":
                            break;
                        case "DESTADDR":
                            sPackage = xEvent.Element("DESTADDR").Value;
                            break;
                        case "NBYTE":
                            sPackage = sPackage + Helper.IntToHex(iPckLen/2);
                            break;
                        case "REG":
                            sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                            break;
                        default:
                            sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                            break;
                    }
                }

                strCrc = Helper.CreateCRC(sPackage);
            }
            catch (Exception ex)
            {
                throw new Exception("В файле команды E9 отсутствует элемент - " + currcomname);
            }
            return sHeader + sPackage + strCrc;
        }

    }
}