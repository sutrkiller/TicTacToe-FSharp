using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TicTacToe.Desktop.Annotations;
using TicTacToe.Core;

namespace TicTacToe.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly Brush _defaultBacgrkoundBrush = new SolidColorBrush(Colors.Gray);
        private readonly Brush _firstBacgrkoundBrush = new SolidColorBrush(Colors.DeepSkyBlue);
        private readonly Brush _secondBacgrkoundBrush = new SolidColorBrush(Colors.PaleVioletRed);
        private readonly Brush _winnerBrush = new SolidColorBrush(Colors.Goldenrod);
        private readonly Style _disabledButtonStyle;
        private readonly Style _enabledButtonStyle;
        private const int FieldSize = 10;
        private const int ButtonSize = 30;

        private Basic.Game _game;
        private readonly Dictionary<Basic.Position, Button> _buttonPositions = new Dictionary<Basic.Position, Button>();
        private bool _endGameScreenVisible;
        private string _endGameScreenText;
        public event PropertyChangedEventHandler PropertyChanged;

        public delegate void ComputerMoveHandler();
        public event ComputerMoveHandler ComputerTurn;

        public bool ComputerPlayerEnabled { get; set; } = true;
        public int Turns => _game?.Turns ?? 0;
        public Brush NextPlayerBrush => Equals(_game?.CurrentPlayer ?? Basic.Player.PlayerOne, Basic.Player.PlayerOne) ? _firstBacgrkoundBrush : _secondBacgrkoundBrush;

        public bool EndGameScreenVisible
        {
            get => _endGameScreenVisible;
            set
            {
                if (value == _endGameScreenVisible) return;
                _endGameScreenVisible = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayGridVisible));
            }
        }

        public bool PlayGridVisible => !EndGameScreenVisible;

        public string EndGameScreenText
        {
            get => _endGameScreenText;
            set
            {
                if (value == _endGameScreenText) return;
                _endGameScreenText = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            _disabledButtonStyle = FindResource("DisabledButton") as Style;
            _enabledButtonStyle = FindResource("EnabledButton") as Style;
            ComputerTurn += HandleComputerPlayer;

            InitializeNewGame();

        }

        private void InitializeNewGame()
        {
            _game = new Basic.Game(FieldSize, FieldSize);
            this._buttonPositions.Clear();
            InitializeGrid();
            EndGameScreenVisible = false;
            OnPropertyChanged(nameof(NextPlayerBrush));
            OnPropertyChanged(nameof(Turns));
        }

        private void InitializeGrid()
        {
            this.MainScreen.Children.Clear();

            for (var yIndex = 0; yIndex < FieldSize; yIndex++)
            {
                for (var xIndex = 0; xIndex < FieldSize; xIndex++)
                {
                    var tag = new Basic.Position(xIndex, yIndex);
                    var button = new Button
                    {
                        Tag = tag,
                        Height = ButtonSize,
                        Width = ButtonSize,
                        Margin = new Thickness(xIndex * (ButtonSize + 1), yIndex * (ButtonSize + 1), 0, 0),
                        Background = _defaultBacgrkoundBrush,
                        Focusable = false
                    };
                    button.Click += HandleHumanPlayer;
                    _buttonPositions[tag] = button;
                    MainScreen.Children.Add(button);
                }
            }

            MainScreen.Height = FieldSize * (ButtonSize + 1);
            MainScreen.Width = FieldSize * (ButtonSize + 1);
        }

        private void HandleHumanPlayer(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var position = (Basic.Position)button.Tag;

            BlockUserInput();
            HandleMove(position);

            if (ComputerPlayerEnabled)
            {
                OnComputerTurn();
            }
            else
            {
                EnableUserInput();
            }
        }

        private void HandleComputerPlayer()
        {
            //TODO async

            EnableUserInput();
        }

        private void BlockUserInput()
        {
            foreach (var childButton in _game.PossibleMoves.Select(x => _buttonPositions[x]))
            {
                childButton.Style = _disabledButtonStyle;
                childButton.Click -= HandleHumanPlayer;
            }
        }

        private void EnableUserInput()
        {
            foreach (var childButton in _game.PossibleMoves.Select(x => _buttonPositions[x]))
            {
                childButton.Style = _enabledButtonStyle;
                childButton.Click += HandleHumanPlayer;
            }
        }

        private void HandleMove(Basic.Position move)
        {
            var button = _buttonPositions.TryGetValue(move, out var tmpButton)
                ? tmpButton
                : throw new ArgumentOutOfRangeException($"Incorrect position ({move.X},{move.Y})", nameof(move));
            if (!_game.CanPlayMove(move)) return;

            button.Background = NextPlayerBrush;

            var result = _game.PlayMove(move);
            HandleTurnEnd(result);
        }

        private void HandleTurnEnd(Basic.TurnResult turnResult)
        {
            if (turnResult.IsWinner || turnResult.IsDraw)
            {
                //this.EndGameScreenVisible = true;
                if (turnResult.IsDraw)
                {
                    this.EndGameScreenText = "It is draw.";
                }
                else
                {
                    turnResult.TryWinner(out var player, out var positions);
                    this.EndGameScreenText = player.IsPlayerOne ? "Player one won!" : "Player two won!";

                    foreach (var button in positions.Select(x => _buttonPositions[x]))
                    {
                        button.Background = _winnerBrush;
                    }
                }
            }

            OnPropertyChanged(nameof(NextPlayerBrush));
            OnPropertyChanged(nameof(Turns));
        }

        private void NewGameButton_OnClick(object sender, RoutedEventArgs e)
        {
            InitializeNewGame();
        }

        protected virtual void OnComputerTurn()
        {
            ComputerTurn?.Invoke();
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
