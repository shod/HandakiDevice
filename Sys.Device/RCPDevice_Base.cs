using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Threading;

namespace Sys.Device
{
    
    
        /// <summary>
        /// Сообщения с RFID-идентификацией
        /// </summary>
        class Action_96_Base : Action, iAction
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
                _resDta.IsEchoСonfirmCP = true;
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
                for (int i = 2; i < arrPackage.Length - 1; i++)
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
                string valcrc = arrMessage[cnt - 1];
                string valreg = arrMessage[1];

                string strCrc = Helper.CreateCRC(valreg + valcrc);

                return valheader + valreg + valcrc + strCrc;
            }
        }
}
