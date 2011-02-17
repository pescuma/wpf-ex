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

namespace org.pescuma.wpfex
{
	internal class ListenerManager<T> where T : FrameworkElement
	{
		public delegate void InitializedEventHandler(T control);

		public delegate void LayoutUpdatedEventHandler(T control);

		private class Listeners
		{
			public EventHandler Initialized;
			public EventHandler LayoutUpdated;
			public RoutedEventHandler Unloaded;
		}

		private readonly Dictionary<T, Listeners> controlToListeners = new Dictionary<T, Listeners>();

		public void AddTo(T control)
		{
			if (IsListeningTo(control))
				return;

			Listeners l = new Listeners();
			l.Initialized = (s, e) =>
			                	{
			                		FireInitialized(control);
			                		FireLayoutUpdated(control);
			                	};
			l.LayoutUpdated = (s, e) => { FireLayoutUpdated(control); };
			l.Unloaded = (s, e) => { RemoveFrom(control); };

			control.Initialized += l.Initialized;
			control.LayoutUpdated += l.LayoutUpdated;
			control.Unloaded += l.Unloaded;

			controlToListeners.Add(control, l);
		}

		public void RemoveFrom(T control)
		{
			if (!IsListeningTo(control))
				return;

			Listeners l = controlToListeners[control];

			control.Initialized -= l.Initialized;
			control.LayoutUpdated -= l.LayoutUpdated;
			control.Unloaded -= l.Unloaded;

			controlToListeners.Remove(control);
		}

		public bool IsListeningTo(T control)
		{
			return controlToListeners.ContainsKey(control);
		}

		public event InitializedEventHandler Initialized;
		public event LayoutUpdatedEventHandler LayoutUpdated;

		private void FireInitialized(T control)
		{
			var ev = Initialized;
			if (ev != null)
				ev(control);
		}

		private void FireLayoutUpdated(T control)
		{
			var ev = LayoutUpdated;
			if (ev != null)
				ev(control);
		}
	}
}
