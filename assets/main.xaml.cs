using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FiveWFiveLGUI
{
    public partial class MainWindow : Window
    {
        static readonly int[] LetterFrequencePosition = { 16, 9, 23, 25, 22, 10, 21, 5, 24, 1, 7, 12, 15, 6, 20, 3, 2, 11, 14, 19, 13, 17, 0, 8, 18, 4 };

        private readonly IProgress<int> progress;

        public MainWindow()
        {
            InitializeComponent();
            progress = new Progress<int>(value => ProgressBar.Value = value);
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private async void ProcessFile_Click(object sender, RoutedEventArgs e)
        {
            string filePath = FilePathTextBox.Text;
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Please select a valid file.");
                return;
            }

            OutputTextBlock.Text = "Processing...";
            ProgressBar.Value = 0;

            await ProcessFileAsync(filePath);
        }

        private async Task ProcessFileAsync(string filePath)
        {
            try
            {
                int wordLength = 5, requiredWordCount = 5;
                Stopwatch stopwatch = Stopwatch.StartNew();

                var bitWords = await Task.Run(() => LoadWords(filePath, wordLength));

                if (bitWords.Count == 0)
                {
                    OutputTextBlock.Text = "No valid words found.";
                    return;
                }

                OutputTextBlock.Text = $"Loaded {bitWords.Count} unique words.";

                int totalCombinations = 0;
                int totalWords = bitWords.Count;
                int processedWords = 0;

                await Task.Run(() =>
                {
                    var keysArray = bitWords.ToArray();
                    Parallel.For(0, keysArray.Length, i =>
                    {
                        int usedBits = keysArray[i];
                        int localTotal = RecursiveFindCombinations(keysArray, usedBits, 1, i - 1, requiredWordCount);
                        Interlocked.Add(ref totalCombinations, localTotal);

                        Interlocked.Increment(ref processedWords);
                        progress.Report((int)((double)processedWords / totalWords * 100));
                    });
                });

                stopwatch.Stop();
                OutputTextBlock.Text += $"\nFound {totalCombinations} valid combinations of {requiredWordCount} words.";
                OutputTextBlock.Text += $"\nTime: {stopwatch.Elapsed.TotalSeconds:F2} seconds.";
            }
            catch (Exception ex)
            {
                OutputTextBlock.Text = $"An error occurred: {ex.Message}";
            }
        }

        static HashSet<int> LoadWords(string filePath, int wordLength)
        {
            var bitWords = new HashSet<int>();
            try
            {
                foreach (var word in File.ReadLines(filePath).Select(line => line.Trim().ToLower()))
                {
                    if (word.Length == wordLength && word.Distinct().Count() == wordLength)
                    {
                        int bits = WordToInt(word);
                        bitWords.Add(bits);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading words: {ex.Message}");
            }
            return bitWords;
        }

        static int WordToInt(string word)
        {
            return word.Aggregate(0, (current, c) => current | (1 << LetterFrequencePosition[c - 'a']));
        }

        static int RecursiveFindCombinations(int[] bitWords, int usedBits, int wordCount, int index, int requiredWordCount)
        {
            if (wordCount == requiredWordCount)
            {
                return 1;
            }
            int total = 0;
            for (int i = index; i >= 0; i--)
            {
                if ((usedBits & bitWords[i]) == 0)
                {
                    total += RecursiveFindCombinations(bitWords, usedBits | bitWords[i], wordCount + 1, i - 1, requiredWordCount);
                }
            }
            return total;
        }
    }
}
