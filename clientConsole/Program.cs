using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace clientConsole
{
    public class TProgramData
    {
        public string iniFileName;
        public string server;
        public string pathBackup;
        public string company;
        public string name;
        public string username;
        public string password;
    }

    public class TFileInfo
    {
        public string name;
        public long size;
        public string dataCreate;
    }

    public class TSendingData
    {
        public string company;
        public string name;
        public string path;
        public long freespace;
        public List<TFileInfo> fileList = new List<TFileInfo>();
    }

    public class TParams
    {
        const string iniDefaultName = "param.ini";
        const string iniSection = "Основные настройки";
        const string iniSmtpServer = "server";
        const string iniPathbackup = "pathbackup";
        const string iniCompany = "company";
        const string iniName = "name";
        const string iniUserName = "username";
        const string iniPassword = "password";

        public void Load(TProgramData programData)
        {
            if (programData.iniFileName.Length > 0)
            {
                if (new FileInfo(programData.iniFileName).Exists)
                {

                    // Если в параметрах указано что брать данные из файла настроек, то
                    // попробует получить из него данные
                    TIniFiles ini = new TIniFiles(programData.iniFileName);
                    // читаем данные из файла
                    programData.server = ini.Read(iniSection, iniSmtpServer);
                    programData.pathBackup = ini.Read(iniSection, iniPathbackup);
                    programData.company = ini.Read(iniSection, iniCompany);
                    programData.name = ini.Read(iniSection, iniName);
                    programData.username = ini.Read(iniSection, iniUserName);
                    programData.password = ini.Read(iniSection, iniPassword);

                }
                else
                {
                    Console.WriteLine("Не найден файл с настройками {0}", programData.iniFileName);
                }
            }
        }

        public void SaveEmptyFile()
        {
            string fullName = AppDomain.CurrentDomain.BaseDirectory + iniDefaultName;
            if (!(new FileInfo(fullName).Exists))
            {
                Console.WriteLine("Создан пустой файл параметров: {0}", fullName);

                // Создадим INI файл с настройками
                TIniFiles ini = new TIniFiles(fullName);

                ini.Write(iniSection, iniSmtpServer, "");
                ini.Write(iniSection, iniPathbackup, "");
                ini.Write(iniSection, iniCompany, "");
                ini.Write(iniSection, iniName, "");
                ini.Write(iniSection, iniUserName, "");
                ini.Write(iniSection, iniPassword, "");
            }
        }
    }

    public class TApp
    {
        private TProgramData programData = new TProgramData();
        private TParam param = new TParam();

        public TApp()
        {
        }

        public TApp(string[] args)
        {
            ParseParam(args);
        }

        public void ParseParam(string[] args)
        {
            param.Add("-f", TTypeParamData.tString, "", "Если указан, то настройки берутся из указанного ini файла, данные из файла имеют больший приоритет чем данные из командной строки");
            param.Add("-s", TTypeParamData.tString, "http://pioner-plus.ru:5060", "Адрес сервера сбора статистики");
            param.Add("-c", TTypeParamData.tString, "myCompany", "Название организации, например \"Рога и копыта\" ");
            param.Add("-n", TTypeParamData.tString, "base1", "Название контролируемого каталога, например \"БазаТорговли\"");
            param.Add("-pb", TTypeParamData.tString, "", "Путь к каталогу");
            param.Add("-u", TTypeParamData.tString, "sender", "Имя пользователя");
            param.Add("-p", TTypeParamData.tString, "", "Пароль");
            param.Parse(args);

            programData.iniFileName = param.Get("-f").data;
            programData.server = param.Get("-s").data;
            programData.pathBackup = param.Get("-pb").data;
            programData.company = param.Get("-c").data;
            programData.name = param.Get("-n").data;
            programData.username = param.Get("-u").data;
            programData.password = param.Get("-p").data;
        }

        public void Start()
        {
            TParams iniParam = new TParams();
            if (param.ParamEmpty)
            {
                Console.WriteLine("13/03/2019 apxi2@yandex.ru");
                Console.WriteLine("Программа контроля создания архивов.");
                Console.WriteLine("При запуске с параметрами контролирует каталог из параметра -pb");
                Console.WriteLine("Отправляет данные о контролируемом каталоге на сервер сбора статистики");
                Console.WriteLine("Параметры: ");
                foreach (TParamData item in param)
                {
                    Console.WriteLine("\t{0}  {1}", item.command, item.descript);
                }

                iniParam.SaveEmptyFile();
            }
            else
            {
                iniParam.Load(programData);
                string serializeData = JsonConvert.SerializeObject(GetData(programData));
                SendInfo(serializeData);
            }
        }

        // return free space on disk
        private long GetFreeSpaceOnDisk(string path)
        {
            long result = 0;
            string driveLetter = path;
            int k = path.LastIndexOf("\\");
            if (k > 0)
                driveLetter = path.Substring(0, k);

            try
            {
                DriveInfo di = new DriveInfo(driveLetter);
                result = di.TotalFreeSpace;
            }
            catch (Exception)
            {
            }
            return result;
        }

        // Create and fill class TSendingData
        public TSendingData GetData(TProgramData programData)
        {
            TSendingData result = null;
            if (programData.pathBackup.Length > 0 && Directory.Exists(programData.pathBackup))
            {
                result = new TSendingData();
                result.company = programData.company;
                result.name = programData.name;
                result.path = programData.pathBackup;
                result.freespace = GetFreeSpaceOnDisk(result.path);

                string[] files = Directory.GetFiles(programData.pathBackup).OrderByDescending(x => new FileInfo(x).CreationTime).ToArray();
                foreach (string file in files)
                {
                    int k = file.LastIndexOf("\\");
                    if (k > 0)
                    {
                        string fileName = file.Substring(k+1);
                        result.fileList.Add(
                            new TFileInfo()
                            {
                                name = fileName,
                                size = new FileInfo(file).Length,
                                dataCreate = Directory.GetCreationTime(file).ToString("dd.MM.yyyy HH:mm:ss")
                            }
                        );
                    }
                }
            }
            else
            {
                Console.WriteLine("Не найден каталог {0}", programData.pathBackup);
            }
            return result;
        }

        public void SendInfo(string serializeData)
        {
            string server = programData.server;
            WebRequest request = WebRequest.Create(server);
            request.Method = "POST";
            request.ContentType = "application/json";
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(serializeData);
            request.ContentLength = byteArray.Length;

            request.Credentials = new NetworkCredential(programData.username, programData.password);

            try
            {
                Console.WriteLine("Попытка подключения к серверу " + server);

                //записываем данные в поток запроса
                using (Stream dataStream = request.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                }

                WebResponse response = request.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        Console.WriteLine(reader.ReadToEnd());
                    }
                }
                response.Close();
                Console.WriteLine("Запрос выполнен...");                

            }
            catch (Exception e)
            {
                Console.WriteLine("Ошибка выполнения запроса к серверу: {0}", e.Message);
                Console.WriteLine("Отправляеммые данные:");
                Console.WriteLine(serializeData);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            TApp app = new TApp(args);
            app.Start();
        }
    }
}
