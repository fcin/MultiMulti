using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MultiMulti.Core.Services;
using NLog;
using OfficeOpenXml;

namespace MultiMulti.Core.Utils
{
    public class ExcelExporter
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly DataService _dataService;

        public ExcelExporter(DataService dataService)
        {
            _dataService = dataService;
        }

        public async Task ExportAll(string filePath, IProgress<Tuple<int, int>> progressCallback)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException(nameof(filePath));
            }

            await Task.Run(() =>
            {
                using (var excel = new ExcelPackage())
                {
                    var worksheet = excel.Workbook.Worksheets.Add("Pary");

                    var allPairsOccurences = _dataService.GetAllPossiblePairsWithOccurences();
                    AddAllPairsColumn(worksheet, allPairsOccurences);

                    var allDraws = _dataService.GetAllData().ToArray();
                    for (var index = 0; index < allDraws.Length; index++)
                    {
                        var columnIndex = index + 4;
                        AddDraw(worksheet, columnIndex, allDraws[index]);
                        if(index % 100 == 0)
                            progressCallback.Report(Tuple.Create(index, allDraws.Length));
                    }

                    excel.SaveAs(new FileInfo(filePath));
                }
            });
        }

        private void AddAllPairsColumn(ExcelWorksheet worksheet, IEnumerable<Pair> allPairs)
        {
            worksheet.Cells[1, 1].Value = "Wszystkie pary";
            worksheet.Cells[1, 2].Value = "Wystąpienia";
            worksheet.Cells[1, 3].Value = "Procentowo";
            var index = 2;
            foreach (var pair in allPairs)
            {
                worksheet.Cells[index, 1].Value = $"{pair.Value}";
                worksheet.Cells[index, 2].Value = pair.Occurences;
                worksheet.Cells[index, 3].Value = $"{pair.OccurencePercentage:F8}%";
                index++;
            }
        }

        private void AddDraw(ExcelWorksheet worksheet, int columnIndex, Data draw)
        {
            try
            {
                worksheet.Cells[1, columnIndex].Value = $"{draw.Added.ToString(new CultureInfo("pl-PL"))}";

                for (int index = 0; index < draw.Pairs.Length; index++)
                {
                    var rowIndex = index + 2;
                    worksheet.Cells[rowIndex, columnIndex].Value = string.Join(", ", draw.Pairs[index]);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}
