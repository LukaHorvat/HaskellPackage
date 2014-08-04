using HDevTools;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaskellPackage
{
	internal class QuickInfoSource : IQuickInfoSource
	{
		private QuickInfoSourceProvider provider;
		private ITextBuffer buffer;

		public QuickInfoSource(QuickInfoSourceProvider provider, ITextBuffer buffer)
		{
			this.provider = provider;
			this.buffer = buffer;
		}

		public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out Microsoft.VisualStudio.Text.ITrackingSpan applicableToSpan)
		{
			var doc = (ITextDocument)buffer.Properties[typeof(ITextDocument)];
			var maybePoint = session.GetTriggerPoint(buffer.CurrentSnapshot);
			if (!maybePoint.HasValue)
			{
				applicableToSpan = null;
				return;
			}
			var point = maybePoint.Value;
			var line = point.GetContainingLine();
			var lineNumber = line.LineNumber + 1;
			var column = 1;
			for (int i = line.Start.Position, offset = 0; i < point.Position; ++i, ++offset)
			{
				if (line.GetText()[offset] == '\t') column += 8; //ghc-mod expects 8-wide tabs
				else column += 1;

			}
			var typeInfo = HDevToolsRunner.GetType(doc.FilePath, line.LineNumber + 1, column);
			if (typeInfo == null)
			{
				applicableToSpan = null;
				return;
			}
			quickInfoContent.Add(typeInfo.Type);
			applicableToSpan = buffer.CurrentSnapshot.CreateTrackingSpan(point.Position, 1, SpanTrackingMode.EdgeInclusive);
		}

		private bool isDisposed;

		public void Dispose()
		{
			if (isDisposed)
			{
				GC.SuppressFinalize(this);
				isDisposed = true;
			}
		}
	}

	[Export(typeof(IQuickInfoSourceProvider))]
	[Name("Tooltip QuickInfo Source")]
	[Order(Before = "Default Quick Info Presenter")]
	[ContentType("haskell")]
	internal class QuickInfoSourceProvider : IQuickInfoSourceProvider
	{
		[Import]
		internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

		[Import]
		internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

		public IQuickInfoSource TryCreateQuickInfoSource(Microsoft.VisualStudio.Text.ITextBuffer textBuffer)
		{
			return new QuickInfoSource(this, textBuffer);
		}
	}

}
