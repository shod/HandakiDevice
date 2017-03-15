using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{
    /// <summary>
    /// Класс для терминала врача
    /// </summary>
    public class TdDevice : Device, IDevice
    {

        string[] lstAction = { "E700", "E701", "E709", "E707", "E765", "E764", "E7PING", "E763" }; //, "E764"

        public TdDevice()
        {            
            InitAction(lstAction);
            _PingHeader = "E7";
        }   
        
        /// <summary>
        /// Получение метаинфрмации
        /// </summary>
        /// <returns></returns>        
        public Dictionary<string, ActionMetaInfo> GetMetaInfo()
        {
            var MetaInfo = new Dictionary<string, ActionMetaInfo>();
            ActionMetaInfo AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tddevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = false;

            MetaInfo.Add("E7", AInfo);
            //MetaInfo.Add("21", "tpdevice");

            return MetaInfo;
        }

        public override string getActionIndex(string txtPackageLine)
        {            
            string sHKey = txtPackageLine.Substring(0, 2);
            string sDKey = txtPackageLine.Substring(8, 2);

            switch (sDKey)
            {
                case "00":                
                case "02":
                case "03":                
                    sHKey = sHKey + "00";
                    break;
                default:
                    sHKey = sHKey + sDKey;
                    break;
            }
            return sHKey;
        }

    }

    
    /// <summary>
    /// Класс для обработки команды вывода на цифровой индикатор
    /// </summary>
    class Action_E700 : Action, iAction
    {        
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "CRC" };

        public Action_E700()
        {
            base._MapProtocol = _MapProtocol;
            base.iPckLen = 14;
        }
        public override string ProcessToDevice(XDocument xDoc)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC" };
            string sPackage = "";
            base._MapProtocol = _MapProtocol;
            string _resDta = base.ProcessToDevice(xDoc);            

            return _resDta;
        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            //string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "CRC" };

            // Это проверка на REG = 64 - это ответ на прошивку номера
            string strReg = txtPackageLine.Substring(8, 2);

            this.iPckLen = base.iPckLen;
            IsEchoСonfirmTODevice = true;
            
            this.iPckLen = 14;
            if (strReg == "00")
            {
                string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC" };
                base._MapProtocol = _MapProtocol;
                //base.IsEchoСonfirmTODevice = false;
                //this.iPckLen = 12;
            }
            
            ResponseData _resDta;            


            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "E7";
            _resDta.strDataOriginal = txtPackageLine;

            return _resDta;
        }

        public override string FormatEchoMessage(string[] arrMessage)
        {
            int cnt = arrMessage.Length;
            string valheader = arrMessage[0];
            string valdest = arrMessage[1];
            string valsource = arrMessage[2];
            string valReg = arrMessage[3];
            string valbody = "";
            for (int i = 3; i < cnt - 1; i++)
            {
                valbody = valbody + arrMessage[i];
            }
            /* Режим - 00 Сброс всех установок
             */
            valbody = valsource + valdest + valReg;
            string strCrc = Helper.CreateCRC(valbody);

            if (valReg == "00"){
                return "";
            }else{
                return valheader + valbody + strCrc;
            }
        }

        public override RequestData getObjectCommand(XDocument xDoc)
        {
            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string strReg = xEvent.Element("PARAMS").Element("REG").Value;

            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = false;

            if (strReg == "00")
            {
                ResData.isDeleteAllCommand = true;
            }
            _QueueCRCCode = "";
            return ResData;
        }
    }

    /// <summary>
    /// Класс для обработки команды PING
    /// </summary>
    class Action_E701 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "PING_ALIVE", "CRC" };
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 14;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "E7";
            return _resDta;
        }

        //override
        public override string FormatEchoMessage(string[] arrMessage)
        {
            int cnt = arrMessage.Length;
            string valheader = arrMessage[0];
            string valdest = arrMessage[1];
            string valsource = arrMessage[2];            
            string valbody = "";
            /*for (int i = 3; i < cnt - 1; i++)
            {
                valbody = valbody + arrMessage[i];
            }*/
            /* Режим - 09
             * Выставляем таймаут в 1 единица*50 сек*/
            //string ttime = "0A";
            //valbody = valsource + valdest + "09" + ttime + "00010000";
            valbody = valsource + valdest + "01";// +ttime + "00000000";
            string strCrc = Helper.CreateCRC(valbody);

            if (cnt == 5)
            {
                return "";
            }
            else
            {
                return valheader + valbody + strCrc;
            }
        }
    }

    /// <summary>
    /// Запрос на проверку связи с РС.
    /// </summary>
    class Action_E707 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "CRC", "PING_ALIVE" };
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 7 * 2;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "E7";
            return _resDta;
        }

        public override string FormatEchoMessage(string[] arrMessage)
        {
            int cnt = arrMessage.Length;
            string valheader = arrMessage[0];
            string valdest = arrMessage[1];
            string valsource = arrMessage[2];
            string valbody = "";
            for (int i = 3; i < cnt - 1; i++)
            {
                valbody = valbody + arrMessage[i];
            }
            /* Режим - 09
             * Выставляем таймаут в 1 единица*50 сек*/
            string ttime = "10";
            valbody = valsource + valdest + "09" + ttime + "00010000";
            string strCrc = Helper.CreateCRC(valbody);
            return valheader + valbody + strCrc;
        }
    }

    /// <summary>
    /// Команда установки режима работы и индикации  ТВ
    /// </summary>
    class Action_E709 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "TSLEEP" ,"LED1", "LED2", "ZUM", "TNUM", "NUM", "CRC" };                

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
                    if (lname == "DESTADDR")
                    {
                        sPackage = xEvent.Element("DESTADDR").Value;
                    }
                    else if (lname == "SOURCEADDR")
                    {
                        sPackage = sPackage + xEvent.Element("SOURCEADDR").Value;
                    }                    
                    else if (lname == "LED1")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                    }
                    else if (lname == "LED2")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                    }
                    else if (lname == "HEADER" || lname == "CRC")
                    {
                        sPackage = sPackage;
                    }
                    else
                    {
                        if (lname == "NUM")
                        {
                            XElement xNum = xDoc.Element("EVENTS").Element("EVENT").Element("PARAMS");

                            foreach (XElement xn in xNum.Elements("NUM"))
                            {
                                sPackage = sPackage + xn.Value;
                            }
                        }
                        else
                        {
                            sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                        }
                    }
                    
                }

                strCrc = Helper.CreateCRC(sPackage);
            }
            catch (Exception ex)
            {
                throw new Exception("В файле команды Action_E709 отсутствует элемент!" + currcomname + xDoc.ToString());
            }
            return sHeader+sPackage + strCrc;
        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = txtPackageLine.Length;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "E7";
            _resDta.strDataOriginal = txtPackageLine;
            return _resDta;
        }

        public override RequestData getObjectCommand(XDocument xDoc)
        {

            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = true;
            ResData.isDeleteSimilarCommand = true;
            _QueueCRCCode = "";
            return ResData;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_E766 : Action, iAction
    {

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = 16;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "E7";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }


    }

    /// <summary>
    /// Класс для обработки сообщения о смене ID устойства
    /// </summary>
    class Action_E764 : Action, iAction
    {

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = 16;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "E7";
            return _resDta;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_E765 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
        public override string ProcessToDevice(XDocument xDoc)
        {
            string sPackage = "";
            base._MapProtocol = _MapProtocol;
            string _resDta = base.ProcessToDevice(xDoc);

            return _resDta;
        }
    }

    /// Класс для пинга устройства
    /// </summary>
    class Action_E763 : Action_E700, iAction
    {
        string[] _MapProtocol =  { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "PING_ALIVE", "CRC" };

        public Action_E763()
        {
            base._MapProtocol = _MapProtocol;
        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "PING_ALIVE", "CRC" };
            base.iPckLen = 12;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            IsEchoСonfirmTODevice = true;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "E7";
            _resDta.strCRCCheck = "63";
            _resDta.strEcho = "";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }
    }

    /// Класс для пинга устройства
    /// </summary>
    class Action_E7PING : Action_E700, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC" };

        public Action_E7PING()
        {
            base._MapProtocol = _MapProtocol;
        }

        public override RequestData getObjectCommand(XDocument xDoc)
        {
            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = true;
            ResData.IntervalSendCommand = 5;
            _QueueCRCCode = "63";
            return ResData;
        }
        public override string ProcessToDevice(XDocument xDoc)
        {
            base._QueueCRCCode = "63";
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC" };
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
                    if (lname == "DESTADDR")
                    {
                        sPackage = xEvent.Element("DESTADDR").Value;
                    }
                    else if (lname == "SOURCEADDR")
                    {
                        sPackage = sPackage + RPCAddr;
                    }
                    else if (lname == "REG")
                    {
                        sPackage = sPackage + base._QueueCRCCode;
                    }
                    else if (lname != "HEADER" && lname != "CRC")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                    }

                }

                strCrc = Helper.CreateCRC(sPackage);
            }
            catch (Exception ex)
            {
                throw new Exception("В файле команды " + this.GetType().Name + " отсутствует элемент - " + currcomname + xDoc.ToString());
            }
            base._QueueCRCCode = "63";
            return sHeader + sPackage + strCrc;
        }

    }

}
