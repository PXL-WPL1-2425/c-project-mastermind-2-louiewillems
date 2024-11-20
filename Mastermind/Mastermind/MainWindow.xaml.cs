using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Mastermind
{
    public partial class MainWindow : Window
    {

        private int attempts = 0;
        private const int maxAttempts = 10;
        private const int timerMaxCount = 20;
        private bool isCorrectGuess = false;
        private DispatcherTimer? _timer;
        private int _timerCount = 0;

        private List<(string name, SolidColorBrush color)> selectedColors = new List<(string name, SolidColorBrush color)>();
        private readonly Dictionary<string, SolidColorBrush> _colorOptions = new Dictionary<string, SolidColorBrush>()
        {
            { "Red", Brushes.Red },
            { "Orange", Brushes.Orange },
            { "Yellow", Brushes.Yellow },
            { "White", Brushes.White },
            { "Green", Brushes.Green },
            { "Blue", Brushes.Blue }
        };
        private readonly List<Label> _labels = new List<Label>();
        private readonly List<ComboBox> _comboBoxes = new List<ComboBox>();

        public MainWindow()
        {
            InitializeComponent();
            InitGame();
        }

        private void validateButton_Click(object sender, RoutedEventArgs e)
        {
            ClearGame(onlyLabels: true);

            if (isCorrectGuess)
            {
                //generate new game
                selectedColors = GenerateRandomColorCodes();
                attempts = 0;
            }

            if (selectedColors.Any() && selectedColors.Count == 4)
            {
                string selectedColorString = string.Join(',', selectedColors.Select(x => x.name));

                ControlColors(selectedColors.Select(x => x.name).ToArray());

                if (!isCorrectGuess)
                {
                    if (attempts >= maxAttempts)
                    {
                        EndGame(isVictory: false);
                    }
                    else
                    {
                        attempts++;
                        StartCountdown();
                    }
                    mainWindow.Title = $"Poging {attempts}";
                }
                else
                {
                    EndGame(isVictory: true);
                }
            }
        }
        private void InitGame()
        {
            attempts = 0;
            mainWindow.Title = "Mastermind";
            selectedColors = GenerateRandomColorCodes();
            _comboBoxes.AddRange(new List<ComboBox>() { chooseCombobox1, chooseCombobox2, chooseCombobox3, chooseCombobox4 });
            _labels.AddRange(new List<Label>() { chooseLabel1, chooseLabel2, chooseLabel3, chooseLabel4 });

            for (int i = 0; i < _comboBoxes.Count(); i++)
            {
                for (int j = 0; j < _colorOptions.Count; j++)
                {
                    _comboBoxes[i].Items.Add(_colorOptions.ElementAt(j).Key);
                }

                _comboBoxes[i].SelectionChanged += OnDropdownSelection;
            }

            StartCountdown();
        }

        /// <summary>
        /// The player has only 10 seconds to complete one phase. The timer starts at 1 and ends at 10
        /// </summary>
        private void StartCountdown()
        {
            _timerCount = 1;
            _timer?.Stop();
            _timer = new DispatcherTimer();
            _timer.Tick += AttemptFinishedTimer;
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Start();
        }

        /// <summary>
        /// When the running timer reaches 10, the attempt will increase. After, the coundown will start again.
        /// </summary>
        private void StopCountdown()
        {
            _timerCount++;
            if (_timerCount == timerMaxCount)
            {
                attempts++;
                mainWindow.Title = $"Poging {attempts}";

                if (attempts == maxAttempts)
                {
                    EndGame(isVictory: false);
                }
                else
                {
                    StartCountdown();
                }
            }
        }

        private void AttemptFinishedTimer(object? sender, EventArgs e)
        {
            StopCountdown();
        }

        private List<(string name, SolidColorBrush color)> GenerateRandomColorCodes()
        {
            List<(string, SolidColorBrush)> selectedOptions = new List<(string, SolidColorBrush)>();

            var rand = new Random();
            for (int i = 0; i < 4; i++)
            {
                if (_colorOptions.ElementAt(rand.Next(0, _colorOptions.Count()))
                    is KeyValuePair<string, SolidColorBrush> keyPair)
                {
                    selectedOptions.Add((keyPair.Key, keyPair.Value));
                }
            }
            return selectedOptions;
        }

        private void OnDropdownSelection(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (_labels.FirstOrDefault(x => x.Name.EndsWith(comboBox.Name.Last())) is Label foundLabel)
                {
                    if (_colorOptions.FirstOrDefault(x => x.Key == comboBox.SelectedValue.ToString())
                            is (string name, SolidColorBrush color) foundColor)
                    {
                        foundLabel.Background = foundColor.Value;
                        foundLabel.BorderThickness = new Thickness(0.3);
                        foundLabel.BorderBrush = Brushes.Gray;
                    }
                }
            }

        }

        private void ControlColors(string[] correctColors)
        {
            if (_comboBoxes.Any(x => x.SelectedValue == null))
            {
                MessageBox.Show("Some values are not selected", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            int boxIndex = 0;
            int correctCount = 0;

            (Brush mainColor, bool isCorrectColor, bool isCorrectPosition)[] historyEntry
                = new (Brush mainColor, bool isCorrectColor, bool isCorrectPosition)[4];

            _comboBoxes.ForEach(box =>
            {
                if (box.SelectedValue is string value)
                {
                    (Brush mainColor, bool isCorrectColor, bool isCorrectPosition) item
                            = new(_colorOptions[value], false, false);
                    if (correctColors.Contains(value))
                    {
                        item.isCorrectColor = true;
                        if (value.Equals(correctColors[boxIndex]))
                        {
                            correctCount++;
                        }
                    }
                    historyEntry[boxIndex] = item;

                }
                boxIndex++;
            });


            AddToHistory(historyEntry);

            if (correctCount == _comboBoxes.Count)
            {
                isCorrectGuess = true;
            }

        }
        private void ClearGame(bool onlyLabels = false)
        {
            //clear labels
            _labels.ForEach(label =>
            {
                label.BorderThickness = new Thickness(0.3);
                label.BorderBrush = Brushes.Gray;

                if (!onlyLabels)
                {
                    label.BorderThickness = new Thickness(0);
                    label.BorderBrush = null;
                    label.Background = null;
                }
            });

            //clear boxes
            if (!onlyLabels)
            {
                _comboBoxes.ForEach(box =>
                {
                    box.SelectedValue = null;
                });
            }

            //mainWindow.Title = "Mastermind";
        }

        private void mainWindow_KeyDown(object sender, KeyEventArgs e)
        {
#if DEBUG
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) &&
                 e.Key == Key.F12
                 )
            {
                ToggleDebug();
            }
#endif
        }

        /// <summary>
        /// Check the color code in debug mode
        /// </summary>
        private void ToggleDebug()
        {
            string selectedColorString = string.Join(',', selectedColors.Select(x => x.name));
            debugTextBox.Text = $"Generated colorcode {selectedColorString}";
            debugTextBox.Visibility = (debugTextBox.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void EndGame(bool isVictory)
        {
            if (isVictory)
            {
                mainWindow.Title = $"Correct!!";
            }
            else
            {
            }
            _timer?.Stop();
        }

        private void AddToHistory((Brush mainColor, bool isCorrectColor, bool isCorrectPosition)[] historyEntry)
        {
            //    foundLabel.BorderThickness = new Thickness(3);
            //    foundLabel.BorderBrush = Brushes.Wheat;
            //    foundLabel.BorderBrush = Brushes.DarkRed;


            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(40) });

            for (int i = 0; i < 4; i++)
            {
                Ellipse circle = new Ellipse();
                circle.Width = 35;
                circle.Height = 35;
                circle.Fill = historyEntry[i].mainColor;
                circle.HorizontalAlignment = HorizontalAlignment.Center;
                circle.VerticalAlignment = VerticalAlignment.Center;
                circle.SetValue(Grid.ColumnProperty, i);

                if (historyEntry[i].isCorrectPosition)
                {
                    circle.StrokeThickness = 2;
                    circle.Stroke = Brushes.DarkRed;
                }
                else if (historyEntry[i].isCorrectColor)
                {
                    circle.StrokeThickness = 2;
                    circle.Stroke = Brushes.Wheat;
                }

                grid.Children.Add(circle);
            }

            historyStackPanel.Children.Add(grid);
        }

    }
}