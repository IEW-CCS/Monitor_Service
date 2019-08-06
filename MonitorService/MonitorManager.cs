using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;

namespace MonitorService
{
    #region Application Initial Configuration Class
    public class cls_MonitorServiceInitial
    {
        public bool db_enabled { get; set; }
        public cls_Database db_info = new cls_Database();
        public string monitor_service_init_path { get; set; }
        public string ccs_gateway_config_path { get; set; }
        public string monitor_status_path { get; set; }
        public string alarm_path { get; set; }
        public string alarm_history_path { get; set; }
    }
    #endregion

    #region Database Connection Information
    public class cls_Database
    {
        public string db_type { get; set; }
        public string data_source { get; set; }
        public string port_id { get; set; }
        public string db_name { get; set; }
        public string user_name { get; set; }
        public string password { get; set; }
        public string provider_name { get; set; }
        public string connection_string { get; set; }
    }
    #endregion

    #region GatewayManager Definition
    public class GateWayManager
    {
        public List<cls_Gateway_Info> gateway_list = new List<cls_Gateway_Info>();

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
    #endregion

    #region Gateway/Device Class Define  For Global used.

    public class cls_Device_Info
    {
        public string device_name { get; set; }
        public string device_type { get; set; }
        public string plc_ip_address { get; set; }
        public string plc_port_id { get; set; }
        public string ble_mac { get; set; }
        public string device_location { get; set; }
        public List<string> ble_service_uuid = new List<string>();
        public ConcurrentDictionary<string, cls_Tag> tag_info = new ConcurrentDictionary<string, cls_Tag>();
        public ConcurrentDictionary<string, cls_CalcTag> calc_tag_info = new ConcurrentDictionary<string, cls_CalcTag>();

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class cls_Gateway_Info
    {
        public string gateway_id { get; set; }
        public string gateway_ip { get; set; }
        public string location { get; set; }
        public bool virtual_flag { get; set; }
        public string virtual_publish_topic { get; set; }
        public List<string> function_list = new List<string>(); //"EDC", "DB"
        public List<cls_Device_Info> device_info = new List<cls_Device_Info>();

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    #endregion

    #region Tag Class Define Class Tag Define   Normal Tag or Calc Tag

    public class cls_Tag
    {
        public string TagName { get; set; }
        public string Type { get; set; }
        public string UUID_Address { get; set; }  // For PLC type Using W1000:W1003  or W1000.1:a   16 bit 代表 0 1 2 3 4 5 6 7 8 9 a b c d e 
        public string Expression { get; set; }
        public string Value { get; set; }
        public double scale { get; set; }
        public double offset { get; set; }
        public string report_flag { get; set; }
        public string db_report_flag { get; set; }
        public string LastUpdateTime { get; set; }
        public string Description { get; set; }
    }

    #endregion

    #region Calculate Tag Class Define Class Tag Define  -  Calc Tag

    public class cls_CalcTag
    {
        public string TagName { get; set; }
        public string Type { get; set; }
        public string Expression { get; set; }
        public string Value { get; set; }
        public string ParamA { get; set; }
        public string ParamB { get; set; }
        public string ParamC { get; set; }
        public string ParamD { get; set; }
        public string ParamE { get; set; }
        public string ParamF { get; set; }
        public string ParamG { get; set; }
        public string ParamH { get; set; }
        public string LastUpdateTime { get; set; }
        public string Description { get; set; }
    }

    #endregion

    #region Monitor Class definition
    public class cls_Monitor_Device_Info
    {
        public string gateway_id { get; set; }
        public string device_id { get; set; }
        public string device_type { get; set; }
        public bool virtual_flag { get; set; }
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

    public class MonitorManager
    {
        public List<cls_Monitor_Device_Info> device_list = new List<cls_Monitor_Device_Info>();

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
    #endregion

    #region Class to define the received ReplyData MQTT message payload

    public class cls_ReplyData_Item
    {
        public string DATA_NAME { get; set; }
        public string DATA_VALUE { get; set; }
    }

    public class cls_ReplyData
    {
        public string Device_ID { get; set; }
        public string IP_Address { get; set; }
        public string Time_Stamp { get; set; }
        public List<cls_ReplyData_Item> EDC_Data = new List<cls_ReplyData_Item>();
    }

    #endregion

    #region Class to define the received HeartBeat MQTT message payload
    public class cls_HeartBeat
    {
        public string Version { get; set; }
        public string Status { get; set; }
        public string HBDatetime { get; set; }
    }
    #endregion

    #region Class to define the received Start Ack MQTT message payload
    public class cls_StartAck
    {
        public string Cmd_Result { get; set; }
        public string Trace_ID { get; set; }
    }
    #endregion

    #region Class to define the received Config Ack MQTT message payload
    public class cls_ConfigAck
    {
        public string Cmd_Result { get; set; }
        public string Trace_ID { get; set; }
    }
    #endregion

    #region Class to define the received ReadData Ack MQTT message payload
    public class cls_ReadDataAck
    {
        public string Cmd_Result { get; set; }
        public string Trace_ID { get; set; }
    }
    #endregion

    #region Class to define the received OTA Ack MQTT message payload
    public class cls_Cmd_OTA_Ack
    {
        public string Cmd_Result { get; set; }
        public string Trace_ID { get; set; }
        public string App_Name { get; set; }
        public string New_Version { get; set; }
        public string MD5_String { get; set; }
        public string Return_Message { get; set; }
    }
    #endregion

    #region Class to define the received Alarm MQTT message payload
    public class cls_Alarm
    {
        public string AlarmCode { get; set; }
        public string AlarmLevel { get; set; }
        public string AlarmApp { get; set; }
        public string DateTime { get; set; }
        public string AlarmDesc { get; set; }
    }
    #endregion

}