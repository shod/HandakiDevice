using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sys.Device
{    
    /// <summary>
    /// Класс системного модуля Handaki
    /// </summary>
    public class HandakiDevice : Device, IDevice
    {
        public Dictionary<Device, Device> MetaClassRcpDevice; // = new Dictionary<Device, Device>();
        string[] lstAction = { "EE00"};

        public HandakiDevice()
        {            
            InitAction(lstAction);
            MetaClassRcpDevice = new Dictionary<Device, Device>();
        }

        /// <summary>
        /// Получение метаинфрмации
        /// </summary>
        /// <returns></returns>        
        public Dictionary<string, ActionMetaInfo> GetMetaInfo()
        {
            var MetaInfo = new Dictionary<string, ActionMetaInfo>();
            ActionMetaInfo AInfo = new ActionMetaInfo();
            AInfo.DeviceName = "shandakidevice";
            AInfo.isCheckEcho = false;
            AInfo.isWaitRequest = false;

            MetaInfo.Add("EE", AInfo);
            //MetaInfo.Add("21", "tpdevice");

            return MetaInfo;
        }
    }
}
