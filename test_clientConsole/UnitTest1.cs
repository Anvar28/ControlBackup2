using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using clientConsole;
using Newtonsoft.Json;

namespace test_clientConsole
{
    [TestClass]
    public class APPTests
    {
        [TestMethod]
        public void GetDataTest()
        {
            clientConsole.TProgramData programData = new clientConsole.TProgramData();
            programData.company = "plus";
            programData.name = "base1";
            programData.pathBackup = @"D:\projects\C#\ControlBackup2\clientConsole\bin\Debug\test\";
            TApp app = new TApp();
            TSendingData data = app.GetData(programData);
            string serializeData = JsonConvert.SerializeObject(data);
            Console.WriteLine(serializeData);
        }
    }
}
