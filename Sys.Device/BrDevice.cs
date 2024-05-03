using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{
   
    /// <summary>
    /// Класс для базового ретранслятора
    /// </summary>
    public class BrDevice : Device, IDevice
    {
        string[] lstAction = { "C9","C964", "C963", "C90C" }; //, "C965", "C9PING", "C999", "C966", "C967"

        string[] _MapProtocol = { "HEADER", "DESTADDR", "ECHO", "REG", "NUM", "CRC" };

        public int RegPosition = 12;

        public BrDevice()
        {            
            InitAction(lstAction);
            _PingHeader = "C9";
        }   
        
        /// <summary>
        /// Получение метаинфрмации
        /// </summary>
        /// <returns></returns>        
        public Dictionary<string, ActionMetaInfo> GetMetaInfo()
        {
            var MetaInfo = new Dictionary<string, ActionMetaInfo>();
            ActionMetaInfo AInfo = new ActionMetaInfo();

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "brdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = false;
            AInfo.CountAttSend = 2;
            MetaInfo.Add("C9", AInfo);

            AInfo.DeviceName = "brdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = false;
            //AInfo.TimeLive = 300;
            AInfo.CountAttSend = 2;
            AInfo.isDeleteCommandAfterSend = true;
            MetaInfo.Add("C964", AInfo); // смена номера устройства

            AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "brdevice";
            AInfo.isCheckEcho = true;
            AInfo.isWaitRequest = false;            
            AInfo.isDeleteCommandAfterSend = true;
            AInfo.CountAttSend = 1;
            MetaInfo.Add("C963", AInfo);
            
            return MetaInfo;
        }

        public string GetMessagePingToMozek(TypePing TypeMessage, ResponseData ResData){
            string _Res = "";
            if(ResData.strIdDevice != "0000"){
                _Res = base.GetMessagePingToMozek(TypeMessage, ResData);
            }
            return _Res;
        }

        public override string getActionIndex(string txtPackageLine)
        {
            //base.RegPosition = this.RegPosition;
            //string sDKey = base.getActionIndex(txtPackageLine);
            return "C90C"; //Всегда используем один byltrc при получении пакета
        }

        /*public override string CreateNewID(XDocument xDoc)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "ECHO", "REG", "NUM", "CRC" };            
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.CreateNewID(XDocument xDoc);
            _resDta.strHead = "C9";
            _resDta.IsEchoСonfirmCP = false;
            _resDta.strEcho = "";
            return _resDta;
        }*/

    }

    /// <summary>
    /// Класс для обработки сообщений общий
    /// </summary>
    class Action_C9 : Action_C90C
    {

    }

    /// <summary>
    /// Класс для обработки сообщения о смене ID устойства
    /// </summary>
    class Action_C964 : Action, iAction
    {
        public int RegPosition = 10;

        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            this.iPckLen = 16;
            ResponseData _resDta;
            base._MapProtocol = _MapProtocol;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "C9";
            _resDta.IsEchoСonfirmCP = false;
            _resDta.strEcho = "";
            return _resDta;
        }

        public override string ProcessToDevice(XDocument xDoc)
        {
            //base._QueueCRCCode = "09";
            //string[] _MapProtocol = { "HEADER", "DESTADDR", "ECHO", "REG", "NUM", "CRC" };
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
                    else if (lname == "ECHO")
                    {
                        sPackage = sPackage + "00";
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
            //base._QueueCRCCode = "09";
            return sHeader + sPackage + strCrc;
        }
    }

    /// <summary>
    /// Класс для обработки сообщения общего пинга всех устройств
    /// </summary>
    class Action_C963 : Action, iAction
    {
        public override string ProcessToDevice(XDocument xDoc)
        {
            //base._QueueCRCCode = "09";
            string[] _MapProtocol = { "HEADER", "DESTADDR", "ECHO", "REG", "NUM", "CRC" };
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
                    else if (lname == "ECHO")
                    {
                        sPackage = sPackage + "00";
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
            //base._QueueCRCCode = "09";
            return sHeader + sPackage + strCrc;
        }
    
    }

    /// <summary>
    /// Класс для обработки сообщения от БР
    /// </summary>
    class Action_C90C : Action, iAction
    {
        private string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "ECHO", "REG", "NUM", "CRC" };
        private int _RegPositon = 12;

        public override ResponseData ProcessDevice(string txtPackageLine)
        {            
            this.iPckLen = 20;
            ResponseData _resDta;
            base._MapProtocol = this._MapProtocol;
            base._RegPositon = this._RegPositon;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strHead = "C9";
            _resDta.IsEchoСonfirmCP = false;
            _resDta.strEcho = "";
            return _resDta;
        }

        public override string[] SplitLine(string DataLine){
            string[] MapProtocol = { "HEADER:2", "DESTADDR:0", "SOURCEADDR:8", "ECHO:2", "REG:2", "NUM:4", "CRC:2" };
            return SplitLine(DataLine, MapProtocol);
        }
        
    }
}
