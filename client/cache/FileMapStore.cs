﻿using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using io.harness.cfsdk.client.cache;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace io.harness.cfsdk.client.api
{
    public class FileMapStore : IStore
    {
        private string storeName;
        public FileMapStore(string name)
        {
            storeName = name;
            Directory.CreateDirectory(name);
            Array.ForEach(Directory.EnumerateFiles(name).ToArray(), f => File.Delete(f));
        }

        public void Close()
        {
            
        }

        public void Delete(string key)
        {
            File.Delete(Path.Combine(storeName, key));
        }

        public object Get(string key, Type t)
        {
            try
            {
                var filePath = Path.Combine(storeName, key);
                if (File.Exists(filePath))
                {
                    var str = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject(str, t);
                }
                return null;
            }
            catch( Exception ex)
            {
                Log.Error("Failure to deserialize data from file storage", ex);
                return null;
            }
        }

        public ICollection<string> Keys()
        {
            return Directory.EnumerateFiles(storeName).Select( f => Path.GetFileName(f)).ToList();
        }

        public void Set(string key, object value)
        {
            var str = JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings {
                ContractResolver = new IncludeNullPropertiesContractResolver()
            });
            File.WriteAllText(Path.Combine(storeName, key), str);
        }
    }
}

class IncludeNullPropertiesContractResolver : DefaultContractResolver
{
    /*
     * NOTE: When property has Required = AllowNull and NullValueHandling = Ignore atributes, 
     * in case of null value property, based on NullValueHandling.Ignore atribute, serialization will create string without property.
     * In that case deserialization will throw exception as value doesn't exist and requred field is AllowNull (internals of Newtonsoft.json library).
     * IncludeNullPropertiesContractResolver will be used as custom Contract Resolver, which will enforce all properties that allo null, 
     * to include null values in serialized output.
     */
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var properties = base.CreateProperties(type, memberSerialization);
        foreach (var p in properties)
        {
            if (p.Required == Required.AllowNull)
            {
                p.NullValueHandling = NullValueHandling.Include;
            }
        }
        return properties;
    }
}
