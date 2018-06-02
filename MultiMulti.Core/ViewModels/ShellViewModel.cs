using Caliburn.Micro;
using Microsoft.Win32;
using MultiMulti.Core.Services;
using MultiMulti.Core.Utils;
using NLog;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using LogManager = NLog.LogManager;

namespace MultiMulti.Core.ViewModels
{
    public class ShellViewModel : Screen
    {
        private const int RequiredSelectedButtonsCount = 20;

        public ObservableCollection<ButtonViewModel> Buttons { get; set; } = new ObservableCollection<ButtonViewModel>();
        public ObservableCollection<DrawHistoryItemViewModel> DrawHistoryItems { get; set; } = new ObservableCollection<DrawHistoryItemViewModel>();
        public ObservableCollection<MostCommonPairModel> MostCommonPairs { get; set; } = new ObservableCollection<MostCommonPairModel>();

        private readonly PermutationProvider _permutationProvider;
        private readonly DataService _dataService;
        private readonly PairAnalyzer _pairAnalyzer;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IWindowManager _windowManager;

        public ShellViewModel(IWindowManager windowManager, PermutationProvider permutationProvider, DataService dataService, PairAnalyzer pairAnalyzer)
        {
            _windowManager = windowManager;
            _permutationProvider = permutationProvider;
            _dataService = dataService;
            _pairAnalyzer = pairAnalyzer;

            for (var index = 1; index <= 80; index++)
            {
                Buttons.Add(new ButtonViewModel(index.ToString(), this));
            }

            GenerateDrawHistoryItems();

            UpdateMostCommonPairs();
        }

        private void GenerateDrawHistoryItems()
        {
            DrawHistoryItems.Clear();

            try
            {
                var data = _dataService.GetDataFromAllFiles();
                foreach (var draw in data)
                {
                    DrawHistoryItems.Add(new DrawHistoryItemViewModel(this, draw));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error occured while trying to generate history items");
                MessageBox.Show(ex.Message, "Blad");
            }
        }

        public bool CanSelectButton() => Buttons.Count(btn => btn.IsSelected) < RequiredSelectedButtonsCount;

        public IEnumerable<IEnumerable<int>> FindPairs()
        {
            try
            {
                var selectedNumbers = Buttons.Where(btn => btn.IsSelected).Select(btn => int.Parse(btn.ButtonText));
                return _permutationProvider.GetPermutations(selectedNumbers, 2);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ex.Message, "Blad");
                return Enumerable.Empty<IEnumerable<int>>();
            }
        }

        public void ExportToExcel()
        {
            try
            {
                var allData = _dataService.GetDataFromAllFiles().ToList();

                // Nothing to export...
                if (allData.Count == 0)
                {
                    MessageBox.Show($"Nie znaleziono żadnych losowań. Proszę najpierw wybrać {RequiredSelectedButtonsCount} liczb i kliknąć 'Dodaj losowanie.'", "Brak losowań");
                    return;
                }

                var excelFileDestinationPath = RequestSaveFilePath();
                if (string.IsNullOrWhiteSpace(excelFileDestinationPath))
                    return;

                using (var excel = new ExcelPackage())
                {
                    var worksheet = excel.Workbook.Worksheets.Add("Pary");

                    var headers = new List<string> { "Wszystkie pary", "Wystąpienia" };
                    headers.AddRange(allData.Select((d, index) => $"Los. {index + 1} ({d.Added.ToShortDateString()})"));
                    var headerRow = new List<string[]> { headers.ToArray() };
                    worksheet.Cells[1, 1].LoadFromArrays(headerRow);

                    //AllPairs + analysis
                    var allPairs = _dataService.GetAllPairs().ToArray();
                    for (var index = 0; index < allPairs.Length; index++)
                    {
                        var pair = allPairs[index].ToArray();
                        var pairArray = pair.ToArray();
                        var pairText = $"{pairArray[0]}, {pairArray[1]}";
                        worksheet.Cells[index + 2, 1].LoadFromArrays(new List<string[]> { new[] { pairText } });

                        var analysis = _pairAnalyzer.Analyze(pair, allData.ToArray());
                        var analysisText = $"{analysis.OccurenceCount} ({analysis.OccurencePercentage:F2}%)";
                        worksheet.Cells[index + 2, 2].LoadFromArrays(new List<string[]> { new[] { analysisText } });
                    }

                    // Draws
                    for (var dataIndex = 0; dataIndex < allData.Count; dataIndex++)
                    {
                        var columnIndex = headers.Count - allData.Count + dataIndex + 1;
                        var currentData = allData[dataIndex];
                        for (var index = 0; index < currentData.Values.Count(); index++)
                        {
                            var rowIndex = index + 2;
                            var currentPairs = currentData.Values.ToList()[index].ToArray();
                            var rowText = $"{string.Join(", ", currentPairs)}";
                            var row = new List<string[]> { new[] { rowText } };
                            worksheet.Cells[rowIndex, columnIndex].LoadFromArrays(row);
                        }
                    }

                    var excelFile = new FileInfo(Path.Combine(Path.GetDirectoryName(excelFileDestinationPath),
                                                 Path.GetFileName(excelFileDestinationPath)));
                    try
                    {
                        excel.SaveAs(excelFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                        MessageBox.Show(
                            "Wystąpił błąd przy zapisywaniu pliku excel. Upewnij się, że wybrany plik nie jest używany przez inny program.",
                            "Blad Excel");
                    }
                }
            }
            catch (Exception ex)
            {
               _logger.Error(ex);
                MessageBox.Show(ex.Message, "Blad");
            }
        }

        public string RequestSaveFilePath()
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = "Wyniki_losowan",
                CheckFileExists = false,
                CreatePrompt = true,
                OverwritePrompt = true,
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                RestoreDirectory = true
            };

            var result = saveFileDialog.ShowDialog();

            if (result != null && result == true)
            {
                return saveFileDialog.FileName;
            }

            return string.Empty;
        }

        public void AddNewDraw()
        {
            var selectedButtonsCount = Buttons.Count(btn => btn.IsSelected);
            if (selectedButtonsCount != RequiredSelectedButtonsCount)
            {
                MessageBox.Show($"Proszę wybrać {RequiredSelectedButtonsCount} liczb.", "Niewystarczająca liczba wartości");
                return;
            }

            try
            {
                var pairs = FindPairs().ToList();

                _dataService.AddData(new Data
                {
                    Id = Guid.NewGuid(),
                    Added = DateTime.Now,
                    Values = pairs,
                    SelectedNumbers = Buttons.Where(btn => btn.IsSelected).Select(btn => int.Parse(btn.ButtonText))
                });

                GenerateDrawHistoryItems();
                UpdateMostCommonPairs();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ex.Message, "Blad");
            }

            //Reset buttons.
            foreach (var button in Buttons)
            {
                button.IsSelected = false;
            }
        }

        public void RemoveDrawHistoryItem(Guid id)
        {
            var index = DrawHistoryItems.ToList().FindIndex(item => item.Id.Equals(id));
            if (index == -1)
            {
                _logger.Warn($"RemoveDrawHistoryItem was not able to find DrawHistoryItem with id={id}");
                return;
            }
            DrawHistoryItems.RemoveAt(index);

            try
            {
                _dataService.DeleteDrawFileWithId(id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ex.Message, "Blad");
            }

            UpdateMostCommonPairs();
        }

        private void UpdateMostCommonPairs()
        {
            try
            {
                MostCommonPairs.Clear();

                var allData = _dataService.GetDataFromAllFiles().ToList();
                var allPairs = _dataService.GetAllPairs().ToArray();

                var result = allPairs.Select(pair => _pairAnalyzer.Analyze(pair.ToArray(), allData.ToArray()))
                    .Where(pair => pair.OccurenceCount > 0)
                    .OrderByDescending(pair => pair.OccurenceCount)
                    .Take(10);
                foreach (var pair in result)
                {
                    MostCommonPairs.Add(new MostCommonPairModel
                    {
                        PairValue = $"[{pair.ValueText}]",
                        PercentageText = $" ({pair.OccurencePercentage:F2}%)",
                        OccurenceText = $" Wystąpiła {pair.OccurenceCount} razy"
                    });
                }
            }
            catch (Exception x)
            {
                _logger.Error(x);
                MessageBox.Show(x.Message, "Blad");
            }
        }

        public void OnHelpClicked()
        {
            _windowManager.ShowDialog(new HelpViewModel());
        }
    }
}
