using Caliburn.Micro;
using Microsoft.Win32;
using MultiMulti.Core.DTOs;
using MultiMulti.Core.Exceptions;
using MultiMulti.Core.Services;
using MultiMulti.Core.Utils;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using LogManager = NLog.LogManager;

namespace MultiMulti.Core.ViewModels
{
    public class ShellViewModel : Conductor<object>
    {
        private const int RequiredSelectedButtonsCount = 20;

        public ObservableCollection<ButtonViewModel> Buttons { get; set; } = new ObservableCollection<ButtonViewModel>();
        public ObservableCollection<PairDto> MostCommonPairs { get; set; } = new ObservableCollection<PairDto>();

        private bool _exportToExcelButtonEnabled = true;

        public bool ExportToExcelButtonEnabled
        {
            get => _exportToExcelButtonEnabled;
            set
            {
                _exportToExcelButtonEnabled = value;
                NotifyOfPropertyChange(() => ExportToExcelButtonEnabled);
            }
        }

        private readonly DataService _dataService;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly IWindowManager _windowManager;
        private readonly ExcelExporter _excelExporter;
        private readonly PermutationProvider _permutationProvider;
        private readonly DrawScraper _drawScraper;

        public ShellViewModel(IWindowManager windowManager, DataService dataService, ExcelExporter excelExporter,
            PermutationProvider permutationProvider, DrawScraper drawScraper)
        {
            _windowManager = windowManager;
            _dataService = dataService;
            _excelExporter = excelExporter;
            _permutationProvider = permutationProvider;
            _drawScraper = drawScraper;

            for (var index = 1; index <= 80; index++)
            {
                Buttons.Add(new ButtonViewModel(index.ToString(), this));
            }
        }

        public bool CanSelectButton() => Buttons.Count(btn => btn.IsSelected) < RequiredSelectedButtonsCount;

        protected override async void OnActivate()
        {
            try
            {
                var progressVm = new ProgressViewModel
                {
                    Message = "Tworzenie bazy danych...",
                    CurrentProgress = 20
                };
                _windowManager.ShowWindow(progressVm);

                _dataService.ImportAll();

                progressVm.Message = "Wyszukiwanie najnowszych losowań w internecie...";
                progressVm.CurrentProgress = 50;

                try
                {
                    var latest = _dataService.GetLatestDraw();
                    var newDatas = await _drawScraper.ScrapeNewestAsync(latest.Added);

                    _dataService.AddData(newDatas);
                }
                catch (DrawParsingException)
                {
                    
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }

                progressVm.Message = "Liczenie najczęściej występujących par...";
                progressVm.CurrentProgress = 80;

                UpdateMostCommmonPairs();
                progressVm.TryClose();
            }
            catch (Exception e)
            {
                MessageBox.Show("Wystąpił bład przy ładowaniu programu.");
                _logger.Error(e);
            }

            base.OnActivate();
        }

        private void UpdateMostCommmonPairs()
        {
            foreach (var pair in _dataService.GetMostCommonPairs())
            {
                var pairDto = new PairDto
                {
                    Pair = pair.Value,
                    Occurences = pair.Occurences,
                    OccurencePercentage = $"{pair.OccurencePercentage:F8}%"
                };
                MostCommonPairs.Add(pairDto);
            }
        }

        public async void ExportToExcel()
        {
            var filePath = RequestSaveFilePath();

            if (string.IsNullOrWhiteSpace(filePath))
                return;

            var progressBarVm = new ProgressViewModel
            {
                Message = "Trwa eksportowanie wszystkich danych do pliku excel. To może potrwać do kilku minut."
            };


            ExportToExcelButtonEnabled = false;

            try
            {
                _windowManager.ShowWindow(progressBarVm);

                var progress = new Progress<Tuple<int, int>>((value) =>
                {
                    progressBarVm.CurrentProgress = (value.Item1 / (double)value.Item2) * 100;
                });

                await _excelExporter.ExportAll(filePath, progress);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Wystąpił błąd przy eksporcie. Proszę się upewnić, że plik {Path.GetFileName(filePath)} nie jest otwarty w innym programie! {Environment.NewLine} {ex.Message}");
                _logger.Error(ex);
            }

            progressBarVm.TryClose();
            ExportToExcelButtonEnabled = true;
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

        public void OnHelpClicked()
        {
            _windowManager.ShowDialog(new HelpViewModel());
        }

        public void AddNewDraw()
        {
            var canAdd = Buttons.Count(b => b.IsSelected) == RequiredSelectedButtonsCount;

            if (!canAdd)
            {
                MessageBox.Show($"Proszę wybrać {RequiredSelectedButtonsCount} liczb");
                return;
            }

            var values = Buttons.Where(btn => btn.IsSelected).Select(btn => int.Parse(btn.ButtonText)).ToArray();
            var pairs = _permutationProvider.GetPermutations(values, 2)
                .Select(p => p.ToArray()[0] + ", " + p.ToArray()[1]).ToArray();

            var data = new Data
            {
                Added = DateTime.Now,
                Values = values,
                Pairs = pairs,
                IsCustom = true
            };

            _dataService.AddData(data);

            foreach (var btn in Buttons)
            {
                btn.IsSelected = false;
            }

            MessageBox.Show($"Dodano nowe losowanie z liczbami: {string.Join(", ", data.Values)}");

            UpdateMostCommmonPairs();
        }
    }
}
