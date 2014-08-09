using ProcessReadWriteUtils;
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
		private ProcessIoManager monitor;
		private List<string> buffer;
		private List<string> inputBuffer;
		private int inputBufferPointer;
		private int readIndex;

		public MyControl()
		{
			InitializeComponent();
			CurrentInstance = this;
			buffer = new List<string>();
			inputBuffer = new List<string>();
			inputBufferPointer = 0;

			var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			timer.Tick += (_, __) =>
			{
				for (int i = 0; i < 50 && readIndex < buffer.Count; ++i, ++readIndex)
				{
					if (readIndex < buffer.Count)
					{
						ghciOutput.AppendText(buffer[readIndex]);
						ghciOutput.CaretIndex = ghciOutput.Text.Length;
						ghciOutput.ScrollToEnd();
					}
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
					monitor.StopMonitoringProcessOutput();
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
			ghci.Start();
			monitor = new ProcessIoManager(ghci);
			monitor.StdoutTextRead += (data) =>
			{
				buffer.Add(data);
			};
			monitor.StderrTextRead += (data) =>
			{
				buffer.Add(data);
			};
			monitor.StartProcessOutputRead();
		}

		private void ghciInput_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				ghciOutput.AppendText(ghciInput.Text);
				if (ghci != null && ghci.HasExited == false) ghci.StandardInput.WriteLine(ghciInput.Text);
				inputBuffer.Add(ghciInput.Text);
				inputBufferPointer = inputBuffer.Count;
				ghciInput.Text = "";
			}
			else if (e.Key == Key.Up)
			{
				if (inputBufferPointer > 0) inputBufferPointer--;
				ghciInput.Text = inputBuffer[inputBufferPointer];
			}
			else if (e.Key == Key.Down)
			{
				if (inputBufferPointer < inputBuffer.Count) inputBufferPointer++;
				if (inputBufferPointer == inputBuffer.Count) ghciInput.Text = "";
				else ghciInput.Text = inputBuffer[inputBufferPointer];
			}
		}
	}
}