using System;
using System.Text;
using System.Net;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;

namespace SSGB
{
    public class SteamStore
    {
        public string UserName { set; get; }
        public string Password { set; get; }

        public string Link { set; get; }
        public int Quantity { set; get; }
        public bool ToMail { get; set; }
        public string Email { get; set; }

        public bool Logged { private set; get; }

        private CookieContainer cookies;

        public CookieContainer Cookies
        {
            get { return cookies; }
            set
            {
                if (value == null)
                    cookies = new CookieContainer();
                else
                    cookies = value;
            }
        }

        private BackgroundWorker loginThread = new BackgroundWorker();
        private BackgroundWorker logoutThread = new BackgroundWorker();
        private BackgroundWorker buyGiftThread = new BackgroundWorker();

        const string _host = "store.steampowered.com";
        public const string _mainsite = "http://" + _host + "/";
        const string _mainsiteS = "https://" + _host + "/";
        const string _comlog = "https://" + _host + "/login/";
        const string _ref = _comlog + "?redir=0";

        const string _getrsa = _comlog + "getrsakey/";
        const string _dologin = _comlog + "dologin/";
        const string _logout = _comlog + "logout/";

        const string rsaReq = "donotcache={0}&username={1}";
        const string loginReq = "donotcache={9}&password={0}&username={1}&twofactorcode={8}&emailauth={2}&loginfriendlyname={3}&captchagid={4}&captcha_text={5}&emailsteamid={6}&rsatimestamp={7}&remember_login=true";
        const string _capcha = "https://" + _host + "/login/rendercaptcha/?gid=";
        const string _refrcap = "https://" + _host + "/actions/RefreshCaptcha/?count=1";

        const string avLink = "http://cdn.akamai.steamstatic.com/steamcommunity/public/images/avatars/bd/";

        const string ageCheckReq = "snr=1_agecheck_agecheck__age-gate&ageDay=1&ageMonth=January&ageYear=1980";

        const string toCartReq = "action=add_to_cart&sessionid={0}&subid={1}";
        const string toCartUrl = _mainsite + "cart/";
        const string giftUrl = _mainsiteS + "checkout/?purchasetype=gift";

        const string initReq = "gidShoppingCart={0}&PaymentMethod=steamaccount&abortPendingTransactions=0&bHasCardInfo=0&CardNumber=&CardExpirationYear=&CardExpirationMonth=&FirstName=&LastName=&Address=&AddressTwo=&Country={1}&City=&State=&PostalCode=&Phone=&ShippingFirstName=&ShippingLastName=&ShippingAddress=&ShippingAddressTwo=&ShippingCountry={1}&ShippingCity=&ShippingState=&ShippingPostalCode=&ShippingPhone=&bIsGift=1&GifteeAccountID={2}&GifteeEmail={3}&GifteeName=Fellow&GiftMessage=SSGB-sends-that&GiftSentiment=&GiftSignature=SSBB&BankAccount=&BankCode=&BankIBAN=&BankBIC=&bSaveBillingAddress=1&gidPaymentID=&bUseRemainingSteamAccount=1&bPreAuthOnly=0";

        const string initUrl = _mainsiteS + "checkout/inittransaction/";

        const string finalReq = "transid={0}&CardCVV2=";
        const string finalUrl = _mainsiteS + "checkout/finalizetransaction/";

        const string getPriceUrl = _mainsiteS + "checkout/getfinalprice/?count=1&transid={0}&purchasetype=gift&microtxnid=-1&cart={1}";
        //const string statusUrl = storesiteS + "checkout/transactionstatus/?count=1&transid=";

        public class RespRSA
        {

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("publickey_mod")]
            public string Module { get; set; }

            [JsonProperty("publickey_exp")]
            public string Exponent { get; set; }

            [JsonProperty("timestamp")]
            public string TimeStamp { get; set; }
        }

        public class RespProcess
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("emailauth_needed")]
            public bool isEmail { get; set; }

            [JsonProperty("captcha_needed")]
            public bool isCaptcha { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("captcha_gid")]
            public string Captcha_Id { get; set; }

            [JsonProperty("emailsteamid")]
            public string Email_Id { get; set; }

            [JsonProperty("bad_captcha")]
            public bool isBadCap { get; set; }

            [JsonProperty("requires_twofactor")]
            public bool isTwoFactor { get; set; }
        }

        public class RespFinal
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("login_complete")]
            public bool isComplete { get; set; }
        }

        public class RespGiftBuy
        {

            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("transid")]
            public string TransId { get; set; }
        }

        public class AccInfo
        {
            public AccInfo(string name, string wallet, string avatar)
            {
                this.Name = name;
                this.Wallet = wallet;
                this.Avatar = avatar;

            }

            public string Name { set; get; }
            public string Wallet { set; get; }
            public string Avatar { set; get; }
        }

        public SteamStore()
        {
            loginThread.WorkerSupportsCancellation = true;
            loginThread.DoWork += new DoWorkEventHandler(loginThread_DoWork);

            logoutThread.WorkerSupportsCancellation = true;
            logoutThread.DoWork += new DoWorkEventHandler(logoutThread_DoWork);

            buyGiftThread.WorkerSupportsCancellation = true;
            buyGiftThread.DoWork += new DoWorkEventHandler(buyGiftThread_DoWork);
        }

        public void Login()
        {
            if (loginThread.IsBusy != true)
            {
                loginThread.RunWorkerAsync();
            }
            else
                loginThread.CancelAsync();

        }

        public void Logout()
        {
            if (logoutThread.IsBusy != true)
            {
                logoutThread.RunWorkerAsync();
            }
        }

        public event eventDelegate delegMessage;
        protected void doMessage(flag myflag, int Id, object message)
        {
            try
            {

                if (delegMessage != null)
                {
                    Control target = delegMessage.Target as Control;

                    if (target != null && target.InvokeRequired)
                    {
                        target.Invoke(delegMessage, new object[] { this, message, Id, myflag });
                    }
                    else
                    {
                        delegMessage(this, message, Id, myflag);
                    }
                }

            }
            catch (Exception)
            {
                // Main.AddtoLog(e.Message);
            }
        }

        private RespRSA GetRSA()
        {
            var rsaJson = SendPost(string.Format(rsaReq, Utils.GetNoCacheTime(), UserName), _getrsa, _ref);
            return JsonConvert.DeserializeObject<RespRSA>(rsaJson);
        }

        public static string EncryptPassword(string password, string modval, string expval)
        {
            RNGCryptoServiceProvider secureRandom = new RNGCryptoServiceProvider();
            byte[] encryptedPasswordBytes;
            using (var rsaEncryptor = new RSACryptoServiceProvider())
            {
                var passwordBytes = Encoding.ASCII.GetBytes(password);
                var rsaParameters = rsaEncryptor.ExportParameters(false);
                rsaParameters.Exponent = Utils.HexStringToByteArray(expval);
                rsaParameters.Modulus = Utils.HexStringToByteArray(modval);
                rsaEncryptor.ImportParameters(rsaParameters);
                encryptedPasswordBytes = rsaEncryptor.Encrypt(passwordBytes, false);
            }

            return Uri.EscapeDataString(Convert.ToBase64String(encryptedPasswordBytes));
        }


        private void logoutThread_DoWork(object sender, DoWorkEventArgs e)
        {
            SendPost("sessionid=" + GetSessId(cookies), _logout, _mainsite);
            doMessage(flag.Rep_progress, -1, "Logouted");
            Logged = false;
        }

        private void loginThread_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Logged = false;
            doMessage(flag.Rep_progress, 0, "Try Login...");

            string mailCode = string.Empty;
            string guardDesc = string.Empty;
            string capchaId = "-1";
            string capchaTxt = string.Empty;
            string mailId = string.Empty;
            string twoFactorCode = string.Empty;

        begin:

            if (worker.CancellationPending == true)
            {
                doMessage(flag.Rep_progress, -1, "Login cancelled!");
                return;
            }

            doMessage(flag.Rep_progress, 20, string.Empty);

            var rRSA = GetRSA();

            if (rRSA == null)
            {
                doMessage(flag.Rep_progress, -1, "Network problem");
                e.Cancel = true;
                return;
            }

            if (worker.CancellationPending == true)
            {
                doMessage(flag.Rep_progress, -1, "Login cancelled!");
                return;
            }

            doMessage(flag.Rep_progress, 40, string.Empty);

            string finalpass = EncryptPassword(Password, rRSA.Module, rRSA.Exponent);


            string MainReq = string.Format(loginReq, finalpass, UserName, mailCode, guardDesc, capchaId,
                                                                          capchaTxt, mailId, rRSA.TimeStamp, twoFactorCode, Utils.GetNoCacheTime());
            string BodyResp = SendPost(MainReq, _dologin, _ref);


            doMessage(flag.Rep_progress, 60, string.Empty);

            if (worker.CancellationPending == true)
            {
                doMessage(flag.Rep_progress, -1, "Login cancelled!");
                return;
            }

            //Checking login problem
            if (BodyResp.Contains("message"))
            {
                var rProcess = JsonConvert.DeserializeObject<RespProcess>(BodyResp);

                //Checking Incorrect Login
                if (rProcess.Message.Contains("Incorrect"))
                {
                    //Main.AddtoLog("Incorrect login");
                    doMessage(flag.Rep_progress, -1, "Incorrect login");
                    e.Cancel = true;
                    return;
                }
                else
                {
                    //Login correct, checking message type...
                    doMessage(flag.Rep_progress, 70, "Checking captcha or guard code.");

                    Dialog guardCheckForm = new Dialog();

                    if (rProcess.isCaptcha)
                    {
                        //Verifying humanity, loading capcha
                        guardCheckForm.capchgroupEnab = true;
                        guardCheckForm.codgroupEnab = false;
                        guardCheckForm.factorgroupEnab = false;

                        string newcap = _capcha + rProcess.Captcha_Id;
                        Utils.StartLoadImgTread(newcap, guardCheckForm.capchImg);
                    }
                    else
                        if (rProcess.isTwoFactor)
                    {
                        //Steam wants two factor code
                        guardCheckForm.capchgroupEnab = false;
                        guardCheckForm.codgroupEnab = false;
                        guardCheckForm.factorgroupEnab = true;
                    }
                    else
                            if (rProcess.isEmail)
                    {
                        //Steam guard wants email code
                        guardCheckForm.capchgroupEnab = false;
                        guardCheckForm.codgroupEnab = true;
                        guardCheckForm.factorgroupEnab = false;
                    }
                    else
                    {

                        doMessage(flag.Rep_progress, -1, rProcess.Message);
                        e.Cancel = true;
                        return;

                        //Whoops!
                        //goto begin;
                    }

                    //Re-assign main request values
                    if (guardCheckForm.ShowDialog() == DialogResult.OK)
                    {
                        mailCode = guardCheckForm.MailCode;
                        twoFactorCode = guardCheckForm.TwoFactorCode;
                        guardDesc = guardCheckForm.GuardDesc;
                        capchaId = rProcess.Captcha_Id;
                        capchaTxt = Uri.EscapeDataString(guardCheckForm.CapchaText);
                        mailId = rProcess.Email_Id;
                        guardCheckForm.Dispose();
                    }
                    else
                    {
                        doMessage(flag.Rep_progress, -1, "Login cancelled");
                        e.Cancel = true;
                        guardCheckForm.Dispose();
                        return;
                    }

                    goto begin;
                }

            }
            else
            {
                //No Messages, Success!
                var rFinal = JsonConvert.DeserializeObject<RespFinal>(BodyResp);

                doMessage(flag.Rep_progress, 80, null);

                if (rFinal.Success && rFinal.isComplete)
                {
                    Logged = true;
                    doMessage(flag.Rep_progress, 90, "Getting User Info..");
                    doMessage(flag.GetUserInfo, 0, GetAccInfo());
                    doMessage(flag.Rep_progress, -2, "Login Success");
                }
                else
                {
                    //Fail
                    //goto begin;
                }

            }

        }

        private AccInfo GetAccInfo()
        {
            string page = SendGet(_mainsite);

            string name = Regex.Match(page, @"(?<='bottom', true \);" + "\">)(.*?)(?=</span>)", RegexOptions.Singleline).ToString();
            if (name == string.Empty)
                return null;

            string wallet = AsciiToStr(Regex.Match(page, "(?<=store_transactions/\">)(.*?)(?=</a>)", RegexOptions.Singleline).ToString());
            //avatars/bd/bddcfdfa09b26c28ea1c9134aace4cd92383348f.jpg" 
            string avatar = avLink + Regex.Match(page, "(?<=/avatars/bd/)(.*?)(?=\" srcset)", RegexOptions.Singleline).ToString();

            return new AccInfo(name, wallet, avatar);
        }


        private static string AsciiToStr(string input)
        {
            Regex r = new Regex("&#[^;]+;");
            return r.Replace(input, delegate (Match match)
            {
                string value = match.Value.ToString().Replace("&#", "").Replace(";", "");
                int asciiCode;
                if (int.TryParse(value, out asciiCode))
                {
                    return Convert.ToChar(asciiCode).ToString();
                }
                else
                {
                    return value;
                }
            });
        }

        public static string GetSessId(CookieContainer coock)
        {
            //sessid sample MTMyMTg5MTk5Mw%3D%3D
            string resId = string.Empty;

            var stcook = coock.GetCookies(new Uri(_mainsite));

            for (int i = 0; i < stcook.Count; i++)
            {
                string cookname = stcook[i].Name.ToString();
                if (cookname == "sessionid")
                {
                    resId = stcook[i].Value.ToString();
                    break;
                }
            }
            return resId;
        }

        public static void RemoveFromCookie(CookieContainer coock, string cockname)
        {
            var stcook = coock.GetCookies(new Uri(_mainsite));

            for (int i = 0; i < stcook.Count; i++)
            {
                string current = stcook[i].Name.ToString();
                if (current == cockname)
                {
                    stcook[i].Expired = true;
                    break;
                }
            }

        }

        public void BuyGift()
        {
            if (Logged)
            {
                if (buyGiftThread.IsBusy != true)
                {
                    buyGiftThread.RunWorkerAsync();
                }
                else
                    buyGiftThread.CancelAsync();

            }
            //else MessageBox.Show("need login");
        }

        private void cancelMessage()
        {
            doMessage(flag.Rep_progress, -3, "Buy Cancelled");
            doMessage(flag.GiftBuyInfo, 0, "Ready.");
        }


        private void buyGiftThread_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            try
            {
                //Clear shopping cart
                RemoveFromCookie(cookies, "shoppingCartGID");

                doMessage(flag.Rep_progress, 0, "Getting Item Info...");

                string page = SendGet(Link);

                //age check
                if (page.Contains("agegate_box"))
                {
                    doMessage(flag.Rep_progress, 10, "Age checking...");
                    var ageLink = Link.Insert(Link.IndexOf(".com/") + 5, "agecheck/");
                    page = SendPost(ageCheckReq, ageLink, ageLink);
                }

                string giftId = Regex.Match(page, "(?<=add_to_cart_)(.*?)(?=\" action=)", RegexOptions.Singleline).ToString();

                if (giftId == string.Empty)
                {
                    doMessage(flag.GiftBuyInfo, 0, "This item not available for buy!");
                    doMessage(flag.Rep_progress, -3, "Buy Error");
                    return;
                }

                int progress = 100 / Quantity;

                for (int i = 0; i < Quantity; i++)
                {
                    doMessage(flag.Rep_progress, progress * (i + 1), string.Format("Buying: {0} of {1}", (i + 1).ToString(), Quantity.ToString()));

                    if (worker.CancellationPending == true)
                    {
                        cancelMessage();
                        return;
                    }

                    doMessage(flag.GiftBuyInfo, 20, "Adding to cart...");
                    string res = SendPost(string.Format(toCartReq, GetSessId(cookies), giftId), toCartUrl, Link);

                    if (worker.CancellationPending == true)
                    {
                        cancelMessage();
                        return;
                    }

                    doMessage(flag.GiftBuyInfo, 30, "Marking as gift...");
                    string res2 = SendGet(giftUrl);

                    if (worker.CancellationPending == true)
                    {
                        cancelMessage();
                        return;
                    }

                    string cartId = Regex.Match(res2, "(?<=id=\"shopping_cart_gid\" value=\")(.*?)(?=\">)", RegexOptions.Singleline).ToString();

                    if (cartId == string.Empty)
                    {
                        doMessage(flag.GiftBuyInfo, 0, "This item can't gift!");
                        doMessage(flag.Rep_progress, -3, "Buy Error");
                        return;
                    }


                    string accId = Regex.Match(res2, @"(?<=this.checked \) SelectGiftRecipient\( )(.*)(?=, \'\'\); CheckFriendDisplay)", RegexOptions.Singleline).ToString(); //
                    string country = Regex.Match(res2, "(?<=billing_country\" value=\")(.*)(?=\" onchange=\"OnUserCountry)", RegexOptions.Singleline).ToString();//

                    string mail = string.Empty;

                    if (ToMail)
                    {
                        mail = Email;
                        accId = "0";
                    }

                    string fullreq = string.Format(initReq, cartId, country, accId, mail);

                    doMessage(flag.GiftBuyInfo, 50, "Buy Initialization...");
                    string res3 = SendPost(fullreq, initUrl, Link);

                    if (worker.CancellationPending == true)
                    {
                        cancelMessage();
                        return;
                    }

                    var giftresp = JsonConvert.DeserializeObject<RespGiftBuy>(res3);

                    if (giftresp.Success)
                    {
                        doMessage(flag.GiftBuyInfo, 70, "Checking final price...");
                        string getPriceResp = SendGet(string.Format(getPriceUrl, giftresp.TransId, cartId));
                        doMessage(flag.GiftBuyInfo, 80, "Buy finishing...");

                        if (worker.CancellationPending == true)
                        {
                            cancelMessage();
                            return;
                        }

                        string finalResp = SendPost(string.Format(finalReq, giftresp.TransId), finalUrl, giftUrl);

                        if (finalResp.Contains("success\":2,"))
                        {
                            doMessage(flag.GiftBuyInfo, 0, "Buy error, check your wallet balance.");
                            Thread.Sleep(2000);
                            break;
                        }
                        else
                        {
                            doMessage(flag.GiftBuyInfo, 100, "Gift №" + (i + 1) + " bought!");
                            Thread.Sleep(2000);
                        }

                        //string status = SendGet(statusUrl + giftresp.TransId, cookieCont, false, true);
                    }

                }

                doMessage(flag.GiftBuyInfo, 0, "Ready.");

                doMessage(flag.Rep_progress, 100, "Getting User Info..");
                doMessage(flag.GetUserInfo, 0, GetAccInfo());

                doMessage(flag.Rep_progress, -3, "Buying process finished!");

            }
            catch (Exception)
            {
                doMessage(flag.GiftBuyInfo, 100, "Unknown error");
                doMessage(flag.Rep_progress, -3, "Buy error");
            }

        }


        private string SendPost(string req, string url, string refer)
        {
            doMessage(flag.StripImg, 0, string.Empty);
            var res = Utils.SendPost(req, url, refer, cookies);
            doMessage(flag.StripImg, 1, string.Empty);

            return res;

        }

        private string SendGet(string url)
        {
            doMessage(flag.StripImg, 0, string.Empty);
            var res = Utils.SendGet(url, cookies);
            doMessage(flag.StripImg, 1, string.Empty);

            return res;
        }
    }
}
