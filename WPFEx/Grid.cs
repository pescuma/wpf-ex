using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace org.pescuma.wpfex
{
    public class Grid : DependencyObject
    {
        private static readonly HashSet<System.Windows.Controls.Grid> GridsListening =
            new HashSet<System.Windows.Controls.Grid>();

        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.RegisterAttached("Columns", typeof (string), typeof (Grid),
                                                new PropertyMetadata("-", ColumnsPropertyChanged),
                                                ValidateColumnsProperty);

        public static void SetColumns(UIElement element, string value)
        {
            element.SetValue(ColumnsProperty, value);
        }

        [AttachedPropertyBrowsableForTypeAttribute(typeof (System.Windows.Controls.Grid))]
        public static string GetColumns(UIElement element)
        {
            return (string) element.GetValue(ColumnsProperty);
        }

        private static bool ValidateColumnsProperty(object obj)
        {
            if (obj as string == null)
                return false;

            string value = (string) obj;

            if (value == "-")
                return true;

            if (value.IndexOf(',') >= 0)
            {
                return value.Split(',').All(ValidateColText);
            }
            else
            {
                return ValidateColText(value);
            }
        }

        private static bool ValidateColText(string str)
        {
            str = str.Trim();

            if (IsStar(str))
                return true;
            if (IsAuto(str))
                return true;
            if (IsInt(str))
                return true;

            return false;
        }

        private static bool IsInt(string str)
        {
            int i;
            return Int32.TryParse(str, out i);
        }

        private static bool IsAuto(string str)
        {
            return string.Equals(str, "Auto", StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool IsStar(string str)
        {
            return str == "*";
        }

        private static string[] SplitCols(string value)
        {
            if (value.IndexOf(',') >= 0)
            {
                string[] cols = value.Split(',');
                for (int i = 0; i < cols.Length; i++)
                    cols[i] = cols[i].Trim();
                return cols;
            }
            else if (IsStar(value))
                return new[] {value};
            else if (IsAuto(value))
                return new[] {value};
            else
                return Enumerable.Repeat("Auto", Int32.Parse(value)).ToArray();
        }

        private static void ColumnsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            System.Windows.Controls.Grid grid = obj as System.Windows.Controls.Grid;
            if (grid == null)
                throw new ArgumentException("Element must be a Grid");

            string value = (string) args.NewValue;
            value = value.Trim();

            if (value == "-")
                // The default, so ignore
                return;

            CreateColumnDefinitions(grid, SplitCols(value));
            SetChildrenPositions(grid);

            AddListeners(grid);
        }

        private static void SetChildrenPositions(System.Windows.Controls.Grid grid)
        {
            if (grid.Children.Count < 1)
                return;

            int numColumns = grid.ColumnDefinitions.Count;
            if (numColumns < 1)
                return;

            int[] occupied = new int[numColumns];

            int col = 0;
            int row = 0;
            foreach (UIElement child in grid.Children)
            {
                System.Windows.Controls.Grid.SetRow(child, row);
                System.Windows.Controls.Grid.SetColumn(child, col);

                int rowSpan = System.Windows.Controls.Grid.GetRowSpan(child);
                int colSpan = System.Windows.Controls.Grid.GetColumnSpan(child);

                // Mark occupied cols
                int lastJ = Math.Min(col + colSpan, numColumns);
                for (int j = col; j < lastJ; j++)
                    occupied[j] = Math.Max(occupied[j], rowSpan);

                // Find next slot
                while (occupied[col] > 0)
                {
                    col++;

                    if (col >= numColumns)
                    {
                        for (int i = 0; i < numColumns; i++)
                            occupied[i] = Math.Max(0, occupied[i] - 1);

                        row++;
                        col = 0;
                    }
                }
            }

            int rowCount = (col == 0 ? row : row + 1);
            CreateMissingRowDefinitions(grid, rowCount);
        }

        private static void CreateMissingRowDefinitions(System.Windows.Controls.Grid grid, int rowCount)
        {
            for (int i = grid.RowDefinitions.Count; i < rowCount; i++)
            {
                RowDefinition def = new RowDefinition();
                def.Height = new GridLength(1, GridUnitType.Auto);

                grid.RowDefinitions.Add(def);
            }
        }

        private static void CreateColumnDefinitions(System.Windows.Controls.Grid grid, string[] cols)
        {
            grid.ColumnDefinitions.Clear();
            foreach (var col in cols)
            {
                ColumnDefinition def = new ColumnDefinition();

                if (col == "*")
                    def.Width = new GridLength(1, GridUnitType.Star);
                else if (string.Equals(col, "Auto", StringComparison.CurrentCultureIgnoreCase))
                    def.Width = new GridLength(1, GridUnitType.Auto);
                else
                    def.Width = new GridLength(Int32.Parse(col), GridUnitType.Pixel);

                grid.ColumnDefinitions.Add(def);
            }
        }

        private static void AddListeners(System.Windows.Controls.Grid grid)
        {
            if (GridsListening.Contains(grid))
                return;
            GridsListening.Add(grid);

            grid.Initialized += delegate { SetChildrenPositions(grid); };
            grid.LayoutUpdated += delegate { SetChildrenPositions(grid); };

            grid.Unloaded += delegate { GridsListening.Remove(grid); };
        }
    }
}