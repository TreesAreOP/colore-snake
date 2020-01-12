using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Colore;
using ColoreColor = Colore.Data.Color;
using KeyboardCustom = Colore.Effects.Keyboard.KeyboardCustom;
using ColoreKey = Colore.Effects.Keyboard.Key;
using Constants = Colore.Effects.Keyboard.KeyboardConstants;
using System.Windows.Threading;
using ColoreSnake.Utils;
using System.Threading.Tasks;

namespace ColoreSnake
{
    public partial class MainWindow : Window
    {
        private struct Position2D
        {
            public static readonly Position2D Left = new Position2D(-1, 0);
            public static readonly Position2D Right = new Position2D(1, 0);
            public static readonly Position2D Up = new Position2D(0, -1);
            public static readonly Position2D Down = new Position2D(0, 1);

            public int x;
            public int y;

            public Position2D(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public static Position2D operator -(Position2D self, Position2D other)
            {
                return new Position2D(self.x - other.x, self.y - other.y);
            }

            public static Position2D operator +(Position2D self, Position2D other)
            {
                return new Position2D(self.x + other.x, self.y + other.y);
            }

            public static Position2D operator /(Position2D self, int other)
            {
                return new Position2D(self.x / other, self.y / other);
            }

            public static bool operator ==(Position2D self, Position2D other)
            {
                return self.x == other.x && self.y == other.y;
            }

            public static bool operator !=(Position2D self, Position2D other)
            {
                return self.x != other.x || self.y != other.y;
            }

            public override bool Equals(object obj)
            {
                if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                return this == (Position2D)obj;
            }

            public override string ToString()
            {
                return "Position2D: [" + this.x + ", " + this.y + "]";
            }
        }

        private bool PlayerDied
        {
            get
            {
                return playerBody.Contains(playerPosition)
                    || playerPosition.x < leftBottomBounds.x || playerPosition.x > rightTopBounds.x
                    || playerPosition.y < rightTopBounds.y || playerPosition.y > leftBottomBounds.y;
            }
        }

        private float fps = 1;
        private IChroma chroma;
        private KeyboardCustom keyboardGrid;

        private Position2D leftBottomBounds = new Position2D(2, 4);
        private Position2D rightTopBounds = new Position2D(12, 1);
        private List<Position2D> freeCells = new List<Position2D>();

        private Position2D targetPosition = new Position2D();
        private Position2D playerPosition = new Position2D();
        private Queue<Position2D> playerBody = new Queue<Position2D>();
        private Position2D currentDirection = Position2D.Right;
        private Position2D lastDirection = Position2D.Right;
        private DispatcherTimer timer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await Initialize();

            timer.Interval = TimeSpan.FromSeconds(1f / fps);
            timer.Tick += UpdateLoop;
            timer.Start();
        }

        private async Task Initialize()
        {
            // get chroma instance
            chroma = await ColoreProvider.CreateNativeAsync();

            // Create the custom Grid
            keyboardGrid = KeyboardCustom.Create();

            // show bounds
            CreateGridBorder(ColoreColor.Yellow);

            // show controls
            keyboardGrid[ColoreKey.Left] = ColoreColor.Blue;
            keyboardGrid[ColoreKey.Right] = ColoreColor.Blue;
            keyboardGrid[ColoreKey.Up] = ColoreColor.Blue;
            keyboardGrid[ColoreKey.Down] = ColoreColor.Blue;

            // set player start position
            currentDirection = lastDirection = Position2D.Right;
            playerPosition = (leftBottomBounds + rightTopBounds) / 2;
            playerBody.Clear();

            // Create a Grid containing all used positions
            freeCells.Clear();
            TraverseGrid((row, col) =>
            {
                var pos = new Position2D(row, col);
                if (pos != playerPosition && pos != targetPosition && !playerBody.Contains(pos))
                {
                    freeCells.Add(pos);
                }
            });

            // Create Target Position
            CreateNextRandomTarget(ColoreColor.Red);

            await chroma.Keyboard.SetCustomAsync(keyboardGrid);
        }

        private async void UpdateLoop(object sender, EventArgs e)
        {
            SetGridColor(playerPosition, ColoreColor.Black);
            playerBody.ToList().ForEach(p => SetGridColor(p, ColoreColor.Black));
            CreateGridBorder(ColoreColor.Yellow);

            MovePlayerInCurrentDirection(
                playerPosition == targetPosition,
                () => CreateNextRandomTarget(ColoreColor.Red));

            if (PlayerDied)
            {
                await Initialize();
            }

            SetGridColor(targetPosition, ColoreColor.Red);
            SetGridColor(playerPosition, ColoreColor.Green);
            playerBody.ToList().ForEach(p => SetGridColor(p, ColoreColor.Green));

            await chroma.Keyboard.SetCustomAsync(keyboardGrid);
        }

        private void MovePlayerInCurrentDirection(bool append = false, Action onAppend = null)
        {
            playerBody.Enqueue(playerPosition);

            playerPosition += lastDirection = currentDirection;
            freeCells.Remove(playerPosition);

            if (append)
            {
                onAppend?.Invoke();
                return;
            }

            if (playerBody.Count > 0)
            {
                freeCells.Add(playerBody.Dequeue());
            }
        }

        private void CreateGridBorder(ColoreColor color)
        {
            // private Position2D leftBottomBounds = new Position2D(2, 4);
            // private Position2D rightTopBounds = new Position2D(13, 1);

            // Top
            for (int i = leftBottomBounds.x; i < rightTopBounds.x; i++)
            {
                SetGridColor(rightTopBounds.y, i, color);
            }

            // Bottom
            for (int i = leftBottomBounds.x; i <= rightTopBounds.x; i++)
            {
                SetGridColor(leftBottomBounds.y, i, color);
            }

            // Left
            for (int i = rightTopBounds.y; i < leftBottomBounds.y; i++)
            {
                SetGridColor(i, leftBottomBounds.x, color);
            }

            // Right
            for (int i = rightTopBounds.y; i <= leftBottomBounds.y; i++)
            {
                SetGridColor(i, rightTopBounds.x, color);
            }

        }

        private void CreateNextRandomTarget(ColoreColor color)
        {
            var rand = new Random();

            targetPosition = freeCells.PopRandomItem();

            SetGridColor(targetPosition, color);
        }

        private void TraverseGrid(Action<int, int> rowCol)
        {
            for (int x = leftBottomBounds.x; x < rightTopBounds.x; x++)
            {
                for (int y = leftBottomBounds.y; y > rightTopBounds.y; y--)
                {
                    rowCol?.Invoke(x, y);
                }
            }
        }

        private void SetGridColor(Position2D targetPosition, ColoreColor color)
        {
           SetGridColor(targetPosition.y, targetPosition.x, color);
        }

        private void SetGridColor(int x, int y, ColoreColor color)
        {
            keyboardGrid[x, y] = color;
        }

        // input
        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            Position2D? newDirection = null;
            switch (e.Key)
            {
                case Key.Right: newDirection = Position2D.Right; break;
                case Key.Left: newDirection = Position2D.Left; break;
                case Key.Up: newDirection = Position2D.Up; break;
                case Key.Down: newDirection = Position2D.Down; break;
            }

            Console.WriteLine("key " + e.Key + "direction " + newDirection);
            if (playerBody.Count == 0 || (newDirection?.x + lastDirection.x != 0
                && newDirection?.y + lastDirection.y != 0))
            {
                currentDirection = newDirection.Value;
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            float.TryParse(((TextBox)sender).Text, out this.fps);
        }
    }
}
