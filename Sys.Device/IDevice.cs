using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;

namespace Sys.Device
{

    public class ActionMetaInfo
    {
        /// <summary>
        /// Имя устройства
        /// </summary>
        public string DeviceName;

        /// <summary>
        /// Ожидать ли эхо-команды от устройства
        /// </summary>
        public bool isCheckEcho;

        /// <summary>
        /// Ожидать запроса от устройства
        /// Устройство просыпается, посылает сигнал последний и ожидает команду обратно
        /// </summary>
        public bool isWaitRequest;

        /// <summary>
        /// Время жизни команды (сек)
        /// </summary>
        public int TimeLive;

        /// <summary>
        /// Удалять ли команду после отсылки (true)
        /// или оставлять до окончания времени жизни команды (TimeLive)
        /// </summary>
        public bool isDeleteCommandAfterSend;

        /// <summary>
        /// Кол-во попыток отсылки команды
        /// </summary>
        public int CountAttSend;        

        public ActionMetaInfo()
        {
            this.DeviceName = "";
            this.isCheckEcho = false;
            this.isWaitRequest = true;
            this.TimeLive = 95;
            this.isDeleteCommandAfterSend = true;
            this.CountAttSend = 5;
        }
    }

    /// <summary>
    /// Описание интерфейса конечных устройств
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// Получение метаинформации об устройстве. 
        /// Список заголовков принимающих
        /// </summary>
        /// <returns>Массив метаданных</returns>
        Dictionary<string, ActionMetaInfo> GetMetaInfo();

        /// <summary>
        /// Преобразователь пакета данных от конечного устройства
        /// </summary>
        /// <param name="txtPackage">Строка с пакетом</param>
        /// <returns>Объект с данными преобразования</returns>
        ResponseData ProcessDevicePackage(string txtPackageLine);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="txtPackageLine"></param>
        /// <returns></returns>
        string ProcessPackageToDevice(XDocument txtPackageLine);

        RequestData ProcessObjectPackageToDevice(XDocument txtPackageLine);

        string GetMessagePingToMozek(TypePing TypeMessage, ResponseData ResData);
 
    }

    public interface iAction
    {
        ResponseData ProcessDevice(string txtPackageLine);
        string ProcessToDevice(XDocument xDoc);
        RequestData getObjectCommand(XDocument xDoc);

        string GetQueueCRCCode(string CRC);
    }

    /// <summary>
    /// Структура ответа от устройства
    /// </summary>
    public struct ResponseData
    {
        public bool IsError;
        /// <summary>
        /// Данные в XML формате
        /// </summary>
        public String strXMLData;
        public String strHead;

        /// <summary>
        /// Для проверки очередей
        /// По-умолчанию равен CRC
        /// </summary>
        public String strCRCCheck;

        public String strIdDevice;
        /// <summary>
        /// Оригинальный пакет для конкретного утсройства
        /// </summary>
        public String strDataOriginal;
        /// <summary>
        /// Остаток от пакета
        /// </summary>
        public String strDataRest;
        /// <summary>
        /// Ответ на сообщение
        /// </summary>
        public String strEcho;
        /// <summary>
        /// Звуковой сигнал
        /// </summary>
        public bool IsBeep;
        /// <summary>
        /// Подтверждение в КП (коммандный пункт)
        /// </summary>
        public bool IsEchoСonfirmCP;
    }

    /// <summary>
    /// Структура описания команды от КП на устройство
    /// </summary>
    public struct RequestData
    {
        /// <summary>
        /// Комманда на устройство
        /// </summary>
        public string strPackage;
        public string strHeader;
        public string strReg;
        public string strDeviceId;
        /// <summary>
        /// Для проверки очередей
        /// По-умолчанию равен CRC
        /// </summary>
        public String strCRCCheck;
        /// <summary>
        /// Флаг удаления всех предыдущих команд в очереди
        /// </summary>
        public bool isDeleteAllCommand;
        /// <summary>
        /// Флаг удаления похожих команд в очереди
        /// </summary>
        public bool isDeleteSimilarCommand;

        /// <summary>
        /// Флаг посылки команды сразу на КП
        /// </summary>
        public bool isSendToMozek;

        /// <summary>
        /// Интервал между посылом команд (сек.)
        /// </summary>
        public int IntervalSendCommand;
    }
    /// <summary>
    /// Тип пинга состояния устройства
    /// </summary>
    public enum TypePing
    {
        Good,
        Warning,
        Error
    }

    /// <summary>
    /// Абстракный класс для устройств
    /// </summary>
    public abstract class Device
    {
        iAction actClass;

        /// <summary>
        /// Список устройств
        /// </summary>
        protected Dictionary<string, iAction> _dicActions;

        /// <summary>
        /// Массив действий для устройства
        /// </summary>        
        public string[] lstAction;

        /// <summary>
        /// Список действий (Action) в устройстве
        /// </summary>                
        protected void InitAction(string[] lstAction)
        {
            _dicActions = new Dictionary<string, iAction>();

            if (lstAction == null)
            {
                lstAction = new string[] { };
            }

            foreach(string Action in lstAction){
                Type TestType = Type.GetType("Sys.Device.Action_" + Action, false, true);
                //если класс не найден
                if (TestType != null)
                {
                    //получаем конструктор
                    System.Reflection.ConstructorInfo ci = TestType.GetConstructor(new Type[] { });

                    //вызываем конструктор
                    iAction Obj = (iAction)ci.Invoke(new object[] { });
                    _dicActions.Add(Action, Obj);
                }
                else
                {
                    Console.WriteLine("Sys.Device.Action_" + Action);
                }
            }
        }

        /// <summary>
        /// Заголовок для пинг-сообщения на командный пункт
        /// </summary>
        public string _PingHeader = "";

        /// <summary>
        /// Функция возвращает индекс действия REG для поиска в массиве действий устройства
        /// </summary>
        /// <param name="txtPackageLine"></param>
        /// <returns></returns>
        public virtual string getActionIndex(string txtPackageLine)
        {
            string sDKey = "00";
            if (txtPackageLine.Length > 8)
            {
                sDKey = txtPackageLine.Substring(0, 2);
                sDKey = sDKey + txtPackageLine.Substring(8, 2); //Находим байт REG
            }
            return sDKey;
        }

        public virtual RequestData GetPingCommand()
        {
            RequestData rData = new RequestData();
            rData.strHeader = _PingHeader;
            rData.strPackage = "";
            return rData;
        }

        /// <summary>
        /// Пакет от устройства
        /// </summary>
        /// <param name="PackageLine"></param>
        /// <returns></returns>

        public ResponseData ProcessDevicePackage(string txtPackageLine)
        {
            ResponseData _resDta = new ResponseData();
                                   
            string sHeaderDKey = getActionIndex(txtPackageLine);// txtPackageLine.Substring(0, 2);
            string sHeaderDKeyReg = sHeaderDKey.Substring(2);

            if (_dicActions.ContainsKey(sHeaderDKey))
            {
                actClass = _dicActions[sHeaderDKey];
                //_resDta.IsEchoСonfirmCP = true;
                _resDta = actClass.ProcessDevice(txtPackageLine);                
            }
            else
            {
                _resDta = new ResponseData();
                _resDta.IsError = true;
                _resDta.strDataRest = "";
            }
            return _resDta;
        }

        /// <summary>
        /// Пакет на устройство
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        public string ProcessPackageToDevice(XDocument xDoc)
        {
            
            string ResData = "";
            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string sHeaderDKey = xDoc.Element("EVENTS").Element("EVENT").Element("HEADER").Value;            
            string strReg = xEvent.Element("PARAMS").Element("REG").Value;
            sHeaderDKey = sHeaderDKey + strReg;

            if (strReg == "64") // Смена номера устройства
            {
                //actClass = _dicActions[sHeaderDKey];
                ResData = CreateNewID(xDoc);
            }
            else if (_dicActions.ContainsKey(sHeaderDKey))
            {
                actClass = _dicActions[sHeaderDKey];
                ResData = actClass.ProcessToDevice(xDoc);                
            }            

            return ResData;
        }

        /// <summary>
        /// Управляющий объект команды на устройство
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        public RequestData ProcessObjectPackageToDevice(XDocument xDoc)
        {
            RequestData ResData = new RequestData();
            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string sHeaderDKey = xDoc.Element("EVENTS").Element("EVENT").Element("HEADER").Value;
            string strReg = xEvent.Element("PARAMS").Element("REG").Value;
            sHeaderDKey = sHeaderDKey + strReg;

               
            if (_dicActions.ContainsKey(sHeaderDKey))
            {
                actClass = _dicActions[sHeaderDKey];
                ResData = actClass.getObjectCommand(xDoc);
                ResData.strDeviceId = xEvent.Element("DESTADDR").Value;             
                ResData.strPackage = ProcessPackageToDevice(xDoc);

                ResData.strReg = strReg;
                ResData.strHeader = ResData.strPackage.Substring(0, 2);
                string CRC = ResData.strPackage.Substring(ResData.strPackage.Length - 2);
                ResData.strCRCCheck = actClass.GetQueueCRCCode(CRC);

                if (ResData.strPackage == "PING")
                {
                    XDocument xDocRedirectPackage = GetSystemPackage(xDoc);
                    ResData.strDeviceId = xDocRedirectPackage.Element("EVENTS").Element("EVENT").Element("DESTADDR").Value;  
                    //RequestData ResDataRedirect = ProcessObjectPackageToDevice(xDocRedirectPackage);
                    ResData.strPackage = xDocRedirectPackage.ToString();
                    ResData.strReg = "PING";
                    ResData.strHeader = xDocRedirectPackage.Element("EVENTS").Element("EVENT").Element("HEADER").Value;
                }

            }   
             
            //ResData.strCRCCheck = actClass.
            //ResData.isDeleteAllCommand = false;
            //ResData.isDeleteSimilarCommand = false;
            return ResData;
        }

        

        /// <summary>
        /// Создание пакета для смены номера устройства
        /// </summary>
        /// <param name="Message"></param>
        /// <returns></returns>
        public string CreateNewID(XDocument xDoc)
        {

            string RPCAddr = "00";
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "NUM", "CRC" };
            string[] _MapValue = { "" };
            string sPackage = "";
            string strCrc;

            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string sHeader = xEvent.Element("HEADER").Value;

            /*Если это команда для терминала пациента*/
            /*if (sHeader == "33")
            {
                sHeader = "39";
            }*/

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
            return sHeader + sPackage + strCrc;
        }

        /// <summary>
        /// Сообщение, PING в КП
        /// </summary>
        /// <param name="TypeMessage"></param>
        /// <param name="ResData"></param>
        /// <returns></returns>
        public string GetMessagePingToMozek(TypePing TypeMessage, ResponseData ResData)
        {
            string _Res = "";
            XDocument doc = new XDocument();
            XElement item;
            XElement events = new XElement("EVENTS");
            XElement ev = new XElement("EVENT");
            XElement _params = new XElement("PARAMS");

            string sHeader = _PingHeader;

            item = new XElement("HEADER");
            item.Value = sHeader;
            ev.Add(item);


            item = new XElement("DESTADDR");
            item.Value = "00";
            ev.Add(item);

            item = new XElement("SOURCEADDR");
            item.Value = ResData.strIdDevice;
            ev.Add(item);

            item = new XElement("TIME");
            int itime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            item.Value = itime.ToString();
            ev.Add(item);

            int iPing = 0;
            switch(TypeMessage)
            {
                case TypePing.Good:
                    iPing = 1;
                    break;
                case TypePing.Warning:
                    iPing = 2;
                    break;
                case TypePing.Error:
                    iPing = 0;
                    break;
            }

            item = new XElement("BAT");
            item.Value = iPing.ToString();
            ev.Add(item);

            item = new XElement("PING_ALIVE");
            item.Value = "1";
            ev.Add(item);

            ev.Add(_params);
            events.Add(ev);
            doc.Add(events);
            _Res = doc.ToString();
            return _Res;
        }

        private XDocument GetSystemPackage(XDocument xDoc)
        {         
            string _Res = "";

            XDocument doc = new XDocument();
            XElement item;
            XElement events = new XElement("EVENTS");
            XElement ev = new XElement("EVENT");
            XElement _params = new XElement("PARAMS");

            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");            
            string sHeader = xEvent.Element("PARAMS").Element("TYPE").Value;
            string sID = xEvent.Element("PARAMS").Element("ID").Value;

            item = new XElement("HEADER");
            item.Value = sHeader;
            ev.Add(item);

            item = new XElement("DESTADDR");
            item.Value = sID;
            ev.Add(item);

            item = new XElement("SOURCEADDR");
            item.Value = "00";
            ev.Add(item);

            item = new XElement("CRC");
            item.Value = "00";
            ev.Add(item);

            item = new XElement("REG");
            item.Value = "PING";
            _params.Add(item);

            ev.Add(_params);
            events.Add(ev);
            doc.Add(events);
            //_Res = doc.ToString();
            return doc;
        }
    }

    /*Абстрактный класс для события*/
    public abstract class Action
    {
        /// <summary>
        /// Код главного устройства
        /// </summary>
        public string RPCAddr = "00";        
        public string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "CRC" };

        public int iPckLen = 6 * 2; /*длина пакета от устройства*/

        public string _QueueCRCCode = "";

        /// <summary>
        /// Эхо подтверждение получения сигнала от устройства.
        /// Посылается от цетрального блока обратно на устройство.
        /// </summary>
        public bool IsEchoСonfirmTODevice = false;

        /// <summary>
        /// Метод возвращающий CRC код для очечреди
        /// </summary>
        /// <returns></returns>
        public virtual string GetQueueCRCCode(string CRC)
        {
            if (_QueueCRCCode == "")
            {
                _QueueCRCCode = CRC;
            }
            return _QueueCRCCode;
        }
        /// <summary>
        /// Преобразование массива в XML
        /// </summary>
        /// <param name="arrPackage"></param>
        /// <returns></returns>
        public virtual string ArrayToXML(string[] arrPackage, string[] _MapProtocol)
        {
            /*Базовый массив параметров
             Остальные пойдут в PARAMS
             */
            List<string> BaseNameParam = new List<string> { "HEADER", "DESTADDR", "SOURCEADDR", "REG", "BAT", "CRC", "PING_ALIVE" };
            XDocument doc = new XDocument();
            XElement item;
            XElement itemp;
            XElement events = new XElement("EVENTS");
            XElement ev = new XElement("EVENT");
            XElement _params = new XElement("PARAMS");            
            string lname;

            item = new XElement("TIME");
            int itime = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            item.Value = itime.ToString();
            ev.Add(item);

            /*string header = arrPackage[0];
            string dest = arrPackage[1];
            string source = arrPackage[2] + arrPackage[3];
            */

            for (int i = 0; i < _MapProtocol.Length-1; i++)
            {
                lname = _MapProtocol[i];                
                
                //добавляем необходимые атрибуты               
                if (BaseNameParam.Contains(lname))
                {
                    //создаем элемент "event"
                    item = new XElement(lname);

                    if (lname == "SOURCEADDR")
                    {
                        item.Value = arrPackage[i];                        
                    }
                    else if (lname == "PING_ALIVE")
                    {
                        item.Value = "1";
                    }                    
                    else if (lname == "BAT")
                    {
                        item.Value = this.GetBatValue(arrPackage);
                    }                   
                    else
                    {
                        item.Value = arrPackage[i];
                    }
                    ev.Add(item);
                }
                else
                {                    
                    itemp = new XElement(lname);
                    itemp.Value = arrPackage[i];
                    if (lname == "TB")
                    {
                        itemp.Value = this.GetTempValue(arrPackage);
                    }
                    _params.Add(itemp);
                }
                
            }
            ev.Add(_params);
            events.Add(ev);
            doc.Add(events);
            return doc.ToString();
        }

        /// <summary>
        /// Виртуальный метод для получения статуса батарейки
        /// </summary>
        public virtual string GetBatValue(string[] arrPackage)
        {
            return "01";
        }

        /// <summary>
        /// Виртуальный метод для получения значения температуры
        /// </summary>
        public virtual string GetTempValue(string[] arrPackage)
        {
            return "00";
        }

        /// <summary>
        /// Разбивает строку на массив от устройства
        /// </summary>
        /// <param name="DataLine"></param>
        /// <returns></returns>
        public static string[] SplitLine(string DataLine)
        {
            string text = DataLine;
            int itemLen = 2;
            int num = DataLine.Length / 2;
            string[] array = new string[num-1];
            int a = 0;
            for (int i = 0; i < num; i++)
            {
                if (i == 2)//This is destination addr
                {
                    array[a] = text.Substring(i * 2, 4);
                    i++;
                }             
                else
                {
                    array[a] = text.Substring(i * 2, 2);
                }
                a++;
            }
            return array;
        }

        /// <summary>
        /// Разбивает строку на массив от устройства при ответе на смену ID
        /// </summary>
        /// <param name="DataLine"></param>
        /// <returns></returns>
        public static string[] SplitLineResID(string DataLine)
        {
            int[] _MapProtocolItemLen = { 2, 2, 4, 2, 4, 2 };
            string text = DataLine;
            int itemLen = 2;
            int num = _MapProtocolItemLen.Length;
            string[] array = new string[num];
            for (int i = 0; i < num; i++)
            {
                
                {
                    itemLen = _MapProtocolItemLen[i];
                    array[i] = text.Substring(0, itemLen);
                    text = text.Substring(itemLen);
                }
            }
            return array;
        }

        /// <summary>
        /// Разбивает строку на массив в пакете к устройству
        /// </summary>
        /// <param name="DataLine"></param>
        /// <returns></returns>
        public static string[] _SplitLine(string DataLine, bool ToDev = false)
        {
            
            string text = DataLine;
            int num = DataLine.Length / 2;
            string[] array = new string[num - 1];
            int a = 0;
            for (int i = 0; i < num; i++)
            {

                if (i == 1)//This is destination addr
                {
                    if (ToDev)
                    {
                        array[a] = text.Substring(i * 2, 4);
                        i++;
                    }
                    else
                    {
                        array[a] = text.Substring(i * 2, 2);
                    }
                }else if (i == 2)//This is source addr
                {
                    if (ToDev)
                    {
                        array[a] = text.Substring(i * 2, 2);
                       
                    }
                    else
                    {
                        array[a] = text.Substring(i * 2, 4);
                        i++;
                    }
                }
                else
                {
                    array[a] = text.Substring(i * 2, 2);
                }
                a++;
            }
            return array;
        }

        
        /// <summary>
        /// Возвращает обработанный пакет от устройства
        /// </summary>
        /// <param name="txtPackageLine"></param>
        /// <returns></returns>
        public virtual ResponseData ProcessDevice(string txtPackageLine)
        {
            ResponseData _resDta = new ResponseData();
            _resDta.IsError = false;

            try
            {
                 
                 txtPackageLine = txtPackageLine.Replace(" ", "").ToUpper();
                 if (txtPackageLine.Length < iPckLen)
                 {
                     _resDta.IsError = true;
                     return _resDta;
                 }

                string txtPackage = txtPackageLine.Substring(0, iPckLen);

                string txtPackageData = txtPackageLine.Substring(2, iPckLen - 4);
                
                _resDta.strDataRest = txtPackageLine.Substring(iPckLen);

                // Это проверка на REG = 64 - это ответ на прошивку номера
                string strReg = txtPackageLine.Substring(8, 2);

                string[] arrPackage;
                if (strReg == "64")
                {
                    arrPackage = SplitLineResID(txtPackage);
                }
                else
                {
                    arrPackage = SplitLine(txtPackage);
                }

                //_resDta.strIdDevice = txtPackageLine.Substring(4, 4);
                _resDta.strIdDevice = arrPackage[2];

                //if (arrPackage.Length+1 != iPckLen / 2)
                if (txtPackage.Length != iPckLen)
                {
                    _resDta.IsError = true;
                }

                string strCrc = Helper.CreateCRC(txtPackageData);

                /*Проверка на верность CRC*/
                if (strCrc != arrPackage[arrPackage.Length - 1])
                {
                    _resDta.IsError = true;
                }
                else if (IsEchoСonfirmTODevice)
                {
                    _resDta.strEcho = FormatEchoMessage(arrPackage);                    
                }
                /*Пакет для КП*/                
                _resDta.strXMLData = ArrayToXML(arrPackage, _MapProtocol);                
            }
            catch (Exception exp)
            {
                _resDta.IsError = true;                
            }

            return _resDta;
        }

        /// <summary>
        /// Создание объекта для команды
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
        public virtual RequestData getObjectCommand(XDocument xDoc)
        {
            RequestData ResData = new RequestData();
            ResData.isDeleteAllCommand = false;
            ResData.isDeleteSimilarCommand = false;
            _QueueCRCCode = "";
            return ResData;
        }

        /// <summary>
        /// Преобразует XML пакет в пакет для посылки в устройство
        /// </summary>
        /// <param name="xDoc"></param>
        /// <returns></returns>
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
            return sHeader + sPackage + strCrc;
        }

        /// <summary>
        /// Создание Echo пакета
        /// </summary>
        /// <param name="arrMessage"></param>
        /// <returns></returns>
        public virtual string FormatEchoMessage(string[] arrMessage)
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
            valbody = valsource + valdest + valbody;
            string strCrc = Helper.CreateCRC(valbody);

            return valheader + valbody + strCrc;
        }

        /// <summary>
        /// Создание сообщения для центрального web мозга обработки
        /// </summary>
        /// <param name="Message"></param>
        public string CreateMessageToMozek(string Message)
        {
            string res = "";
            string[] _MapProtocol = { "HEADER", "DESTADDR", "SOURCEADDR", "BAT", "PING_ALIVE"};
            string[] _MapValue = {""};
            //ArrayToXML(
            return res;
        }

        /// <summary>
        /// Создание эхо комманды подтверждения в командный пункт 
        /// </summary>
        /// <param name="strDoc"></param>
        /// <returns></returns>
        public string CreateEchoMessageToMozek(string strDoc)
        {            
            XDocument xDoc = XDocument.Parse(strDoc);

            XDocument doc = new XDocument();
            XElement item;
            XElement itemp;
            XElement events = new XElement("EVENTS");
            XElement ev = new XElement("EVENT");
            XElement _params = new XElement("PARAMS");

            XElement xEvent = xDoc.Element("EVENTS").Element("EVENT");
            string sHeader = xEvent.Element("HEADER").Value;

            /*reach (string lname in xDoc)
            {

            }*/

            return doc.ToString();
        }
    }
}
