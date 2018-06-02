using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MultiMulti.Core.Exceptions;
using NLog;

namespace MultiMulti.Core.Utils
{
    public class DrawScraper
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly PermutationProvider _permutationProvider;

        public DrawScraper(PermutationProvider permutationProvider)
        {
            _permutationProvider = permutationProvider;
        }

        public async Task<Data[]> ScrapeNewestAsync(DateTime from)
        {
            var newData = new List<Data>();

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("https://www.wynikilotto.net.pl/multi-multi/wyniki/100/");
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                using (var response = (HttpWebResponse)await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.Load(reader);

                    try
                    {
                        foreach (var descendant in htmlDoc.GetElementbyId("tabela").SelectNodes(".//tr").Skip(1))
                        {
                            var dateString = descendant.ChildNodes[1].InnerText.Trim();
                            var date = DateTime.ParseExact(dateString, "dd.MM.yyyy", CultureInfo.InvariantCulture);
                            var time = int.Parse(descendant.ChildNodes[2].InnerText.Trim());
                            var values = string.Join("", descendant.ChildNodes[3].InnerText
                                    .Where(character => char.IsNumber(character) || character == ' '))
                                .Split(' ')
                                .Select(int.Parse)
                                .Reverse() // Skip last element, because
                                .Skip(1)   // last element is that "magic" number or whatever
                                .ToArray();

                            // Wrong read.
                            if (values.Length != 20)
                                continue;

                            var dateTime = date + TimeSpan.FromHours(time);

                            if (dateTime <= from)
                                continue;

                            var pairs = _permutationProvider.GetPermutations(values, 2)
                                .Select(p => p.ToArray()[0] + ", " + p.ToArray()[1]).ToArray();

                            var data = new Data
                            {
                                Added = dateTime,
                                IsCustom = false,
                                Values = values,
                                Pairs = pairs
                            };

                            newData.Add(data);
                        }
                    }
                    catch (Exception)
                    {
                        throw new DrawParsingException();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return newData.ToArray();
        }
    }
}
