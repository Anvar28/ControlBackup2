using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace clientConsole
{
    public enum TTypeParamData { tBoll, tString }
    public class TParamData
    {

        public string command;
        public TTypeParamData type;
        public string data;
        public string descript;

        public TParamData(string lCommand, TTypeParamData ltype, string defData, string lDescript = "")
        {
            command = lCommand;
            type = ltype;
            data = defData;
            descript = lDescript;
        }
    }

    public class TParam : IEnumerable, IEnumerator
    {
        private Dictionary<string, TParamData> paramList;
        int index = -1;
        private bool _paramEmpty = true;
        public bool ParamEmpty { get { return _paramEmpty; } }

        public TParam()
        {
            paramList = new Dictionary<string, TParamData>();
        }

        public void Add(string lCommand, TTypeParamData ltype, string defData, string lDescript = "")
        {
            paramList.Add(lCommand, new TParamData(lCommand, ltype, defData, lDescript));
        }

        public TParamData Get(string lCommand)
        {
            return paramList[lCommand];
        }

        public void Parse(string[] args)
        {
            // Заполним параметры из командной строки
            int count = args.Count();
            for (int i = 0; i < paramList.Count(); i++)
            {
                _paramEmpty = false;
                var itemParam = paramList.ElementAt(i);
                string key = itemParam.Key;
                TParamData value = itemParam.Value;
                for (int j = 0; j < count; j++)
                {
                    if (key == args[j])
                    {
                        if (value.type == TTypeParamData.tBoll)
                        {
                            value.data = "True";
                        }
                        else
                        {
                            // Следущий должно идти значение параметра
                            if (j + 1 < count)
                            {
                                value.data = args[j + 1];
                            }
                        }
                    }
                }
            }

        }

        // Реализуем интерфейс IEnumerable
        public IEnumerator GetEnumerator()
        {
            return this;
        }

        // Реализуем интерфейс IEnumerator
        public bool MoveNext()
        {
            if (index == paramList.Count() - 1)
            {
                Reset();
                return false;
            }

            index++;
            return true;
        }

        public void Reset()
        {
            index = -1;
        }

        public object Current
        {
            get
            {
                return paramList.ElementAt(index).Value;
            }
        }
    }
}
