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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace org.pescuma.wpfex
{
	public class WrapPanel : DependencyObject
	{
		#region Listeners

		private static readonly ListenerManager<System.Windows.Controls.WrapPanel> Listeners =
			new ListenerManager<System.Windows.Controls.WrapPanel>();

		static WrapPanel()
		{
			Listeners.LayoutUpdated += wrapPanel => ComputeCellSpacing(wrapPanel);
		}

		#endregion

		#region CellSpacing

		public static readonly DependencyProperty CellSpacingProperty =
			DependencyProperty.RegisterAttached("CellSpacing", typeof (int?), typeof (WrapPanel),
			                                    new PropertyMetadata(null, CellSpacingPropertyChanged),
			                                    ValidateCellSpacingProperty);

		public static void SetCellSpacing(UIElement element, int? value)
		{
			element.SetValue(CellSpacingProperty, value);
		}

		[AttachedPropertyBrowsableForTypeAttribute(typeof (System.Windows.Controls.WrapPanel))]
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
			System.Windows.Controls.WrapPanel wrapPanel = obj as System.Windows.Controls.WrapPanel;
			if (wrapPanel == null)
				throw new ArgumentException("Element must be a WrapPanel");

			if (args.NewValue == null)
			{
				if (GetJustify(wrapPanel))
				{
					ComputeCellSpacing(wrapPanel, true);
				}
				else if (Listeners.IsListeningTo(wrapPanel))
				{
					Listeners.RemoveFrom(wrapPanel);

					RemoveCellSpacing(wrapPanel);
				}
			}
			else
			{
				ComputeCellSpacing(wrapPanel);

				Listeners.AddTo(wrapPanel);
			}
		}

		#endregion

		#region Justify

		public static readonly DependencyProperty JustifyProperty =
			DependencyProperty.RegisterAttached("Justify", typeof (bool), typeof (WrapPanel),
			                                    new PropertyMetadata(false, JustifyPropertyChanged),
			                                    ValidateJustifyProperty);

		public static void SetJustify(UIElement element, bool value)
		{
			element.SetValue(JustifyProperty, value);
		}

		[AttachedPropertyBrowsableForTypeAttribute(typeof (System.Windows.Controls.WrapPanel))]
		public static bool GetJustify(UIElement element)
		{
			return (bool) element.GetValue(JustifyProperty);
		}

		private static bool ValidateJustifyProperty(object obj)
		{
			if (obj == null)
				return true;

			if (!(obj is bool))
				return false;

			return true;
		}

		private static void JustifyPropertyChanged(DependencyObject obj,
		                                           DependencyPropertyChangedEventArgs args)
		{
			System.Windows.Controls.WrapPanel wrapPanel = obj as System.Windows.Controls.WrapPanel;
			if (wrapPanel == null)
				throw new ArgumentException("Element must be a WrapPanel");

			if ((bool) args.NewValue == false)
			{
				if (GetCellSpacing(wrapPanel) != null)
				{
					ComputeCellSpacing(wrapPanel);
				}
				else if (Listeners.IsListeningTo(wrapPanel))
				{
					Listeners.RemoveFrom(wrapPanel);

					RemoveCellSpacing(wrapPanel);
				}
			}
			else
			{
				ComputeCellSpacing(wrapPanel);

				Listeners.AddTo(wrapPanel);
			}
		}

		#endregion

		private static void ComputeCellSpacing(System.Windows.Controls.WrapPanel wrapPanel,
		                                       bool forceZeroSpacing = false)
		{
			int? aSpacing = forceZeroSpacing ? 0 : GetCellSpacing(wrapPanel);
			bool justify = GetJustify(wrapPanel);

			if (aSpacing == null && !justify)
				return;

			int spacing = Math.Max(0, aSpacing ?? 0);

			List<FrameworkElement> controls = FilterValidElements(wrapPanel);
			if (controls.Count < 1)
				return;

			var wrapPanelSize = wrapPanel.RenderSize;
			if (wrapPanelSize.Width == 0 || wrapPanelSize.Height == 0)
				return;

			// We have to do this twice to avoid setting a margin when it did not change.
			// This is to avoid endless re-layouts
			List<Thickness> margins = new List<Thickness>();
			List<int> newLines = new List<int>();

			if (wrapPanel.Orientation == Orientation.Horizontal)
			{
				ComputeMarginsForHorizontal(margins, newLines, controls, wrapPanelSize, spacing);

				if (justify)
					JustfyHorizontal(margins, newLines, controls, wrapPanelSize);

				if (aSpacing == null)
					KeepMargins(margins, controls, false, true, false, true);
			}
			else if (wrapPanel.Orientation == Orientation.Vertical)
			{
				ComputeMarginsForVertical(margins, newLines, controls, wrapPanelSize, spacing);

				if (justify)
					JustfyVertical(margins, newLines, controls, wrapPanelSize);

				if (aSpacing == null)
					KeepMargins(margins, controls, true, false, true, false);
			}
			else
			{
				return;
			}

			int i = 0;
			foreach (UIElement child in wrapPanel.Children)
			{
				if (!(child is FrameworkElement))
					// TODO
					continue;

				var el = (FrameworkElement) child;

				SetMargin(el, margins[i]);

				i++;
			}
		}

		private static void KeepMargins(List<Thickness> margins, List<FrameworkElement> controls,
		                                bool left, bool top, bool right, bool bottom)
		{
			for (int i = 0; i < margins.Count; i++)
			{
				var margin = margins[i];
				var control = controls[i];

				margins[i] = new Thickness(left ? control.Margin.Left : margin.Left,
				                           top ? control.Margin.Top : margin.Top,
				                           right ? control.Margin.Right : margin.Right,
				                           bottom ? control.Margin.Bottom : margin.Bottom);
			}
		}

		private static List<FrameworkElement> FilterValidElements(
			System.Windows.Controls.WrapPanel wrapPanel)
		{
			List<FrameworkElement> result = new List<FrameworkElement>();

			foreach (UIElement child in wrapPanel.Children)
			{
				if (!(child is FrameworkElement))
					// TODO
					continue;

				result.Add((FrameworkElement) child);
			}

			return result;
		}

		private static void ComputeMarginsForVertical(List<Thickness> margins, List<int> newLines,
		                                              List<FrameworkElement> controls, Size wrapPanelSize,
		                                              int spacing)
		{
			var height = wrapPanelSize.Height;

			double y = 0;
			bool first = true;
			int col = 0;

			newLines.Add(0);

			for (int i = 0; i < controls.Count; i++)
			{
				var el = controls[i];
				var elHeight = LayoutInformation.GetLayoutSlot(el).Height - el.Margin.Top - el.Margin.Bottom;

				if (first)
				{
					margins.Add(new Thickness(0, 0, 0, 0));
				}
				else if (y + spacing + elHeight > height)
				{
					var leftOver = height - y;
					if (leftOver >= elHeight)
					{
						Thickness last = margins[margins.Count - 1];
						margins.RemoveAt(margins.Count - 1);
						margins.Add(new Thickness(last.Left, last.Top, 0,
						                          leftOver - elHeight + Math.Min(1, elHeight/2)));
					}

					margins.Add(new Thickness(spacing, 0, 0, 0));

					newLines.Add(i);
					col++;
					first = true;
					y = 0;
				}
				else
				{
					margins.Add(new Thickness((col > 0 ? spacing : 0), spacing, 0, 0));
				}

				if (!first)
					y += spacing;
				y += elHeight;

				first = false;
			}

			newLines.Add(controls.Count);
		}

		private static void JustfyVertical(List<Thickness> margins, List<int> newLines,
		                                   List<FrameworkElement> controls, Size wrapPanelSize)
		{
			for (int i = 0; i < newLines.Count - 1; i++)
			{
				int start = newLines[i];
				int end = newLines[i];

				if (end == start + 1)
					continue;

				double toSpread = wrapPanelSize.Height;

				for (int j = start; j < end; j++)
				{
					var el = controls[i];
					var elHeight = LayoutInformation.GetLayoutSlot(el).Height - el.Margin.Top - el.Margin.Bottom;

					toSpread -= elHeight + margins[j].Top;
				}

				toSpread /= end - start - 1;

				for (int j = start + 1; j < end; j++)
				{
					margins[j] = new Thickness(margins[j].Left, margins[j].Top + toSpread, 0, 0);
				}
			}
		}

		private static void ComputeMarginsForHorizontal(List<Thickness> margins, List<int> newLines,
		                                                List<FrameworkElement> controls,
		                                                Size wrapPanelSize, int spacing)
		{
			var width = wrapPanelSize.Width;

			double x = 0;
			bool first = true;
			int row = 0;

			newLines.Add(0);

			for (int i = 0; i < controls.Count; i++)
			{
				var el = controls[i];
				var elWidth = LayoutInformation.GetLayoutSlot(el).Width - el.Margin.Left - el.Margin.Right;

				if (first)
				{
					margins.Add(new Thickness(0, 0, 0, 0));
				}
				else if (x + spacing + elWidth > width)
				{
					var leftOver = width - x;
					if (leftOver >= elWidth)
					{
						Thickness last = margins[margins.Count - 1];
						margins.RemoveAt(margins.Count - 1);
						margins.Add(new Thickness(last.Left, last.Top, leftOver - elWidth + Math.Min(1, elWidth/2), 0));
					}

					margins.Add(new Thickness(0, spacing, 0, 0));

					newLines.Add(i);
					row++;
					first = true;
					x = 0;
				}
				else
				{
					margins.Add(new Thickness(spacing, (row > 0 ? spacing : 0), 0, 0));
				}

				if (!first)
					x += spacing;
				x += elWidth;

				first = false;
			}

			newLines.Add(controls.Count);
		}

		private static void JustfyHorizontal(List<Thickness> margins, List<int> newLines,
		                                     List<FrameworkElement> controls, Size wrapPanelSize)
		{
			for (int i = 0; i < newLines.Count - 1; i++)
			{
				int start = newLines[i];
				int end = newLines[i + 1];

				if (end == start + 1)
					continue;

				double toSpread = wrapPanelSize.Width;

				for (int j = start; j < end; j++)
				{
					var el = controls[j];
					var elWidth = LayoutInformation.GetLayoutSlot(el).Width - el.Margin.Left - el.Margin.Right;

					toSpread -= elWidth + margins[j].Left;
				}

				toSpread /= end - start - 1;

				for (int j = start + 1; j < end; j++)
				{
					margins[j] = new Thickness(margins[j].Left + toSpread, margins[j].Top, 0, 0);
				}
			}
		}

		private static void SetMargin(FrameworkElement el, Thickness margin)
		{
			if (el.Margin.Left == margin.Left && el.Margin.Top == margin.Top
			    && el.Margin.Right == margin.Right && el.Margin.Bottom == margin.Bottom)
				return;

			el.Margin = margin;
		}

		private static void RemoveCellSpacing(System.Windows.Controls.WrapPanel wrapPanel)
		{
			foreach (UIElement child in wrapPanel.Children)
			{
				if (!(child is FrameworkElement))
					// TODO
					continue;

				FrameworkElement el = (FrameworkElement) child;

				el.Margin = new Thickness(0);
			}
		}
	}
}
