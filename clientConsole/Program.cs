using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace clientConsole
{
    class TProgramData
    {
        public string server;
        public string pathBackup;
        public string company;
        public string name;
    }

    class TFileInfo
    {
        public string name;
        public long size;
        public DateTime dataCreate;
    }

    class TSendingData
    {
        public string company;
        public string name;
        public List<TFileInfo> fileList = new List<TFileInfo>();
    }

    class TApp
    {
        private TProgramData programData = new TProgramData();
        private TParam param = new TParam();

        public TApp(string[] args)
        {
            ParseParam(args);
        }

        void ParseParam(string[] args)
        {
            param.Add("-f", TTypeParamData.tBoll, "", "Если указан, то настройки берутся из param.ini файла. Файл с пустыми настройками создан.");
            param.Add("-s", TTypeParamData.tString, "pioner-plus.ru:5060", "Адрес сервера сбора статистики");
            param.Add("-c", TTypeParamData.tString, "myCompany", "Название организации, например \"Рога и копыта\" ");
            param.Add("-n", TTypeParamData.tString, "base1", "Название контролируемого каталога, например \"БазаТорговли\"");
            param.Add("-pb", TTypeParamData.tString, "", "Путь к каталогу");
            param.Parse(args);

            programData.server = param.Get("-s").data;
            programData.pathBackup = param.Get("-pb").data;
            programData.company = param.Get("-c").data;
            programData.name = param.Get("-n").data;
        }

        public void Start()
        {
            LoadFromIni();
            SendInfo();
        }

        // Load property fom ini file, 
        void LoadFromIni()
        {
            TIniFiles ini = new TIniFiles("param.ini");
            string iniSection = "Основные настройки";
            string iniSmtpServer = "server";
            string iniPathbackup = "pathbackup";
            string iniCompany = "company";
            string iniName = "name";

            if (param.ParamEmpty)
            {
                // Создадим INI файл с настройками
                ini.Write(iniSection, iniSmtpServer, "");
                ini.Write(iniSection, iniPathbackup, "");
                ini.Write(iniSection, iniCompany, "");
                ini.Write(iniSection, iniName, "");

                Console.WriteLine("13/03/2019 apxi2@yandex.ru");
                Console.WriteLine("Программа контроля создания архивов.");
                Console.WriteLine("При запуске с параметрами контролирует каталог из параметра -pb");
                Console.WriteLine("Отправляет данные о контролируемом каталоге на сервер сбора статистики");
                Console.WriteLine("Парамтеры: ");
                foreach (TParamData item in param)
                {
                    Console.WriteLine("\t{0}  {1}", item.command, item.descript);
                }

            }

            // Если в параметрах указано что брать данные из файла настроек, то
            // попробует получить из него данные

            if (param.Get("-f").data != "")
            {
                // читаем данные из файла
                programData.server = ini.Read(iniSection, iniSmtpServer);
                programData.pathBackup = ini.Read(iniSection, iniPathbackup);
                programData.company = ini.Read(iniSection, iniCompany);
                programData.name = ini.Read(iniSection, iniName);
            }
        }

        // Create and fill class TSendingData
        TSendingData GetData(TProgramData programData)
        {
            TSendingData result = null;
            if (programData.pathBackup.Length > 0 && Directory.Exists(programData.pathBackup))
            {
                result = new TSendingData();
                result.company = programData.company;
                result.name = programData.name;

                string[] files = Directory.GetFiles(programData.pathBackup).OrderByDescending(x => new FileInfo(x).CreationTime).ToArray();
                foreach (string file in files)
                {
                    result.fileList.Add(
                        new TFileInfo()
                        {
                            name = file,
                            size = new FileInfo(file).Length,
                            dataCreate = Directory.GetCreationTime(file)
                        }
                    );
                }
            }
            else
            {
                Console.WriteLine("Не найден каталог {0}", programData.pathBackup);
            }
            return result;
        }

        void SendInfo()
        {
            TSendingData data = GetData(programData);
            string serializeData = JsonConvert.SerializeObject(data);

            WebRequest request = WebRequest.Create("http://" + programData.server);
            request.Method = "POST";
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(serializeData);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;

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
