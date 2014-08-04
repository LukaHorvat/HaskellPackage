using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LukaHorvat.GHCi
{
	/// <summary>
	/// Interaction logic for MyControl.xaml
	/// </summary>
	public partial class MyControl : UserControl
	{
		public static MyControl CurrentInstance;

		private Process ghci;
		private List<string> buffer;
		private int readIndex;

		public MyControl()
		{
			InitializeComponent();
			CurrentInstance = this;
			buffer = new List<string>();

			var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			timer.Tick += (_, __) =>
			{
				if (readIndex < buffer.Count)
				{
					ghciOutput.AppendText(buffer[readIndex] + "\n");
					ghciOutput.CaretIndex = ghciOutput.Text.Length;
					ghciOutput.ScrollToEnd();
					readIndex++;
				}
			};
			timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
			timer.Start();
		}

		public void RestartInstance(string path)
		{
			if (ghci != null)
			{
				if (!ghci.HasExited)
				{
					ghci.CancelOutputRead();
					ghci.Kill();
				}
				ghci.Dispose();

				ghciOutput.Text += "\n\n\n\n";
			}
			buffer.Clear();
			readIndex = 0;
			ghci = new Process
			{
				StartInfo = new ProcessStartInfo("ghci")
				{
					WorkingDirectory = System.IO.Path.GetDirectoryName(path),
					Arguments = System.IO.Path.GetFileName(path),
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
			ghci.OutputDataReceived += (obj, data) =>
			{
				buffer.Add(data.Data);
			};
			ghci.ErrorDataReceived += (obj, data) =>
			{
				buffer.Add(data.Data);
			};
			ghci.Start();
			ghci.BeginOutputReadLine();
			ghci.BeginErrorReadLine();
		}

		private void ghciInput_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				ghciOutput.Text += "> " + ghciInput.Text + "\n";
				ghci.StandardInput.WriteLine(ghciInput.Text + "\n");
				ghciInput.Text = "";
			}
		}

		private void ghciOutput_GotFocus(object sender, RoutedEventArgs e)
		{
			ghciInput.Focus();
			Keyboard.Focus(ghciInput);
		}
	}
}