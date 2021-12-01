using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CourseProject.Model
{
    public class FTP
    {
        private readonly string _address;
        private const int SizeBuffer = 1024;

        public FTP(string server = "ftp://127.0.0.1", int port = 21)
        {
            _address = server + ":" + port + "/";
        }

        public FtpStatusCode SendFile(string pathFile)
        {
            string nameFile = "file";
            Console.WriteLine(nameFile);
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_address + nameFile);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            using (FileStream fileStream = new FileStream(pathFile, FileMode.Open))
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    byte[] buffer = new byte[SizeBuffer];
                    int size;

                    while ((size = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                        requestStream.Write(buffer, 0, size);
                }

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                return response.StatusCode;
            }
        }

        public FtpStatusCode SendFile(string filename, string data)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_address + filename);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            using (MemoryStream stringStream = new MemoryStream(Encoding.Default.GetBytes(data)))
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    byte[] buffer = new byte[SizeBuffer];
                    int size;

                    while ((size = stringStream.Read(buffer, 0, buffer.Length)) > 0)
                        requestStream.Write(buffer, 0, size);
                }

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                return response.StatusCode;
            }
        }

        public FtpStatusCode SendFile(string filename, byte[] data)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_address + filename);
            request.Method = WebRequestMethods.Ftp.UploadFile;

            using (MemoryStream stringStream = new MemoryStream(data))
            {
                using (Stream requestStream = request.GetRequestStream())
                {
                    byte[] buffer = new byte[SizeBuffer];
                    int size;

                    while ((size = stringStream.Read(buffer, 0, buffer.Length)) > 0)
                        requestStream.Write(buffer, 0, size);
                }

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();

                return response.StatusCode;
            }
        }

        public FtpStatusCode GetFile(string nameFile, string localPath)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_address + nameFile);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            using (Stream responseStream = response.GetResponseStream())
            {
                using (FileStream fs = new FileStream(localPath, FileMode.Create))
                {
                    byte[] buffer = new byte[SizeBuffer];
                    int size;
                    while ((size = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                        fs.Write(buffer, 0, size);
                }
                response = (FtpWebResponse)request.GetResponse();

                return response.StatusCode;
            }
        }

        public byte[] GetData(string nameFile)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_address + nameFile);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            using (Stream responseStream = response.GetResponseStream())
            {
                byte[] buffer = new byte[SizeBuffer];
                int size;
                size = responseStream.Read(buffer, 0, buffer.Length);            
                Array.Resize(ref buffer, size);
                

                return buffer;
              
            }
        }

        public List<string> GetListFile()
        {

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(_address);
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string fileName;
            List<string> listFile = new List<string>();
            while ((fileName = reader.ReadLine()) != null)
                listFile.Add(fileName);

            reader.Close();
            responseStream.Close();
            response.Close();
            return listFile;
        }
    }
}

