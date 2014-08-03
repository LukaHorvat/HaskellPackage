using EnvDTE;
using HDevTools;
using LukaHorvat.GHCi;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HaskellPackage
{
	[Export(typeof(IViewTaggerProvider))]
	[ContentType("haskell")]
	[TagType(typeof(ErrorTag))]
	internal class ErrorTaggerProvider : IViewTaggerProvider
	{
		[Import]
		internal SVsServiceProvider ServiceProvider;

		//[Import]
		//internal IVsTextView vsTextView;

		private static ErrorListProvider errorListProvider;

		private DTE dte;
		private Events events;
		private DocumentEvents docEvents;
		private DTEEvents dteEvents;

		private static SortedSet<string> filesStartedFor = new SortedSet<string>();

		private static DateTime lastDiagnosticSignature;
		private static List<ErrorReportEntry> aggregator = new List<ErrorReportEntry>();

		private static event Action<string> saveEvent;
		private static Dictionary<Document, Action<string>> handlersByDocument = new Dictionary<Document, Action<string>>();

		private static bool hDevServerStarted = false;

		private void KillServers()
		{
			while (System.Diagnostics.Process.GetProcessesByName("hdevtools").Any())
			{
				HDevToolsRunner.StopServer();
				System.Threading.Thread.Sleep(100);
			}
		}

		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			var document = (ITextDocument)buffer.Properties[typeof(ITextDocument)];

			if (errorListProvider == null)
			{
				dte = (DTE)ServiceProvider.GetService(typeof(DTE));
				events = dte.Events;
				dteEvents = events.DTEEvents;
				docEvents = events.DocumentEvents;
				errorListProvider = new ErrorListProvider(ServiceProvider);
				docEvents.DocumentSaved += doc => { if (saveEvent != null) saveEvent.Invoke(doc.FullName); };
				dteEvents.OnBeginShutdown += delegate
				{
					if (hDevServerStarted)
					{
						KillServers();
						hDevServerStarted = false;
					}
				};
				if (!hDevServerStarted)
				{
					KillServers();
					hDevServerStarted = true;
					HDevToolsRunner.StartServer(document.FilePath);
				}
			}

			if (!filesStartedFor.Contains(document.FilePath))
			{
				IVsUIHierarchy hierarchy;
				uint itemId;
				IVsWindowFrame frame;
				if (VsShellUtilities.IsDocumentOpen(ServiceProvider, document.FilePath, Guid.Empty, out hierarchy, out itemId, out frame))
				{
					var tv = VsShellUtilities.GetTextView(frame);
					IOleCommandTarget nextCommandTarget;
					var cmdHandler = new CommandHandler(tv);
					tv.AddCommandFilter(cmdHandler, out nextCommandTarget);
					cmdHandler.NextCommandTarget = nextCommandTarget;
				}
				filesStartedFor.Add(document.FilePath);

				Action<string> handler = doc =>
				{
					var lastMod = new FileInfo(doc).LastWriteTime;
					if (lastDiagnosticSignature != lastMod)
					{
						lastDiagnosticSignature = lastMod;
						errorListProvider.Tasks.Clear();
						errorListProvider.Refresh();
						aggregator.Clear();
						if (MyControl.CurrentInstance != null)
						{
							MyControl.CurrentInstance.RestartInstance(doc);
						}
					}
					new System.Threading.Thread(() =>
					{
						var errors = Diagnostics.GetErrors(buffer, document);
						foreach (var error in errors)
						{
							lock (aggregator)
							{
								bool skip = aggregator.Any(entry =>
									entry.Line == error.Line &&
									entry.Column == error.Column &&
									entry.FileName == error.FileName &&
									entry.Message == error.Message
								);
								if (skip) continue;
								var errorTask = new ErrorTask
								{
									ErrorCategory = error.Severity,
									Category = TaskCategory.BuildCompile,
									Text = error.Message,
									Document = error.FileName,
									Line = error.Line,
									Column = error.Column,
								};
								errorTask.Navigate += (sender, args) =>
								{
									errorTask.Line++;
									errorListProvider.Navigate(errorTask, new Guid("{7651A701-06E5-11D1-8EBD-00A0C90F26EA}") /* EnvDTE.Constants.vsViewKindCode */);
									errorTask.Line--;
								};
								errorListProvider.Tasks.Add(errorTask);
								aggregator.Add(error);
							}
						}
						errorListProvider.Refresh();
						errorListProvider.Show();
					}).Start();
				};
				saveEvent += handler;
				textView.Closed += delegate
				{
					saveEvent -= handler;
				};
				saveEvent.Invoke(document.FilePath);
			}
			return new ErrorTagger() as ITagger<T>;
		}
	}

	internal class ErrorTagger : ITagger<ErrorTag>
	{
		public IEnumerable<ITagSpan<ErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			yield break;
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged
		{
			add { }
			remove { }
		}
	}


	/*
	internal class ErrorTagger : ITagger<ErrorTag>
	{
		ITextBuffer buffer;
		ITextView view;
		ITextDocument document;
		FileInfo info;
		ErrorListProvider errorListProvider;
		static Dictionary<string, Tuple<DateTime, List<string>>> cache = new Dictionary<string, Tuple<DateTime, List<string>>>();
		static Dictionary<string, bool> working = new Dictionary<string, bool>();

		public ErrorTagger(ITextView view, ITextBuffer buffer, ErrorListProvider errorListProvider)
		{
			this.view = view;
			this.buffer = buffer;
			this.document = (ITextDocument)buffer.Properties[typeof(ITextDocument)];
			this.info = new FileInfo(document.FilePath);
			this.errorListProvider = errorListProvider;
			//this.lastSave = info.LastWriteTime;
		}

		public void Refresh()
		{
			TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(buffer.CurrentSnapshot, 0, buffer.CurrentSnapshot.Length)));
		}

		public IEnumerable<ITagSpan<ErrorTag>> GetTags(Microsoft.VisualStudio.Text.NormalizedSnapshotSpanCollection spans)
		{
			List<string> report = null;
			if (cache.ContainsKey(document.FilePath))
			{
				var entry = cache[document.FilePath];
				info.Refresh();
				if (info.LastWriteTime == entry.Item1)
				{
					report = entry.Item2;
				}
			}
			if (report == null)
			{
				if (!working.ContainsKey(document.FilePath))
				{
					working[document.FilePath] = true;
					new System.Threading.Thread(() =>
					{
						info.Refresh();
						cache[document.FilePath] = Tuple.Create(info.LastWriteTime, report);
						working.Remove(document.FilePath);
						errorListProvider.Tasks.Clear();
						this.Refresh();
					}).Start();
				}
				yield break;
			}
			foreach (var errorString in report)
			{
				var regex = new Regex(@"((\w:)?[^:]*):([0-9]*):([0-9]*)(.*)");
				var match = regex.Match(errorString);
				var path = match.Groups[1].Value;
				var line = Int32.Parse(match.Groups[3].Value);
				var column = Int32.Parse(match.Groups[4].Value);
				var err = match.Groups[5].Value;
				var fullPath = FullPathRelativeTo(Path.GetDirectoryName(document.FilePath), path);
				if (fullPath != document.FilePath) continue;
				if (line - 1 >= buffer.CurrentSnapshot.Lines.Count()) continue;
				var lineStr = buffer.CurrentSnapshot.Lines.ElementAt(line - 1);
				var tag = new SnapshotSpan(buffer.CurrentSnapshot, new Span(lineStr.Start.Position, lineStr.GetText().Length));
				errorListProvider.Tasks.Add(new ErrorTask()
				{
					ErrorCategory = TaskErrorCategory.Error,
					Category = TaskCategory.BuildCompile,
					Text = err,
					Document = document.FilePath,
					Line = line,
					Column = column
				});
				yield return new TagSpan<ErrorTag>(tag, new ErrorTag("syntax error", err));
			}
		}

		public event EventHandler<Microsoft.VisualStudio.Text.SnapshotSpanEventArgs> TagsChanged;
	}
	 */

	internal class ErrorReportEntry
	{
		public string FileName;
		public string Message;
		public int Line, Column;
		public TaskErrorCategory Severity;

		public override string ToString()
		{
			return Path.GetFileName(FileName) + ": " + Message;
		}
	}

	internal class Diagnostics
	{
		public static string FullPathRelativeTo(string root, string partialPath)
		{
			string oldRoot = Directory.GetCurrentDirectory();
			try
			{
				Directory.SetCurrentDirectory(root);
				return Path.GetFullPath(partialPath);
			}
			finally
			{
				Directory.SetCurrentDirectory(oldRoot);
			}
		}

		public static IEnumerable<ErrorReportEntry> GetErrors(ITextBuffer buffer, ITextDocument document)
		{
			var report = HDevToolsRunner.Check(document.FilePath).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(err => err.Replace('\0', '\n')).ToList();
			foreach (var errorString in report)
			{
				var regex = new Regex(@"((\w:)?[^:]*):([0-9]*):([0-9]*):(.*)");
				var match = regex.Match(errorString);
				var path = match.Groups[1].Value;
				var line = Int32.Parse(match.Groups[3].Value) - 1;
				var column = Int32.Parse(match.Groups[4].Value) - 1;
				var err = match.Groups[5].Value;
				TaskErrorCategory severity = TaskErrorCategory.Error;
				if (err.StartsWith("Warning:"))
				{
					err = err.Substring("Warning:".Length);
					severity = TaskErrorCategory.Warning;
				}
				var fullPath = FullPathRelativeTo(Path.GetDirectoryName(document.FilePath), path);
				yield return new ErrorReportEntry
				{
					FileName = fullPath,
					Column = column,
					Line = line,
					Message = err,
					Severity = severity
				};
			}
		}
	}
}
