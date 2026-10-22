using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace 美团镭雕辅助工具
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 打开设备（通过设备索引打开对应的CH341设备连接，返回设备句柄用于后续通信）
        /// </summary>
        /// <param name="iIndex"></param>
        /// <returns></returns>
        [DllImport("CH341DLLA64.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern IntPtr CH341OpenDevice(uint iIndex);

        /// <summary>
        /// 关闭设备（通过设备索引关闭对应的CH341设备连接，释放资源）
        /// </summary>
        /// <param name="iIndex"></param>
        [DllImport("CH341DLLA64.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void CH341CloseDevice(uint iIndex);

        /// <summary>
        /// 获取I2C设备信息（通过CH341芯片与I2C设备通信，读取指定寄存器的数据并转换为SN）
        /// </summary>
        /// <param name="iIndex"></param>
        /// <param name="iWriteLength"></param>
        /// <param name="pWriteBuffer"></param>
        /// <param name="iReadLength"></param>
        /// <param name="pReadBuffer"></param>
        /// <returns></returns>
        [DllImport("CH341DLLA64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int CH341StreamI2C(
        int iIndex, // 设备索引（对应m_deviceIndex）
        int iWriteLength, // 写入长度
        byte[] pWriteBuffer, // 写入缓冲区
        int iReadLength, // 读取长度
        byte[] pReadBuffer // 读取缓冲区
        );


        /// <summary>
        /// 定时器（用于定时检测I2C设备状态和更新UI）
        /// </summary>
        private System.Windows.Forms.Timer timer;

        /// <summary>
        /// 程序是否运行中（控制TCP服务器和镭雕流程）
        /// </summary>
        public bool IsRun = false;
        /// <summary>
        /// 镭雕流程是否正在执行（避免重复弹窗和流程冲突）
        /// </summary>
        private bool IsAction = false;
        /// <summary>
        /// 服务器实例（负责TCP通信，接收客户端连接和数据）
        /// </summary>
        private TcpServer server;
        /// <summary>
        /// 连接状态（标识当前是否有客户端连接，控制数据发送目标和UI提示）
        /// </summary>
        private bool IsConnected = false;
        /// <summary>
        /// 连接终端地址（用于发送数据时指定目标客户端）
        /// </summary>
        private string endPoint = string.Empty;
        /// <summary>
        /// 接收客户端请求标志（当接收到特定请求时，允许开始镭雕流程）
        /// </summary>
        private bool IsAsk = false;
        /// <summary>
        /// 最后一次读取的I2C数据（用于检测数据变化，触发镭雕流程开始）
        /// </summary>
        private string LastData = "";
        /// <summary>
        /// 格式验证结果（通过正则表达式验证SN格式，更新UI提示）
        /// </summary>
        private bool IsRegexOk = false;
        /// <summary>
        /// 获取I2C数据（通过调用CH341StreamI2C与I2C设备通信，读取数据并转换为SN字符串；这个字段用于存储当前从I2C设备读取到的数据，以便在定时器Tick事件中进行比较和处理；初始值为空字符串，表示尚未读取到有效数据）
        /// </summary>
        private string GetI2CData = string.Empty;
        /// <summary>
        /// 输出数据（在获取I2C数据后，根据配置的前缀和读取到的数据构造最终要发送给客户端的SN字符串；这个字段用于存储经过处理后的SN字符串，以便在需要发送给客户端时使用；初始值为空字符串，表示尚未构造出有效的SN字符串）
        /// </summary>
        //private string OutData = string.Empty;
        private string GetRequestField = string.Empty;
        private string GetServerInfoUrl = "http://192.168.8.8/box_cms_v3/mes_v1/getDutFields.php?model=T8FJ&dut_number_name=sn&dut_number_value={0}";
        private List<string> FieldList = new List<string>();
        private Hashtable FieldValueTable = new Hashtable();
        private InputView inputView = null;
        private bool IsGetFieldValue = false;
        private string GetFieldValue = string.Empty;
        private int num = 0;
        private string realsn = string.Empty;

        public Form1()
        {
            InitializeComponent();
            //InitNotifyIcon();
            Text += Assembly.GetExecutingAssembly().GetName().Version;
            InitConfig();
        }

        /// <summary>
        /// 初始化配置（从应用设置中加载IP、端口和扫描字段等参数，更新UI显示）
        /// </summary>
        private void InitConfig()
        {
            try
            {
                IP.Text = Properties.Settings.Default.IP + ":" + Properties.Settings.Default.Port;
                Field.Text = Properties.Settings.Default.Field;
                FieldList = Field.Text.Split(',').ToList();
                CheckField.Text = Properties.Settings.Default.CheckField;
            }
            catch (Exception ex)
            {
                ShowLog($"初始化配置异常: {ex.Message}");
            }

        }

        /// <summary>
        /// 运行按钮点击事件处理程序（当用户点击运行按钮时，根据当前运行状态切换IsRun标志，初始化服务器或停止服务器，并更新日志显示；最后调用StateChange方法更新按钮的文本和颜色以反映当前状态）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Run_Btn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsRun)
                {
                    IsRun = true;
                    ShowLog("运行服务！");
                    InitServer();
                    InitTimer();
                }
                else
                {
                    IsRun = false;
                    ShowLog("停止服务！");
                    StopTcpServer();
                    DisposeTimer();
                    //num = 0;
                    //RunNum.Text = "0";
                }

                Clear_Btn.PerformClick();
                StateChange();
            }
            catch (Exception ex)
            {
                ShowLog($"运行按钮点击异常: {ex.Message}");
            }

        }

        /// <summary>
        /// 按键状态改变（根据IsRun标志的值更新Run_Btn按钮的文本和背景颜色，以直观地显示当前服务的运行状态；当IsRun为true时，按钮显示“运行中”并且背景为浅绿色；当IsRun为false时，按钮显示“未运行”并且背景为红色）
        /// </summary>
        private void StateChange()
        {
            if (IsRun)
            {
                Run_Btn.Text = "运行中";
                Run_Btn.BackColor = Color.LightGreen;
            }
            else
            {
                Run_Btn.Text = "未运行";
                Run_Btn.BackColor = Color.Red;
            }
        }

        /// <summary>
        /// 初始化定时器（创建一个Timer实例，设置间隔为100毫秒，绑定Tick事件处理程序，并启动定时器用于周期性检测I2C设备状态）
        /// </summary>
        private void InitTimer()
        {
            try
            {
                if (timer == null)
                {
                    timer = new System.Windows.Forms.Timer();
                    int delay = int.Parse(Properties.Settings.Default.ReadDelay);
                    timer.Interval = delay; // 500毫秒
                    timer.Tick += Timer_Tick;
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                ShowLog($"初始化定时器异常: {ex.Message}");
            }

        }

        private void DisposeTimer()
        {
            try
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= Timer_Tick;
                    timer.Dispose();
                    timer = null;
                    Data.Text = "获取数据中...";
                    SN.Text = "获取数据中...";
                    realsn = string.Empty;
                    Data.ForeColor = Color.Red;
                    SN.ForeColor = Color.Red;
                    CheckSNRegex();
                }
            }
            catch (Exception ex)
            {
                ShowLog($"销毁定时器异常: {ex.Message}");
            }

        }

        /// <summary>
        /// 定时器Tick事件处理程序（每次触发时读取I2C设备信息，更新UI显示，并根据数据变化和连接状态控制镭雕流程的开始和提示信息的更新）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                CheckAskState();
                CheckConnectedState();
                GetI2CData = GetI2CInfo();
                
                CheckInput();
                if (!string.IsNullOrEmpty(GetI2CData) & GetI2CData != LastData)
                {
                    if (GetI2CData == "-1")
                    {
                        Data.Text = "获取数据中...";
                        SN.Text = "获取数据中...";
                        Data.ForeColor = Color.Red;
                        SN.ForeColor = Color.Red;
                    }
                    else
                    {
                        Data.Text = GetI2CData;
                        SN.Text = Properties.Settings.Default.Prefix + GetI2CData;
                        ShowLog($"获取I2C信息SN={SN.Text}");
                        string Response = PostUrl(string.Format(GetServerInfoUrl, SN.Text));
                        JObject jsonResult = JObject.Parse(Response);
                        if (jsonResult["status"]?.ToString() == "0")
                        {
                            JObject jsonField = (JObject)jsonResult["fields"];
                            FieldValueTable = new Hashtable();
                            foreach (var field in jsonField)
                            {
                                FieldValueTable[field.Key] = field.Value.ToString();
                                ShowLog($"获取服务器信息{field.Key}: {field.Value}");
                            }
                        }
                        else
                        {
                            switch (jsonResult["status"]?.ToString())
                            {
                                case "1":
                                    ShowLog($"{SN.Text}设备状态为已测试！无法镭雕！");
                                    break;
                                case "2":
                                    ShowLog($"{SN.Text}设备状态为已包装！无法镭雕！");
                                    break;
                                case "3":
                                    ShowLog($"{SN.Text}设备状态为已称重！无法镭雕！");
                                    break;
                                case "4":
                                    ShowLog($"{SN.Text}设备状态为已装箱！无法镭雕！");
                                    break;
                                case "100":
                                    ShowLog($"{SN.Text}设备状态为已发货！无法镭雕！");
                                    break;
                                case "101":
                                    ShowLog($"{SN.Text}设备状态为已借机！无法镭雕！");
                                    break;
                            }
                        }
                        Data.ForeColor = Color.Green;
                        SN.ForeColor = Color.Green;
                    }
                    LastData = GetI2CData;
                    CheckSNRegex();
                }

                if (IsRegexOk & IsAsk & IsConnected & IsGetFieldValue & FieldValueTable.Count > 0)
                {
                    StartAction();
                    
                }
            }
            catch (Exception ex)
            {
                ShowLog($"定时器Tick异常: {ex.Message}");
            }

        }


        private void CheckSNRegex()
        {
            Regex snregex = new Regex(Properties.Settings.Default.SNRegex);
            IsRegexOk = snregex.IsMatch(SN.Text);
            if (IsRegexOk)
            {
                Regex_Result.Text = "格式正常";
                Regex_Result.ForeColor = Color.Green;
                realsn = SN.Text;
            }
            else
            {
                Regex_Result.Text = "格式异常";
                Regex_Result.ForeColor = Color.Red;
            }
        }

        private void CheckAskState()
        {
            if (IsAsk)
            {
                Ask_State.Text = "请求中";
                Ask_State.ForeColor = Color.Green;
            }
            else
            {
                Ask_State.Text = "等待请求";
                Ask_State.ForeColor = Color.Red;
            }
        }

        private void CheckConnectedState()
        {
            if (IsConnected)
            {
                IsConnected_State.Text = "已连接";
                IsConnected_State.ForeColor = Color.Green;
            }
            else
            {
                IsConnected_State.Text = "未连接";
                IsConnected_State.ForeColor = Color.Red;
            }
        }

        private bool CheckInput()
        {
            bool res = false;
            if (IsRun &  inputView == null)
            {
                if(!IsGetFieldValue | string.IsNullOrEmpty(FieldValue.Text))
                {
                    inputView = new InputView();
                    DialogResult result = inputView.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        res = true;
                        GetFieldValue = inputView.InputText;
                        IsGetFieldValue = true;
                        FieldValue.Text = GetFieldValue;
                        ShowLog($"弹窗输入{CheckField.Text}字段值为{GetFieldValue}");
                        inputView = null;
                    }
                    else if (result == DialogResult.Abort)
                    {
                        Run_Btn.PerformClick();
                    }
                    else
                    {
                        inputView = null;
                    }
                }

            }

            return res;
        }

        /// <summary>
        /// 开始镭雕流程（当满足条件时弹出确认窗口，用户确认后发送SN数据给客户端，并更新日志显示；如果用户取消，则仅更新日志显示；整个流程通过IsAction标志控制，避免重复执行）
        /// </summary>
        private void StartAction()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Data.Text) && Data.Text != "-1")
                {
                    if (IsRun && IsConnected && IsAsk && IsRegexOk && IsGetFieldValue)
                    {
                        if (!IsAction)
                        {
                            IsAction = true;
                            if (FieldValueTable[CheckField.Text].ToString() == GetFieldValue)
                            {
                                ShowLog($"服务器{CheckField.Text}字段值{FieldValueTable[CheckField.Text].ToString()}与输入值{GetFieldValue}一致，可以进行镭雕");
                                StartView start = new StartView();
                                DialogResult result = start.ShowDialog();
                                if (result == DialogResult.Yes)
                                {
                                    TcpServerSend(realsn);
                                    ShowLog("开始镭雕");
                                    num++;
                                    RunNum.Text = num.ToString();
                                    ShowLog("镭雕次数：" + num.ToString());
                                    IsGetFieldValue = false;
                                }
                                else
                                {
                                    ShowLog("手动取消镭雕");
                                }
                            }
                            else
                            {
                                StartView start = new StartView();
                                start.Tips.Text = $"服务器{CheckField.Text}字段值{FieldValueTable[CheckField.Text].ToString()}与输入值{GetFieldValue}不一致，点击空格取消镭雕";
                                start.SetColor(Color.Red);
                                DialogResult result = start.ShowDialog();
                                ShowLog($"服务器{CheckField.Text}字段值{FieldValueTable[CheckField.Text].ToString()}与输入值{GetFieldValue}不一致，取消镭雕");
                                EndAction();
                            }

                            
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                ShowLog($"开始镭雕异常: {ex.Message}");
            }

        }

        private void EndAction()
        {
            IsAsk = false;
            IsAction = false;
            realsn = string.Empty;
            GetFieldValue = string.Empty;
            FieldValueTable.Clear();
            FieldValue.Text = string.Empty;
        }

        /// <summary>
        /// 清除数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clear_Btn_Click(object sender, EventArgs e)
        {
            try
            {
                FieldValue.Text = string.Empty;
                GetFieldValue = string.Empty;
                FieldValueTable.Clear();
                inputView = null;
                IsGetFieldValue = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"清除数据异常:{ex.Message}");
            }

        }


        /// <summary>
        /// 获取I2C设备信息（通过调用CH341OpenDevice打开设备连接，构造写缓冲区指定I2C写地址和寄存器起始地址，准备读缓冲区，调用CH341StreamI2C进行通信，如果成功则处理读取到的数据并转换为SN字符串，最后关闭设备连接并返回结果）
        /// </summary>
        /// <returns></returns>
        private string GetI2CInfo()
        {
            string result = string.Empty;
            try
            {
                CH341OpenDevice(0);
                byte[] data = null;
                const uint DEVICE_INDEX = 0;
                const byte BMP280_ADDR_7BIT = 0x50;

                // 1. 构造写缓冲区：[I2C写地址, 寄存器起始地址]
                byte i2cWriteAddr = BMP280_ADDR_7BIT << 1; // 转为 8 位写地址
                byte[] writeBuffer = { i2cWriteAddr, 0x08 };       // 从 0xF7 开始读

                // 2. 准备读缓冲区（6 字节）
                byte[] readBuffer = new byte[8];

                // 3. 调用 CH341StreamI2C
                int success = CH341StreamI2C(
                    iIndex: (int)DEVICE_INDEX,
                    iWriteLength: writeBuffer.Length,
                    pWriteBuffer: writeBuffer,
                    iReadLength: readBuffer.Length,
                    pReadBuffer: readBuffer);

                if (success == 1)
                {
                    data = readBuffer;
                    Array.Reverse(data);
                    string hex = ToHexString(data);
                    string sn = HexToDecimal(hex);
                    result = sn;
                }
            }
            finally
            {
                CH341CloseDevice(0);
            }
            

            
            return result;
        }

        /// <summary>
        /// byte数组转十六进制字符串（将byte数组中的每个字节转换为两位大写十六进制字符串，并连接成一个完整的字符串返回；如果输入为null，则返回null）
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToHexString(byte[] bytes)
        {
            if (bytes == null)
                return null;
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X2")); // "X2" 表示两位大写十六进制
            }
            return sb.ToString();
        }

        /// <summary>
        /// Hex
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public string HexToDecimal(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                return "0";

            // 移除可能的 0x 前缀和空格
            hex = hex.Trim().Replace(" ", "");
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2);

            // 检查是否为空
            if (string.IsNullOrEmpty(hex))
                return "0";

            // 转换为十进制数（支持大数）
            try
            {
                long dec = Convert.ToInt64(hex, 16);
                return dec.ToString();
            }
            catch (FormatException)
            {
                throw new ArgumentException($"无效的十六进制字符串: {hex}");
            }
            catch (OverflowException)
            {
                // 如果数值太大（超过 long），改用 BigInteger（.NET Core 2.1+）
                return HandleLargeHex(hex);
            }
        }

        /// <summary>
        /// Hex字符串转大数（当十六进制字符串表示的数值超过 long 类型的范围时，使用 BigInteger 进行转换，确保能够正确处理任意长度的十六进制数，并返回其十进制字符串表示）
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        private static string HandleLargeHex(string hex)
        {
            var bigNum = System.Numerics.BigInteger.Parse("0" + hex,
                System.Globalization.NumberStyles.HexNumber);
            return bigNum.ToString();
        }

        /// <summary>
        /// 保存设置（将当前UI中IP、端口和扫描字段的值保存到应用设置中，以便下次启动时能够加载这些配置；调用Properties.Settings.Default.Save()方法将更改持久化到配置文件中）
        /// </summary>
        private void SaveSettings()
        {
            //Properties.Settings.Default.IP = IP.Text;
            //Properties.Settings.Default.Port = Port.Text;
            Properties.Settings.Default.Field = Field.Text;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// 初始化服务器（首先使用正则表达式验证IP地址的格式，确保输入的IP地址合法；然后尝试将端口号转换为整数，并检查其是否在有效范围内（1-65535）；接着检查扫描字段是否为空；如果所有验证都通过，则调用SaveSettings方法保存当前配置，并调用CreateTcpServer方法创建TCP服务器；根据创建结果更新日志显示；如果任何验证失败，则在日志中显示相应的错误信息）
        /// </summary>
        private void InitServer()
        {
            Regex IPregex = new Regex("^((2(5[0-5]|[0-4]\\d))|[0-1]?\\d{1,2})(\\.((2(5[0-5]|[0-4]\\d))|[0-1]?\\d{1,2})){3}$");
            if (IPregex.IsMatch(Properties.Settings.Default.IP))
            {
                if (int.TryParse(Properties.Settings.Default.Port, out int p) && p >= 1 && p <= 65535)
                {
                    if (!string.IsNullOrWhiteSpace(Field.Text))
                    {
                        //SaveSettings();
                        if (CreateTcpServer())
                        {
                            ShowLog("服务运行中...");
                        }
                        else
                        {
                            ShowLog("服务运行失败！");
                        }
                    }
                    else
                    {
                        ShowLog("机型、扫描字段不能为空！");
                    }
                }
                else
                {
                    ShowLog("端口格式错误！范围:1-65535");
                }
            }
            else
            {
                ShowLog("IP格式错误！例子:127.0.0.1");
            }
        }

        /// <summary>
        /// 创建TCP服务器（尝试创建一个TcpServer实例，绑定指定的IP地址和端口，并订阅相关事件以处理客户端连接、断开和数据接收；在事件处理程序中更新日志显示和连接状态；最后调用StartAsync方法启动服务器；如果过程中发生任何异常，则捕获并在日志中显示错误信息；返回一个布尔值表示服务器是否成功创建）
        /// </summary>
        /// <returns></returns>
        private bool CreateTcpServer()
        {
            bool result = true;
            try
            {
                server = new TcpServer(IPAddress.Parse(Properties.Settings.Default.IP), int.Parse(Properties.Settings.Default.Port));
                // 订阅事件（自动在 UI 线程执行）
                server.ClientConnected += (s, e) =>
                {
                    ShowLog($"[+] 客户端 {e.Client.EndPoint} 已连接");
                    endPoint = e.Client.EndPoint.ToString();
                    IsConnected = true;
                };

                server.ClientDisconnected += (s, e) =>
                {
                    ShowLog($"[-] 客户端 {e.EndPoint} 已断开");
                    endPoint = string.Empty;
                    IsConnected = false;
                    IsAsk = false;
                };

                server.DataReceived += (s, e) =>
                {
                    ShowLog($"[ {e.Client.EndPoint}]接收到客户端信息: {e.Data}");
                    List<string> Fields = Field.Text.Split(',').ToList();
                    if (Fields.Contains(e.Data.Replace("End", "")))
                    {
                        ShowLog($"[ {e.Client.EndPoint}]客户端请求获取数据: {e.Data}");
                        if (FieldList[0].ToString() == e.Data)
                        {
                            IsAsk = true;
                            //CheckInput();
                        }
                        else if (FieldList.Contains(e.Data))
                        {
                            if (!string.IsNullOrEmpty(FieldValueTable[e.Data]?.ToString()))
                            {
                                TcpServerSend(FieldValueTable[e.Data].ToString());
                            }
                        }
                        else if (e.Data.EndsWith("End"))
                        {
                            if (!string.IsNullOrEmpty(FieldValueTable[e.Data.Replace("End", "")]?.ToString()))
                            {
                                TcpServerSend(FieldValueTable[e.Data.Replace("End", "")].ToString());
                            }
                            EndAction();
                        }
                    }
                    else if (e.Data == "Date")
                    {
                        while (true)
                        {
                            if (!string.IsNullOrEmpty(realsn))
                            {
                                string year = DateTime.Now.ToString("yyyy");
                                string month = realsn.Substring(8, 2);
                                string day = realsn.Substring(10, 2);
                                TcpServerSend($"{year}/{month}/{day}");
                                break;
                            }
                            Thread.Sleep(100);
                        }
                    }
                    //else if (e.Data.Contains("SetSN:"))
                    //{
                    //    GetI2CData = e.Data.Replace("SetSN:", "");
                    //}
                };

                server.LogMessage += (s, msg) =>
                {
                    ShowLog(msg);
                };

                server.Start();
                ShowLog($"TCP服务器创建");
            }
            catch (Exception ex)
            {
                result = false;
                ShowLog($"TCP服务器异常: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// 停止TCP服务器（如果服务器实例不为null，则调用StopAsync方法停止服务器，释放资源，并在日志中显示服务器已销毁的消息；最后将服务器实例设置为null以清除引用）
        /// </summary>
        private void StopTcpServer()
        {
            if (server != null)
            {
                server.Stop();
                server.Dispose();
                ShowLog("TCP服务器销毁");
                server = null;
            }
        }

        /// <summary>
        /// 发送数据到客户端（如果服务器实例不为null且要发送的信息不为空或仅包含空白字符，则调用服务器的SendToClient方法，指定目标终端地址和要发送的信息；在日志中显示向哪个客户端发送了什么数据的消息）
        /// </summary>
        /// <param name="info"></param>
        private void TcpServerSend(string info)
        {
            if (server == null || string.IsNullOrWhiteSpace(info))
                return;
            server.SendToClient(endPoint, info);
            ShowLog($"向客户端{endPoint}，发送数据:{info}");
        }

        /// <summary>
        /// 设置按钮点击事件处理程序（当用户点击设置按钮时，创建一个SetupView窗口实例并显示为模态对话框；用户在设置窗口中进行配置后关闭窗口，返回到主界面；整个过程允许用户在独立的窗口中修改配置参数，而不会干扰主界面的操作和显示）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Setup_Btn_Click(object sender, EventArgs e)
        {
            SetupView setup = new SetupView();
            setup.ShowDialog();
            InitConfig();
        }

        /// <summary>
        /// 关闭窗口事件处理程序（当用户尝试关闭主窗口时，弹出一个确认对话框询问是否将工具最小化到托盘；如果用户选择是，则取消关闭操作，隐藏主窗口，并显示一个气泡提示通知用户程序已最小化到托盘；如果用户选择否，则允许窗口正常关闭；整个过程通过检查CloseReason来确定关闭的原因，并通过设置e.Cancel来控制是否取消关闭操作）
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            //if (e.CloseReason == CloseReason.UserClosing)
            //{
            //    DialogResult result = MessageBox.Show("是否最小化工具至托盘！", "提示", MessageBoxButtons.YesNo);
            //    if (result == DialogResult.Yes)
            //    {
            //        e.Cancel = true; // 取消关闭
            //        this.Hide();     // 隐藏窗体
            //        notifyIcon.ShowBalloonTip(1000, "提示", "程序已最小化到托盘", ToolTipIcon.Info);
            //    }

            //}
            base.OnFormClosing(e);
        }

        /// <summary>
        /// 显示窗口事件处理程序（当用户点击右键菜单中的“显示窗口”选项时，调用ShowWindow方法恢复主窗口的显示；ShowWindow方法将主窗口设置为可见，恢复正常窗口状态，并激活到前台，以确保用户能够看到并操作主窗口）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowWindow_Click(object sender, EventArgs e)
        {
            ShowWindow();
        }

        /// <summary>
        /// 显示窗口方法（将主窗口设置为可见，恢复正常窗口状态，并激活到前台，以确保用户能够看到并操作主窗口；这个方法在用户点击托盘图标或右键菜单中的“显示窗口”选项时被调用，用于从托盘状态恢复主窗口的显示）
        /// </summary>
        private void ShowWindow()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal; // 恢复正常窗口状态
            this.Activate(); // 激活到前台
        }

        /// <summary>
        /// 退出事件处理程序（当用户点击右键菜单中的“退出”选项时，首先将托盘图标设置为不可见以避免残留，然后调用Application.Exit()方法退出整个应用程序；这个方法确保在退出应用程序之前正确清理托盘图标资源，避免在系统托盘中留下无效的图标）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Exit_Click(object sender, EventArgs e)
        {
            notifyIcon.Visible = false; // 隐藏托盘图标
            Application.Exit();         // 退出整个应用
        }

        /// <summary>
        /// 关闭窗口事件处理程序（当主窗口被关闭时，调用StopTcpServer方法停止TCP服务器，释放相关资源；同时调用notifyIcon.Dispose()方法销毁托盘图标，确保在程序退出后不会在系统托盘中留下残留的图标；最后调用base.OnFormClosed(e)以执行基类的关闭逻辑；这个方法确保在主窗口关闭时正确清理所有资源，包括服务器和托盘图标，避免资源泄漏和用户界面残留问题）
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            StopTcpServer();
            notifyIcon?.Dispose(); // 重要！避免托盘图标残留
            base.OnFormClosed(e);
        }

        /// <summary>
        /// 显示日志方法（当需要在日志文本框中显示信息时，调用这个方法；它使用Invoke方法确保在UI线程上更新日志文本框的内容，添加新的日志信息并滚动到最新位置；同时调用WriteLogs方法将日志信息写入文件，以便持久化保存日志记录；这个方法提供了一个统一的接口来处理日志显示和文件写入，确保日志信息能够及时显示在界面上并且被保存到文件中）
        /// </summary>
        /// <param name="info"></param>
        private void ShowLog(string info)
        {
            Log.Invoke(new Action(() =>
            {
                Log.AppendText($"{Environment.NewLine}[{DateTime.Now:HH:mm:ss}] {info}");
                Log.SelectionLength = 0; // 取消选择
                Log.ScrollToCaret(); // 滚动到最新日志
            }));
            WriteLogs(info);
        }

        /// <summary>
        /// 保存日志方法（将传入的日志信息写入到一个以当前日期命名的日志文件中；首先获取当前应用程序的基目录，构造日志文件夹路径，如果文件夹不存在则创建；然后构造日志文件的完整路径，使用StreamWriter以追加模式打开文件，并将日志信息写入文件中；最后关闭StreamWriter释放资源；这个方法确保每条日志信息都被持久化保存到对应日期的日志文件中，便于后续查看和分析）
        /// </summary>
        /// <param name="logInfo"></param>
        public void WriteLogs(string logInfo)
        {
            string FloderPath = AppDomain.CurrentDomain.BaseDirectory;
            string FilePath = FloderPath.Replace("/", "\\") + "Logs\\";
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }

            FilePath += DateTime.Now.ToString("yyyyMMdd") + "log.txt";
            StreamWriter writer = new StreamWriter(FilePath, true, System.Text.Encoding.UTF8);
            writer.WriteLine(DateTime.Now.ToString("HH:mm:ss--") + logInfo);
            writer.Close();
        }

        /// <summary>
        /// Post请求方法（当需要向指定URL发送POST请求时，调用这个方法；它创建一个HttpWebRequest对象，设置请求方法为POST，内容类型为application/json，并将传入的JSON字符串转换为字节数组写入请求流；然后获取响应并读取响应流中的内容，最后返回响应内容作为字符串；这个方法提供了一个通用的接口来发送POST请求并获取响应结果，可以用于与服务器进行通信，发送数据并处理服务器的响应）
        /// </summary>
        /// <param name="Url"></param>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        private string PostUrl(string Url, string jsonStr = "")
        {
            //创建一个HTTP请求  
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            //Post请求方式  
            request.Method = "POST";
            //内容类型
            request.ContentType = "application/json";
            //设置参数，并进行URL编码 
            string paraUrlCoded = jsonStr;//System.Web.HttpUtility.UrlEncode(jsonParas);   
            byte[] payload;
            //将Json字符串转化为字节  
            payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
            //设置请求的ContentLength   
            request.ContentLength = payload.Length;
            //发送请求，获得请求流 
            Stream writer;
            try
            {
                writer = request.GetRequestStream();//获取用于写入请求数据的Stream对象
            }
            catch (Exception ex)
            {
                writer = null;
                throw ex;
            }
            //将请求参数写入流
            writer.Write(payload, 0, payload.Length);
            writer.Close();//关闭请求流
                           // String strValue = "";//strValue为http响应所返回的字符流
            HttpWebResponse response;
            try
            {
                //获得响应流
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException ex)
            {
                response = ex.Response as HttpWebResponse;
            }
            Stream s = response.GetResponseStream();
            //  Stream postData = Request.InputStream;
            StreamReader sRead = new StreamReader(s);
            string postContent = sRead.ReadToEnd();
            sRead.Close();
            return postContent;//返回Json数据
        }


    }
}

