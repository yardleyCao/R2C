using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using ZaZaNiao;
using iVision;
using System.Timers;

namespace SocketServer
{
    public partial class FrmServer : Form
    {
        //实例一个服务器对象
        private CSocketServer m_sockServer = null;
        //实例一个委托，含有3个参数，不知道是什么意思
        private CSocketServer.ReceiveCallBack m_recvCallBack = null;

        //定义委托
        private delegate void SetTextCallBack(string strValue);
        //实例上面的委托//更新log
        private SetTextCallBack setCallBack;

        //定义委托//获取服务器的客户端列表，并赋值给下拉列表框
        private delegate void OpComboCallBack(string strValue);
        private OpComboCallBack opCmbCallBack;

        //创建一个相机
        private camraControl camra1 = null;
        private string ipCamra1="192.168.24.114";

        public FrmServer()
        {
            InitializeComponent();

            txt_IP.Text = "127.0.0.1";
            txt_Port.Text = "60000";

            m_sockServer = new CSocketServer();
            setCallBack = ReceiveMsg;
            //委托函数要执行什么功能？//获取服务器的客户端列表，并赋值给下拉列表框
            opCmbCallBack = OpCmbItem;
        }

        //开始监听按钮单击事件
        private void btn_Start_Click(object sender, EventArgs e)
        {
            //委托赋值，参数一致/注册回调
            m_recvCallBack = ServerRecv;
            //初始化服务，将接收消息的函数传到哪里去了？首先处理了“开启监听”
            m_sockServer.InitService(txt_IP.Text, txt_Port.Text,m_recvCallBack);
        }

        /// 服务器端不停的接收客户端发送的消息，服务器接收消息是一个新线程，所以用到委托invoke
        private void ServerRecv(string strClient,byte[] byteMsg,int nLen)
        {
            string strReceiveMsg = "";
            string strReceiveMsg2 = "";
            if (string.IsNullOrEmpty(strClient))
            {//客户端为空
                string str = Encoding.Default.GetString(byteMsg, 0, nLen);
                if (str.Contains("远程主机"))
                {
                    this.Invoke(opCmbCallBack, str);
                }
                strReceiveMsg = str;
            }
            else
            {
                string str = Encoding.Default.GetString(byteMsg, 0, nLen);
                strReceiveMsg = DateTime.Now.ToString()+" 接收：" + strClient + "的消息:" + str;
                try
                {
                    camra1.chgJob(str);//测试程序切换过程
                                       //List<string> Clits = m_sockServer.GetClientIPList();//获取客户端列表
                    string strIp = strClient;
                    string strMsg = camra1.currentJob();//发送给客户端的消息
                    byte[] buffer = Encoding.Default.GetBytes(strMsg);
                    //发送
                    m_sockServer.SendMsg(strIp, buffer);
                    strReceiveMsg2 = DateTime.Now.ToString() + " 发送：" + strIp + "的消息:" + strMsg;
                }
                catch
                {
                    strReceiveMsg2 = "检查相机连接或重新启动本程序";
                }
            }
            
            this.txt_Log.Invoke(setCallBack,strReceiveMsg);
            this.txt_Log.Invoke(setCallBack, strReceiveMsg2);
        }

        //接收的消息添加进日志
        private void ReceiveMsg(string strMsg)
        {
            this.txt_Log.AppendText(strMsg + " \r \n");
        }

        //获取服务器的客户端列表，并赋值给下拉列表框
        private void OpCmbItem(string strItem)
        {
            //下拉选择框
            cmb_Socket.Items.Clear();
            cmb_Socket.Text = "";
            //列表，服务器客户端列表
            List<string> Clits = m_sockServer.GetClientIPList();
            foreach (string strClt in Clits)
            {
                cmb_Socket.Items.Add(strClt);
            }
            if (cmb_Socket.Items.Count>0)
            {
                cmb_Socket.SelectedIndex = 0;
            }
        }

        /// 停止监听
        private void btn_StopListen_Click(object sender, EventArgs e)
        {
            m_sockServer.CloseService();
        }

        //窗口载入事件
        private void FrmServer_Load(object sender, EventArgs e)
        {
            //委托赋值，参数一致/注册回调
            m_recvCallBack = ServerRecv;
            //初始化服务，将接收消息的函数传到哪里去了？首先处理了“开启监听”
            m_sockServer.InitService(txt_IP.Text, txt_Port.Text, m_recvCallBack);
            camra1 = new camraControl();
            camra1.Login(ipCamra1, 23);
        }

        private void FrmServer_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                notifyIcon1.Visible = true;
                this.Hide();
            }
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.WindowState = FormWindowState.Minimized;    //使关闭时窗口向右下角缩小的效果
                notifyIcon1.Visible = true;
                this.Hide();
                return;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            notifyIcon1.Visible = false;
            this.Show();
            WindowState = FormWindowState.Normal;
            this.Focus();
        }

        private void FrmServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;    //使关闭时窗口向右下角缩小的效果
            notifyIcon1.Visible = true;
            this.Hide();
            return;
        }
    }
}