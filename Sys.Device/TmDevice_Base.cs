using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{
      /// <summary>
    /// Класс для обработки сообщения c RFID-идентификацией длинные номера!!!
    /// </summary>
    class Action_7720_Base : Action, iAction
    {
        string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "RFID", "CRC" };
        
        public override ResponseData ProcessDevice(string txtPackageLine)
        {
            this.iPckLen = 14 * 2;
            ResponseData _resDta;
            IsEchoСonfirmTODevice = true;
            _resDta = base.ProcessDevice(txtPackageLine);
            _resDta.strIdDevice = RPCAddr;
            _resDta.strHead = "77";
            _resDta.IsEchoСonfirmCP = true;
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
            item.Value = arrPackage[2];
            ev.Add(item);

            //создаем элемент для "event"            
            item = new XElement("REG");
            item.Value = arrPackage[3];
            ev.Add(item);

            //создаем элемент "event"                
            item = new XElement("RFID");
            for (int i = 4; i < arrPackage.Length - 1; i++)
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
    }
}
