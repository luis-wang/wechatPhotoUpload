using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace AnnouncementWithPhoto.Controllers
{
    public class HomeController : Controller
    {
        private static WEAAccessToken weAccessToken = null;
        private static WEAJsApiTicket jsTicket = null;
        private string wechatImgGet = "https://qyapi.weixin.qq.com/cgi-bin/media/get?access_token={0}&media_id={1}";
        private static void Init()
        {
            if (weAccessToken == null || !weAccessToken.IsValid)
            {
                weAccessToken = GetWEAAccessToken();
            }
        }
        private static WEAAccessToken GetWEAAccessToken()
        {
            WEAAccessToken token = null;
            string CorpID = "wx4939e0f62caad7dc";
            string CorpSecret = "7dRwScpmbpaKBzf8RxjuSgSJgnNjykpkWk68ivTO5hGe3VpywnqKUQwO2VL9AUVe";
            string weaTokenFetchUrl = string.Format("https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={0}&corpsecret={1}", CorpID, CorpSecret);
            using (var webClient = new WebClient())
            {

                string response = webClient.DownloadString(weaTokenFetchUrl);
                token = JsonConvert.DeserializeObject<WEAAccessToken>(response);
            }
            return token;
        }
        public ActionResult Index()
        {
            Init();
            ViewData["timestamp"] = DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            Random random = new Random();
            ViewData["nonceStr"] = new string(
                Enumerable.Repeat("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 16)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray()); ;
            string weaTokenFetchUrl = string.Format(@"https://qyapi.weixin.qq.com/cgi-bin/get_jsapi_ticket?access_token={0}", weAccessToken.AccessToken);
            if (jsTicket==null||!jsTicket.IsValid)
            {
                using (var webClient = new WebClient())
                {
                    string response = webClient.DownloadString(weaTokenFetchUrl);
                    jsTicket = JsonConvert.DeserializeObject<WEAJsApiTicket>(response);
                }
            }
            byte[] hashData = Encoding.Default.GetBytes(string.Format("jsapi_ticket={0}&noncestr={1}&timestamp={2}&url={3}", jsTicket.Ticket, ViewData["nonceStr"].ToString(), ViewData["timestamp"].ToString(), Request.Url.AbsoluteUri));
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] hashResult = sha.ComputeHash(hashData);
            string signature = BitConverter.ToString(hashResult).Replace("-", string.Empty).ToLower();
            ViewData["signature"] = signature;
            return View();
        }
        [HttpPost]
        public ActionResult Index(string[] serverIds)
        {
            string remotEuRL = null;
            string downloadFileName = null;
            List<string> imgSrc = new List<string>();
            try
            {
                Init();
                if (serverIds != null && serverIds.Length > 0)
                {
                    foreach (var item in serverIds)
                    {
                        using (var webClient = new WebClient())
                        {
                            downloadFileName = string.Format("/Photo/Image_{0:0000}{1:00}{2:00}_{3}.jpg", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Ticks.ToString());
                            remotEuRL = string.Format(wechatImgGet, weAccessToken.AccessToken, item);
                            webClient.DownloadFile(remotEuRL, Server.MapPath(downloadFileName));
                            imgSrc.Add(downloadFileName);
                        }

                    }
                }
                return Json(new { success = true, imgSrc = imgSrc.ToArray() });
            }
            catch (Exception e)
            {

                return Json(new { success = false, message =e.Message+ remotEuRL+":"+ downloadFileName+JsonConvert.SerializeObject(e) });
            }

        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
    [DataContract]
    public class WEAAccessToken
    {
        [DataMember(Name = "access_token")]
        public string AccessToken { get; set; }

        [DataMember(Name = "expires_in")]
        public long ExpiresIn { get; set; }

        private DateTime expireOn;

        public DateTime ExpiresOn
        {
            get
            {
                if (ExpiresIn <= 0)
                {
                    return expireOn;
                }
                else
                {
                    return DateTime.UtcNow.AddSeconds(ExpiresIn);
                }
            }
            set
            {
                expireOn = value;
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.AccessToken) && this.ExpiresOn > DateTime.UtcNow;
            }
        }
    }
    [DataContract]
    public class WEAJsApiTicket
    {
        [DataMember(Name = "ticket")]
        public string Ticket { get; set; }

        [DataMember(Name = "expires_in")]
        public long ExpiresIn { get; set; }

        private DateTime expireOn;

        public DateTime ExpiresOn
        {
            get
            {
                if (ExpiresIn <= 0)
                {
                    return expireOn;
                }
                else
                {
                    return DateTime.UtcNow.AddSeconds(ExpiresIn);
                }
            }
            set
            {
                expireOn = value;
            }
        }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrEmpty(this.Ticket) && this.ExpiresOn > DateTime.UtcNow;
            }
        }
    }
}