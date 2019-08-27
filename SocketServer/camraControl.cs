using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Windows.Forms;

namespace iVision
{
    class camraControl
    {
        //private string user = "admin";//相机用户名
        //private string password = "";//相机登录密码
        private string ip;//相机地址
        private int port;//相机端口
        private TcpClient nativeClient;
        private NetworkStream stream;
        public bool connected;
        public camraControl()
        {
        }
        #region 登录相机
        public void Login(string IP, int portNum)
        {
            ip = IP;
            port = portNum;
            try
            {
                nativeClient = new TcpClient(ip, port);
                string result="";
                while (result!= "User Logged In")
                {
                    switch (result)
                    {
                        case "User:":
                            writeMsg("admin\r\n");
                            result = readMsg()[0];
                            break;
                        case "Password:":
                            writeMsg(" ");
                            result = readMsg()[0];
                            break;
                        default:
                            if (result.Contains("User:"))
                            {
                                result = "User:";
                            }
                            else
                            {
                                result = readMsg()[0];
                            }
                            break;
                    }
                }
                Console.WriteLine("连接成功"+ip.ToString());
            }
            catch(SocketException e)
            {
                MessageBox.Show(e.ToString());
            }
            finally
            {
                //connected = nativeClient.Connected;
            }
        }
        #endregion
        #region 控制指令
        public void writeMsg(string msg)
        {
            try
            {
                Console.WriteLine(msg);
                msg = msg + "\r\n";
                byte[] data = Encoding.UTF8.GetBytes(msg);
                stream.Write(data, 0, data.Length);
                //string result = readMsg();
                //Console.WriteLine(result);
            }
            catch(SocketException e)
            {
                MessageBox.Show(e.ToString());
            }
        }
        #endregion
        #region 指令执行结果
        public string[] readMsg()
        {
            try
            {
                stream = nativeClient.GetStream();//通过网络流进行数据的交换  
                byte[] buffer = new byte[1024];
                int bytes = stream.Read(buffer, 0, 1024);
                string responseData = Encoding.UTF8.GetString(buffer, 0, bytes);
                responseData = responseData.Replace("\r\n", ",");
                string[] data = responseData.Split(',');
                foreach (string txt in data)
                {
                    Console.WriteLine(txt);
                }
                
                return data;
            }
            catch
            {
                MessageBox.Show("Please check server.");
                return null;
            }
        }
        #endregion
        #region 当前程序
        public string currentJob()
        {
            string job="";
            writeMsg("GJ");
            string[] temp = readMsg();
            job = temp[1];
            return job;
        }
        #endregion
        #region 程序列表
        public string[] listFile()
        {
            writeMsg("Get Filelist");
            string[] files = new string[1024];
            string[] fs = readMsg();
            int i=0;
            foreach (string file in fs)
            {
                if(file.EndsWith(".job"))
                {
                    files[i] = file;
                    i += 1;
                }
            }
            files = files.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            return files;
        }
        #endregion
        #region 切换程序
        public void chgJob(string job)
        {
            job = "LF" + job;
            writeMsg("GO");
            string temp = readMsg()[0];
            if(temp=="1")
            {
                writeMsg("SO0");
                if(readMsg()[0]=="1")
                {
                    writeMsg(job);
                    if(readMsg()[0] == "1")
                    {
                        //切换成功
                        writeMsg("SO1");
                        readMsg();
                    }
                }
            }
            if (temp == "0")
            {
                writeMsg(job);
                readMsg();
            }
        }
        #endregion
    }
}
