using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NLog;

namespace MultiMulti.Core.Services
{
    public class DataService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private string _savePath;
        public string SavePath
        {
            get
            {
                if(string.IsNullOrEmpty(_savePath))
                    _savePath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                return _savePath;
            }
        }

        public void AddData(Data data)
        {
            var jsonData = JsonConvert.SerializeObject(data, Formatting.None);
            var filePath = Path.Combine(SavePath, $"data_{data.Id}.txt");
            File.WriteAllText(filePath, jsonData);
        }

        public IEnumerable<Data> GetDataFromAllFiles()
        {
            var files = Directory.GetFiles(SavePath).Where(file => file.Contains("data_"))
                            .Select(f => JsonConvert.DeserializeObject<Data>(string.Join("", File.ReadAllLines(f))));
            foreach (var file in files.OrderBy(f => f.Added))
            {
                yield return file;
            }
        }

        private IEnumerable<IEnumerable<int>> _allPairsCached;

        public IEnumerable<IEnumerable<int>> GetAllPairs()
        {
            if (_allPairsCached != null)
                return _allPairsCached;

            try
            {
                var allPairs = new List<IEnumerable<int>>();
                foreach (var line in File.ReadLines(Path.Combine(SavePath, "AllPairs.txt")))
                {
                    var pairs = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
                    allPairs.Add(new[] { pairs[0], pairs[1] });
                }
                _allPairsCached = allPairs;
                return allPairs;
            }
            catch (Exception x)
            {
                _logger.Error(x);
                throw;
            }
        }

        public void DeleteDrawFileWithId(Guid id)
        {
            try
            {
                var fileName = $"data_{id}.txt";
                File.Delete(Path.Combine(SavePath, fileName));
            }
            catch (DirectoryNotFoundException dnfe)
            {
                _logger.Error(dnfe, "Directory not found");
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw;
            }
        }
    }
}
