using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace ExampleParser
{
    class Program
    {
        public static string ParseExternalReferences()
        {
            string json = File.ReadAllText("C:\\Users\\ssingh\\source\\repos\\ExampleParser\\storage.json");

            var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            var parsedExamples = ParseExternalReferencesInternal(values);

            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var finalJson = JsonSerializer.Serialize(parsedExamples, jsonSerializerOptions);
            File.WriteAllText("C:\\Users\\ssingh\\source\\repos\\ExampleParser\\TextFile1.json", finalJson);

            return finalJson;
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
            ParseExternalReferences();
        }

        static void Main1(string[] args)
        {
            /*
            if (args.Length == 0)
            {
                throw new ArgumentException("Please provide the path to OpenAPI 3.0 file.");
            }
            */

            var dictionary = new Dictionary<string, object>();
            string json = File.ReadAllText("C:\\Users\\ssingh\\source\\repos\\ExampleParser\\storage.json");
            var values = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            //            foreach (KeyValuePair<string, object> value in values)
            //            {
            //                if (!value.Key.Equals("paths"))
            //                    dictionary.Add(value.Key, value.Value);
            //            }

            if (values.TryGetValue("paths", out var obj))
            {
                var pathValues = JsonSerializer.Deserialize<Dictionary<string, object>>(obj.ToString());

                var dictionaryApi = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> keyValuePath in pathValues)
                {
                    var apiObject = pathValues[keyValuePath.Key];
                    var apiValues = JsonSerializer.Deserialize<Dictionary<string, object>>(apiObject.ToString());

                    var dictionaryCrud = new Dictionary<string, object>();
                    foreach (var keyValueApi in apiValues)
                    {
                        var crudOperationObject = apiValues[keyValueApi.Key];
                        var crudOperationValues = JsonSerializer.Deserialize<Dictionary<string, object>>(crudOperationObject.ToString());

                        var dictionaryResponses = new Dictionary<string, object>();
                        foreach (var keyValueCrudOperation in crudOperationValues)
                        {
                            if (keyValueCrudOperation.Key.Equals("responses", StringComparison.OrdinalIgnoreCase))
                            {
                                crudOperationValues.TryGetValue("responses", out var responseObject);
                                var responseValues = JsonSerializer.Deserialize<Dictionary<string, object>>(responseObject.ToString());
                                var dictionaryResponse = new Dictionary<string, object>();
                                foreach (var keyValueResponse in responseValues)
                                {
                                    var responseCodeObject = responseValues[keyValueResponse.Key];
                                    var responseCodeValues = JsonSerializer.Deserialize<Dictionary<string, object>>(responseCodeObject.ToString());

                                    dictionaryResponse.Add(keyValueResponse.Key, ExtractResponseCodesDictionary(responseCodeValues));
                                }

                                dictionaryResponses.Add("responses", dictionaryResponse);
                            }
                            else
                            {
                                dictionaryResponses.Add(keyValueCrudOperation.Key, keyValueApi.Value);
                            }
                        }

                        dictionaryCrud.Add(keyValueApi.Key, dictionaryResponses);
                    }

                    dictionaryApi.Add(keyValuePath.Key, dictionaryCrud);
                }

                dictionary.Add("paths", dictionaryApi);
            }

            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var finalJson = JsonSerializer.Serialize(dictionary, jsonSerializerOptions);
            File.WriteAllText("C:\\Users\\ssingh\\source\\repos\\ExampleParser\\TextFile1.json", finalJson);
        }

        private static IDictionary<string, object> ExtractExampleDictionary(IDictionary<string, object> exampleValues)
        {
            var dictionaryExample = new Dictionary<string, object>();
            foreach (var keyValueArray in exampleValues)
            {
                if (keyValueArray.Key.Equals("externalValue", StringComparison.OrdinalIgnoreCase))
                {
                    exampleValues.TryGetValue("externalValue", out var externalValue);

                    using var client = new HttpClient();
                    var content = client.GetStringAsync(externalValue.ToString()).GetAwaiter().GetResult();
                    var externalExampleJson = JsonSerializer.Deserialize<Dictionary<string, object>>(content);

                    dictionaryExample.Add("externalValue", externalExampleJson);
                }
                else
                {
                    dictionaryExample.Add(keyValueArray.Key, keyValueArray.Value);
                }
            }

            return dictionaryExample;
        }

        private static IDictionary<string, object> ExtractMimeTypesDictionary(IDictionary<string, object> mimeValues)
        {
            var dictionaryMimeTypes = new Dictionary<string, object>();
            foreach (var keyValueMimeValue in mimeValues)
            {
                if (keyValueMimeValue.Key.Equals("example", StringComparison.OrdinalIgnoreCase))
                {
                    mimeValues.TryGetValue("example", out var exampleObject);
                    var exampleValues = JsonSerializer.Deserialize<Dictionary<string, object>>(exampleObject.ToString());

                    if (exampleValues.TryGetValue("externalValue", out var externalValue))
                    {
                        using var client = new HttpClient();
                        var content = client.GetStringAsync(externalValue.ToString()).GetAwaiter().GetResult();
                        exampleValues["externalValue"] = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    }

                    dictionaryMimeTypes.Add(keyValueMimeValue.Key, exampleValues);
                }
                else if (keyValueMimeValue.Key.Equals("examples", StringComparison.OrdinalIgnoreCase))
                {
                    mimeValues.TryGetValue("examples", out var examplesObject);
                    var examplesArray = JsonSerializer.Deserialize<Dictionary<string, object>>(examplesObject.ToString());

                    var dictionaryArray = new Dictionary<string, object>();
                    foreach (var keyValueExamplesArray in examplesArray)
                    {
                        var arrayObject = examplesArray[keyValueExamplesArray.Key];
                        var arrayValues = JsonSerializer.Deserialize<Dictionary<string, object>>(arrayObject.ToString());

                        var dictionaryExamples = ExtractExampleDictionary(arrayValues);
                        dictionaryArray.Add(keyValueExamplesArray.Key, dictionaryExamples);
                    }

                    dictionaryMimeTypes.Add(keyValueMimeValue.Key, dictionaryArray);
                }
                else
                {
                    dictionaryMimeTypes.Add(keyValueMimeValue.Key, keyValueMimeValue.Value);
                }
            }

            return dictionaryMimeTypes;
        }

        private static IDictionary<string, object> ExtractResponseCodesDictionary(IDictionary<string, object> responseCodeValues)
        {
            var dictionaryResponseCodes = new Dictionary<string, object>();
            foreach (var keyValueResponseCode in responseCodeValues)
            {
                if (keyValueResponseCode.Key.Equals("content", StringComparison.OrdinalIgnoreCase))
                {
                    responseCodeValues.TryGetValue("content", out var contentObject);
                    var contentValues = JsonSerializer.Deserialize<Dictionary<string, object>>(contentObject.ToString());

                    var dictionaryMimeTypes = new Dictionary<string, object>();
                    foreach (var keyValueContent in contentValues)
                    {
                        var mimeObject = contentValues[keyValueContent.Key];
                        var mimeValues = JsonSerializer.Deserialize<Dictionary<string, object>>(mimeObject.ToString());

                        dictionaryMimeTypes.Add(keyValueContent.Key, ExtractMimeTypesDictionary(mimeValues));
                    }

                    dictionaryResponseCodes.Add("content", dictionaryMimeTypes);
                }
                else
                {
                    dictionaryResponseCodes.Add(keyValueResponseCode.Key, keyValueResponseCode.Value);
                }
            }

            return dictionaryResponseCodes;
        }
    }
}
