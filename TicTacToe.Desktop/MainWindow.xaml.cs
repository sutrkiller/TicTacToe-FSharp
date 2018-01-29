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
        private readonly Style _disabledButtonStyle;
        private readonly Style _enabledButtonStyle;
        private const int FieldSize = 40;
        private const int ButtonSize = 30;

        private Basic.Game _game;
        private readonly Dictionary<Basic.Position, Button> _buttonPositions = new Dictionary<Basic.Position, Button>();
        private bool _endGameScreenVisible;
        private string _endGameScreenText;

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

            NewGameFSharp();
        }

        private void NewGameFSharp()
        {
            _game = new Basic.Game(FieldSize, FieldSize);
            this._buttonPositions.Clear();
            RedrawGrid();
            EndGameScreenVisible = false;
            OnPropertyChanged(nameof(NextPlayerBrush));
            OnPropertyChanged(nameof(Turns));
        }

        private void RedrawGrid()
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
                    button.Click += HandleMove;
                    _buttonPositions[tag] = button;
                    MainScreen.Children.Add(button);
                }
            }

            MainScreen.Height = FieldSize * (ButtonSize + 1);
            MainScreen.Width = FieldSize * (ButtonSize + 1);
        }

        private void HandleMove(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var position = (Basic.Position) button.Tag;

            if (!_game.CanPlayMove(position)) return;

            if (_game.Turns == 0)
            {
                foreach (var childButton in MainScreen.Children.OfType<Button>())
                {
                    childButton.Style = _disabledButtonStyle;
                }
            }
            else
            {
                button.Style = _disabledButtonStyle;
            }
            button.Background = NextPlayerBrush;

            var lastPossibleMoves = _game.PossibleMoves;
            var result = _game.PlayMove(position);

            if (result.IsWinner || result.IsDraw)
            {
                foreach (var lastPossibleMoveButton in lastPossibleMoves.Select(x => _buttonPositions[x]))
                {
                    lastPossibleMoveButton.Style = _disabledButtonStyle;
                }

                this.EndGameScreenVisible = true;
                this.EndGameScreenText = result.IsDraw ? "It is draw." : (result.TryWinner(out var player) && player.IsPlayerOne ? "Player one won!" : "Player two won!");
            }
            else
            {
                foreach (var newPossibleMoveButton in _game.PossibleMoves.Select(x => _buttonPositions[x]))
                {
                    newPossibleMoveButton.Style = _enabledButtonStyle;
                }
            }

            OnPropertyChanged(nameof(NextPlayerBrush));
            OnPropertyChanged(nameof(Turns));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NewGameButton_OnClick(object sender, RoutedEventArgs e)
        {
            NewGameFSharp();
        }
    }
}
