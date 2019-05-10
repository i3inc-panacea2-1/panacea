using Panacea.Core;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panacea.Implementations
{
    public class PanaceaSerializer : ISerializer
    {
        public PanaceaSerializer()
        {
            JsConfig.ConvertObjectTypesIntoStringDictionary = true;
        }

        public T Deserialize<T>(string text)
        {
            return JsonSerializer.DeserializeFromString<T>(text);
        }

        public object Deserialize(string text, Type t)
        {
            return JsonSerializer.DeserializeFromString(text, t);
        }

        public string Serialize<T>(T obj)
        {
            return JsonSerializer.SerializeToString<T>(obj);
        }
    }
}
