using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace ExampleParser
{
    class Program
    {
        public static void ParseExternalReferences(string filePath)
        {
            string json = File.ReadAllText(filePath);

            var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            var parsedExamples = ParseExternalReferencesInternal(values);

            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var finalJson = JsonSerializer.Serialize(parsedExamples, jsonSerializerOptions);
            File.WriteAllText(filePath, finalJson);
        }

        public static Dictionary<string, object> ParseExternalReferencesInternal(Dictionary<string, object> values)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> keyValuePair in values)
            {
                var obj = (JsonElement)values[keyValuePair.Key];

                if (obj.ValueKind == JsonValueKind.False 
                    || obj.ValueKind == JsonValueKind.True 
                    || obj.ValueKind == JsonValueKind.Number)
                {
                    dictionary[keyValuePair.Key] = keyValuePair.Value;
                }
                else if (obj.ValueKind == JsonValueKind.Object)
                {
                    var objectDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(obj.ToString());
                    dictionary[keyValuePair.Key] = ParseExternalReferencesInternal(objectDictionary);
                }
                else if (obj.ValueKind.Equals(JsonValueKind.String))
                {
                    var value = keyValuePair.Value.ToString();
                    object valueObject = null;

                    if (value.StartsWith("#https"))
                    {
                        using var client = new HttpClient();
                        var response = client.GetStringAsync(value.Substring(1)).GetAwaiter().GetResult();
                        try
                        {
                            valueObject = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                        }
                        catch
                        {
                            valueObject = JsonSerializer.Deserialize<string>(response); ;
                        }
                    }

                    if (valueObject == null)
                    {
                        dictionary[keyValuePair.Key] = value;
                    }
                    else
                    {
                        dictionary[keyValuePair.Key] = valueObject;
                    }
                }
                else if (obj.ValueKind.Equals(JsonValueKind.Array))
                {
                    try
                    {
                        var listDictionary = new List<Dictionary<string, object>>();
                        var objectDictionary = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(obj.ToString());
                        foreach (var dict in objectDictionary)
                        {
                            listDictionary.Add(ParseExternalReferencesInternal(dict));
                        }

                        dictionary[keyValuePair.Key] = listDictionary;
                    }
                    catch
                    {
                        dictionary[keyValuePair.Key] = obj;
                    }
                }
            }

            return dictionary;
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Please provide file path.");
            }
            ParseExternalReferences(args[0]);
        }
    }
}
