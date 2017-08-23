using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using SSGB.Properties;

namespace SSGB
{
    public delegate void eventDelegate(object sender, object data, int Id, flag myflag);

    [Flags]
    public enum flag
    {
        GetUserInfo,
        Login_success,
        Login_cancel,
        Login_error,
        Logout_,
        Rep_progress,
        GiftBuyInfo,
        StripImg
    }

    public partial class MainForm : Form
    {
        public static SteamStore steamAuth = new SteamStore();
        Settings settings = Settings.Default;

        const string giftImgLink = "http://cdn.akamai.steamstatic.com/steam/{1}/{0}/capsule_sm_120.jpg";
        const string appLink = SteamStore._mainsite + "app/";
        const string subLink = SteamStore._mainsite + "sub/";
        const string aboutStr = "SGGB - Steam Store Gift Buyer tool" + "\r\n" +
                               "Copyright © 2015 Maxx53" + "\r\n" +
                               "Email: demmaxx@gmail.com" + "\r\n" +
                               "Website: http://maxx53.com";



        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (steamAuth.Logged)
            {
                steamAuth.Logout();
            }
            else
            {
                toolStripProgressBar1.Visible = true;
                steamAuth.UserName = textBox1.Text;
                steamAuth.Password = textBox2.Text;
                steamAuth.Login();
                SetButton(sender as Button, 3);
            }

        }


        private static void SetButton(Button ourButt, byte type)
        {
            switch (type)
            {
                case 1:
                    ourButt.Image = (Image)Properties.Resources.login;
                    ourButt.Text = "Login";
                    break;
                case 2:
                    ourButt.Image = (Image)Properties.Resources.logout;
                    ourButt.Text = "Logout";
                    break;
                case 3:
                    ourButt.Image = (Image)Properties.Resources.cancel;
                    ourButt.Text = "Cancel";
                    break;
                case 4:
                    ourButt.Image = (Image)Properties.Resources.start;
                    ourButt.Text = "Process Buy";
                    break;
                default:
                    break;
            }

        }

        public void Event_Message(object sender, object data, int Id, flag myflag)
        {
            if (data == null)
                return;

            string message = data.ToString();

            switch (myflag)
            {
                case flag.GetUserInfo:

                    var accinfo = (SteamStore.AccInfo)data;
                    label10.Text = accinfo.Name;
                    label5.Text = accinfo.Wallet;
                    Utils.StartLoadImgTread(accinfo.Avatar, pictureBox2);

                    break;
                case flag.Rep_progress:


                    if (message != string.Empty)
                    toolStripStatusLabel1.Text = message;

                    if (Id < 0)
                    {
                        switch (Id)
                        {
                            case -1:
                                //login
                                SetButton(button1, 1);
                                break;
                            case -2:
                                //logout
                                SetButton(button1, 2);
                                break;
                            case -3:
                                //buy cancell
                                SetButton(button2, 4);
                                break;
                            default:
                                break;
                        }
                        toolStripProgressBar1.Visible = false;
                        toolStripProgressBar1.Value = 0;

                    }
                    else
                    {
                        toolStripProgressBar1.Value = Id;
                    }

                    break;
                case flag.GiftBuyInfo:
                    progressBar1.Value = Id;
                    label6.Text = message.ToString();
                    break;

                case flag.StripImg:
                    if (Id == 0)
                        toolStripStatusImg.Image = Properties.Resources.working;
                    else
                        toolStripStatusImg.Image = Properties.Resources.ready;
                    break;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
           Utils.SaveBinary("cookies.dat", steamAuth.Cookies);
           settings.lastLogin = textBox1.Text;
           settings.lastPass = textBox2.Text;
           settings.loginOnstart = checkBox1.Checked;
           settings.email = textBox4.Text;
           settings.sendToEmail = checkBox2.Checked;
           settings.Save();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Icon = Icon.FromHandle(Properties.Resources.gift.GetHicon());

            steamAuth.delegMessage += new eventDelegate(Event_Message);

            steamAuth.Cookies = (CookieContainer)Utils.LoadBinary("cookies.dat");
            textBox1.Text = settings.lastLogin;
            textBox2.Text = settings.lastPass;
            checkBox1.Checked = settings.loginOnstart;
            checkBox2.Checked = settings.sendToEmail;
            textBox4.Text = settings.email;

            if (checkBox1.Checked)
                button1.PerformClick();

        }

        //http://store.steampowered.com/sub/54408/

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

            string[] parse = textBox3.Text.Split('/');

            if (textBox3.Text.Contains(SteamStore._mainsite) && parse.Length > 4 && (parse[3] == "sub" || parse[3] == "app"))
            {
                Utils.StartLoadImgTread(string.Format(giftImgLink, parse[4], parse[3] + "s"), pictureBox1);
                button2.Enabled = true; 
            }
            else
            {
                pictureBox1.Image = null;
                button2.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (steamAuth.Logged)
            {

                toolStripProgressBar1.Visible = true;
                steamAuth.Link = textBox3.Text;
                steamAuth.Quantity = (int)buyUpDown.Value;
                steamAuth.ToMail = checkBox2.Checked;
                steamAuth.Email = textBox4.Text;
                steamAuth.BuyGift();
                SetButton(sender as Button, 3);
            }
            else
                MessageBox.Show("Login first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

            MessageBox.Show(aboutStr,"About" , MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
