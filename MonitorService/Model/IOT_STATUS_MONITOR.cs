using System;
using System.Collections.Generic;
using System.Text;

namespace MonitorService.Model
{
    public class IOT_STATUS_MONITOR
    {
        public int id { get; set; }
        public string gateway_id { get; set; }
        public string device_id { get; set; }
        public string device_type { get; set; }
        public string virtual_flag { get; set; }
        public string plc_ip { get; set; }
        public string plc_port { get; set; }
        public string device_status { get; set; }
        public string iotclient_status { get; set; }
        public string hb_status { get; set; }
        public string device_location { get; set; }
        public DateTime last_edc_time { get; set; }
        public DateTime hb_report_time { get; set; }
        public string last_alarm_code { get; set; }
        public string last_alarm_app { get; set; }
        public string last_alarm_message { get; set; }
        public DateTime last_alarm_datetime { get; set; }
    }
}
