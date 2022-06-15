using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{
    /// <summary>
    /// Класс для терминала пациента
    /// </summary>
    public class TpDevice : Device, IDevice
    {

        string[] lstAction = { "3300", "33", "21", "39", "3321", "3365", "3364" };

        public TpDevice()
        {                      
            InitAction(lstAction);
            _PingHeader = "21";
        }   

        /// <summary>
        /// Получение метаинфрмации
        /// </summary>
        /// <returns></returns>        
        public Dictionary<string, ActionMetaInfo> GetMetaInfo()
        {
            var MetaInfo = new Dictionary<string, ActionMetaInfo>();
            ActionMetaInfo AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = true;
            AInfo.isDeleteCommandAfterSend = true;
            AInfo.CountAttSend = 1;
            AInfo.TimeLive = 120;
            MetaInfo.Add("33", AInfo); //Сообщение о нажатии на кнопки  ТП

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 3600*48;
            MetaInfo.Add("21", AInfo); //Периодическое сообщение от ТП

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 3600 * 48;
            MetaInfo.Add("3321", AInfo); //Периодическое сообщение от ТП

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 3600;
            MetaInfo.Add("39", AInfo); // пустой класс

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 300;
            MetaInfo.Add("3364", AInfo); // смена номера устройства

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tpdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = true;
            AInfo.TimeLive = 300;    
            MetaInfo.Add("3365", AInfo); // смена номера частоты
            

            return MetaInfo;
        }

        public override string getActionIndex(string txtPackageLine)
        {
            string sDKey = txtPackageLine.Substring(0, 2);
            /*if (sDKey == "33")
            {
                string reg = txtPackageLine.Substring(2, 2);
                if (reg == "21")
                {
                    sDKey = sDKey + reg; //Находим байт REG  
                }
            }*/
            return sDKey;
        }        

    }

    /// <summary>
    /// Класс для обработки сообщения о нажатии кнопки
    /// </summary>
    class Action_33 : Action_3300
    {

    }

    class Action_3300 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC" };
        int iPckLen = 6 * 2;

        public ResponseData ProcessDevice(string txtPackageLine){
            ResponseData _resDta = new ResponseData();
            string[] var_mapProtocol;

            _resDta.strHead = "33";
            _resDta.IsError = false;
                        
            txtPackageLine = txtPackageLine.Replace(" ","");

            if (txtPackageLine.Length < iPckLen)
            {
                _resDta.IsError = true;
                return _resDta;
            }

            string txtPackage = txtPackageLine.Substring(0,iPckLen);
            string txtPackageData = txtPackageLine.Substring(2,iPckLen-4);

            string[] arrPackage = SplitLine(txtPackage);
            
            _resDta.strIdDevice = arrPackage[2];
            _resDta.strDataRest = txtPackageLine.Substring(iPckLen);                        
            

            if (arrPackage.Length+1 != iPckLen / 2)
            {
                _resDta.IsError = true;
            }

            string strCrc = Helper.CreateCRC(txtPackageData);

            /*Обработка режимов */

            if ((arrPackage[3] == "03") | (arrPackage[3] == "07"))
            {                                
                var_mapProtocol = new string[]
                {
                    //,"PING_ALIVE"
                  "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC", "BAT"
                };
            }else{
                var_mapProtocol = _MapProtocol;

                if (arrPackage[3] == "01" || arrPackage[3] == "04")
                {
                    _resDta.IsBeep = true;                        
                }                
            }


            /*Проверка на верность CRC*/
            if (strCrc != arrPackage[arrPackage.Length - 1])
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
                foreach (string lname in _MapProtocol)
                {
                    currcomname = lname;

                    switch (lname)
                    {
                        case "DESTADDR":
                            sPackage = xEvent.Element("DESTADDR").Value;
                            break;
                        case "SOURCEADDR":
                            sPackage = sPackage + RPCAddr;
                            break;                        
                        case "REG":
                            sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                            break;
                        default:                            
                            break;
                    }                    
                }

                strCrc = Helper.CreateCRC(sPackage);
            }
            catch (Exception ex)
            {
                throw new Exception("В файле команды 33 отсутствует элемент - " + currcomname);
            }
            return sHeader+sPackage + strCrc;
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
            string valdest = arrMessage[1];
            string valsource = arrMessage[2];
            string valbody = "";
            for(int i=3; i<cnt-1; i++){
                valbody = arrMessage[i];
            }
            valbody = valsource+valdest+valbody;
            string strCrc = Helper.CreateCRC(valbody);

            return valheader + valbody + strCrc;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_39 : Action, iAction
    {

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = 16;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "39";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_3965 : Action, iAction
    {

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = 16;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "39";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения для установки TSLEEP
    /// </summary>
    class Action_3321 : Action, iAction
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
    /// Класс для обработки сообщения о пинге от устройства
    /// </summary>
    class Action_21 : Action, iAction
    {
        //"PING_ALIVE" 
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "TB", "RSSI", "CRC", "PING_ALIVE"};
        int iPckLen = (8 * 2)+2;

        public ResponseData ProcessDevice(string txtPackageLine)
        {
            ResponseData _resDta = new ResponseData();
            _resDta.strHead = "21";
            _resDta.IsError = false;

            txtPackageLine = txtPackageLine.Replace(" ", "");

            if (txtPackageLine.Length < iPckLen)
            {
                _resDta.IsError = true;
                return _resDta;
            }

            string txtPackage = txtPackageLine.Substring(0, iPckLen);
            string txtPackageData = txtPackageLine.Substring(2, iPckLen - 4);

            _resDta.strIdDevice = txtPackageLine.Substring(4, 4);
            _resDta.strDataRest = txtPackageLine.Substring(iPckLen);

            string[] arrPackage = SplitLine(txtPackage);
            

            if (arrPackage.Length+1 != iPckLen / 2)
            {
                _resDta.IsError = true;
            }

            string strCrc = Helper.CreateCRC(txtPackageData);

            /*Проверка на верность CRC*/
            if (strCrc != arrPackage[arrPackage.Length - 1])
            {
                _resDta.IsError = true;
            }
            else
            {
                _resDta.strEcho = FormatEchoMessage(arrPackage);
                arrPackage[0] = "33";
                arrPackage[3] = "00";
                _resDta.strXMLData = ArrayToXML(arrPackage, _MapProtocol);
            }
            arrPackage[0] = "21";
            _resDta.IsEchoСonfirmCP = false;
            return _resDta;
        }

        /// <summary>
        /// Создание Echo пакета
        /// </summary>
        /// <param name="arrMessage"></param>
        /// <returns></returns>
        public override string FormatEchoMessage(string[] arrMessage)
        {
            int cnt = arrMessage.Length;
            string valheader = arrMessage[0];
            string valdest = arrMessage[1];
            string valsource = arrMessage[2];

            string tsleep = "00";
            
            string valbody = valsource + valdest + tsleep;
            string strCrc = Helper.CreateCRC(valbody);

            return valheader + valbody + strCrc;
        }

        public override string GetBatValue(string[] arrPackage)
        {
            string res = "00";
            double ibat = Convert.ToDouble(Helper.HexToInt(arrPackage[4]));

            ibat = ibat * 0.03;
            /*if (ibat > 0)
            {
                res = "01";
            }*/
            res = ibat.ToString();
            return res;
        }

        public override string GetTempValue(string[] arrPackage)
        {
            string res = "00";
            Int32 ibat = Convert.ToInt32(Helper.HexToInt(arrPackage[5]));

            ibat = ibat - 128;
            /*if (ibat > 0)
            {
                res = "01";
            }*/
            res = ibat.ToString();
            return res;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_3365 : Action, iAction
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
    class Action_3364 : Action, iAction
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
