using System;
using System.IO;
using Kernel.Interface;
using Kernel.Common;
using Kernel.QueueManager;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using Newtonsoft.Json;
using MonitorService.Model;

namespace MonitorService
{
    public class MonitorService : IService, IDisposable
    {
        private string _SeviceName = "MonitorService";
        private readonly ILogger<MonitorService> _logger;
        public cls_MonitorServiceInitial service_initial;
        public bool gw_info_loaded = false;
        public GateWayManager gw_manager;
        public MonitorManager monitor_manager = new MonitorManager();
        public IOT_DbContext db;

        public string ServiceName
        {
            get { return this._SeviceName; }
        }

        public void Init()
        {
            //Console.WriteLine("Test MonitorService Init()");
            if(LoadInitial())
            {
                Console.WriteLine("Load Initial File successful");
                if(LoadGatewayConfig())
                {
                    gw_info_loaded = true;
                }
            }
            
            if(gw_info_loaded)
            {
                if(BuildMonitorInformationFromFile())
                {
                    if (this.service_initial.db_enabled)
                    {
                        if(ConnectDatabase())
                        {
                            BuildMonitorInformationToDB();
                        }
                    }
                }
            }

        }

        public void Dispose()
        {

        }

        public MonitorService(ILoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            _logger = loggerFactory.CreateLogger<MonitorService>();
        }

        public void ReceiveHeartBeat(xmlMessage InputData)
        {
            if(!gw_info_loaded)
            {
                return;
            }

            // Parse Mqtt Topic
            string[] Topic = InputData.MQTTTopic.Split('/');    // /IEW/GateWay/Device/Status/HeartBeat
            string GateWayID = Topic[2].ToString();
            string DeviceID = Topic[3].ToString();

            cls_HeartBeat hb = new cls_HeartBeat();
            hb = JsonConvert.DeserializeObject<cls_HeartBeat>(InputData.MQTTPayload.ToString());

            cls_Monitor_Device_Info mdv = this.monitor_manager.device_list.Where(o => o.gateway_id == GateWayID && o.device_id == DeviceID).FirstOrDefault();
            if (mdv != null)
            {
                mdv.device_status = hb.Status;
                mdv.hb_status = hb.Status;
                mdv.hb_report_time = DateTime.ParseExact(hb.HBDatetime, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            }

            SaveMonitorInformation(GateWayID, DeviceID);
        }

        public void StartAck(xmlMessage InputData)
        {
            if (!gw_info_loaded)
            {
                return;
            }

            // Parse Mqtt Topic
            string[] Topic = InputData.MQTTTopic.Split('/');    // /IEW/GateWay/Device/Cmd/Start/Ack
            string GateWayID = Topic[2].ToString();
            string DeviceID = Topic[3].ToString();

            cls_StartAck sc = new cls_StartAck();
            sc = JsonConvert.DeserializeObject<cls_StartAck>(InputData.MQTTPayload.ToString());

            cls_Monitor_Device_Info mdv = this.monitor_manager.device_list.Where(o => o.gateway_id == GateWayID && o.device_id == DeviceID).FirstOrDefault();
            if (mdv != null)
            {
                if (sc.Cmd_Result == "OK")
                {
                    mdv.device_status = "Ready";
                    mdv.hb_status = "Ready";
                    mdv.hb_report_time = DateTime.ParseExact(sc.Trace_ID, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                }
                else
                {
                    mdv.device_status = "Down";
                    mdv.hb_status = "Down";
                    mdv.hb_report_time = DateTime.ParseExact(sc.Trace_ID, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                }
            }

            SaveMonitorInformation(GateWayID, DeviceID);
        }

        public void ReadDataAck(xmlMessage InputData)
        {
            if (!gw_info_loaded)
            {
                return;
            }

            // Parse Mqtt Topic
            string[] Topic = InputData.MQTTTopic.Split('/');    // /IEW/GateWay/Device/Cmd/ReadData/Ack
            string GateWayID = Topic[2].ToString();
            string DeviceID = Topic[3].ToString();

            cls_ReadDataAck rc = new cls_ReadDataAck();
            rc = JsonConvert.DeserializeObject<cls_ReadDataAck>(InputData.MQTTPayload.ToString());

            cls_Monitor_Device_Info mdv = this.monitor_manager.device_list.Where(o => o.gateway_id == GateWayID && o.device_id == DeviceID).FirstOrDefault();

            if (mdv != null)
            {
                if (rc.Cmd_Result == "OK")
                {
                    mdv.device_status = "Idle";
                    mdv.hb_status = "Idle";
                    mdv.hb_report_time = DateTime.ParseExact(rc.Trace_ID, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                }
                else
                {
                    mdv.device_status = "Down";
                    mdv.hb_status = "Down";
                    mdv.hb_report_time = DateTime.ParseExact(rc.Trace_ID, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                }
            }

            SaveMonitorInformation(GateWayID, DeviceID);
        }

        public void ConfigAck(xmlMessage InputData)
        {
            if (!gw_info_loaded)
            {
                return;
            }

            // Parse Mqtt Topic
            string[] Topic = InputData.MQTTTopic.Split('/');    // /IEW/GateWay/Device/Cmd/Config/Ack
            string GateWayID = Topic[2].ToString();
            string DeviceID = Topic[3].ToString();

            cls_ConfigAck ca = new cls_ConfigAck();
            ca = JsonConvert.DeserializeObject<cls_ConfigAck>(InputData.MQTTPayload.ToString());

            cls_Monitor_Device_Info mdv = this.monitor_manager.device_list.Where(o => o.gateway_id == GateWayID && o.device_id == DeviceID).FirstOrDefault();
            if (mdv != null)
            {

                if (ca.Cmd_Result == "OK")
                {
                    mdv.iotclient_status = "Ready";
                }
                else
                {
                    mdv.iotclient_status = "Off";
                }
            }

            SaveMonitorInformation(GateWayID, DeviceID);
        }

        public void ReceiveAlarm(xmlMessage InputData)
        {
            if (!gw_info_loaded)
            {
                return;
            }

            // Parse Mqtt Topic
            string[] Topic = InputData.MQTTTopic.Split('/');    // /IEW/GateWay/Device/Status/Alarm
            string GateWayID = Topic[2].ToString();
            string DeviceID = Topic[3].ToString();

            cls_Alarm ca = new cls_Alarm();
            ca = JsonConvert.DeserializeObject<cls_Alarm>(InputData.MQTTPayload.ToString());

            cls_Monitor_Device_Info mdv = this.monitor_manager.device_list.Where(o => o.gateway_id == GateWayID && o.device_id == DeviceID).FirstOrDefault();
            if (mdv != null)
            {
                mdv.last_alarm_code = ca.AlarmCode;
                mdv.last_alarm_app = ca.AlarmApp;
                mdv.last_alarm_datetime = DateTime.ParseExact(ca.DateTime, "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                mdv.last_alarm_message = ca.AlarmDesc;
            }

            SaveMonitorInformation(GateWayID, DeviceID);
        }

        public void OTAAck(xmlMessage InputData)
        {
            /*
            // Parse Mqtt Topic
            string[] Topic = InputData.MQTTTopic.Split('/');    // /IEW/GateWay/Device/Cmd/OTA/Ack
            string GateWayID = Topic[2].ToString();
            string DeviceID = Topic[3].ToString();

            cls_Cmd_OTA_Ack ota_ack = new cls_Cmd_OTA_Ack();
            ota_ack = JsonConvert.DeserializeObject<cls_Cmd_OTA_Ack>(InputData.MQTTPayload.ToString());
            if(ota_ack.Cmd_Result == "OK")
            {
                if (ota_ack.App_Name == "IOT")
                {
                    cls_OTA_Gateway_Info ogi = ObjectManager.OTAManager.ota_iot_list.Where(p => p.gateway_id == GateWayID).FirstOrDefault();
                    if (ogi != null)
                    {
                        if (ota_ack.New_Version == ogi.ap_new_version)
                        {
                            ogi.ap_version = ogi.ap_new_version;
                            ogi.ap_last_store_path_name = ogi.ap_new_store_path_name;
                            ogi.md5_last_string = ogi.md5_new_string;
                            ogi.ap_new_version = "";
                            ogi.ap_new_store_path_name = "";
                            ogi.md5_new_string = "";
                            ogi.update_status = "OK";
                            ogi.last_update_time = DateTime.Now;
                            ogi.status_message = ota_ack.Return_Message;
                        }
                    }
                }
                else if (ota_ack.App_Name == "WORKER")
                {
                    cls_OTA_Gateway_Info ogi = ObjectManager.OTAManager.ota_worker_list.Where(p => p.gateway_id == GateWayID).FirstOrDefault();
                    if (ogi != null)
                    {
                        if (ota_ack.New_Version == ogi.ap_new_version)
                        {
                            ogi.ap_version = ogi.ap_new_version;
                            ogi.ap_last_store_path_name = ogi.ap_new_store_path_name;
                            ogi.md5_last_string = ogi.md5_new_string;
                            ogi.ap_new_version = "";
                            ogi.ap_new_store_path_name = "";
                            ogi.md5_new_string = "";
                            ogi.update_status = "OK";
                            ogi.last_update_time = DateTime.Now;
                            ogi.status_message = ota_ack.Return_Message;
                        }
                    }
                }
                else if (ota_ack.App_Name == "FIRMWARE")
                {
                    cls_OTA_Device_Info odi = ObjectManager.OTAManager.ota_firmware_list.Where(p => (p.gateway_id == GateWayID) && (p.device_id == DeviceID)).FirstOrDefault();
                    if (odi != null)
                    {
                        if (ota_ack.New_Version == odi.ap_new_version)
                        {
                            odi.ap_version = odi.ap_new_version;
                            odi.ap_last_store_path_name = odi.ap_new_store_path_name;
                            odi.md5_last_string = odi.md5_new_string;
                            odi.ap_new_version = "";
                            odi.ap_new_store_path_name = "";
                            odi.md5_new_string = "";
                            odi.update_status = "OK";
                            odi.last_update_time = DateTime.Now;
                            odi.status_message = ota_ack.Return_Message;
                        }
                    }
                }
            }
            else //OTA Result is NG
            {
                if (ota_ack.App_Name == "IOT")
                {
                    cls_OTA_Gateway_Info ogi = ObjectManager.OTAManager.ota_iot_list.Where(p => p.gateway_id == GateWayID).FirstOrDefault();
                    if (ogi != null)
                    {
                        ogi.update_status = ota_ack.Cmd_Result;
                        ogi.status_message = ota_ack.Return_Message;
                        ogi.last_update_time = DateTime.Now;
                    }
                }
                else if (ota_ack.App_Name == "WORKER")
                {
                    cls_OTA_Gateway_Info ogi = ObjectManager.OTAManager.ota_worker_list.Where(p => p.gateway_id == GateWayID).FirstOrDefault();
                    if (ogi != null)
                    {
                        ogi.update_status = ota_ack.Cmd_Result;
                        ogi.status_message = ota_ack.Return_Message;
                        ogi.last_update_time = DateTime.Now;
                    }
                }
                else if (ota_ack.App_Name == "FIRMWARE")
                {
                    cls_OTA_Device_Info odi = ObjectManager.OTAManager.ota_firmware_list.Where(p => (p.gateway_id == GateWayID) && (p.device_id == DeviceID)).FirstOrDefault();
                    if (odi != null)
                    {
                        odi.update_status = ota_ack.Cmd_Result;
                        odi.status_message = ota_ack.Return_Message;
                        odi.last_update_time = DateTime.Now;
                    }
                }
            }
            */
        }

        private bool LoadInitial()
        {
            try
            {
                if (!File.Exists("C:\\Gateway\\Config\\Monitor_Service_Init.json"))
                {
                    Console.WriteLine("No Service Initial file exists!");
                    this.service_initial = new cls_MonitorServiceInitial();
                    return true;
                }

                StreamReader inputFile = new StreamReader("C:\\Gateway\\Config\\Monitor_Service_Init.json");

                string json_string = inputFile.ReadToEnd();

                this.service_initial = JsonConvert.DeserializeObject<cls_MonitorServiceInitial>(json_string);

                if (this.service_initial == null)
                {
                    Console.WriteLine("No Initial config exists!");
                    return false;
                }

                inputFile.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Service  Initial file loading error -> " + ex.Message, "Error");
                return false;
            }

            return true;
        }

        private bool LoadGatewayConfig()
        {
            try
            {
                if (!File.Exists(this.service_initial.ccs_gateway_config_path))
                {
                    Console.WriteLine("No Gateway Congatefig file exists!");
                    
                    return false;
                }

                StreamReader inputFile = new StreamReader(this.service_initial.ccs_gateway_config_path);

                string json_string = inputFile.ReadToEnd();

                this.gw_manager = JsonConvert.DeserializeObject<GateWayManager>(json_string);

                if (this.gw_manager.gateway_list == null)
                {
                    Console.WriteLine("No Gateway config exists!");
                    return false;
                }

                inputFile.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Gateway Config file loading error -> " + ex.Message, "Error");
                return false;
            }

            return true;
        }

        private bool BuildMonitorInformationFromFile()
        {
            //Build Monitor Status file
            try
            {
                string json_string;

                if (!File.Exists(this.service_initial.monitor_status_path))
                {
                    //FileStream fs = File.Create(this.service_initial.monitor_status_path);
                    //fs.Close();

                    if (this.gw_manager.gateway_list.Count > 0)
                    {
                        foreach (cls_Gateway_Info gi in this.gw_manager.gateway_list)
                        {
                            if (gi.device_info.Count > 0)
                            {
                                foreach (cls_Device_Info di in gi.device_info)
                                {
                                    cls_Monitor_Device_Info mdi = new cls_Monitor_Device_Info();
                                    mdi.gateway_id = gi.gateway_id;
                                    mdi.device_id = di.device_name;
                                    mdi.virtual_flag = gi.virtual_flag;
                                    mdi.device_type = di.device_type;
                                    mdi.device_status = "Off";
                                    mdi.iotclient_status = "Off";
                                    mdi.hb_status = "Off";
                                    mdi.plc_ip = di.plc_ip_address;
                                    mdi.plc_port = di.plc_port_id;
                                    mdi.device_location = di.device_location;
                                    this.monitor_manager.device_list.Add(mdi);
                                }
                            }
                        }
                    }
                }
                else
                {
                    StreamReader inputFile = new StreamReader(this.service_initial.monitor_status_path);
                    json_string = inputFile.ReadToEnd();
                    inputFile.Close();

                    this.monitor_manager = JsonConvert.DeserializeObject<MonitorManager>(json_string);
                    if (this.gw_manager.gateway_list.Count > 0)
                    {
                        foreach (cls_Gateway_Info gi in this.gw_manager.gateway_list)
                        {
                            if (gi.device_info.Count > 0)
                            {
                                foreach (cls_Device_Info di in gi.device_info)
                                {
                                    cls_Monitor_Device_Info mdi = this.monitor_manager.device_list.Where(p => (p.gateway_id == gi.gateway_id) && (p.device_id == di.device_name)).FirstOrDefault();
                                    if(mdi == null)
                                    {
                                        cls_Monitor_Device_Info tmp = new cls_Monitor_Device_Info();
                                        tmp.gateway_id = gi.gateway_id;
                                        tmp.device_id = di.device_name;
                                        tmp.virtual_flag = gi.virtual_flag;
                                        tmp.device_type = di.device_type;
                                        tmp.device_status = "Off";
                                        tmp.iotclient_status = "Off";
                                        tmp.hb_status = "Off";
                                        tmp.plc_ip = di.plc_ip_address;
                                        tmp.plc_port = di.plc_port_id;
                                        tmp.device_location = di.device_location;
                                        this.monitor_manager.device_list.Add(tmp);
                                    }
                                }
                            }
                        }
                    }
                }

                json_string = JsonConvert.SerializeObject(this.monitor_manager, Newtonsoft.Json.Formatting.Indented);
                StreamWriter output = new StreamWriter(this.service_initial.monitor_status_path);
                output.Write(json_string);
                output.Close();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Build Monitor Status file failed -> " + ex.Message, "Error");
                return false;
            }
        }

        private bool ConnectDatabase()
        {
            try
            {
                System.Diagnostics.Debug.Print("DB COnnect Start" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                this.db = new IOT_DbContext(this.service_initial.db_info.db_type, this.service_initial.db_info.connection_string);

                System.Diagnostics.Debug.Print("DB COnnect End & init Start" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                this.db.ChangeTracker.AutoDetectChangesEnabled = false;
                this.db.ChangeTracker.LazyLoadingEnabled = false;
                this.db.IOT_STATUS_MONITOR.FirstOrDefault();

                System.Diagnostics.Debug.Print("DB COnnect init End" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Connect Database error -> " + ex.Message);
                return false;
            }
        }

        private void BuildMonitorInformationToDB()
        {
            try
            {
                if (this.monitor_manager.device_list.Count > 0)
                {
                    System.Diagnostics.Debug.Print("BuildMonitorInformationToDB() --> Start to Insert IOT_STATUS_MONITOR:  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

                    var tmp = this.db.IOT_STATUS_MONITOR.Where(x => x.id > 0);
                    this.db.IOT_STATUS_MONITOR.RemoveRange(tmp);
                    this.db.SaveChanges();

                    foreach (cls_Monitor_Device_Info mdi in this.monitor_manager.device_list)
                    {
                        IOT_STATUS_MONITOR ism = new IOT_STATUS_MONITOR();
                        ism.gateway_id = mdi.gateway_id;
                        ism.device_id = mdi.device_id;
                        if (mdi.virtual_flag)
                        {
                            ism.virtual_flag = "Y";
                        }
                        else
                        {
                            ism.virtual_flag = "N";
                        }
                        ism.device_type = mdi.device_type;
                        ism.device_status = mdi.device_status;
                        ism.iotclient_status = mdi.iotclient_status;
                        ism.hb_status = mdi.hb_status;
                        ism.plc_ip = mdi.plc_ip;
                        ism.plc_port = mdi.plc_port;
                        ism.device_location = mdi.device_location;
                        ism.last_alarm_code = mdi.last_alarm_code;
                        ism.last_alarm_app = mdi.last_alarm_app;
                        ism.last_edc_time = mdi.last_edc_time;
                        ism.hb_report_time = mdi.hb_report_time;
                        ism.last_alarm_datetime = mdi.last_alarm_datetime;
                        this.db.IOT_STATUS_MONITOR.Add(ism);
                    }
                    this.db.SaveChanges();

                    System.Diagnostics.Debug.Print("BuildMonitorInformationToDB() --> End to Insert IOT_STATUS_MONITOR:  " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("BuildMonitorInformationToDB error -> " + ex.Message);
            }
        }

        private void SaveMonitorInformation(string gateway_id, string device_id)
        {
            string json_string;

            json_string = JsonConvert.SerializeObject(this.monitor_manager, Newtonsoft.Json.Formatting.Indented);
            StreamWriter output = new StreamWriter(this.service_initial.monitor_status_path);
            output.Write(json_string);
            output.Close();

            if(this.service_initial.db_enabled)
            {
                UpdateMonitorDB(gateway_id, device_id);
            }
        }

        private void UpdateMonitorDB(string gateway_id, string device_id)
        {
            var db_record = this.db.IOT_STATUS_MONITOR.Where(p => (p.gateway_id == gateway_id) && (p.device_id == device_id)).FirstOrDefault();
            cls_Monitor_Device_Info mdi = this.monitor_manager.device_list.Where(o => (o.gateway_id == gateway_id) && (o.device_id == device_id)).FirstOrDefault();
            if((db_record != null) && (mdi != null))
            {
                db_record.device_status = mdi.device_status;
                db_record.iotclient_status = mdi.iotclient_status;
                db_record.hb_status = mdi.hb_status;
                db_record.last_alarm_code = mdi.last_alarm_code;
                db_record.last_alarm_app = mdi.last_alarm_app;
                db_record.last_edc_time = mdi.last_edc_time;
                db_record.hb_report_time = mdi.hb_report_time;
                db_record.last_alarm_datetime = mdi.last_alarm_datetime;
                db_record.last_alarm_message = mdi.last_alarm_message;
                this.db.IOT_STATUS_MONITOR.Update(db_record);
                this.db.SaveChanges();
            }
        }
    }
}
