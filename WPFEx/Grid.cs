// 
// Copyright (c) 2010 Ricardo Pescuma Domenecci
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace org.pescuma.wpfex
{
	public class Grid : DependencyObject
	{
		#region Listeners

		private class Listeners
		{
			public EventHandler Initialized;
			public EventHandler LayoutUpdated;
			public RoutedEventHandler Unloaded;
		}

		private static readonly Dictionary<System.Windows.Controls.Grid, Listeners> GridsListening =
			new Dictionary<System.Windows.Controls.Grid, Listeners>();

		private static void AddListeners(System.Windows.Controls.Grid grid)
		{
			if (HasListeners(grid))
				return;

			Listeners l = new Listeners();
			l.Initialized = delegate { OnGridInitialized(grid); };
			l.LayoutUpdated = delegate { OnLayoutUpdated(grid); };
			l.Unloaded = delegate { RemoveListeners(grid); };

			grid.Initialized += l.Initialized;
			grid.LayoutUpdated += l.LayoutUpdated;
			grid.Unloaded += l.Unloaded;

			GridsListening.Add(grid, l);
		}

		private static void RemoveListeners(System.Windows.Controls.Grid grid)
		{
			if (!HasListeners(grid))
				return;

			Listeners l = GridsListening[grid];

			grid.Initialized -= l.Initialized;
			grid.LayoutUpdated -= l.LayoutUpdated;
			grid.Unloaded -= l.Unloaded;

			GridsListening.Remove(grid);
		}

		private static bool HasListeners(System.Windows.Controls.Grid grid)
		{
			return GridsListening.ContainsKey(grid);
		}

		private static void OnGridInitialized(System.Windows.Controls.Grid grid)
		{
			SetChildrenPositions(grid);
			CreateRowDefinitions(grid);
			ComputeCellSpacing(grid);
		}

		private static void OnLayoutUpdated(System.Windows.Controls.Grid grid)
		{
			SetChildrenPositions(grid);
			CreateRowDefinitions(grid);
			ComputeCellSpacing(grid);
		}

		#endregion

		#region Columns

		public static readonly DependencyProperty ColumnsProperty =
			DependencyProperty.RegisterAttached("Columns", typeof (string), typeof (Grid),
			                                    new PropertyMetadata(null, ColumnsPropertyChanged),
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
			if (obj == null)
				return true;

			if (!(obj is string))
				return false;

			string value = (string) obj;
			value = value.Trim();

			if (value == "")
				return false;

			return value.Split(',').All(ValidateColText);
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
			value = value.Trim();

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

		private static void ColumnsPropertyChanged(DependencyObject obj,
		                                           DependencyPropertyChangedEventArgs args)
		{
			System.Windows.Controls.Grid grid = obj as System.Windows.Controls.Grid;
			if (grid == null)
				throw new ArgumentException("Element must be a Grid");

			string value = (string) args.NewValue;

			if (value == null)
			{
				if (GetRows(grid) == null)
				{
					if (HasListeners(grid))
					{
						RemoveListeners(grid);

						RemoveColumnAndRowsDefinitions(grid);
					}
				}
			}
			else
			{
				CreateColumnDefinitions(grid, value);
				SetChildrenPositions(grid);
				CreateRowDefinitions(grid);

				AddListeners(grid);
			}
		}

		private static void RemoveColumnAndRowsDefinitions(System.Windows.Controls.Grid grid)
		{
			foreach (UIElement child in grid.Children)
			{
				System.Windows.Controls.Grid.SetRow(child, 0);
				System.Windows.Controls.Grid.SetColumn(child, 0);
			}

			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();
		}

		private static void SetChildrenPositions(System.Windows.Controls.Grid grid)
		{
			if (grid.Children.Count < 1)
				return;

			if (GetColumns(grid) == null)
				return;

			int numColumns = grid.ColumnDefinitions.Count;
			if (numColumns < 1)
				return;

			int[] occupied = new int[numColumns];

			int col = 0;
			int row = 0;
			foreach (UIElement child in grid.Children)
			{
				if (row != System.Windows.Controls.Grid.GetRow(child))
					System.Windows.Controls.Grid.SetRow(child, row);

				if (col != System.Windows.Controls.Grid.GetColumn(child))
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
		}

		private static void CreateColumnDefinitions(System.Windows.Controls.Grid grid, string cols)
		{
			var colDefs = CreateColumnDefinitions(cols);

			// Remove if has more than needed
			if (colDefs.Count < grid.ColumnDefinitions.Count)
				grid.ColumnDefinitions.RemoveRange(colDefs.Count, grid.ColumnDefinitions.Count - colDefs.Count);

			// Merge existing
			for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
			{
				var current = grid.ColumnDefinitions[i];
				var expected = colDefs[i];

				if (current.Width != expected.Width)
				{
					grid.ColumnDefinitions.RemoveAt(i);
					grid.ColumnDefinitions.Insert(i, expected);
				}
			}

			// Add missing
			for (int i = grid.ColumnDefinitions.Count; i < colDefs.Count; i++)
				grid.ColumnDefinitions.Add(colDefs[i]);
		}

		private static List<ColumnDefinition> CreateColumnDefinitions(string cols)
		{
			List<ColumnDefinition> result = new List<ColumnDefinition>();

			foreach (var col in SplitCols(cols))
				result.Add(ToColumnDefinition(col.Trim()));

			return result;
		}

		private static ColumnDefinition ToColumnDefinition(string col)
		{
			ColumnDefinition def = new ColumnDefinition();

			if (col == "*")
				def.Width = new GridLength(1, GridUnitType.Star);
			else if (string.Equals(col, "Auto", StringComparison.CurrentCultureIgnoreCase))
				def.Width = new GridLength(1, GridUnitType.Auto);
			else
				def.Width = new GridLength(Int32.Parse(col), GridUnitType.Pixel);

			return def;
		}

		#endregion

		#region Rows

		private const string DefaultRowStyle = "Auto";

		public static readonly DependencyProperty RowsProperty =
			DependencyProperty.RegisterAttached("Rows", typeof (string), typeof (Grid),
			                                    new PropertyMetadata(null, RowsPropertyChanged),
			                                    ValidateRowsProperty);

		public static void SetRows(UIElement element, string value)
		{
			element.SetValue(RowsProperty, value);
		}

		[AttachedPropertyBrowsableForTypeAttribute(typeof (System.Windows.Controls.Grid))]
		public static string GetRows(UIElement element)
		{
			return (string) element.GetValue(RowsProperty);
		}

		private static bool ValidateRowsProperty(object obj)
		{
			if (obj == null)
				return true;

			if (!(obj is string))
				return false;

			string value = (string) obj;
			value = value.Trim();

			if (value == "")
				return false;

			bool foundExtension = false;
			foreach (var block in value.Split(','))
			{
				var text = block.Trim();
				if (text.EndsWith("..."))
				{
					if (foundExtension)
						return false;

					foundExtension = true;
					text = text.Substring(0, block.Length - 3);
				}

				if (!ValidateColText(text))
					return false;
			}

			return true;
		}

		private static void RowsPropertyChanged(DependencyObject obj,
		                                        DependencyPropertyChangedEventArgs args)
		{
			System.Windows.Controls.Grid grid = obj as System.Windows.Controls.Grid;
			if (grid == null)
				throw new ArgumentException("Element must be a Grid");

			string value = (string) args.NewValue;

			if (value == null)
			{
				if (GetColumns(grid) == null)
				{
					if (HasListeners(grid))
					{
						RemoveListeners(grid);

						RemoveColumnAndRowsDefinitions(grid);
					}
				}
			}
			else
			{
				CreateRowDefinitions(grid);

				AddListeners(grid);
			}
		}

		private class RowsConfig
		{
			public readonly List<string> Begin = new List<string>();
			public readonly string Extends;
			public readonly List<string> End = new List<string>();

			public RowsConfig(string def)
			{
				bool foundExtension = false;
				foreach (var block in def.Split(','))
				{
					if (block.EndsWith("..."))
					{
						if (foundExtension)
							throw new InvalidOperationException();

						foundExtension = true;
						Extends = block.Substring(0, block.Length - 3);
					}
					else if (foundExtension)
					{
						End.Add(block);
					}
					else
					{
						Begin.Add(block);
					}
				}
			}

			public int MinRows
			{
				get { return Begin.Count + End.Count; }
			}
		}

		private static void CreateRowDefinitions(System.Windows.Controls.Grid grid)
		{
			if (grid.Children.Count < 1)
				return;

			string rows = GetRows(grid);

			if (rows == null && GetColumns(grid) == null)
				return;

			int rowCount = FindRowCount(grid);

			if (rows == null)
			{
				// Add missing
				for (int i = grid.RowDefinitions.Count; i < rowCount; i++)
					grid.RowDefinitions.Add(ToRowDefinition(DefaultRowStyle));

				// Remove if has more than needed
				if (rowCount < grid.RowDefinitions.Count)
					grid.RowDefinitions.RemoveRange(rowCount, grid.RowDefinitions.Count - rowCount);
			}
			else
			{
				var rowDefs = CreateRowDefinitions(rowCount, rows);

				// Remove if has more than needed
				if (rowDefs.Count < grid.RowDefinitions.Count)
					grid.RowDefinitions.RemoveRange(rowDefs.Count, grid.RowDefinitions.Count - rowDefs.Count);

				// Merge existing
				for (int i = 0; i < grid.RowDefinitions.Count; i++)
				{
					var current = grid.RowDefinitions[i];
					var expected = rowDefs[i];

					if (current.Height != expected.Height)
					{
						grid.RowDefinitions.RemoveAt(i);
						grid.RowDefinitions.Insert(i, expected);
					}
				}

				// Add missing
				for (int i = grid.RowDefinitions.Count; i < rowDefs.Count; i++)
					grid.RowDefinitions.Add(rowDefs[i]);
			}
		}

		private static List<RowDefinition> CreateRowDefinitions(int rowCount, string rows)
		{
			List<RowDefinition> result = new List<RowDefinition>();

			RowsConfig cfg = new RowsConfig(rows);

			int extensionLines = Math.Max(0, rowCount - cfg.MinRows);
			int afterRows = 0;
			if (cfg.Extends == null)
			{
				afterRows = extensionLines;
				extensionLines = 0;
			}

			foreach (var row in cfg.Begin)
				result.Add(ToRowDefinition(row));

			for (int i = 0; i < extensionLines; i++)
				result.Add(ToRowDefinition(cfg.Extends));

			foreach (var row in cfg.End)
				result.Add(ToRowDefinition(row));

			for (int i = 0; i < afterRows; i++)
				result.Add(ToRowDefinition(DefaultRowStyle));

			return result;
		}

		private static RowDefinition ToRowDefinition(string row)
		{
			RowDefinition def = new RowDefinition();

			if (row == "*")
				def.Height = new GridLength(1, GridUnitType.Star);
			else if (string.Equals(row, "Auto", StringComparison.CurrentCultureIgnoreCase))
				def.Height = new GridLength(1, GridUnitType.Auto);
			else
				def.Height = new GridLength(Int32.Parse(row), GridUnitType.Pixel);

			return def;
		}

		private static int FindRowCount(System.Windows.Controls.Grid grid)
		{
			int numRows = 0;
			foreach (UIElement child in grid.Children)
			{
				int row = System.Windows.Controls.Grid.GetRow(child);
				int rowSpan = System.Windows.Controls.Grid.GetRowSpan(child);

				numRows = Math.Max(numRows, row + rowSpan);
			}
			return numRows;
		}

		#endregion Rows

		#region CellSpacing

		public static readonly DependencyProperty CellSpacingProperty =
			DependencyProperty.RegisterAttached("CellSpacing", typeof (int?), typeof (Grid),
			                                    new PropertyMetadata(null, CellSpacingPropertyChanged),
			                                    ValidateCellSpacingProperty);

		public static void SetCellSpacing(UIElement element, int? value)
		{
			element.SetValue(CellSpacingProperty, value);
		}

		[AttachedPropertyBrowsableForTypeAttribute(typeof (System.Windows.Controls.Grid))]
		public static int? GetCellSpacing(UIElement element)
		{
			return (int?) element.GetValue(CellSpacingProperty);
		}

		private static bool ValidateCellSpacingProperty(object obj)
		{
			if (obj == null)
				return true;

			if (!(obj is int))
				return false;

			return (int) obj >= 0;
		}

		private static void CellSpacingPropertyChanged(DependencyObject obj,
		                                               DependencyPropertyChangedEventArgs args)
		{
			System.Windows.Controls.Grid grid = obj as System.Windows.Controls.Grid;
			if (grid == null)
				throw new ArgumentException("Element must be a Grid");

			if (args.NewValue == null)
			{
				if (HasListeners(grid))
				{
					RemoveListeners(grid);

					RemoveCellSpacing(grid);
				}
			}
			else
			{
				ComputeCellSpacing(grid);

				AddListeners(grid);
			}
		}

		private static void ComputeCellSpacing(System.Windows.Controls.Grid grid)
		{
			int? spacing = GetCellSpacing(grid);
			if (spacing == null || spacing < 0)
				return;

			foreach (UIElement child in grid.Children)
			{
				if (!(child is FrameworkElement))
					// TODO
					continue;

				FrameworkElement el = (FrameworkElement) child;

				int row = System.Windows.Controls.Grid.GetRow(el);
				int col = System.Windows.Controls.Grid.GetColumn(el);

				double top = (double) (row > 0 ? spacing : 0);
				const double bottom = 0;
				double left = (double) (col > 0 ? spacing : 0);
				const double right = 0;

				el.Margin = new Thickness(left, top, right, bottom);
			}
		}

		private static void RemoveCellSpacing(System.Windows.Controls.Grid grid)
		{
			foreach (UIElement child in grid.Children)
			{
				if (!(child is FrameworkElement))
					// TODO
					continue;

				FrameworkElement el = (FrameworkElement) child;

				el.Margin = new Thickness(0);
			}
		}

		#endregion
	}
}
