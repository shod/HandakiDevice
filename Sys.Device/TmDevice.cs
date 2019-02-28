using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{
    /// <summary>
    /// Класс для терминала медсестры
    /// </summary>
    public class TmDevice : Device, IDevice
    {

        string[] lstAction = { "7701", "7720", "7722", "771E", "772C", "770A", "7764", "7709", "7765", "77PING" };

        public TmDevice()
        {            
            InitAction(lstAction);
            _PingHeader = "77";
        }   
        
        /// <summary>
        /// Получение метаинфрмации
        /// </summary>
        /// <returns></returns>        
        public Dictionary<string, ActionMetaInfo> GetMetaInfo()
        {
            var MetaInfo = new Dictionary<string, ActionMetaInfo>();
            ActionMetaInfo AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "tmdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = false;
            
            MetaInfo.Add("77", AInfo);
            //MetaInfo.Add("21", "tpdevice");

            return MetaInfo;
        }

        public string GetMessagePingToMozek(TypePing TypeMessage, ResponseData ResData){
            string _Res = "";
            if(ResData.strIdDevice != "0000"){
                _Res = base.GetMessagePingToMozek(TypeMessage, ResData);
            }
            return _Res;
        }

    }

    /// <summary>
    /// Класс для обработки температурного датчика
    /// </summary>
    class Action_7709 : Action, iAction
    {
        public override RequestData getObjectCommand(XDocument xDoc)
        {

            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = true;
            ResData.IntervalSendCommand = 5;
            _QueueCRCCode = "09";
            return ResData;
        }
        public override string ProcessToDevice(XDocument xDoc)
        {            
            this.iPckLen = 30;            
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC" };
            base._QueueCRCCode = "09";
            base._MapProtocol = _MapProtocol;            
            return base.ProcessToDevice(xDoc);
        }
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "TB", "PING_ALIVE","CRC" };
            this.iPckLen = 30;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            IsEchoСonfirmTODevice = true;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "77";
            _resDta.strCRCCheck = "09";
            _resDta.strEcho = "";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }

        public virtual string FormatEchoMessage(string[] arrMessage)
        {
            return "";
        }
        public override string GetTempValue(string[] arrPackage)
        {
            string res = "00";
            //arrPackage[5];
            string temp = arrPackage[5] + arrPackage[4];
            string sbat = Helper.HexToBinary(temp);
            string l1 = sbat.Substring(12, 4);
            string sign = sbat.Substring(0, 4);
            string num = sbat.Substring(4, 8);

            int ires = Convert.ToInt32(num, 2);
            if (sign == "1111")
            {
                ires = ires * -1;
            }


            res = ires.ToString();
            return res;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене ID устойства
    /// </summary>
    class Action_7764 : Action, iAction
    {

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = 16;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "77";
            _resDta.IsEchoСonfirmCP = false;
            _resDta.strEcho = "";            
            return _resDta;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения о смене частоты устойства
    /// </summary>
    class Action_7765 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
        public override string ProcessToDevice(XDocument xDoc)
        {
            string sPackage = "";
            base._MapProtocol = _MapProtocol;
            string _resDta = base.ProcessToDevice(xDoc);

            return _resDta;
        }

        /*public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = _MapProtocol.Length+2;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "77";
            //_resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }*/
    }

    /// <summary>
    /// Класс для установки режимов индикации (фонарей)
    /// </summary>
    class Action_7701 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "LED1", "LED2", "LED3", "LED4", "LED5", "ZUM", "CRC" };

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "LED1", "LED2", "LED3", "LED4", "LED5", "ZUM", "CRC" };
            this.iPckLen = (_MapProtocol.Length + 1) * 2;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strDataOriginal = txtPackageLine;// _resDta.strEcho;
            _resDta.strEcho = "";
            _resDta.strXMLData = "";
            _resDta.strHead = "77";
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
                    if (lname == "DESTADDR")
                    {
                        sPackage = xEvent.Element("DESTADDR").Value;
                    }
                    else if (lname == "SOURCEADDR")
                    {
                        sPackage = sPackage + RPCAddr;
                    }
                    else if (lname == "LED1")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                    }
                    else if (lname == "LED2")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                    }
                    else if (lname == "LED3")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                    }
                    else if (lname == "LED4")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
                    }
                    else if (lname == "LED5")
                    {
                        sPackage = sPackage + xEvent.Element("PARAMS").Element(lname).Value;
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
                throw new Exception("В файле команды 77 отсутствует элемент!"+currcomname);
            }
            return sHeader+sPackage + strCrc;
        }

        public override RequestData getObjectCommand(XDocument xDoc)
        {
            
            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = true;
            _QueueCRCCode = "";
            return ResData;
        }
    }

     /// <summary>
    /// Класс для обработки сообщения c RFID-идентификацией
    /// Раскоментировать для работы с короткими номерами карт
    /// </summary>
    class Action_7720 : Action_7720_Base
    {
        public override string ArrayToXML(string[] arrPackage, string[] _MapProtocol)
        {
            XDocument doc = new XDocument();
            XElement item;
            XElement events = new XElement("EVENTS");
            XElement ev = new XElement("EVENT");
            XElement pr = new XElement("PARAMS");

            item = new XElement("TIME");
            int itime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            item.Value = itime.ToString();
            ev.Add(item);

            //создаем элемент для "event"                   
            item = new XElement("HEADER");
            item.Value = arrPackage[0];
            ev.Add(item);

            //создаем элемент для "event"                   
            item = new XElement("SOURCEADDR");
            item.Value = arrPackage[2];
            ev.Add(item);

            //создаем элемент для "event"            
            item = new XElement("REG");
            item.Value = arrPackage[3];
            ev.Add(item);

            //создаем элемент "event"                
            item = new XElement("RFID");
            for (int i = 4; i < 4+6; i++)
            {
                //складываем все цифры карты
                item.Value = item.Value + arrPackage[i];
            }
            item.Value = item.Value.Substring(2);
            pr.Add(item);
            ev.Add(pr);
            events.Add(ev);
            doc.Add(events);
            return doc.ToString();
        }
         
    }

    /// Класс для обработки вызова врача
    /// </summary>
    class Action_7722 : Action_7720_Base /*: Action, iAction*/
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "RFID", "CRC" };

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 6 * 2;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "77";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }
    }
     

    /// <summary>
    /// Класс для обработки нажатия на кнопку
    /// </summary>
    class Action_771E : Action, iAction
    {
        public string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "CRC" };

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 6 * 2;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "77";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }       
    }

    /// <summary>
    /// Класс для обработки сообщения о пропадании питания
    /// Аналогично PING_ALIVE
    /// </summary>
    class Action_772C : Action, iAction
    {
        public string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "PING_ALIVE", "CRC" };

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 6 * 2;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "77";
            _resDta.IsEchoСonfirmCP = true;
            return _resDta;
        }

        public override string GetBatValue(string[] arrPackage)
        {
            return "00";
        }
    }

    /// <summary>
    /// Класс для обработки команды вывода на цифровой индикатор
    /// </summary>
    class Action_770A : Action, iAction
    {
        public string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };

        public Action_770A()
        {
            base._MapProtocol = _MapProtocol;
        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = (_MapProtocol.Length+1) * 2;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strXMLData = "";
            _resDta.strDataOriginal = txtPackageLine;// _resDta.strEcho;
            _resDta.strEcho = "";
            _resDta.strHead = "77";
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
                    if (lname == "DESTADDR")
                    {
                        sPackage = xEvent.Element("DESTADDR").Value;
                    }
                    else if (lname == "SOURCEADDR")
                    {
                        sPackage = sPackage + xEvent.Element("SOURCEADDR").Value;
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
                throw new Exception("В файле команды 77 отсутствует элемент!" + currcomname);
            }
            return sHeader + sPackage + strCrc;
        }

        public override RequestData getObjectCommand(XDocument xDoc)
        {
            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string strNum = xEvent.Element("PARAMS").Element("NUM").Value;

            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = false;

            if (strNum == "00")
            {
                ResData.isDeleteSimilarCommand = true;
            }
            _QueueCRCCode = "";
            return ResData;
        }
    }

    /// Класс для пинга устройства
    /// </summary>
    class Action_77PING : Action_7709, iAction
    {
        public override RequestData getObjectCommand(XDocument xDoc)
        {
            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = true;
            ResData.IntervalSendCommand = 5;
            _QueueCRCCode = "09";
            return ResData;
        }
        public override string ProcessToDevice(XDocument xDoc)
        {
            base._QueueCRCCode = "09";
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
                        sPackage = sPackage + "09";
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
            base._QueueCRCCode = "09";
            return sHeader + sPackage + strCrc;
        }
    }
}
