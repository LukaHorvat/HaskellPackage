using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaskellPackage
{
	class CommandHandler : IOleCommandTarget
	{
		public IOleCommandTarget NextCommandTarget;
		public IVsTextView TextView;

		public CommandHandler(IVsTextView textView)
		{
			TextView = textView;
		}

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
			{
				switch (nCmdID)
				{
					case (uint)VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
					case (uint)VSConstants.VSStd2KCmdID.COMMENTBLOCK:
						ProcessSelection(line => "--" + line);
						return VSConstants.S_OK;
					case (uint)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
					case (uint)VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
						ProcessSelection(line => RemoveComment(line));
						return VSConstants.S_OK;
					case (uint)VSConstants.VSStd97CmdID.GotoDefn:
						
						return VSConstants.S_OK;
				}
			}
			return NextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		private void ProcessSelection(Func<string, string> operation)
		{
			int startLine, startCol, endLine, endCol;
			string text;
			TextView.GetSelection(out startLine, out startCol, out endLine, out endCol);
			if (startLine > endLine)
			{
				var temp = startLine;
				startLine = endLine;
				endLine = temp;
			}
			TextView.GetTextStream(startLine, 0, endLine + 1, 0, out text);
			var lines = Regex.Split(text, "\r\n|\r|\n")
				.Select(line => Tuple.Create(line, operation(line)))
				.ToList();
			for (var i = startLine; i <= endLine; ++i)
			{
				var line = lines[i - startLine];
				TextView.ReplaceTextOnLine(i, 0, line.Item1.Length, line.Item2, line.Item2.Length);
			}
		}

		private string RemoveComment(string line)
		{
			var regex = new Regex(@"(\s*)((--)?)(.*)");
			var match = regex.Match(line);
			return match.Groups[1].Value + match.Groups[4].Value;
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			if (pguidCmdGroup == typeof(VSConstants.VSStd2KCmdID).GUID)
			{
				for (int i = 0; i < cCmds; i++)
				{
					switch ((VSConstants.VSStd2KCmdID)prgCmds[i].cmdID)
					{
						case VSConstants.VSStd2KCmdID.OUTLN_COLLAPSE_TO_DEF:
						case VSConstants.VSStd2KCmdID.COMMENT_BLOCK:
						case VSConstants.VSStd2KCmdID.COMMENTBLOCK:
						case VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK:
						case VSConstants.VSStd2KCmdID.UNCOMMENTBLOCK:
							prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
							return VSConstants.S_OK;
					}
				}
			}
			else if (pguidCmdGroup == typeof(VSConstants.VSStd97CmdID).GUID)
			{
				for (int i = 0; i < cCmds; i++)
				{
					switch ((VSConstants.VSStd97CmdID)prgCmds[i].cmdID)
					{
						case VSConstants.VSStd97CmdID.GotoDefn:
							prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
							return VSConstants.S_OK;
					}
				}
			}

			return NextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}
	}
}
