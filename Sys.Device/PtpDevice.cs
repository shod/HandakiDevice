using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{
    /// <summary>
    /// Класс для переносного терминала пациента
    /// </summary>
    public class PTpDevice : Device, IDevice
    {

        string[] lstAction = { "35", "3500", "3501", "3565", "3564", "3509"};

        public PTpDevice()
        {                      
            InitAction(lstAction);
            _PingHeader = "35";
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
            AInfo.DeviceName = "ptpdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = true;
            AInfo.isDeleteCommandAfterSend = false;
            AInfo.TimeLive = 120;
            MetaInfo.Add("35", AInfo); //Сообщение о нажатии на кнопки  ТП


            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "ptpdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 300;
            MetaInfo.Add("3564", AInfo); // смена номера устройства

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 300;    
            MetaInfo.Add("3565", AInfo); // смена номера частоты
            
            /*AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 300;    
            MetaInfo.Add("3501", AInfo); // Нажата кнопка-1*/

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 300;
            MetaInfo.Add("3509", AInfo); // таймера ПТП периодического выхода на связь, количества попыток соединения и времени ожидания ответа от РС

            return MetaInfo;
        }        

    }

    /// <summary>
    /// Класс для обработки сообщения о нажатии кнопки
    /// </summary>
    class Action_35 : Action, iAction
    {
        public string[] _MapProtocol;
        public string[] _MapProtocolP;
        public int iPckLen;

        public Action_35()
        {
            this._MapProtocol = new[] { "HEADER", "SOURCEADDR", "REG", "NBYTE", "BAT", "CRC", "UID", "RSII" };
            this._MapProtocolP = new[] { "HEADER:2", "SOURCEADDR:4", "REG:2", "NBYTE:2", "BAT:2", "CRC:2", "UID:8", "RSII:2" };
            this.iPckLen = 24;
        }

        public virtual ResponseData ProcessDevice(string txtPackageLine)
        {
            ResponseData _resDta = new ResponseData();
            string[] var_mapProtocol;
            var_mapProtocol = _MapProtocol;

            _resDta.strHead = "35";
            _resDta.IsError = false;

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

            if (arrPackage.Length != _MapProtocol.Length || arrPackage[7].ToUpper() == "FF")
            {
                _resDta.IsError = true;
            }

            _resDta.IsBeep = true;

            if (arrPackage[2] == "00")
            {
                _resDta.IsBeep = false;
            }

            string txtPackageData = arrPackage[1] + arrPackage[2] + arrPackage[3] + arrPackage[4];
            string strCrc = Helper.CreateCRC(txtPackageData);

            /*Проверка на верность CRC*/
            if (strCrc != arrPackage[5])
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

        public virtual string ProcessToDevice(XDocument xDoc)
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
                            sPackage = sPackage + Helper.IntToHex(this.iPckLen / 2);
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
                throw new Exception("В файле команды 35 отсутствует элемент - " + currcomname);
            }
            return sHeader + sPackage + strCrc;
        }

        /// <summary>
        /// Создание Echo пакета
        /// </summary>
        /// <param name="arrMessage"></param>
        /// <returns></returns>
        public string FormatEchoMessage(string[] arrMessage)
        {
            int cnt = arrMessage.Length;
            string valheader = arrMessage[0];
            string valsource = arrMessage[1];
            string valbody = "";

            valbody = valsource + arrMessage[2] + "06";//arrMessage[3];
            string strCrc = Helper.CreateCRC(valbody);

            return valheader + valbody + strCrc;
        }

        public override string GetBatValue(string[] arrPackage)
        {
            string res = "00";
            double ibat = Convert.ToDouble(Helper.HexToInt(arrPackage[4]));

            ibat = ibat * 0.03;
 
            res = ibat.ToString();
            return res;
        }
    }

    class Action_3500 : Action_35
    {
        
        public Action_3500()
        {
            this._MapProtocol = new[] { "HEADER", "SOURCEADDR", "REG", "NBYTE", "BAT", "CRC", "UID", "RSII" };
            this._MapProtocolP = new[] { "HEADER:2", "DESTADDR:4", "REG:2", "NBYTE:2", "BAT:2", "CRC:2", "UID:8", "RSII:2" };
            this.iPckLen = 24;

            /*this._MapProtocol = new[] { "HEADER", "DESTADDR", "REG", "NBYTE", "CRC" };
            this._MapProtocolP = new[] { "HEADER:2", "DESTADDR:4", "REG:2", "NBYTE:2", "CRC:2" };
            this.iPckLen = 12;*/

            
        }

       
    }

    class Action_3501 : Action_35
    {

        public Action_3501()
        {
            this._MapProtocol = new[] { "HEADER", "SOURCEADDR", "REG", "NBYTE", "BAT", "CRC", "UID", "RSII" };
            this._MapProtocolP = new[] { "HEADER:2", "SOURCEADDR:4", "REG:2", "NBYTE:2", "BAT:2", "CRC:2", "UID:8", "RSII:2" };
            this.iPckLen = 24;
        }


    }


    class Action_3509 : Action_35
    {

        public Action_3509()
        {
            this._MapProtocol = new[] { "HEADER", "DESTADDR", "REG", "NBYTE", "PING_NS", "PING_WT", "TSLEEP", "CRC" };
            this._MapProtocolP = new[] { "HEADER:2", "DESTADDR:4", "REG:2", "NBYTE:2", "PING_NS:2", "PING_WT:2", "TSLEEP:4", "CRC:2" };
            this.iPckLen = 20;

        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {

            string[] _MapProtocol = new[] { "HEADER", "SOURCEADDR", "REG", "NBYTE", "PING_NS", "PING_WT", "TSLEEP", "CRC", "UID", "RSII" };
            string[] _MapProtocolP = new[] { "HEADER:2", "SOURCEADDR:4", "REG:2", "NBYTE:2", "PING_NS:2", "PING_WT:2", "TSLEEP:4", "CRC:2", "UID:8", "RSII:2" };
            this.iPckLen = 30;
            ResponseData _resDta = new ResponseData();
            string[] var_mapProtocol;
            var_mapProtocol = _MapProtocol;

            _resDta.strHead = "35";
            _resDta.IsError = false;

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

            if (arrPackage.Length != _MapProtocol.Length || arrPackage[7].ToUpper() == "FF")
            {
                _resDta.IsError = true;
            }
            
            string txtPackageData = arrPackage[1] + arrPackage[2] + arrPackage[3] + arrPackage[4] + arrPackage[5] + arrPackage[6];
            string strCrc = Helper.CreateCRC(txtPackageData);

            /*Проверка на верность CRC*/
            if (strCrc != arrPackage[7])
            {
                _resDta.IsError = true;
            }
            else
            {
                _resDta.strEcho = FormatEchoMessage(arrPackage);
                arrPackage[8] = arrPackage[8].Substring(4, 4); // UID
                _resDta.strXMLData = ArrayToXML(arrPackage, var_mapProtocol);
            }
            _resDta.IsEchoСonfirmCP = false;
            _resDta.IsBeep = false;
            return _resDta;
        }

        public override string ProcessToDevice(XDocument xDoc){
            string[] _MapProtocol = new[] { "HEADER", "DESTADDR", "REG", "NBYTE", "PING_NS", "PING_WT", "TSLEEP", "CRC" };
            string[] _MapProtocolP = new[] { "HEADER:2", "DESTADDR:4", "REG:2", "NBYTE:2", "PING_NS:2", "PING_WT:2", "TSLEEP:4", "CRC:2" };
            this.iPckLen = 20;

            base._MapProtocol = _MapProtocol;
            string sreRes = base.ProcessToDevice(xDoc);            

            return sreRes;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_3539 : Action, iAction
    {

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = 16;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "3539";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }
    }


    /// <summary>
    /// Класс для обработки сообщения для установки TSLEEP
    /// </summary>
    class Action_3521 : Action, iAction
    {

        public override string ProcessToDevice(XDocument xDoc)
        {
            this.iPckLen = 30;
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "TSLEEP", "CRC" };

            base._MapProtocol = _MapProtocol;
            string sreRes = base.ProcessToDevice(xDoc);
            sreRes = "21"+sreRes.Substring(2);

            return sreRes;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_3565 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
        public override RequestData getObjectCommand(XDocument xDoc)
        {
            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = true;                                    
            return ResData;
        }
        public override string ProcessToDevice(XDocument xDoc)
        {
            string sPackage = "";
            base._MapProtocol = _MapProtocol;
            string _resDta = base.ProcessToDevice(xDoc);
            return _resDta;
        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = _MapProtocol.Length+2;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене номера устойства
    /// </summary>
    class Action_3564 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
        public override RequestData getObjectCommand(XDocument xDoc)
        {
            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = true;            
            return ResData;
        }
        public override string ProcessToDevice(XDocument xDoc)
        {
            string sPackage = "";
            base._MapProtocol = _MapProtocol;
            string _resDta = base.ProcessToDevice(xDoc);            
            return _resDta;
        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = _MapProtocol.Length + 2;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }
    }
}
