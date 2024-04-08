using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Manifest_Creator
{
    public partial class Form1 : Form
    {
        string Path = "";
        long pack_size = 0;
        string big_patch, small_patch;
        bool big_changed=false, small_changed=false;
        const string SERVER_ADRESS = "ftp://eternalrifts.ru/Test_catalog2/";
        const string SERVER_ADRESS2 = "ftp://eternalrifts.ru/Test_catalog2";
        const string SERVER_LOGIN = "u1851278_upload";
        const string SERVER_PASSW = "jS9uR8wF8u";
        public Form1()
        {
            InitializeComponent();
            progressBar1.Value = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
        }
        private void Folder_Calc(string folder)
        {
            string[] list_folders = Directory.GetDirectories(folder);
            foreach (string f in list_folders)
            {
                Folder_Calc(f);
            }
            DirectoryInfo di = new DirectoryInfo(folder);
            FileInfo[] list_files = di.GetFiles();
            foreach (FileInfo f in list_files)
            {
                pack_size += f.Length / 1024;
            }
        }
        private static FtpWebRequest CreatePostRequest(string file_name)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(SERVER_ADRESS + file_name); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }

        private FtpWebRequest CreateFolderRequest(string folder_name)
        {
            string f = folder_name.Remove(0, Path.Length + 1);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(SERVER_ADRESS + f); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }
        private FtpWebRequest RecreateFolderRequest(string folder_name)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(folder_name); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }

        private static FtpWebRequest CreateDownloadRequest(string file_name)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(SERVER_ADRESS + file_name); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }
        private static FtpWebRequest CreateDeleteFileRequest(string file_name)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(SERVER_ADRESS2 + file_name); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.DeleteFile;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }
        private static FtpWebRequest CreateDeleteFolderRequest(string folder_name)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(SERVER_ADRESS2 + folder_name); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.RemoveDirectory;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }
        private static FtpWebRequest CreateListDirectoryRequest(string folder_name)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(SERVER_ADRESS2 + folder_name); // запрос файла манифеста
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(SERVER_LOGIN, SERVER_PASSW);
            request.EnableSsl = true;

            return request;
        }
        
        private void increace_progress(long i)
        {
            progressBar1.Value += Convert.ToInt32(i / 1024);
        }
        private void change_richbox(string new_string)
        {
            richTextBox1.Text = new_string;
        }
        private void File_Write(string Fpath, StreamWriter writer)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

            FileInfo fi = new FileInfo(Fpath);
            Action<long> a = increace_progress;
            Invoke(a, fi.Length);
            string filename = Fpath.Remove(0, Path.Length + 1);
            Action<string> change = change_richbox;
            Invoke(change, filename);
            writer.Write(filename);
            var req = CreatePostRequest(filename);
            using (var md5 = MD5.Create())
            {
                var file = File.Open(Fpath, FileMode.Open);   //считаем мд5
                
                var hash = md5.ComputeHash(file);
                var MD5 = BitConverter.ToString(hash).Replace("-", "");
                file.Close();
                using (var reqstr = req.GetRequestStream())
                {
                    FileStream fs = new FileStream(Fpath,FileMode.Open);
                    byte[] buffer = new byte[4096];
                    int size = 0;

                    while ((size = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        reqstr.Write(buffer, 0, size);
                    }

                    fs.Close();
                }
                writer.WriteLine("|" + MD5 + "|" + Convert.ToString(fi.Length/1024));
            }
        }
        private void Folder_Open(string folder, StreamWriter writer)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

            string[] list_folders = Directory.GetDirectories(folder);
            foreach (string f in list_folders)
            {
                try
                {
                    var req = CreateFolderRequest(f);
                    var resp = req.GetResponse();
                    resp.Close();
                }
                catch (WebException e)
                {
                }
                Folder_Open(f, writer);
            }
            

            string[] list_files = Directory.GetFiles(folder);
            foreach (string f in list_files)
            {
                File_Write(f, writer);
            }
            
        }
        void Server_Folder_Open(string foldername)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

            var req = CreateListDirectoryRequest(foldername);
            using (var resp = req.GetResponse())
            {
                using (var stream = resp.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        while (reader.Peek() >= 0)
                        {
                            var line = reader.ReadLine();
                            string[] tokens = line.Split(new[] { ' ' }, 9, StringSplitOptions.RemoveEmptyEntries);
                            string name = tokens[8];
                            if (line[0] == 'd')
                            {

                                if (name != "." && name != "..")
                                {
                                    Server_Folder_Open(foldername + "/" + name);
                                    
                                }
                                
                            }
                            else
                            {
                                CreateDeleteFileRequest(foldername + "/" + name).GetResponse();
                            }
                        }
                        if (foldername != "")
                            CreateDeleteFolderRequest(foldername).GetResponse();
                    }
                }
            }
        }
        private void Method_for_button()
        {


            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

            

            if (big_changed)
            {
                Action<string> change = change_richbox;
                Invoke(change, "Cleanup");
                Server_Folder_Open("");
                //var req = CreateDeleteFolderRequest(SERVER_ADRESS);

                //var resp = req.GetResponse();

                //req = RecreateFolderRequest(SERVER_ADRESS);

                //resp = req.GetResponse();


                StreamWriter writer = new StreamWriter("Info.txt");

                Folder_Open(Path, writer);
                writer.Close();

                var req = CreatePostRequest("Info.txt");

                using (var reqstr = req.GetRequestStream())
                {
                    FileStream fs = new FileStream("Info.txt", FileMode.Open);
                    byte[] buffer = new byte[4096];
                    int size = 0;

                    while ((size = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        reqstr.Write(buffer, 0, size);
                    }

                    fs.Close();
                }
            }
            else
            {
                if (small_changed)
                {
                    FtpWebRequest request = CreateDownloadRequest("Info.txt");

                    try
                    {

                        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())  // получение ответа и запись данных на диск
                        {
                            using (Stream responseStream = response.GetResponseStream())
                            {
                                using (StreamReader reader = new StreamReader(responseStream))
                                {
                                    using (StreamWriter writer = new StreamWriter("Server_Info.txt"))
                                    {
                                        while (reader.Peek() >= 0)
                                        {
                                            string s = reader.ReadLine() + "\n";
                                            writer.Write(s);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (WebException e)
                    {

                    }

                    StreamWriter w = new StreamWriter("Info.txt");

                    Folder_Open(Path, w);
                    w.Close();
                    using (StreamWriter newInfo = new StreamWriter("NewInfo.txt"))
                    {
                        using (StreamReader server = new StreamReader("Server_Info.txt"))
                        {


                            while (server.Peek() >= 0)
                            {
                                bool flag = true;
                                string line = server.ReadLine();
                                string[] server_Fileinfo = line.Split('|');
                                using (StreamReader reader = new StreamReader("Info.txt"))
                                {
                                    while (reader.Peek() >= 0)
                                    {
                                        string s = reader.ReadLine();
                                        string[] fileInfo = s.Split('|');
                                        if (server_Fileinfo[0] == fileInfo[0])
                                        {
                                            flag = false;
                                            break;
                                        }

                                    }
                                    if (flag)
                                    {
                                        newInfo.WriteLine(line);
                                    }
                                }
                            }
                        }
                        using (StreamReader reader = new StreamReader("Info.txt"))
                        {
                            while (reader.Peek() >= 0)
                            {
                                newInfo.WriteLine(reader.ReadLine());
                            }
                        }
                    }


                    var req = CreatePostRequest("Info.txt");

                    using (var reqstr = req.GetRequestStream())
                    {
                        FileStream fs = new FileStream("NewInfo.txt", FileMode.Open);
                        byte[] buffer = new byte[4096];
                        int size = 0;

                        while ((size = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            reqstr.Write(buffer, 0, size);
                        }

                        fs.Close();
                    }

                }
            }
            var reqv = CreatePostRequest("Version.txt");

            using (var resp = reqv.GetRequestStream())
            {


                resp.Write(Encoding.UTF8.GetBytes(Big_Patch_Text.Text), 0, Big_Patch_Text.Text.Length);
                resp.Write(Encoding.UTF8.GetBytes("|"), 0, 1);
                resp.Write(Encoding.UTF8.GetBytes(Small_Patch_Text.Text), 0, Small_Patch_Text.Text.Length);
                big_patch = Big_Patch_Text.Text;
                small_patch = Small_Patch_Text.Text;
                small_changed = false;
                big_changed = false;

            }


        }
        private async void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            button1.Enabled = false;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Path = dialog.SelectedPath;
                progressBar1.Value = 0;
                pack_size = 0;
                Folder_Calc(Path);
                progressBar1.Maximum = Convert.ToInt32(pack_size);
                await Task.Run(() => Method_for_button());
                button1.Text = "Done";
                richTextBox1.Text = "";
            }
            button1.Enabled = true;
        }

        private void Big_Patch_Text_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToInt32(Big_Patch_Text.Text) < Convert.ToInt32(big_patch))
                {
                    richTextBox1.Text = "Версия патча не может быть меньше текущей";
                    Big_Patch_Text.Text = big_patch;
                    
                }
            }
            catch(FormatException)
            {
                Big_Patch_Text.Text = big_patch;
                richTextBox1.Text = "Для номера патча используй только цифры";

            }
            big_changed = Big_Patch_Text.Text != big_patch;
        }

        private void Small_Patch_Text_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToInt32(Small_Patch_Text.Text) < Convert.ToInt32(small_patch) && !big_changed)
                {
                    richTextBox1.Text = "Версия патча не может быть меньше текущей";
                    Small_Patch_Text.Text = small_patch;
                }
            }
            catch(FormatException)
            {
                Small_Patch_Text.Text = small_patch;
                richTextBox1.Text = "Для номера патча используй только цифры";
            }
            small_changed = Small_Patch_Text.Text != small_patch;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback +=
            delegate (object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                        System.Security.Cryptography.X509Certificates.X509Chain chain,
                        System.Net.Security.SslPolicyErrors sslPolicyErrors)
            {
                return true; // **** Always accept
            };

            FtpWebRequest request = CreateDownloadRequest("Version.txt");
            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())  // получение версии
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {


                            string s = reader.ReadLine();
                            if (string.IsNullOrEmpty(s))
                            {
                                big_patch = "0";
                                small_patch = "0";
                                Big_Patch_Text.Text = "0";
                                Small_Patch_Text.Text = "0";
                            }
                            else
                            {
                                big_patch = s.Split('|')[0];
                                small_patch = s.Split('|')[1];
                                Big_Patch_Text.Text = big_patch;
                                Small_Patch_Text.Text = small_patch;
                            }


                        }
                    }
                }
            }
            catch(WebException ex)
            {
                big_patch = "0";
                small_patch = "0";
                Big_Patch_Text.Text = "0";
                Small_Patch_Text.Text = "0";
            }

        }
    }
}