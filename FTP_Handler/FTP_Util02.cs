using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FTP_Handler
{
    /// <summary>
    /// FTP Client(不允許匿名登入)
    /// 上傳、下載已測試，其餘未測試
    /// </summary>
    public class FTPClient
    {
        #region 建構函式
        /// <summary>
        /// 預設建構函式
        /// </summary>
        public FTPClient()
        {
            this.ftpServerIP = "";
            this.remoteFilePath = "";
            this.ftpUserID = "";
            this.ftpPassword = "";
            this.ftpServerPort = 21;
            this.bConnected = false;
        }

        /// <summary>
        /// 建構函式
        /// </summary>
        /// <param name="remoteHost">FTP伺服器IP地址</param>
        /// <param name="remotePath">當前伺服器目錄</param>
        /// <param name="remoteUser">Ftp 伺服器登入使用者賬號</param>
        /// <param name="remotePass">Ftp 伺服器登入使用者密碼</param>
        /// <param name="remotePort">FTP伺服器埠</param>
        public FTPClient(string ftpServerIP, string remoteFilePath, string ftpUserID, string ftpPassword, int ftpServerPort, bool anonymousAccess = false)
        {
            this.ftpServerIP = ftpServerIP;
            this.remoteFilePath = remoteFilePath;
            this.ftpUserID = ftpUserID;
            this.ftpPassword = ftpPassword;
            this.ftpServerPort = ftpServerPort;
            this.Connect();
        }
        #endregion

        #region 登陸欄位、屬性
        /// <summary>
        /// FTP伺服器IP地址
        /// </summary>
        private string ftpServerIP;
        public string FtpServerIP
        {
            get
            {
                return ftpServerIP;
            }
            set
            {
                this.ftpServerIP = value;
            }
        }
        /// <summary>
        /// FTP伺服器埠
        /// </summary>
        private int ftpServerPort;
        public int FtpServerPort
        {
            get
            {
                return ftpServerPort;
            }
            set
            {
                this.ftpServerPort = value;
            }
        }
        /// <summary>
        /// 當前伺服器目錄
        /// </summary>
        private string remoteFilePath;
        public string RemoteFilePath
        {
            get
            {
                return remoteFilePath;
            }
            set
            {
                this.remoteFilePath = value;
            }
        }
        /// <summary>
        /// Ftp 伺服器登入使用者賬號
        /// </summary>
        private string ftpUserID;
        public string FtpUserID
        {
            set
            {
                this.ftpUserID = value;
            }
        }
        /// <summary>
        /// Ftp 伺服器使用者登入密碼
        /// </summary>
        private string ftpPassword;
        public string FtpPassword
        {
            set
            {
                this.ftpPassword = value;
            }
        }

        /// <summary>
        /// 是否登入
        /// </summary>
        private bool bConnected;
        public bool Connected
        {
            get
            {
                return this.bConnected;
            }
        }
        #endregion

        #region 連結
        /// <summary>
        /// 建立連線 
        /// </summary>
        public void Connect()
        {
            socketControl = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(FtpServerIP), ftpServerPort);
            // 連結
            try
            {
                socketControl.Connect(ep);
            }
            catch (Exception)
            {
                throw new IOException("Couldn't connect to remote server");
            }

            // 獲取應答碼
            ReadReply();
            if (iReplyCode != 220)
            {
                DisConnect();
                throw new IOException(strReply.Substring(4));
            }

            // 登陸
            SendCommand("USER " + ftpUserID);
            if (!(iReplyCode == 331 || iReplyCode == 230))
            {
                CloseSocketConnect();//關閉連線
                throw new IOException(strReply.Substring(4));
            }
            if (iReplyCode != 230)
            {
                SendCommand("PASS " + ftpPassword);
                if (!(iReplyCode == 230 || iReplyCode == 202))
                {
                    CloseSocketConnect();//關閉連線
                    throw new IOException(strReply.Substring(4));
                }
            }

            bConnected = true;

            // 切換到初始目錄
            if (!string.IsNullOrEmpty(remoteFilePath))
            {
                ChDir(remoteFilePath);
            }
        }


        /// <summary>
        /// 關閉連線
        /// </summary>
        public void DisConnect()
        {
            if (socketControl != null)
            {
                SendCommand("QUIT");
            }
            CloseSocketConnect();
        }

        #endregion

        #region 傳輸模式

        /// <summary>
        /// 傳輸模式:二進位制型別、ASCII型別
        /// </summary>
        public enum TransferType
        {
            Binary,
            ASCII
        };

        /// <summary>
        /// 設定傳輸模式
        /// </summary>
        /// <param name="ttType">傳輸模式</param>
        public void SetTransferType(TransferType ttType)
        {
            if (ttType == TransferType.Binary)
            {
                SendCommand("TYPE I");//binary型別傳輸
            }
            else
            {
                SendCommand("TYPE A");//ASCII型別傳輸
            }
            if (iReplyCode != 200)
            {
                throw new IOException(strReply.Substring(4));
            }
            else
            {
                trType = ttType;
            }
        }


        /// <summary>
        /// 獲得傳輸模式
        /// </summary>
        /// <returns>傳輸模式</returns>
        public TransferType GetTransferType()
        {
            return trType;
        }

        #endregion

        #region 檔案操作
        /// <summary>
        /// 獲得檔案列表
        /// </summary>
        /// <param name="strMask">檔名的匹配字串</param>
        /// <returns></returns>
        public string[] Dir(string strMask)
        {
            // 建立連結
            if (!bConnected)
            {
                Connect();
            }

            //建立進行資料連線的socket
            Socket socketData = CreateDataSocket();

            //傳送命令
            SendCommand("LIST " + strMask);

            //分析應答程式碼
            if (!(iReplyCode == 150 || iReplyCode == 125 || iReplyCode == 226))
            {
                throw new IOException(strReply.Substring(4));
            }

            //獲得結果
            strMsg = "";
            while (true)
            {
                int iBytes = socketData.Receive(buffer, buffer.Length, 0);
                strMsg += GB2312.GetString(buffer, 0, iBytes);
                if (iBytes < buffer.Length)
                {
                    break;
                }
            }
            char[] seperator = { '\n' };
            string[] strsFileList = strMsg.Split(seperator);
            socketData.Close();//資料socket關閉時也會有返回碼
            if (iReplyCode != 226)
            {
                ReadReply();
                if (iReplyCode != 226)
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
            return strsFileList;
        }


        /// <summary>
        /// 獲取檔案大小
        /// </summary>
        /// <param name="strFileName">檔名</param>
        /// <returns>檔案大小</returns>
        public long GetFileSize(string strFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("SIZE " + Path.GetFileName(strFileName));
            long lSize = 0;
            if (iReplyCode == 213)
            {
                lSize = Int64.Parse(strReply.Substring(4));
            }
            else
            {
                throw new IOException(strReply.Substring(4));
            }
            return lSize;
        }


        /// <summary>
        /// 刪除
        /// </summary>
        /// <param name="strFileName">待刪除檔名</param>
        public void Delete(string strFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("DELE " + strFileName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
        }


        /// <summary>
        /// 重新命名(如果新檔名與已有檔案重名,將覆蓋已有檔案)
        /// </summary>
        /// <param name="strOldFileName">舊檔名</param>
        /// <param name="strNewFileName">新檔名</param>
        public void Rename(string strOldFileName, string strNewFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("RNFR " + strOldFileName);
            if (iReplyCode != 350)
            {
                throw new IOException(strReply.Substring(4));
            }
            //  如果新檔名與原有檔案重名,將覆蓋原有檔案
            SendCommand("RNTO " + strNewFileName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
        }
        #endregion

        #region 上傳和下載
        /// <summary>
        /// 下載一批檔案
        /// </summary>
        /// <param name="strFileNameMask">檔名的匹配字串</param>
        /// <param name="strFolder">本地目錄(不得以\結束)</param>
        public void Download(string strFileNameMask, string strFolder)
        {
            if (!bConnected)
            {
                Connect();
            }
            string[] strFiles = Dir(strFileNameMask);
            foreach (string strFile in strFiles)
            {
                if (!strFile.Equals(""))//一般來說strFiles的最後一個元素可能是空字串
                {
                    if (strFile.LastIndexOf(".") > -1)
                    {
                        Download(strFile.Replace("\r", ""), strFolder, strFile.Replace("\r", ""));
                    }
                }
            }
        }

        /// <summary>
        /// 下載目錄
        /// </summary>
        /// <param name="strRemoteFileName">要下載的檔名</param>
        /// <param name="strFolder">本地目錄(不得以\結束)</param>
        /// <param name="strLocalFileName">儲存在本地時的檔名</param>
        public void Download(string strRemoteFileName, string strFolder, string strLocalFileName)
        {
            if (strLocalFileName.StartsWith("-r"))
            {
                string[] infos = strLocalFileName.Split(' ');
                strRemoteFileName = strLocalFileName = infos[infos.Length - 1];
                if (!this.bConnected)
                {
                    this.Connect();
                }
                SetTransferType(TransferType.Binary);
                if (strLocalFileName.Equals(""))
                {
                    strLocalFileName = strRemoteFileName;
                }
                if (!File.Exists(strLocalFileName))
                {
                    Stream st = File.Create(strLocalFileName);
                    st.Close();
                }
                FileStream output = new
                    FileStream(strFolder + "\\" + strLocalFileName, FileMode.Create);
                Socket socketData = CreateDataSocket();
                SendCommand("RETR " + strRemoteFileName);
                if (!(iReplyCode == 150 || iReplyCode == 125
                || iReplyCode == 226 || iReplyCode == 250))
                {
                    throw new IOException(strReply.Substring(4));
                }
                int receiveBytes = 0;
                while (true)
                {
                    int iBytes = socketData.Receive(buffer, buffer.Length, 0);
                    receiveBytes = receiveBytes + iBytes;
                    output.Write(buffer, 0, iBytes);
                    if (iBytes <= 0)
                    {
                        break;
                    }
                }
                output.Close();
                if (socketData.Connected)
                {
                    socketData.Close();
                }
                if (!(iReplyCode == 226 || iReplyCode == 250))
                {
                    ReadReply();
                    if (!(iReplyCode == 226 || iReplyCode == 250))
                    {
                        throw new IOException(strReply.Substring(4));
                    }
                }
            }
        }

        /// <summary>
        /// 下載一個檔案
        /// </summary>
        /// <param name="strRemoteFileName">要下載的檔名</param>
        /// <param name="strFolder">本地目錄(不得以\結束)</param>
        /// <param name="strLocalFileName">儲存在本地時的檔名</param>
        public void DownloadFile(string strRemoteFileName, string strFolder, string strLocalFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SetTransferType(TransferType.Binary);
            if (strLocalFileName.Equals(""))
            {
                strLocalFileName = strRemoteFileName;
            }
            if (!File.Exists(strLocalFileName))
            {
                Stream st = File.Create(strLocalFileName);
                st.Close();
            }

            FileStream output = new
                FileStream(strFolder + "\\" + strLocalFileName, FileMode.Create);
            Socket socketData = CreateDataSocket();
            SendCommand("RETR " + strRemoteFileName);
            if (!(iReplyCode == 150 || iReplyCode == 125
            || iReplyCode == 226 || iReplyCode == 250))
            {
                throw new IOException(strReply.Substring(4));
            }
            while (true)
            {
                int iBytes = socketData.Receive(buffer, buffer.Length, 0);
                output.Write(buffer, 0, iBytes);
                if (iBytes <= 0)
                {
                    break;
                }
            }
            output.Close();
            if (socketData.Connected)
            {
                socketData.Close();
            }
            if (!(iReplyCode == 226 || iReplyCode == 250))
            {
                ReadReply();
                if (!(iReplyCode == 226 || iReplyCode == 250))
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
        }

        /// <summary>
        /// 下載一個檔案(斷點續傳)
        /// </summary>
        /// <param name="strRemoteFileName">要下載的檔名</param>
        /// <param name="strFolder">本地目錄(不得以\結束)</param>
        /// <param name="strLocalFileName">儲存在本地時的檔名</param>
        /// <param name="size">已下載檔案流長度</param>
        public void DownloadBrokenFile(string strRemoteFileName, string strFolder, string strLocalFileName, long size)
        {
            if (!bConnected)
            {
                Connect();
            }
            SetTransferType(TransferType.Binary);
            FileStream output = new
                FileStream(strFolder + "\\" + strLocalFileName, FileMode.Append);
            Socket socketData = CreateDataSocket();
            SendCommand("REST " + size.ToString());
            SendCommand("RETR " + strRemoteFileName);
            if (!(iReplyCode == 150 || iReplyCode == 125
            || iReplyCode == 226 || iReplyCode == 250))
            {
                throw new IOException(strReply.Substring(4));
            }
            while (true)
            {
                int iBytes = socketData.Receive(buffer, buffer.Length, 0);
                output.Write(buffer, 0, iBytes);
                if (iBytes <= 0)
                {
                    break;
                }
            }
            output.Close();
            if (socketData.Connected)
            {
                socketData.Close();
            }
            if (!(iReplyCode == 226 || iReplyCode == 250))
            {
                ReadReply();
                if (!(iReplyCode == 226 || iReplyCode == 250))
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
        }



        /// <summary>
        /// 上傳一批檔案
        /// </summary>
        /// <param name="strFolder">本地目錄(不得以\結束)</param>
        /// <param name="strFileNameMask">檔名匹配字元(可以包含*和?)</param>
        public void Upload(string strFolder, string strFileNameMask)
        {
            string[] strFiles = Directory.GetFiles(strFolder, strFileNameMask);
            foreach (string strFile in strFiles)
            {
                //strFile是完整的檔名(包含路徑)
                Upload(strFile);
            }
        }


        /// <summary>
        /// 上傳一個檔案
        /// </summary>
        /// <param name="strFileName">本地檔名</param>
        public void Upload(string strFileName)
        {
            if (!bConnected)
            {
                Connect();
            }
            Socket socketData = CreateDataSocket();
            SendCommand("STOR " + Path.GetFileName(strFileName));
            if (!(iReplyCode == 125 || iReplyCode == 150))
            {
                throw new IOException(strReply.Substring(4));
            }
            FileStream input = new
            FileStream(strFileName, FileMode.Open);
            int iBytes = 0;
            while ((iBytes = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                socketData.Send(buffer, iBytes, 0);
            }
            input.Close();
            if (socketData.Connected)
            {
                socketData.Close();
            }
            if (!(iReplyCode == 226 || iReplyCode == 250))
            {
                ReadReply();
                if (!(iReplyCode == 226 || iReplyCode == 250))
                {
                    throw new IOException(strReply.Substring(4));
                }
            }
        }

        #endregion

        #region 目錄操作
        /// <summary>
        /// 建立目錄
        /// </summary>
        /// <param name="strDirName">目錄名</param>
        public void MkDir(string strDirName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("MKD " + strDirName);
            if (iReplyCode != 257)
            {
                throw new IOException(strReply.Substring(4));
            }
        }


        /// <summary>
        /// 刪除目錄
        /// </summary>
        /// <param name="strDirName">目錄名</param>
        public void RmDir(string strDirName)
        {
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("RMD " + strDirName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
        }


        /// <summary>
        /// 改變目錄
        /// </summary>
        /// <param name="strDirName">新的工作目錄名</param>
        public void ChDir(string strDirName)
        {
            if (strDirName.Equals(".") || strDirName.Equals(""))
            {
                return;
            }
            if (!bConnected)
            {
                Connect();
            }
            SendCommand("CWD " + strDirName);
            if (iReplyCode != 250)
            {
                throw new IOException(strReply.Substring(4));
            }
            this.remoteFilePath = strDirName;
        }

        #endregion

        #region 內部變數
        /// <summary>
        /// 伺服器返回的應答資訊(包含應答碼)
        /// </summary>
        private string strMsg;
        /// <summary>
        /// 伺服器返回的應答資訊(包含應答碼)
        /// </summary>
        private string strReply;
        /// <summary>
        /// 伺服器返回的應答碼
        /// </summary>
        private int iReplyCode;
        /// <summary>
        /// 進行控制連線的socket
        /// </summary>
        private Socket socketControl;
        /// <summary>
        /// 傳輸模式
        /// </summary>
        private TransferType trType;
        /// <summary>
        /// 接收和傳送資料的緩衝區
        /// </summary>
        private static int BLOCK_SIZE = 512;
        Byte[] buffer = new Byte[BLOCK_SIZE];
        /// <summary>
        /// 編碼方式(為防止出現中文亂碼採用 GB2312編碼方式)
        /// </summary>
        Encoding GB2312 = Encoding.GetEncoding("gb2312");
        #endregion

        #region 內部函式
        /// <summary>
        /// 將一行應答字串記錄在strReply和strMsg
        /// 應答碼記錄在iReplyCode
        /// </summary>
        private void ReadReply()
        {
            strMsg = "";
            strReply = ReadLine();
            iReplyCode = Int32.Parse(strReply.Substring(0, 3));
        }

        /// <summary>
        /// 建立進行資料連線的socket
        /// </summary>
        /// <returns>資料連線socket</returns>
        private Socket CreateDataSocket()
        {
            SendCommand("PASV");
            if (iReplyCode != 227)
            {
                throw new IOException(strReply.Substring(4));
            }
            int index1 = strReply.IndexOf('(');
            int index2 = strReply.IndexOf(')');
            string ipData =
            strReply.Substring(index1 + 1, index2 - index1 - 1);
            int[] parts = new int[6];
            int len = ipData.Length;
            int partCount = 0;
            string buf = "";
            for (int i = 0; i < len && partCount <= 6; i++)
            {
                char ch = Char.Parse(ipData.Substring(i, 1));
                if (Char.IsDigit(ch))
                    buf += ch;
                else if (ch != ',')
                {
                    throw new IOException("Malformed PASV strReply: " +
                    strReply);
                }
                if (ch == ',' || i + 1 == len)
                {
                    try
                    {
                        parts[partCount++] = Int32.Parse(buf);
                        buf = "";
                    }
                    catch (Exception)
                    {
                        throw new IOException("Malformed PASV strReply: " +
                         strReply);
                    }
                }
            }
            string ipAddress = parts[0] + "." + parts[1] + "." +
            parts[2] + "." + parts[3];
            int port = (parts[4] << 8) + parts[5];
            Socket s = new
            Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new
            IPEndPoint(IPAddress.Parse(ipAddress), port);
            try
            {
                s.Connect(ep);
            }
            catch (Exception)
            {
                throw new IOException("Can't connect to remote server");
            }
            return s;
        }


        /// <summary>
        /// 關閉socket連線(用於登入以前)
        /// </summary>
        private void CloseSocketConnect()
        {
            if (socketControl != null)
            {
                socketControl.Close();
                socketControl = null;
            }
            bConnected = false;
        }

        /// <summary>
        /// 讀取Socket返回的所有字串
        /// </summary>
        /// <returns>包含應答碼的字串行</returns>
        private string ReadLine()
        {
            while (true)
            {
                int iBytes = socketControl.Receive(buffer, buffer.Length, 0);
                strMsg += GB2312.GetString(buffer, 0, iBytes);
                if (iBytes < buffer.Length)
                {
                    break;
                }
            }
            char[] seperator = { '\n' };
            string[] mess = strMsg.Split(seperator);
            if (strMsg.Length > 2)
            {
                strMsg = mess[mess.Length - 2];
                //seperator[0]是10,換行符是由13和0組成的,分隔後10後面雖沒有字串,
                //但也會分配為空字串給後面(也是最後一個)字串陣列,
                //所以最後一個mess是沒用的空字串
                //但為什麼不直接取mess[0],因為只有最後一行字串應答碼與資訊之間有空格
            }
            else
            {
                strMsg = mess[0];
            }
            if (!strMsg.Substring(3, 1).Equals(" "))//返回字串正確的是以應答碼(如220開頭,後面接一空格,再接問候字串)
            {
                return ReadLine();
            }
            return strMsg;
        }


        /// <summary>
        /// 傳送命令並獲取應答碼和最後一行應答字串
        /// </summary>
        /// <param name="strCommand">命令</param>
        private void SendCommand(String strCommand)
        {
            Byte[] cmdBytes =
            GB2312.GetBytes((strCommand + "\r\n").ToCharArray());
            socketControl.Send(cmdBytes, cmdBytes.Length, 0);
            ReadReply();
        }

        #endregion

    }
}
