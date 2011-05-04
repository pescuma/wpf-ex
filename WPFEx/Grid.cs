using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace org.pescuma.wpfex
{
	public class Grid : DependencyObject
	{
		#region Listeners

		private static readonly ListenerManager<System.Windows.Controls.Grid> Listeners =
			new ListenerManager<System.Windows.Controls.Grid>();

		static Grid()
		{
			Listeners.LayoutUpdated += grid =>
			                           	{
			                           		SetChildrenPositions(grid);
			                           		CreateRowDefinitions(grid);
			                           		ComputeCellSpacing(grid);
			                           	};
		}

		#endregion

		#region Columns

		private const string DefaultColumnStyle = "Auto";

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

		private static readonly string DoubleRE = @"[0-9]*(\.[0-9]*)?";

		private static readonly Regex RangeRE =
			new Regex(@"\|\s*(" + DoubleRE + @")?\s*:\s*(" + DoubleRE + @")?$");

		private static readonly Regex StarRE = new Regex(@"^" + DoubleRE + @"\*$");

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

			var m = RangeRE.Match(str);
			if (m.Success)
				str = str.Substring(0, m.Index).Trim();

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
			return string.Equals(str, "Auto", StringComparison.InvariantCultureIgnoreCase);
		}

		private static bool IsStar(string str)
		{
			return StarRE.IsMatch(str);
		}

		private class Range
		{
			public double? Min;
			public double? Max;
		}

		private static GridLength ToGridLength(string str, out Range range)
		{
			str = str.Trim();

			range = new Range();
			range.Min = null;
			range.Max = null;

			var m = RangeRE.Match(str);
			if (m.Success)
			{
				var min = m.Groups[1].ToString().Trim();
				if (min != "")
					range.Min = Convert.ToDouble(min, CultureInfo.InvariantCulture);

				var max = m.Groups[3].ToString().Trim();
				if (max != "")
					range.Max = Convert.ToDouble(max, CultureInfo.InvariantCulture);

				str = str.Substring(0, m.Index).Trim();
			}

			var ret = new GridLengthConverter().ConvertFrom(null, CultureInfo.InvariantCulture, str);
			if (ret == null)
				throw new ArgumentException();

			return (GridLength) ret;
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
			else if (IsInt(value))
				return Enumerable.Repeat("Auto", Int32.Parse(value)).ToArray();
			else
				return new[] {value};
		}

		private static void ColumnsPropertyChanged(DependencyObject obj,
		                                           DependencyPropertyChangedEventArgs args)
		{
			System.Windows.Controls.Grid grid = obj as System.Windows.Controls.Grid;
			if (grid == null)
				throw new ArgumentException("Element must be a Grid");

			string value = (string) args.NewValue;

			if (value == null && GetRows(grid) == null)
			{
				if (Listeners.IsListeningTo(grid))
				{
					Listeners.RemoveFrom(grid);

					RemoveColumnAndRowsDefinitions(grid);
				}
			}
			else
			{
				CreateColumnDefinitions(grid, value ?? DefaultColumnStyle);
				SetChildrenPositions(grid);
				CreateRowDefinitions(grid);

				Listeners.AddTo(grid);
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

			if (GetColumns(grid) == null && GetRows(grid) == null)
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
			var colDefs = CreateColumnDefinitions(cols, GetCellSpacing(grid));

			// Remove if has more than needed
			if (colDefs.Count < grid.ColumnDefinitions.Count)
				grid.ColumnDefinitions.RemoveRange(colDefs.Count, grid.ColumnDefinitions.Count - colDefs.Count);

			// Merge existing
			for (int i = 0; i < grid.ColumnDefinitions.Count; i++)
			{
				var current = grid.ColumnDefinitions[i];
				var expected = colDefs[i];

				current.Width = expected.Width;
				current.MinWidth = expected.MinWidth;
				current.MaxWidth = expected.MaxWidth;
			}

			// Add missing
			for (int i = grid.ColumnDefinitions.Count; i < colDefs.Count; i++)
				grid.ColumnDefinitions.Add(colDefs[i]);
		}

		private static List<ColumnDefinition> CreateColumnDefinitions(string cols, int? cellSpacing)
		{
			List<ColumnDefinition> result = new List<ColumnDefinition>();

			foreach (var col in SplitCols(cols))
				result.Add(ToColumnDefinition(col, result.Count > 0 ? cellSpacing : 0));

			return result;
		}

		private static ColumnDefinition ToColumnDefinition(string col, int? cellSpacing)
		{
			ColumnDefinition def = new ColumnDefinition();

			Range range;

			def.Width = ToGridLength(col, out range);

			if (range.Min != null)
				def.MinWidth = (double) range.Min + (cellSpacing ?? 0);
			if (range.Max != null)
				def.MaxWidth = (double) range.Max + (cellSpacing ?? 0);

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
					text = text.Substring(0, block.Length - 3).Trim();
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
			var columns = GetColumns(grid);

			if (value == null && columns == null)
			{
				if (Listeners.IsListeningTo(grid))
				{
					Listeners.RemoveFrom(grid);

					RemoveColumnAndRowsDefinitions(grid);
				}
			}
			else
			{
				if (columns == null)
				{
					CreateColumnDefinitions(grid, DefaultColumnStyle);
					SetChildrenPositions(grid);
				}

				CreateRowDefinitions(grid);

				Listeners.AddTo(grid);
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
						Extends = block.Substring(0, block.Length - 3).Trim();
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
					grid.RowDefinitions.Add(ToRowDefinition(DefaultRowStyle, i > 0 ? GetCellSpacing(grid) : 0));

				// Remove if has more than needed
				if (rowCount < grid.RowDefinitions.Count)
					grid.RowDefinitions.RemoveRange(rowCount, grid.RowDefinitions.Count - rowCount);
			}
			else
			{
				var rowDefs = CreateRowDefinitions(rowCount, rows, GetCellSpacing(grid));

				// Remove if has more than needed
				if (rowDefs.Count < grid.RowDefinitions.Count)
					grid.RowDefinitions.RemoveRange(rowDefs.Count, grid.RowDefinitions.Count - rowDefs.Count);

				// Merge existing
				for (int i = 0; i < grid.RowDefinitions.Count; i++)
				{
					var current = grid.RowDefinitions[i];
					var expected = rowDefs[i];

					if (expected.Height != current.Height)
						current.Height = expected.Height;

					if (expected.MinHeight != current.MinHeight)
						current.MinHeight = expected.MinHeight;

					if (expected.MaxHeight != current.MaxHeight)
						current.MaxHeight = expected.MaxHeight;
				}

				// Add missing
				for (int i = grid.RowDefinitions.Count; i < rowDefs.Count; i++)
					grid.RowDefinitions.Add(rowDefs[i]);
			}
		}

		private static List<RowDefinition> CreateRowDefinitions(int rowCount, string rows,
		                                                        int? cellSpacing)
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
				result.Add(ToRowDefinition(row, result.Count > 0 ? cellSpacing : 0));

			for (int i = 0; i < extensionLines; i++)
				result.Add(ToRowDefinition(cfg.Extends, result.Count > 0 ? cellSpacing : 0));

			foreach (var row in cfg.End)
				result.Add(ToRowDefinition(row, result.Count > 0 ? cellSpacing : 0));

			for (int i = 0; i < afterRows; i++)
				result.Add(ToRowDefinition(DefaultRowStyle, result.Count > 0 ? cellSpacing : 0));

			return result;
		}

		private static RowDefinition ToRowDefinition(string row, int? cellSpacing)
		{
			RowDefinition def = new RowDefinition();

			Range range;
			def.Height = ToGridLength(row, out range);

			if (range.Min != null)
				def.MinHeight = (double) range.Min + (cellSpacing ?? 0);

			if (range.Max != null)
				def.MaxHeight = (double) range.Max + (cellSpacing ?? 0);

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
				if (Listeners.IsListeningTo(grid))
				{
					Listeners.RemoveFrom(grid);

					RemoveCellSpacing(grid);
				}
			}
			else
			{
				ComputeCellSpacing(grid);

				Listeners.AddTo(grid);
			}

			var columns = GetColumns(grid);
			if (columns != null || GetRows(grid) != null)
			{
				CreateColumnDefinitions(grid, columns ?? DefaultColumnStyle);
				CreateRowDefinitions(grid);
			}
		}

		private static void ComputeCellSpacing(System.Windows.Controls.Grid grid)
		{
			if (grid.Children.Count < 1)
				return;

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
