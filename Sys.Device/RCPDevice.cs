using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Threading;

namespace Sys.Device
{
    public class RCPDevice : Device, IDevice
    {


        string[] lstAction = { "73", "96", "75", "9665", "9678", "9679", "9611","96PING" , "9696" };

        public RCPDevice()
        {         
            InitAction(lstAction);
            _PingHeader = "96";
        }



            /*_dicActions = new Dictionary<string, iAction>();                        

            foreach(string Action in lstAction){
                Type TestType = Type.GetType("Sys.Device.Action_" + Action, false, true);
                //если класс не найден
                if (TestType != null)
                {
                    //получаем конструктор
                    System.Reflection.ConstructorInfo ci = TestType.GetConstructor(new Type[] { });

                    //вызываем конструтор
                    iAction Obj = (iAction)ci.Invoke(new object[] { });
                    _dicActions.Add(Action, Obj);
                }
                else
                {
                    Console.WriteLine("Sys.Device.Action_" + Action);
                }
            }
        }
        */
        public Dictionary<string, ActionMetaInfo> GetMetaInfo()
        {
            var MetaInfo = new Dictionary<string, ActionMetaInfo>();
            ActionMetaInfo AInfo;
            AInfo = new ActionMetaInfo();            

            AInfo.DeviceName = "rcpdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = false;
            MetaInfo.Add("73", AInfo);

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "rcpdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = false;
            MetaInfo.Add("75", AInfo);

            /*AInfo.isCheckEcho = false;
            MetaInfo.Add("77", AInfo);
            */

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "rcpdevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = false;
            MetaInfo.Add("96", AInfo);
            return MetaInfo;
        }

        public override string getActionIndex(string txtPackageLine)
        {
            string sDKey = txtPackageLine.Substring(0, 2);

            if (sDKey == "96")
            {
                string reg = txtPackageLine.Substring(2, 2);
                if (reg == "96" || reg == "95")
                {
                    sDKey = sDKey + reg; //Находим байт REG  
                }             
            }
            return sDKey;
        }

    }

    /// <summary>
    /// Сообщения с RFID-идентификацией
    /// </summary>
    class Action_96 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "REG", "CARD", "CRC" };
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 11 * 2;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strIdDevice = RPCAddr;
            _resDta.strHead = "96";
            Thread.Sleep(300);
            return _resDta;
        }
       

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
            item.Value = RPCAddr;
            ev.Add(item);

            //создаем элемент для "event"            
            item = new XElement("REG");
            item.Value = arrPackage[1];
            ev.Add(item);            

            //создаем элемент "event"                
            item = new XElement("RFID");
            for (int i = 2; i < arrPackage.Length-1; i++)
            {                                
                //складываем все цифры карты
                item.Value = item.Value + arrPackage[i];                
            }
            pr.Add(item);
            ev.Add(pr);
            events.Add(ev);
            doc.Add(events);
            return doc.ToString();
        }

        public override string FormatEchoMessage(string[] arrMessage)
        {
            int cnt = arrMessage.Length;
            string valheader = arrMessage[0];
            string valcrc = arrMessage[cnt-1];
            string valreg = arrMessage[1];
                        
            string strCrc = Helper.CreateCRC(valreg+valcrc);

            return valheader + valreg + valcrc + strCrc;
        }
    }


    /// <summary>
    /// Команда установки частотного канала радиомодуля БК
    /// </summary>
    class Action_9665 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "REG", "NUM", "CRC" };
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 4 * 2;
            ResponseData _resDta = new ResponseData();
            //_resDta = base.ProcessDevice(txtPackageLine);            
            _resDta.strIdDevice = RPCAddr;
            _resDta.IsError = false;
            _resDta.strHead = "96";
            _resDta.strXMLData = "";
            Thread.Sleep(300);
            return _resDta;
        }

        public string ProcessToDevice(XDocument xDoc)
        {
            string sPackage = "";
            string strCrc;
            this.iPckLen = _MapProtocol.Length * 2;
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
                        case "HEADER":
                        case "DESTADDR":
                        case "SOURCEADDR":
                        case "CRC":
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
                throw new Exception("В файле команды 96 отсутствует элемент - " + currcomname);
            }
            return sHeader + sPackage + strCrc;
        }
    }


    /// <summary>
    /// Команда выключения РЦП
    /// </summary>
    class Action_9678 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "REG", "CRC" };

        public string ProcessToDevice(XDocument xDoc)
        {
            return "78";
        }
    }

    /// <summary>
    /// Команда на КП, что РЦП недоступен
    /// </summary>
    class Action_9679 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "REG", "BAT", "PING_ALIVE", "CRC" };

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 7 * 2;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            string CRC = Helper.CreateCRC(txtPackageLine.Substring(2));
            _resDta = base.ProcessDevice(txtPackageLine + CRC);
            _resDta.strIdDevice = RPCAddr;
            _resDta.strHead = "96";
            Thread.Sleep(300);
            return _resDta;
        }
    }

    class Action_73 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER"};
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 1 * 2;
            ResponseData _resDta = new ResponseData();
            _resDta.IsError = false;
            _resDta.strHead = "73";
            _resDta.strEcho = "";
            _resDta.strDataOriginal = txtPackageLine;
            _resDta.strXMLData = "";
            _resDta.strDataRest = "";
            _resDta.strIdDevice = RPCAddr;
            return _resDta;
        }
    }

    /// <summary>
    /// Пакет пинга РЦП
    /// </summary>
    class Action_75 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER" };
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 1 * 2;
            ResponseData _resDta = new ResponseData();
            _resDta.IsError = false;
            _resDta.strHead = "75";
            _resDta.strEcho = "73";
            _resDta.strDataOriginal = "";
            _resDta.strXMLData = "";
            _resDta.strDataRest = "";
            _resDta.strIdDevice = RPCAddr;
            return _resDta;
        }
    }

    /// <summary>
    /// Команда на пинг каких-то устройств
    /// Содежит команды для какого устройства делать пинг
    /// Processor обращается к классу Action с PING (Пример:Action_77PING)
    /// </summary>
    class Action_9611 : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "REG", "TYPE", "ID", "CRC" };

        public string ProcessToDevice(XDocument xDoc)
        {
            this.iPckLen = 30;
            //string[] _MapProtocol = { "HEADER", "REG", "TYPE", "ID", "CRC" };
            base._QueueCRCCode = "11";
            base._MapProtocol = _MapProtocol;
            return "PING";
            //return base.ProcessToDevice(xDoc);
        }
    }

    class Action_96PING : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "REG", "TYPE", "ID", "CRC" };

        public override RequestData getObjectCommand(XDocument xDoc)
        {
            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = false;
            ResData.isSendToMozek = true;
            _QueueCRCCode = "09";
            
            return ResData;
        }

        public string ProcessToDevice(XDocument xDoc)
        {
            string sHeader = "96";
            string sPackage = "96";
            this.iPckLen = 30;
            //string[] _MapProtocol = { "HEADER", "REG", "TYPE", "ID", "CRC" };
            base._QueueCRCCode = "PING";            
            base._MapProtocol = _MapProtocol;
            string strCrc = Helper.CreateCRC(sPackage);
  
            return sHeader + sPackage + strCrc;
        }

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "PING_ALIVE", "CRC" };
            base._MapProtocol = _MapProtocol;
            this.iPckLen = 7 * 2;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            txtPackageLine = "96000000110112";
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strIdDevice = RPCAddr;
            _resDta.strHead = "96";
            return _resDta;
        }
        public override string ArrayToXML(string[] arrPackage, string[] _MapProtocol)
        {
            arrPackage[2] = "00";
            string Res = base.ArrayToXML(arrPackage, _MapProtocol);
            return Res;
        }

        /// <summary>
        /// Виртуальный метод для получения статуса батарейки
        /// </summary>
        public override string GetBatValue(string[] arrPackage)
        {
            return "1";
        }

    }

    class Action_9696 : Action_96PING, iAction
    {

    }
}
