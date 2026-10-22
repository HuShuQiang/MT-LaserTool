using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerAPI
{
    public class ServerApi
    {
        private string IP { get; set; }
        private string Json { get; set; }
        public ServerApi(string ip = "192.168.8.8")
        {
            IP = ip;
        }

        /// <summary>
        /// 获取机型列表
        /// </summary>
        /// <returns></returns>
        public List<string> GetModels()
        {
            List<string> ModelsList = new List<string>();
            string url = "http://" + IP + "/box_cms_v3/mes_v1/getModels.php";
            Json = Post(url, "");
            GetModels Info = JsonConvert.DeserializeObject<GetModels>(Json);
            string modeljson = JsonConvert.SerializeObject(Info.model);
            List<Models> Models = JsonConvert.DeserializeObject<List<Models>>(modeljson);
            for (int i = 0; i < Models.Count; i++)
            {
                ModelsList.Add(Models[i].model_name);
            }
            ModelsList.Sort();
            return ModelsList;
        }

        /// <summary>
        /// 获取机型的字段
        /// </summary>
        /// <param name="model">机型</param>
        /// <param name="uniqueItem">是否为唯一字段，0为所有字段，1为唯一且不可重复字段</param>
        /// <returns></returns>
        public List<string> GetModelFields(string model, int uniqueItem = 0)
        {
            List<string> FieldsList = new List<string>();
            string url = "http://" + IP + "/box_cms_v3/mes_v1/getModelFields.php?model=" + model + "&uniqueItem=" + uniqueItem;
            Json = Post(url, "");
            GetModelFields Info = JsonConvert.DeserializeObject<GetModelFields>(Json);
            string fieldsjson = JsonConvert.SerializeObject(Info.Fields);
            List<Fields> Fields = JsonConvert.DeserializeObject<List<Fields>>(fieldsjson);
            for (int i = 0; i < Fields.Count; i++)
            {
                FieldsList.Add(Fields[i].item_name);
            }
            FieldsList.Sort();
            return FieldsList;
        }

        /// <summary>
        /// 通过已知字段和字段值，获取设备的所有绑定字段信息
        /// </summary>
        /// <param name="model">机型</param>
        /// <param name="fields">字段</param>
        /// <param name="value">字段值</param>
        /// <returns></returns>
        public Hashtable GetDutFieldsAsync(string model, string fields, string value)
        {
            Hashtable DutFieldsList = new Hashtable();
            try
            {
                string url = "http://" + IP + "/box_cms_v3/mes_v1/getDutFields.php?model=" + model + "&dut_number_name=" + fields + "&dut_number_value=" + value;
                Json = Post(url, "");
                if (!Json.Contains("{\"resultCode\":\"9999\",\"resultMsg\":\"查询到的数据为空！\"}"))
                {
                    JObject jArray = (JObject)JsonConvert.DeserializeObject(Json);
                    DutFieldsList = JsonConvert.DeserializeObject<Hashtable>(jArray["fields"].ToString());
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(Json);
            }


            return DutFieldsList;
        }

        public async Task<Hashtable> GetDutFields(string model, string fields, string value)
        {
            Hashtable DutFieldsList = new Hashtable();
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("http://192.168.8.8/box_cms_v3/mes_v1/getDutFields.php"),
                Content = new MultipartFormDataContent
                 {

                    new StringContent(model)
                    {
                        Headers =
                        {
                            ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "model",
                            }
                        }
                    },
                    new StringContent(fields)
                    {
                        Headers =
                        {
                            ContentDisposition = new ContentDispositionHeaderValue("form-data")
                            {
                                Name = "dut_number_name",
                            }
                        }
                    },
                    new StringContent(value)
                    {
                        Headers =
                         {
                                ContentDisposition = new ContentDispositionHeaderValue("form-data")
                                {
                                    Name = "dut_number_value",
                                }
                         }
                    },
                }
            };
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var Json = await response.Content.ReadAsStringAsync();
                Console.WriteLine(Json);
                if (!Json.Contains("{\"resultCode\":\"9999\",\"resultMsg\":\"查询到的数据为空！\"}"))
                {
                    JObject jArray = (JObject)JsonConvert.DeserializeObject(Json);
                    DutFieldsList = JsonConvert.DeserializeObject<Hashtable>(jArray["fields"].ToString());
                }
            }
            return DutFieldsList;
        }

        /// <summary>
        /// 获取设备的测试工站的状态信息
        /// </summary>
        /// <param name="model">机型</param>
        /// <param name="fields">字段</param>
        /// <param name="value">字段值</param>
        /// <returns></returns>
        public Hashtable GetStationStatus(string model, string fields, string value)
        {
            Hashtable StationStatus = new Hashtable();
            string url = "http://" + IP + "/box_cms_v3/mes_v1/getDutTestFlowResult.php?model=" + model + "&dut_number_name=" + fields + "&dut_number_value=" + value;
            Json = Post(url, "");
            JObject jArray = (JObject)JsonConvert.DeserializeObject(Json);
            StationStatus = JsonConvert.DeserializeObject<Hashtable>(jArray["stations"].ToString());
            return StationStatus;
        }

        /// <summary>
        /// 上传测试工站结果值
        /// </summary>
        /// <param name="model">机型</param>
        /// <param name="fields">字段</param>
        /// <param name="value">字段值</param>
        /// <param name="stationinfo">过站信息类</param>
        /// <param name="JsonStr">所需上传的Json格式数据</param>
        /// <returns></returns>
        public string PostStationResult(string model, string fields, string value, Stationinfo stationinfo, string JsonStr)
        {
            string url = "http://" + IP + "/box_cms_v3/mes_v1/postTestDataStr.php?model=" + model + "&dut_number_name=" + fields + "&dut_number_value=" + value + "&station=" + stationinfo.station + "&elapsed=" + stationinfo.elapsed + "&is_pass=" + stationinfo.ispass + "&retry_cnt=" + stationinfo.retry + "&is_misdetect=" + stationinfo.ismisdetect;
            Json = Post(url, JsonStr);
            return Json;
        }

        /// <summary>
        /// 绑定机型数据
        /// </summary>
        /// <param name="model">机型</param>
        /// <param name="fields">字段</param>
        /// <param name="value">字段值</param>
        /// <param name="JsonStr">所需上传的Json格式数据</param>
        /// <returns></returns>
        public string BindFields(string model, string fields, string value, string JsonStr)
        {
            string url = "http://" + model + "/box_cms_v3/mes_v1/bindDutFields.php?model=" + model + "&dut_number_name=" + fields + "&dut_number_value=" + value;
            Json = Post(url, JsonStr);
            return Json;
        }


        /// <summary>
        /// Post请求
        /// </summary>
        /// <param name="Url">访问链接</param>
        /// <param name="jsonStr">所需上传的json格式数据</param>
        /// <returns></returns>
        public string Post(string Url, string jsonStr)
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

    /// <summary>
    /// 测试过站信息
    /// </summary>
    public class Stationinfo
    {
        public string station { get; set; }
        public int elapsed { get; set; }
        public bool ispass { get; set; }
        public int retry { get; set; }
        public bool ismisdetect { get; set; }
    }

    public class GetModels
    {
        public string resultCode { get; set; }
        public string resultMsg { get; set; }
        public List<Models> model { get; set; }

    }

    public class Models
    {
        public int model_id { get; set; }
        public string model_name { get; set; }
    }

    public class GetModelFields
    {
        public string resultCode { get; set; }
        public string resultMsg { get; set; }
        public List<Fields> Fields { get; set; }

    }

    public class Fields
    {
        public string item_key { get; set; }
        public string item_name { get; set; }
    }

    public class DutFields
    {
        public string status { get; set; }
        public string hasBurned { get; set; }
        public JObject fields { get; set; }
        public string dut_number_name { get; set; }
        public string dut_number_value { get; set; }
        public string resultCode { get; set; }
        public string resultMsg { get; set; }
    }

}
