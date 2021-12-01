using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CourseProject.Model;
using Microsoft.Win32;

namespace CourseProject
{
    class MainViewModel : BaseViewModel
    {
        E2 e2 = new E2();
        ElGamal elgamal = new ElGamal();
        Scrambler sc = new Scrambler();
        List<String> textBoxMessages = new List<string>();
        string _textBoxText = new string("");
        FTP ftp = new FTP();
        ElGamal.EncryptedMessage encryptedKey = new ElGamal.EncryptedMessage();
        string sourseFilename;
        string destFilemane;
        #region EncryptionModes
        private bool _ecb = true;
        private bool _cbc;
        private bool _ofb;
        private bool _cfb;
        public bool ECB
        {
            get => _ecb;
            set
            {
                _ecb = value;
                sc.encryptionMode = Scrambler.EncryptionMode.ECB;
                if (value)
                {
                    CBC = false;
                    OFB = false;
                    CFB = false;
                    LogMessage("Установлен режим шифрования ECB.");
                }

                OnPropertyChanged(nameof(ECB));
            }
        }
        public bool CBC
        {
            get => _cbc;
            set
            {
                _cbc = value;
                sc.encryptionMode = Scrambler.EncryptionMode.CBC;
                if (value)
                {
                    ECB = false;
                    OFB = false;
                    CFB = false;
                    LogMessage("Установлен режим шифрования CBC.");
                }

                OnPropertyChanged(nameof(CBC));
            }
        }
        public bool OFB
        {
            get => _ofb;
            set
            {
                _ofb = value;
                sc.encryptionMode = Scrambler.EncryptionMode.OFB;
                if (value)
                {
                    ECB = false;
                    CBC = false;
                    CFB = false;
                    LogMessage("Установлен режим шифрования OFB.");
                }

                OnPropertyChanged(nameof(OFB));
            }
        }
        public bool CFB
        {
            get => _cfb;
            set
            {
                _cfb = value;
                sc.encryptionMode = Scrambler.EncryptionMode.CFB;
                if (value)
                {
                    ECB = false;
                    CBC = false;
                    OFB = false;
                    LogMessage("Установлен режим шифрования CFB.");
                }

                OnPropertyChanged(nameof(CFB));
            }
        }
        #endregion

        #region ICommands

        private ICommand _encryptFileCommand;
        public ICommand EncryptFileCommand =>
            _encryptFileCommand ?? (_encryptFileCommand = new RelayCommand(async _ => await EncryptFile()));
        private ICommand _decryptFileCommand;
        public ICommand DecryptFileCommand =>
            _decryptFileCommand ?? (_decryptFileCommand = new RelayCommand(async _ => await DecryptFile()));

        private ICommand _encryptSymmKeyCommand;
        public ICommand EncryptSymmKeyCommand =>
            _encryptSymmKeyCommand ?? (_encryptSymmKeyCommand = new RelayCommand(async _ => await EncryptKey()));

        private ICommand _decryptSymmKeyCommand;
        public ICommand DecryptSymmKeyCommand =>
            _decryptSymmKeyCommand ?? (_decryptSymmKeyCommand = new RelayCommand(async _ => await DecryptKey()));

        private ICommand _generateE2KeyCommand;
        public ICommand GenerateE2KeyCommand =>
            _generateE2KeyCommand ?? (_generateE2KeyCommand = new RelayCommand(async _ => await GenerateE2Key()));

        private ICommand _generateElGamalKeyCommand;
        public ICommand GenerateElGamalKeyCommand =>
            _generateElGamalKeyCommand ?? (_generateElGamalKeyCommand = new RelayCommand(async _ => await GenerateElGamalKey()));
        private ICommand _generateIVCommand;
        public ICommand GenerateIVCommand =>
            _generateIVCommand ?? (_generateIVCommand = new RelayCommand(async _ => await GenerateIV()));

        private ICommand _uploadPublicKeyFile;
        public ICommand UploadPublicKeyFile =>
            _uploadPublicKeyFile ?? (_uploadPublicKeyFile = new RelayCommand(async _ => await UploadPublicKey()));

        private ICommand _uploadE2KeyFile;
        public ICommand UploadE2KeyFile =>
            _uploadE2KeyFile ?? (_uploadE2KeyFile = new RelayCommand(async _ => await UploadE2Key()));

        private ICommand _uploadIVFileCommand;
        public ICommand UploadIVFileCommand =>
            _uploadIVFileCommand ?? (_uploadIVFileCommand = new RelayCommand(async _ => await UploadIV()));
        private ICommand _downloadIVFileCommand;
        public ICommand DownloadIVFileCommand =>
            _downloadIVFileCommand ?? (_downloadIVFileCommand = new RelayCommand(async _ => await DownloadIV()));

        private ICommand _downloadFileCommand;
        public ICommand DownloadFileCommand =>
            _downloadFileCommand ?? (_downloadFileCommand = new RelayCommand(async _ => await DownloadFile()));

        private ICommand _uploadFileCommand;
        public ICommand UploadFileCommand =>
            _uploadFileCommand ?? (_uploadFileCommand = new RelayCommand(async _ => await UploadFile()));

        private ICommand _downloadPublicKeyFileCommand;
        public ICommand DownloadPublicKeyFileCommand =>
            _downloadPublicKeyFileCommand ?? (_downloadPublicKeyFileCommand = new RelayCommand(async _ => await DownloadPublicKey()));

        private ICommand _downloadE2FileCommand;
        public ICommand DownloadE2KeyFileCommand =>
            _downloadE2FileCommand ?? (_downloadE2FileCommand = new RelayCommand(async _ => await DownloadE2Key()));
        #endregion

        public string TextBoxText
        {
            get =>
                _textBoxText;

            set
            {
                _textBoxText = value;
                OnPropertiesChanged(nameof(TextBoxText));
            }
        }

        async Task UploadFile()
        {
            if ((sourseFilename = ChooseFile()) == null) return;

            Task task = Task.Run(() =>
            {
                LogMessage(String.Format("Загружаем файл {0}  на сервер...", sourseFilename));
                ftp.SendFile(sourseFilename);
                LogMessage(String.Format("Файл {0} успешно загружен на сервер!", sourseFilename));
            });
            await task;
        }

        async Task DownloadFile()
        {
            if ((destFilemane = SaveFile()) == null) return;
            Task task = Task.Run(() =>
            {
                LogMessage(String.Format("Получаем файл {0} с сервера...", destFilemane));
                ftp.GetFile("file", destFilemane);
                LogMessage(String.Format("Файл {0} успешно  загружен", destFilemane));
            });
            await task;
        }

        async Task EncryptFile()
        {
            if ((sourseFilename = ChooseFile()) == null || (destFilemane = SaveFile()) == null) return;
            sc.e2 = e2;
            if (CheckEncrypt())
            {
                LogMessage("Ошибка шифрования! Один или несколько парамтеров, необходимых для шифрования, отсутсвтвуют.");
                return;
            }
            Task task = Task.Run(() =>
            {
                LogMessage("Шифруем файл " + sourseFilename);
                LogMessage("\n");
                sc.EncryptFile(sourseFilename, destFilemane, LogProgress);
                LogMessage("Зашифровали в файл" + destFilemane);
            });
            await task;
        }

        async Task DecryptFile()
        {
            if ((sourseFilename = ChooseFile()) == null || (destFilemane = SaveFile()) == null) return;
            sc.e2 = e2;
            if (CheckEncrypt())
            {
                LogMessage("Ошибка дешифрования! Один или несколько парамтеров, необходимых для дешифрования, отсутсвтвуют.");
                return;
            }
            Task task = Task.Run(() =>
            {

                LogMessage("Дешифруем файл " + sourseFilename);
                LogMessage("\n");
                sc.DecryptFile(sourseFilename, destFilemane, LogProgress);
                LogMessage("Дешифровали в файл " + destFilemane);
            });
            await task;
        }

        async Task UploadPublicKey()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Загружаем публичный ключ на сервер...");
                ftp.SendFile("publicKey.txt", String.Format(elgamal.PublicKey.ToString()));
                LogMessage("Публичный ключ успешно загружен на сервер!");
            });
            await task;
        }

        async Task UploadE2Key()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Загружаем зашифрованный ключ E2 на сервер...");
                ftp.SendFile("Key.txt", String.Format(encryptedKey.ToString()));
                LogMessage("Зашифрованный ключ успешно загружен на сервер!");
            });
            await task;
        }


        async Task UploadIV()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Загружаем вектор инициализации на сервер...");
                ftp.SendFile("IV.txt", sc.IV);
                LogMessage("Вектор инициализации успешно загружен на сервер!");
            });
            await task;
        }

        async Task DownloadIV()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Получаем вектор инициализации с сервера...");
                sc.IV = ftp.GetData("IV.txt");
                LogMessage(String.Format("Вектор инициализации успешно получен с сервера!\nIV:{0}", new BigInteger(sc.IV).ToString("X")));
            });
            await task;
        }

        async Task DownloadE2Key()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Получаем зашифрованный ключ E2 с сервера...");
                string res = Encoding.Default.GetString(ftp.GetData("Key.txt"));
                encryptedKey.Parse(res.Split("\n"));
                LogMessage(String.Format("Зашифрованный ключ E2 успешно получен с сервера!"));
            });
            await task;
        }


        async Task DownloadPublicKey()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Получаем публичный ключ с сервера...");
                string res = Encoding.Default.GetString(ftp.GetData("publicKey.txt"));
                elgamal.PublicKey.Parse(res.Split("\n"));
                LogMessage(String.Format("Публичный ключ успешно получен с сервера!\n{0}\n", elgamal.PublicKey.ToString()));
            });
            await task;
        }

        async Task EncryptKey()
        {
            if (e2.Key == null)
            {
                LogMessage("Ошибка! Ключ E2 не сгенерирован!");
                return;
            }
            if (elgamal.PublicKey.p == 0 || elgamal.PublicKey.g == 0 || elgamal.PublicKey.y == 0)
            {
                LogMessage("Ошибка! Отсутствует ключ ElGamal!");
            }
            Task task = Task.Run(() =>
            {

                LogMessage("Шифруем ключ E2...");
                encryptedKey = elgamal.Encrypt(e2.Key);
                LogMessage(String.Format("Ключ E2 зашифрован успешно!\nЗашифрованный ключ:{0}", encryptedKey.ToString()));

            });
            await task;
        }


        async Task DecryptKey()
        {
            if (encryptedKey.a == 0 && encryptedKey.b == 0)
            {
                LogMessage("Ошибка! Отсутствует зашифрованный ключ E2 !");
                return;
            }
            Task task = Task.Run(() =>
            {
                LogMessage("Дешифруем ключ E2...");
                byte[] decryptedKey = elgamal.Decrypt(encryptedKey);
                e2.Key = decryptedKey;
                LogMessage(String.Format("Ключ E2 дешифрован успешно!"));
            });
            await task;
        }

        async Task GenerateIV()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Генерируем вектор инициализации...");
                sc.GenerateIV(16);
                LogMessage(String.Format("Вектор инициализации сгенерирован успешно!\nIV:{0}", new BigInteger(sc.IV).ToString("X")));
            });
            await task;
        }
        async Task GenerateElGamalKey()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Генерируем публичный ключ ElGamal...");
                elgamal.GenerateKeys(16);
                LogMessage(String.Format("Публичный ключ ElGamal сгенерирован успешно!\nКлюч:{0}\n", elgamal.PublicKey.ToString()));
            });
            await task;
        }
        async Task GenerateE2Key()
        {
            Task task = Task.Run(() =>
            {
                LogMessage("Генерируем ключ E2...");
                e2.GenerateKey();
                LogMessage(String.Format("Ключ E2 сгенерирован успешно!\nКлюч:{0}", new BigInteger(e2.Key)));
            });
            await task;
        }

        private string ChooseFile()
        {
            string filename = null;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                filename = openFileDialog.FileName;
            }
            return filename;
        }


        private string SaveFile()
        {
            string filename = null;
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                filename = saveFileDialog.FileName;

            }

            return filename;

        }
        void LogProgress(double progress)
        {
            textBoxMessages.RemoveAt(textBoxMessages.Count - 1);
            textBoxMessages.Add(String.Format("{0}%", Convert.ToInt32(progress)));
            TextBoxText = String.Join("\n", textBoxMessages);
        }
        void LogMessage(String message)
        {
            textBoxMessages.Add(message);
            TextBoxText = String.Join("\n", textBoxMessages);
        }
        bool CheckEncrypt()
        {
            return elgamal.PublicKey.p == 0 || e2.Key == null || (sc.encryptionMode != Scrambler.EncryptionMode.ECB && sc.IV == null);
        }
    }
}
