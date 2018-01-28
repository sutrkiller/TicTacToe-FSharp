using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TicTacToe.Core;
using TicTacToe.Desktop.Annotations;
using static TicTacToe.Core.Basic;

namespace TicTacToe.Desktop
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Brush _defaultBacgrkoundBrush = new SolidColorBrush(Colors.Gray);
        private readonly Brush _firstBacgrkoundBrush = new SolidColorBrush(Colors.DeepSkyBlue);
        private readonly Brush _secondBacgrkoundBrush = new SolidColorBrush(Colors.PaleVioletRed);
        private readonly Style _disabledButtonStyle;
        private readonly Style _enabledButtonStyle;
        private const int FieldSize = 40;
        private const int ButtonSize = 30;

        //private int _player = 1;
        //private readonly int[][] _playField;
        //private int _turns;
        //private readonly HashSet<ValueTuple<int, int>> _possibleMoves = new HashSet<(int, int)>();
        //private readonly Dictionary<ValueTuple<int, int>, Button> _buttonsPositions = new Dictionary<(int, int), Button>();
        //public int Turns
        //{
        //    get => _turns;
        //    private set
        //    {
        //        if (value == _turns) return;
        //        _turns = value;
        //        OnPropertyChanged();
        //        OnPropertyChanged(nameof(NextBrush));
        //    }
        //}
        //public Brush NextBrush => _player == 1 ? _firstBacgrkoundBrush : _secondBacgrkoundBrush;

        private Game _game;
        private readonly Dictionary<Position, Button> _buttonPositions = new Dictionary<Position, Button>();

        public int Turns
        {
            get => _game?.Turns ?? 0;
        }
        public Brush NextPlayerBrush => Equals(_game?.CurrentPlayer ?? Player.PlayerOne, Player.PlayerOne) ? _firstBacgrkoundBrush : _secondBacgrkoundBrush;

        public MainWindow()
        {
            InitializeComponent();
            _disabledButtonStyle = FindResource("DisabledButton") as Style;
            _enabledButtonStyle = FindResource("EnabledButton") as Style;
            //_playField = new int[FieldSize][];
            //for (var yIndex = 0; yIndex < FieldSize; yIndex++)
            //{
            //    _playField[yIndex] = new int[FieldSize];
            //}
            //Redraw();

            NewGameFSharp();
        }

        private void NewGameFSharp()
        {
            _game = new Game(FieldSize, FieldSize);
            RedrawGrid();
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
                    var tag = new Position(xIndex, yIndex);
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
                    this._buttonPositions[tag] = button;
                    this.MainScreen.Children.Add(button);
                }
            }

            MainScreen.Height = FieldSize * (ButtonSize + 1);
            MainScreen.Width = FieldSize * (ButtonSize + 1);
        }

        private void HandleMove(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var position = (Position) button.Tag;

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

            _game.PlayMove(position);

            foreach (var newPossibleMoveButton in _game.PossibleMoves.Select(x => _buttonPositions[x]))
            {
                newPossibleMoveButton.Style = _enabledButtonStyle;
            }

            var patterns = Patterns.Winning;

            var watch = Stopwatch.StartNew();
            var match = _game.MatchFirstFlipPlayer(new byte[,] {{2, 1}, {0, 0}});
            var textResult = match.IsNone() ? "None" : $"({match.Value.X},{match.Value.Y})";
            Console.WriteLine($"Comparison result: {textResult} in {watch.ElapsedMilliseconds}ms");

            OnPropertyChanged(nameof(NextPlayerBrush));
            OnPropertyChanged(nameof(Turns));
        }

        //private void Redraw()
        //{

        //    this.MainScreen.Children.Clear();

        //    for (var yIndex = 0; yIndex < FieldSize; yIndex++)
        //    {
        //        for (var xIndex = 0; xIndex < FieldSize; xIndex++)
        //        {
        //            var tag = new ButtonTag(xIndex, yIndex);
        //            var button = new Button
        //            {
        //                Tag = tag,
        //                Height = ButtonSize,
        //                Width = ButtonSize,
        //                Margin = new Thickness(xIndex * (ButtonSize + 1), yIndex * (ButtonSize + 1), 0, 0),
        //                Background = _defaultBacgrkoundBrush,
        //                Focusable = false
        //            };
        //            _possibleMoves.Add(tag.Position);
        //            _buttonsPositions[tag.Position] = button;
        //            button.Click += HandleClick;
        //            this.MainScreen.Children.Add(button);
        //        }
        //    }

        //    MainScreen.Height = FieldSize * (ButtonSize + 1);
        //    MainScreen.Width = FieldSize * (ButtonSize + 1);
        //}

        //private void HandleClick(object sender, RoutedEventArgs e)
        //{
        //    var button = sender as Button;
        //    var tag = button.Tag as ButtonTag;
        //    var (xIndex, yIndex) = tag.Position;

        //    if (!_possibleMoves.Contains(tag.Position)) return;

        //    button.Background = _player == 1 ? _firstBacgrkoundBrush : _secondBacgrkoundBrush;

        //    if (Turns == 0)
        //    {
        //        _possibleMoves.Clear();
        //        foreach (var buttonChild in MainScreen.Children.OfType<Button>())
        //        {
        //            buttonChild.Style = _disabledButtonStyle;
        //        }
        //    }

        //    button.Style = _disabledButtonStyle;
        //    _possibleMoves.Remove(tag.Position);

        //    var newPossibleMoves = WhereEmpty(tag.Neigbours).Except(_possibleMoves);

        //    foreach (var newPosition in newPossibleMoves)
        //    {
        //        _buttonsPositions[newPosition].Style = _enabledButtonStyle;
        //        _possibleMoves.Add(newPosition);
        //    }

        //    _playField[yIndex][xIndex] = _player;

        //    _player = (_player % 2) + 1;
        //    Turns++;

        //    IEnumerable<ValueTuple<int, int>> WhereEmpty(IEnumerable<ValueTuple<int, int>> values) => values.Where(x => _playField[x.Item2][x.Item1] == 0);
        //}


        private class ButtonTag
        {
            private static readonly List<ValueTuple<int, int>> AllDirections = new List<(int, int)>{(-1, -1), (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0)};

            public ValueTuple<int, int> Position { get; }

            public List<ValueTuple<int, int>> Neigbours { get; }

            public ButtonTag(int xIndex, int yIndex)
            {
                Position = (xIndex, yIndex);
                Neigbours = AllDirections.Select((dir) => (xIndex + dir.Item1, yIndex + dir.Item2)).Where(pos => pos.Item1 >= 0 && pos.Item1 < FieldSize && pos.Item2 >= 0 && pos.Item2 < FieldSize).ToList();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
