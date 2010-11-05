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
			ComputeCellSpacing(grid);
		}

		private static void OnLayoutUpdated(System.Windows.Controls.Grid grid)
		{
			SetChildrenPositions(grid);
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
				if (HasListeners(grid))
				{
					RemoveListeners(grid);

					RemoveColumnAndRowsDefinitions(grid);
				}
			}
			else
			{
				CreateColumnDefinitions(grid, SplitCols(value));
				SetChildrenPositions(grid);

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
			if (GetColumns(grid) == null)
				return;

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
			UpdateRowDefinitions(grid, rowCount);
		}

		private static void UpdateRowDefinitions(System.Windows.Controls.Grid grid, int rowCount)
		{
			// Add missing
			for (int i = grid.RowDefinitions.Count; i < rowCount; i++)
			{
				RowDefinition def = new RowDefinition();
				def.Height = new GridLength(1, GridUnitType.Auto);

				grid.RowDefinitions.Add(def);
			}

			// Remove if has more than needed
			while (rowCount < grid.RowDefinitions.Count)
			{
				grid.RowDefinitions.RemoveAt(rowCount);
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

		#endregion

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
				double bottom = 0;
				double left = (double) (col > 0 ? spacing : 0);
				double right = 0;

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
