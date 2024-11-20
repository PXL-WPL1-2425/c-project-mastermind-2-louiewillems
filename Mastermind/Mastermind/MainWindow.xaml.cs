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
        private bool isCorrectGuess = false;
        private int gamePoints = 100;
        private int attempts = 0;
        private const int maxAttempts = 10;
        private DispatcherTimer? _timer;
        private int _timerCount = 0;
        private const int timerMaxCount = 10;

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
            StartGame();
        }

        private void validateButton_Click(object sender, RoutedEventArgs e)
        {
            if (isCorrectGuess || attempts >= maxAttempts)
                return;

            attempts++;
            if (selectedColors.Any() && selectedColors.Count == 4)
            {
                ControlColors(selectedColors.Select(x => x.name).ToArray());

                resultLabel.Content = $"POGING: {attempts} / 10\t SCORE: {gamePoints}";

                if (!isCorrectGuess)
                {
                    if (attempts >= maxAttempts)
                    {
                        EndGame(isVictory: false);
                    }
                    else
                    {
                        StartCountdown();
                    }
                }
                else
                {
                    EndGame(isVictory: true);
                }
            }
        }

        private void StartGame()
        {
            attempts = 0;
            gamePoints = 100;
            selectedColors = GenerateRandomColorCodes();
            resultLabel.Content = $"POGING: {attempts} / 10\t SCORE: {gamePoints}";
            historyStackPanel.Children.Clear();

            if (!_comboBoxes.Any())
            {
                //init combobox/labels
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
            }
            //clear labels
            _labels.ForEach(label =>
            {
                label.BorderBrush = null;
                label.Background = null;
            });

            //clear boxes
            _comboBoxes.ForEach(box =>
            {
                box.SelectedValue = null;
            });

            StartCountdown();
        }

        private void AttemptFinishedTimer(object? sender, EventArgs e)
        {
            if (attempts >= maxAttempts)
                EndGame(isVictory: false);

            StopCountdown();
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
                resultLabel.Content = $"POGING: {attempts} / 10\t SCORE: {gamePoints}";

                if (attempts >= maxAttempts)
                {
                    EndGame(isVictory: false);
                }
                else
                {
                    StartCountdown();
                }
            }
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
            if (sender is ComboBox comboBox && comboBox.SelectedValue is string value)
            {
                if (_labels.FirstOrDefault(x => x.Name.EndsWith(comboBox.Name.Last())) is Label foundLabel)
                {
                    if (_colorOptions.FirstOrDefault(x => x.Key == value)
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
                    int penaltyPoints = 2;
                    (Brush mainColor, bool isCorrectColor, bool isCorrectPosition) item
                            = new(_colorOptions[value], false, false);
                    if (correctColors.Contains(value))
                    {
                        item.isCorrectColor = true;
                        penaltyPoints = 1;

                        if (value.Equals(correctColors[boxIndex]))
                        {
                            item.isCorrectPosition = true;
                            penaltyPoints = 0;
                            correctCount++;
                        }
                    }
                    historyEntry[boxIndex] = item;
                    gamePoints -= penaltyPoints;

                }
                boxIndex++;
            });


            AddToHistory(historyEntry);

            if (correctCount == _comboBoxes.Count)
            {
                isCorrectGuess = true;
            }

        }
        private void AddToHistory((Brush mainColor, bool isCorrectColor, bool isCorrectPosition)[] historyEntry)
        {

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
        
        private void EndGame(bool isVictory)
        {
            _timer?.Stop();

            string title = "YOU LOOSE";
            string message = $"You failed!! De correcte code was {string.Join(' ', selectedColors.Select(x => x.name))}. Nog een proberen?";
            MessageBoxImage icon = MessageBoxImage.Question;

            if (isVictory)
            {
                title = "WINNER";
                message = $"Code is gekraakt in {attempts} pogingen! Wil je nog een proberen?";
                icon = MessageBoxImage.Information;
            }

            if (MessageBox.Show(message, title, MessageBoxButton.YesNo, icon) == MessageBoxResult.Yes)
            {
                StartGame();
            }
            else
            {
                ExitApp();
            }
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
        private void ExitApp()
        {
            Environment.Exit(0);
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Wilt u het spel vroegtijd beeindigen?", $"poging {attempts}/10", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                e.Cancel = true;
        }
    }
}