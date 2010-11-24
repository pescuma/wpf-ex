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
			Listeners.LayoutUpdated += ComputeCellSpacing;
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
			System.Windows.Controls.WrapPanel wrapPanel = obj as System.Windows.Controls.WrapPanel;
			if (wrapPanel == null)
				throw new ArgumentException("Element must be a WrapPanel");

			if (args.NewValue == null)
			{
				if (Listeners.IsListeningTo(wrapPanel))
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

		private static void ComputeCellSpacing(System.Windows.Controls.WrapPanel wrapPanel)
		{
			int? aSpacing = GetCellSpacing(wrapPanel);
			if (aSpacing == null || aSpacing < 0)
				return;

			int spacing = (int) aSpacing;

			if (!HasValidElement(wrapPanel))
				return;

			var wrapPanelSize = wrapPanel.RenderSize;
			if (wrapPanelSize.Width == 0 || wrapPanelSize.Height == 0)
				return;

			// We have to do this twice to avoid setting a margin when it did not change.
			// This is to avoid endless re-layouts
			List<Thickness> margins = new List<Thickness>();

			if (wrapPanel.Orientation == Orientation.Horizontal)
			{
				var width = wrapPanelSize.Width;

				double x = 0;
				bool first = true;
				int row = 0;

				foreach (UIElement child in wrapPanel.Children)
				{
					if (!(child is FrameworkElement))
						// TODO
						continue;

					var el = (FrameworkElement) child;
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
			}
			else if (wrapPanel.Orientation == Orientation.Vertical)
			{
				var height = wrapPanelSize.Height;

				double y = 0;
				bool first = true;
				int col = 0;

				foreach (UIElement child in wrapPanel.Children)
				{
					if (!(child is FrameworkElement))
						// TODO
						continue;

					var el = (FrameworkElement) child;
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

		private static void SetMargin(FrameworkElement el, Thickness margin)
		{
			if (el.Margin.Left == margin.Left && el.Margin.Top == margin.Top
			    && el.Margin.Right == margin.Right && el.Margin.Bottom == margin.Bottom)
				return;

			el.Margin = margin;
		}

		private static bool HasValidElement(System.Windows.Controls.WrapPanel wrapPanel)
		{
			foreach (UIElement child in wrapPanel.Children)
			{
				if (!(child is FrameworkElement))
					// TODO
					continue;

				return true;
			}

			return false;
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

		#endregion
	}
}
