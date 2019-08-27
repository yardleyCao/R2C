/********************************************
 *  Ver 2018/08/02
 *  By Criss
 *  鸟叔机器视觉培训 51Halcon机器视觉
 ********************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ZaZaNiao
{
    //服务器
    class CSocketServer
    {
        //定义接收消息的回调
        public delegate void ReceiveCallBack(string strClient,byte[] bitData,int nLength);
        //委托对象
        private ReceiveCallBack m_recvCallBack = null;

        //将远程连接的客户端的IP地址和Socket存入集合中。这是一个字典，按key访问值
        private Dictionary<string, Socket> m_dicSocket = new Dictionary<string, Socket>();

        //用于通信的Socket
        private Socket m_socketSend;
        //用于监听的SOCKET
        private Socket m_socketWatch;

        //创建监听连接的线程
        private Thread AcceptSocketThread;

        //初始化服务
        public void InitService(string address, string port, ReceiveCallBack recvCall = null)
        {
            try
            {
                m_socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //获取ip地址
                IPAddress ip = IPAddress.Parse(address.Trim());
                //创建端口号
                IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32(port.Trim()));
                //绑定IP地址和端口号
                m_socketWatch.Bind(point);
                //开始监听:设置最大连接请求数目
                m_socketWatch.Listen(10);

                if (recvCall != null)
                {
                    m_recvCallBack = recvCall;
                    string strMsg = "开启监听成功！";
                    byte[] buffer = Encoding.Default.GetBytes(strMsg);
                    m_recvCallBack("", buffer, buffer.Length);
                }

                //创建线程
                AcceptSocketThread = new Thread(new ParameterizedThreadStart(StartListen));
                AcceptSocketThread.IsBackground = true;
                AcceptSocketThread.Start(m_socketWatch);

            }
            catch (SocketException ex)
            {
                byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                m_recvCallBack("", buffer, buffer.Length);
            }
        }

        //关闭服务
        public void CloseService()
        {
            m_socketWatch.Close();
            m_socketSend.Close();
            //终止线程
            AcceptSocketThread.Abort();
        }

        //获取客户端连接列表
        public List<string> GetClientIPList()
        {
            List<string> listArr = new List<string>();
            foreach(KeyValuePair<string,Socket> kvp  in m_dicSocket)
            {
                listArr.Add(kvp.Key);
            }
            return listArr;
        }

        //发送消息
        public void SendMsg(string strClient, byte[] strMsg)
        {
            try
            {
                List<byte> list = new List<byte>();
                //list.Add(0); //消息标志位
                list.AddRange(strMsg);
                //将泛型集合转换为数组
                byte[] newBuffer = list.ToArray();
                //获得用户选择的IP地址
                Socket sock = m_dicSocket[strClient];
                if (sock != null)
                {
                    sock.Send(newBuffer);
                }
               
            }
            catch (SocketException ex)
            {
                if (m_recvCallBack != null)
                {
                    byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                    m_recvCallBack("", buffer, buffer.Length);
                }
            }
        }

        //发送文件
        public void SendFile(string strClient,string strFilePath)
        {
            try
            {
                List<byte> list = new List<byte>();
                //获取要发送的文件的路径
                string strPath = strFilePath.Trim();
                using (FileStream sw = new FileStream(strPath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[2048];
                    int r = sw.Read(buffer, 0, buffer.Length);
                    list.Add(1);
                    list.AddRange(buffer);

                    byte[] newBuffer = list.ToArray();
                    Socket sock = m_dicSocket[strClient];
                    if (sock != null)
                    {
                        sock.Send(newBuffer, SocketFlags.None);
                    }
                }         
            }
            catch (SocketException ex)
            {
                if (m_recvCallBack != null)
                {
                    byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                    m_recvCallBack("", buffer,buffer.Length);
                }
            }
        }

        //一直监听，连上一个就创建一个接收消息的线程
        private void StartListen(object obj)
        {
            Socket socketWatch = obj as Socket;
            while (true)
            {
                try
                {
                    //等待客户端的连接，并且创建一个用于通信的Socket
                    m_socketSend = socketWatch.Accept();
                    //获取远程主机的ip地址和端口号
                    string strIp = m_socketSend.RemoteEndPoint.ToString();
                    m_dicSocket.Add(strIp, m_socketSend);//加入远程主机列表
                    string strMsg = "远程主机：" + m_socketSend.RemoteEndPoint + "连接成功！";
                    if (m_recvCallBack != null)//写出连接成功的消息
                    {
                        byte[] buffer = Encoding.Default.GetBytes(strMsg);
                        m_recvCallBack("", buffer, buffer.Length);
                    }

                    //定义接收客户端消息的线程
                    Thread threadReceive = new Thread(new ParameterizedThreadStart(Receive));
                    threadReceive.IsBackground = true;
                    threadReceive.Start(m_socketSend);//远程端参数
                 }
                catch(SocketException ex)
                {
                    if (m_recvCallBack != null)
                    {
                        byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                        m_recvCallBack("", buffer, buffer.Length);
                    }
                }
            }
        }

        //
        private void Receive(object obj)
        {
            Socket socketSend = obj as Socket;
            try
            {

                while (true)
                {
                    //客户端连接成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[2048];
                    //实际接收到的有效字节数
                    int count = socketSend.Receive(buffer);
                    if (count == 0)//count 表示客户端关闭，要退出循环,抛出错误,原来的break不合适，无法删除无效连接
                    {
                        throw new SocketException(10054);
                    }
                    else
                    {
                        if (m_recvCallBack != null)
                        {
                            string strClient = socketSend.RemoteEndPoint.ToString();
                            m_recvCallBack(strClient, buffer, count);//写出接收的消息
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    //客户端中断
                    if (m_dicSocket.ContainsValue(socketSend))
                    {
                        m_dicSocket.Remove(socketSend.RemoteEndPoint.ToString());
                    }
                }
                if (ex.ErrorCode == 10053)
                {
                    //服务器关闭
                    m_dicSocket.Clear();
                }

                byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                m_recvCallBack("", buffer, buffer.Length);
            }
        }
    }

    //客户端
    class CSocketClient
    {
        //定义接收消息的回调
        public delegate void ReceiveCallBack(byte[] bitData, int nLength);
        //声明委托对象
        private ReceiveCallBack m_recvCallBack = null;

        // 创建连接的Socket
        private Socket m_socketSend;
        // 创建接收客户端发送消息的线程
        private Thread threadReceive;

        // 初始化服务
        public void InitService(string address, string port, ReceiveCallBack recvCall = null)
        {
            try
            {
                m_socketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(address.Trim());
                m_socketSend.Connect(ip, Convert.ToInt32(port.Trim()));
                // 实例化回调
                if (recvCall != null)
                {
                    m_recvCallBack = recvCall;
                }

                // 开启一个新的线程不停的接收服务器发送消息的线程
                threadReceive = new Thread(new ThreadStart(Receive));
                // 设置为后台线程
                threadReceive.IsBackground = true;
                threadReceive.Start();
            }
            catch (SocketException ex)
            {
                byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                if (m_recvCallBack != null)
                {
                    m_recvCallBack(buffer, buffer.Length);
                }
            }
        }

        // 关闭服务
        public void CloseService()
        {
            //关闭socket
            m_socketSend.Close();
            //终止线程
            threadReceive.Abort();
        }

        // 获取服务器IP地址
        public string GetServerIP()
        {
            if(m_socketSend.Connected)
            {
                return m_socketSend.RemoteEndPoint.ToString();
            }
            return "";
        }

        private void Receive()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[2048];
                    // 实际接收到的字节数
                    int recvLen = m_socketSend.Receive(buffer);
                    if (recvLen == 0)
                    {
                        break;
                    }
                    else
                    {
                        if (m_recvCallBack != null)
                        {
                            m_recvCallBack(buffer, recvLen);
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                m_recvCallBack(buffer,buffer.Length);
            }
        }

        public void SendMsg(string strMsg)
        {
            try
            {
                strMsg = strMsg.Trim();
                byte[] buffer = new byte[2048];
                buffer = Encoding.Default.GetBytes(strMsg);
                int receive = m_socketSend.Send(buffer);
            }
            catch (SocketException ex)
            {
                byte[] buffer = Encoding.Default.GetBytes(ex.Message);
                m_recvCallBack(buffer, buffer.Length);
            }
        }
    }
}
