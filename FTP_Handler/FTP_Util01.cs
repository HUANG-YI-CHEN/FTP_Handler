using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace FTP_Handler
{
    /// <summary>
    /// ftp方式檔案下載上傳
    /// </summary>
    public static class FileUpDownload
    {
        #region 變數屬性
        /// <summary>
        /// Ftp伺服器ip
        /// </summary>
        public static string FtpServerIP = string.Empty;
        /// <summary>
        /// Ftp 指定使用者名稱
        /// </summary>
        public static string FtpUserID = string.Empty;
        /// <summary>
        /// Ftp 指定使用者密碼
        /// </summary>
        public static string FtpPassword = string.Empty;

        #endregion

        #region 從FTP伺服器下載檔案，指定本地路徑和本地檔名
        /// <summary>
        /// 從FTP伺服器下載檔案，指定本地路徑和本地檔名
        /// </summary>
        /// <param name="remoteFileName">遠端檔名</param>
        /// <param name="localFileName">儲存本地的檔名（包含路徑）</param>
        /// <param name="ifCredential">是否啟用身份驗證（false：表示允許使用者匿名下載）</param>
        /// <param name="updateProgress">報告進度的處理(第一個引數：總大小，第二個引數：當前進度)</param>
        /// <returns>是否下載成功</returns>
        public static bool FtpDownload(string remoteFileName, string localFileName, bool ifCredential, Action<int, int> updateProgress = null)
        {
            FtpWebRequest reqFTP, ftpsize;
            Stream ftpStream = null;
            FtpWebResponse response = null;
            FileStream outputStream = null;
            try
            {

                outputStream = new FileStream(localFileName, FileMode.Create);
                if (FtpServerIP == null || FtpServerIP.Trim().Length == 0)
                {
                    throw new Exception("ftp下載目標伺服器地址未設定！");
                }
                Uri uri = new Uri("ftp://" + FtpServerIP + "/" + remoteFileName);
                ftpsize = (FtpWebRequest)FtpWebRequest.Create(uri);
                ftpsize.UseBinary = true;

                reqFTP = (FtpWebRequest)FtpWebRequest.Create(uri);
                reqFTP.UseBinary = true;
                reqFTP.KeepAlive = false;
                if (ifCredential)//使用使用者身份認證
                {
                    ftpsize.Credentials = new NetworkCredential(FtpUserID, FtpPassword);
                    reqFTP.Credentials = new NetworkCredential(FtpUserID, FtpPassword);
                }
                ftpsize.Method = WebRequestMethods.Ftp.GetFileSize;
                FtpWebResponse re = (FtpWebResponse)ftpsize.GetResponse();
                long totalBytes = re.ContentLength;
                re.Close();

                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                response = (FtpWebResponse)reqFTP.GetResponse();
                ftpStream = response.GetResponseStream();

                //更新進度  
                if (updateProgress != null)
                {
                    updateProgress((int)totalBytes, 0);//更新進度條   
                }
                long totalDownloadedByte = 0;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];
                readCount = ftpStream.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    totalDownloadedByte = readCount + totalDownloadedByte;
                    outputStream.Write(buffer, 0, readCount);
                    //更新進度  
                    if (updateProgress != null)
                    {
                        updateProgress((int)totalBytes, (int)totalDownloadedByte);//更新進度條   
                    }
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }
                ftpStream.Close();
                outputStream.Close();
                response.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
            finally
            {
                if (ftpStream != null)
                {
                    ftpStream.Close();
                }
                if (outputStream != null)
                {
                    outputStream.Close();
                }
                if (response != null)
                {
                    response.Close();
                }
            }
        }
        /// <summary>
        /// 從FTP伺服器下載檔案，指定本地路徑和本地檔名（支援斷點下載）
        /// </summary>
        /// <param name="remoteFileName">遠端檔名</param>
        /// <param name="localFileName">儲存本地的檔名（包含路徑）</param>
        /// <param name="ifCredential">是否啟用身份驗證（false：表示允許使用者匿名下載）</param>
        /// <param name="size">已下載檔案流大小</param>
        /// <param name="updateProgress">報告進度的處理(第一個引數：總大小，第二個引數：當前進度)</param>
        /// <returns>是否下載成功</returns>
        public static bool FtpBrokenDownload(string remoteFileName, string localFileName, bool ifCredential, long size, Action<int, int> updateProgress = null)
        {
            FtpWebRequest reqFTP, ftpsize;
            Stream ftpStream = null;
            FtpWebResponse response = null;
            FileStream outputStream = null;
            try
            {

                outputStream = new FileStream(localFileName, FileMode.Append);
                if (FtpServerIP == null || FtpServerIP.Trim().Length == 0)
                {
                    throw new Exception("ftp下載目標伺服器地址未設定！");
                }
                Uri uri = new Uri("ftp://" + FtpServerIP + "/" + remoteFileName);
                ftpsize = (FtpWebRequest)FtpWebRequest.Create(uri);
                ftpsize.UseBinary = true;
                ftpsize.ContentOffset = size;

                reqFTP = (FtpWebRequest)FtpWebRequest.Create(uri);
                reqFTP.UseBinary = true;
                reqFTP.KeepAlive = false;
                reqFTP.ContentOffset = size;
                if (ifCredential)//使用使用者身份認證
                {
                    ftpsize.Credentials = new NetworkCredential(FtpUserID, FtpPassword);
                    reqFTP.Credentials = new NetworkCredential(FtpUserID, FtpPassword);
                }
                ftpsize.Method = WebRequestMethods.Ftp.GetFileSize;
                FtpWebResponse re = (FtpWebResponse)ftpsize.GetResponse();
                long totalBytes = re.ContentLength;
                re.Close();

                reqFTP.Method = WebRequestMethods.Ftp.DownloadFile;
                response = (FtpWebResponse)reqFTP.GetResponse();
                ftpStream = response.GetResponseStream();

                //更新進度  
                if (updateProgress != null)
                {
                    updateProgress((int)totalBytes, 0);//更新進度條   
                }
                long totalDownloadedByte = 0;
                int bufferSize = 2048;
                int readCount;
                byte[] buffer = new byte[bufferSize];
                readCount = ftpStream.Read(buffer, 0, bufferSize);
                while (readCount > 0)
                {
                    totalDownloadedByte = readCount + totalDownloadedByte;
                    outputStream.Write(buffer, 0, readCount);
                    //更新進度  
                    if (updateProgress != null)
                    {
                        updateProgress((int)totalBytes, (int)totalDownloadedByte);//更新進度條   
                    }
                    readCount = ftpStream.Read(buffer, 0, bufferSize);
                }
                ftpStream.Close();
                outputStream.Close();
                response.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
            finally
            {
                if (ftpStream != null)
                {
                    ftpStream.Close();
                }
                if (outputStream != null)
                {
                    outputStream.Close();
                }
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// 從FTP伺服器下載檔案，指定本地路徑和本地檔名
        /// </summary>
        /// <param name="remoteFileName">遠端檔名</param>
        /// <param name="localFileName">儲存本地的檔名（包含路徑）</param>
        /// <param name="ifCredential">是否啟用身份驗證（false：表示允許使用者匿名下載）</param>
        /// <param name="updateProgress">報告進度的處理(第一個引數：總大小，第二個引數：當前進度)</param>
        /// <param name="brokenOpen">是否斷點下載：true 會在localFileName 找是否存在已經下載的檔案，並計算檔案流大小</param>
        /// <returns>是否下載成功</returns>
        public static bool FtpDownload(string remoteFileName, string localFileName, bool ifCredential, bool brokenOpen, Action<int, int> updateProgress = null)
        {
            if (brokenOpen)
            {
                try
                {
                    long size = 0;
                    if (File.Exists(localFileName))
                    {
                        using (FileStream outputStream = new FileStream(localFileName, FileMode.Open))
                        {
                            size = outputStream.Length;
                        }
                    }
                    return FtpBrokenDownload(remoteFileName, localFileName, ifCredential, size, updateProgress);
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                return FtpDownload(remoteFileName, localFileName, ifCredential, updateProgress);
            }
        }
        #endregion

        #region 上傳檔案到FTP伺服器
        /// <summary>
        /// 上傳檔案到FTP伺服器
        /// </summary>
        /// <param name="localFullPath">本地帶有完整路徑的檔名</param>
        /// <param name="updateProgress">報告進度的處理(第一個引數：總大小，第二個引數：當前進度)</param>
        /// <returns>是否下載成功</returns>
        public static bool FtpUploadFile(string localFullPathName, Action<int, int> updateProgress = null)
        {
            FtpWebRequest reqFTP;
            Stream stream = null;
            FtpWebResponse response = null;
            FileStream fs = null;
            try
            {
                FileInfo finfo = new FileInfo(localFullPathName);
                if (FtpServerIP == null || FtpServerIP.Trim().Length == 0)
                {
                    throw new Exception("ftp上傳目標伺服器地址未設定！");
                }
                Uri uri = new Uri("ftp://" + FtpServerIP + "/" + finfo.Name);
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(uri);
                reqFTP.KeepAlive = false;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(FtpUserID, FtpPassword);//使用者，密碼
                reqFTP.Method = WebRequestMethods.Ftp.UploadFile;//向伺服器發出下載請求命令
                reqFTP.ContentLength = finfo.Length;//為request指定上傳檔案的大小
                response = reqFTP.GetResponse() as FtpWebResponse;
                reqFTP.ContentLength = finfo.Length;
                int buffLength = 1024;
                byte[] buff = new byte[buffLength];
                int contentLen;
                fs = finfo.OpenRead();
                stream = reqFTP.GetRequestStream();
                contentLen = fs.Read(buff, 0, buffLength);
                int allbye = (int)finfo.Length;
                //更新進度  
                if (updateProgress != null)
                {
                    updateProgress((int)allbye, 0);//更新進度條   
                }
                int startbye = 0;
                while (contentLen != 0)
                {
                    startbye = contentLen + startbye;
                    stream.Write(buff, 0, contentLen);
                    //更新進度  
                    if (updateProgress != null)
                    {
                        updateProgress((int)allbye, (int)startbye);//更新進度條   
                    }
                    contentLen = fs.Read(buff, 0, buffLength);
                }
                stream.Close();
                fs.Close();
                response.Close();
                return true;

            }
            catch (Exception)
            {
                return false;
                throw;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// 上傳檔案到FTP伺服器(斷點續傳)
        /// </summary>
        /// <param name="localFullPath">本地檔案全路徑名稱：C:\Users\JianKunKing\Desktop\IronPython指令碼測試工具</param>
        /// <param name="remoteFilepath">遠端檔案所在資料夾路徑</param>
        /// <param name="updateProgress">報告進度的處理(第一個引數：總大小，第二個引數：當前進度)</param>
        /// <returns></returns>       
        public static bool FtpUploadBroken(string localFullPath, string remoteFilepath, Action<int, int> updateProgress = null)
        {
            if (remoteFilepath == null)
            {
                remoteFilepath = "";
            }
            string newFileName = string.Empty;
            bool success = true;
            FileInfo fileInf = new FileInfo(localFullPath);
            long allbye = (long)fileInf.Length;
            if (fileInf.Name.IndexOf("#") == -1)
            {
                newFileName = RemoveSpaces(fileInf.Name);
            }
            else
            {
                newFileName = fileInf.Name.Replace("#", "＃");
                newFileName = RemoveSpaces(newFileName);
            }
            long startfilesize = GetFileSize(newFileName, remoteFilepath);
            if (startfilesize >= allbye)
            {
                return false;
            }
            long startbye = startfilesize;
            //更新進度  
            if (updateProgress != null)
            {
                updateProgress((int)allbye, (int)startfilesize);//更新進度條   
            }

            string uri;
            if (remoteFilepath.Length == 0)
            {
                uri = "ftp://" + FtpServerIP + "/" + newFileName;
            }
            else
            {
                uri = "ftp://" + FtpServerIP + "/" + remoteFilepath + "/" + newFileName;
            }
            FtpWebRequest reqFTP;
            // 根據uri建立FtpWebRequest物件 
            reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(uri));
            // ftp使用者名稱和密碼 
            reqFTP.Credentials = new NetworkCredential(FtpUserID, FtpPassword);
            // 預設為true，連線不會被關閉 
            // 在一個命令之後被執行 
            reqFTP.KeepAlive = false;
            // 指定執行什麼命令 
            reqFTP.Method = WebRequestMethods.Ftp.AppendFile;
            // 指定資料傳輸型別 
            reqFTP.UseBinary = true;
            // 上傳檔案時通知伺服器檔案的大小 
            reqFTP.ContentLength = fileInf.Length;
            int buffLength = 2048;// 緩衝大小設定為2kb 
            byte[] buff = new byte[buffLength];
            // 開啟一個檔案流 (System.IO.FileStream) 去讀上傳的檔案 
            FileStream fs = fileInf.OpenRead();
            Stream strm = null;
            try
            {
                // 把上傳的檔案寫入流 
                strm = reqFTP.GetRequestStream();
                // 每次讀檔案流的2kb   
                fs.Seek(startfilesize, 0);
                int contentLen = fs.Read(buff, 0, buffLength);
                // 流內容沒有結束 
                while (contentLen != 0)
                {
                    // 把內容從file stream 寫入 upload stream 
                    strm.Write(buff, 0, contentLen);
                    contentLen = fs.Read(buff, 0, buffLength);
                    startbye += contentLen;
                    //更新進度  
                    if (updateProgress != null)
                    {
                        updateProgress((int)allbye, (int)startbye);//更新進度條   
                    }
                }
                // 關閉兩個流 
                strm.Close();
                fs.Close();
            }
            catch
            {
                success = false;
                throw;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                }
                if (strm != null)
                {
                    strm.Close();
                }
            }
            return success;
        }

        /// <summary>
        /// 去除空格
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string RemoveSpaces(string str)
        {
            string a = "";
            CharEnumerator CEnumerator = str.GetEnumerator();
            while (CEnumerator.MoveNext())
            {
                byte[] array = new byte[1];
                array = System.Text.Encoding.ASCII.GetBytes(CEnumerator.Current.ToString());
                int asciicode = (short)(array[0]);
                if (asciicode != 32)
                {
                    a += CEnumerator.Current.ToString();
                }
            }
            string sdate = System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString() + System.DateTime.Now.Day.ToString() + System.DateTime.Now.Hour.ToString()
                + System.DateTime.Now.Minute.ToString() + System.DateTime.Now.Second.ToString() + System.DateTime.Now.Millisecond.ToString();
            return a.Split('.')[a.Split('.').Length - 2] + "." + a.Split('.')[a.Split('.').Length - 1];
        }
        /// <summary>
        /// 獲取已上傳檔案大小
        /// </summary>
        /// <param name="filename">檔名稱</param>
        /// <param name="path">伺服器檔案路徑</param>
        /// <returns></returns>
        public static long GetFileSize(string filename, string remoteFilepath)
        {
            long filesize = 0;
            try
            {
                FtpWebRequest reqFTP;
                FileInfo fi = new FileInfo(filename);
                string uri;
                if (remoteFilepath.Length == 0)
                {
                    uri = "ftp://" + FtpServerIP + "/" + fi.Name;
                }
                else
                {
                    uri = "ftp://" + FtpServerIP + "/" + remoteFilepath + "/" + fi.Name;
                }
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(uri);
                reqFTP.KeepAlive = false;
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(FtpUserID, FtpPassword);//使用者，密碼
                reqFTP.Method = WebRequestMethods.Ftp.GetFileSize;
                FtpWebResponse response = (FtpWebResponse)reqFTP.GetResponse();
                filesize = response.ContentLength;
                return filesize;
            }
            catch
            {
                return 0;
            }
        }

        //public void Connect(String path, string ftpUserID, string ftpPassword)//連線ftp
        //{
        //    // 根據uri建立FtpWebRequest物件
        //    reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri(path));
        //    // 指定資料傳輸型別
        //    reqFTP.UseBinary = true;
        //    // ftp使用者名稱和密碼
        //    reqFTP.Credentials = new NetworkCredential(ftpUserID, ftpPassword);
        //}

        #endregion

    }
}