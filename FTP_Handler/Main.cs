using FluentFTP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace FTP_Handler
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // 創建 FTP client
            FtpClient client = new FtpClient("123.123.123.123");
            // 如果您不指定登錄憑證，我們將使用"anonymous"用戶帳戶
            client.Credentials = new NetworkCredential("david", "pass123");
            //開始連接Server
            client.Connect();
            //獲取“/htdocs”文件夾中的文件和目錄列表
            foreach (FtpListItem item in client.GetListing("/htdocs"))
            {
                //如果是 file
                if (item.Type == FtpFileSystemObjectType.File)
                {
                    // get the file size
                    long size = client.GetFileSize(item.FullName);
                }
                // 獲取文件或文件夾的修改日期/時間
                DateTime time = client.GetModifiedTime(item.FullName);
                // 計算服務器端文件的哈希值(默認算法)
                FtpHash hash = client.GetChecksum(item.FullName);
            }
            //上傳 file
            client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/MyVideo.mp4");
            // 上傳的文件重命名
            client.Rename("/htdocs/MyVideo.mp4", "/htdocs/MyVideo_2.mp4");
            // 下載文件
            client.DownloadFile(@"C:\MyVideo_2.mp4", "/htdocs/MyVideo_2.mp4");
            // 刪除文件
            client.DeleteFile("/htdocs/MyVideo_2.mp4");
            // 遞歸刪除文件夾
            client.DeleteDirectory("/htdocs/extras/");
            // 判斷文件是否存在
            if (client.FileExists("/htdocs/big2.txt")) { }
            // 判斷文件夾是否存在
            if (client.DirectoryExists("/htdocs/extras/")) { }
            //上傳一個文件，重試3次才放棄
            client.RetryAttempts = 3;
            client.UploadFile(@"C:\MyVideo.mp4", "/htdocs/big.txt", FtpRemoteExists.Overwrite, false, FtpVerify.Retry);
            // 斷開連接! good bye!
            client.Disconnect();
        }
    }
}
