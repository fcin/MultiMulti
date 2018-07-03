using LiteDB;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using MultiMulti.Core.Utils;

namespace MultiMulti.Core.Services
{
    public class DataService
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private string _resourcesPath;
        public string ResourcesPath
        {
            get
            {
                if(string.IsNullOrEmpty(_resourcesPath))
                    _resourcesPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                return _resourcesPath;
            }
        }

        private readonly PermutationProvider _permutationProvider;

        public DataService(PermutationProvider permutationProvider)
        {
            _permutationProvider = permutationProvider;
        }

        public void ImportAll()
        {
            try
            {
                using (var db = new LiteDatabase(Path.Combine(ResourcesPath, "dataDb.db")))
                {
                    if (db.CollectionExists("data"))
                    {
                        return;
                    }

                    var allRecords = File.ReadAllLines(Path.Combine(ResourcesPath, "ml.txt"));
                    
                    var allData = new List<Data>();

                    foreach (var record in allRecords)
                    {
                        var columns = record.Split(' ');
                        var id = columns[0].Substring(0, columns[0].Length - 1);
                        var date = DateTime.ParseExact(columns[1], "dd.MM.yyyy", CultureInfo.InvariantCulture);
                        var prev = allData.LastOrDefault();

                        if (prev != null && prev.Added.Day == date.Day)
                            date += TimeSpan.FromHours(21); // Second draw of the day.
                        else
                            date += TimeSpan.FromHours(14);

                        var values = columns[2].Split(',').Select(int.Parse).OrderByDescending(b => b).ToArray();
                        var pairs = _permutationProvider.GetPermutations(values, 2)
                            .Select(p => p.ToArray()[0] + ", " + p.ToArray()[1]).ToArray();

                        var data = new Data
                        {
                            Id = int.Parse(id),
                            Added = date,
                            Values = values,
                            Pairs = pairs,
                            IsCustom = false
                        };

                        allData.Add(data);
                    }
                    var a = allData.Where(d => d.Pairs.Contains("2, 6")).ToList();
                    var dataCollection = db.GetCollection<Data>("data");
                    dataCollection.InsertBulk(allData);

                    dataCollection.EnsureIndex(col => col.Added);
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        public IEnumerable<Pair> GetMostCommonPairs()
        {
            try
            {
                using (var db = new LiteDatabase(Path.Combine(ResourcesPath, "dataDb.db")))
                {
                    if (!db.CollectionExists("data"))
                    {
                        throw new InvalidOperationException("Database does not exist!");
                    }

                    var dataCollection = db.GetCollection<Data>("data");
                    if (dataCollection.Count() == 0)
                        ImportAll();

                    var allPairs = dataCollection.FindAll().SelectMany(p => p.Pairs).ToArray();
                    var allPairsCount = allPairs.Length;

                    var mostCommonPairs =
                    (from pair in allPairs
                        group pair by pair
                        into c
                        let count = c.Count()
                        orderby count descending
                        select new Pair
                        {
                            Value = c.Key,
                            Occurences = count,
                            OccurencePercentage = ((double)count / allPairsCount) * 100
                        }).Take(100).ToArray();

                    return mostCommonPairs;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return Enumerable.Empty<Pair>();
            }
        }

        public IEnumerable<Data> GetAllData()
        {
            using (var db = new LiteDatabase(Path.Combine(ResourcesPath, "dataDb.db")))
            {
                if (!db.CollectionExists("data"))
                {
                    throw new InvalidOperationException("Database does not exist!");
                }

                var dataCollection = db.GetCollection<Data>("data");
                if (dataCollection.Count() == 0)
                    ImportAll();

                return dataCollection.FindAll().ToArray();
            }
        }

        public IEnumerable<string> GetAllPossiblePairs()
        {
            try
            {
                return File.ReadAllLines(Path.Combine(ResourcesPath, "AllPairs.txt"));
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return Enumerable.Empty<string>();
            }
        }

        public IEnumerable<Pair> GetAllPossiblePairsWithOccurences()
        {
            try
            {
                using (var db = new LiteDatabase(Path.Combine(ResourcesPath, "dataDb.db")))
                {
                    if (!db.CollectionExists("data"))
                    {
                        throw new InvalidOperationException("Database does not exist!");
                    }

                    var dataCollection = db.GetCollection<Data>("data");
                    if (dataCollection.Count() == 0)
                        ImportAll();

                    var allPairs = dataCollection.FindAll().SelectMany(p => p.Pairs).ToArray();
                    var allPairsCount = allPairs.Length;

                    var debug = allPairs.Count(p => p == "6, 2");

                    var pairs =
                        allPairs.GroupBy(pair => pair)
                            .Select(c => new { c, count = c.Count() })
                            .OrderBy(t => t.c.Key, new PairComparer())
                            .Select(t => new Pair
                            {
                                Value = t.c.Key,
                                Occurences = t.count,
                                OccurencePercentage = ((double)t.count / allPairsCount) * 100
                            }).ToArray();

                    return pairs;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return Enumerable.Empty<Pair>();
            }
        }

        public void AddData(Data data)
        {
            using (var db = new LiteDatabase(Path.Combine(ResourcesPath, "dataDb.db")))
            {
                if (!db.CollectionExists("data"))
                {
                    throw new InvalidOperationException("Database does not exist!");
                }

                var dataCollection = db.GetCollection<Data>("data");

                dataCollection.Insert(data);
            }
        }

        public void AddData(IEnumerable<Data> data)
        {
            using (var db = new LiteDatabase(Path.Combine(ResourcesPath, "dataDb.db")))
            {
                if (!db.CollectionExists("data"))
                {
                    throw new InvalidOperationException("Database does not exist!");
                }

                var dataCollection = db.GetCollection<Data>("data");

                dataCollection.InsertBulk(data);
            }
        }

        public Data GetLatestDraw()
        {
            using (var db = new LiteDatabase(Path.Combine(ResourcesPath, "dataDb.db")))
            {
                var dataCollection = db.GetCollection<Data>("data");

                var latest = dataCollection.Find(
                    Query.And(
                        Query.All("Added", Query.Descending), 
                        Query.Where("IsCustom", value => value.AsBoolean == false))
                    , 0, 1).FirstOrDefault();
                return latest;
            }
        }

        private class PairComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var firstPair = x.Split(',').Select(int.Parse).ToArray();
                var secondPair = y.Split(',').Select(int.Parse).ToArray();

                if (firstPair[0] > secondPair[0])
                    return 1;
                if (firstPair[0] < secondPair[0])
                    return -1;

                if (firstPair[1] > secondPair[1])
                    return 1;
                if (firstPair[1] < secondPair[1])
                    return -1;

                return 0;
            }
        }
    }
}
