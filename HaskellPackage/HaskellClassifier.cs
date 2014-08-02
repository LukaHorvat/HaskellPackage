using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Windows.Media;

namespace HaskellPackage
{
	[Export(typeof(ITaggerProvider))]
	[ContentType("haskell")]
	[TagType(typeof(ClassificationTag))]
	internal sealed class HaskellClassifierProvider : ITaggerProvider
	{
		[Export]
		[Name("haskell")]
		[BaseDefinition("code")]
		internal static ContentTypeDefinition HaskellContentType = null;

		[Export]
		[FileExtension(".hs")]
		[ContentType("haskell")]
		internal static FileExtensionToContentTypeDefinition HaskellFileType = null;

		[Import]
		internal IClassificationTypeRegistryService classificationTypeRegistry = null;

		[Import]
		internal IBufferTagAggregatorFactoryService aggregatorFactory = null;

		public ITagger<T> CreateTagger<T>(Microsoft.VisualStudio.Text.ITextBuffer buffer) where T : ITag
		{
			ITagAggregator<HaskellTokenTag> haskellTagAggregator = aggregatorFactory.CreateTagAggregator<HaskellTokenTag>(buffer);
			return new HaskellClassifier(buffer, haskellTagAggregator, classificationTypeRegistry) as ITagger<T>;
		}
	}

	internal sealed class HaskellClassifier : ITagger<ClassificationTag>
	{
		ITextBuffer buffer;
		ITagAggregator<HaskellTokenTag> aggregator;
		IDictionary<HaskellTokenTypes, IClassificationType> haskellTypes;

		internal HaskellClassifier(ITextBuffer buffer, ITagAggregator<HaskellTokenTag> haskellTagAggregator, IClassificationTypeRegistryService typeService)
		{
			this.buffer = buffer;
			aggregator = haskellTagAggregator;
			haskellTypes = new Dictionary<HaskellTokenTypes, IClassificationType>();
			foreach (var pair in HaskellEnumToName.Mapping)
			{
				haskellTypes[pair.Key] = typeService.GetClassificationType(pair.Value);
			}
			
			haskellTypes[HaskellTokenTypes.HaskellNumber] = typeService.GetClassificationType(PredefinedClassificationTypeNames.Number);
			haskellTypes[HaskellTokenTypes.HaskellIdentifier] = typeService.GetClassificationType(PredefinedClassificationTypeNames.Identifier);
			haskellTypes[HaskellTokenTypes.HaskellKeyword] = typeService.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
			haskellTypes[HaskellTokenTypes.HaskellOperator] = typeService.GetClassificationType(PredefinedClassificationTypeNames.Operator);
			haskellTypes[HaskellTokenTypes.HaskellString] = typeService.GetClassificationType(PredefinedClassificationTypeNames.String);
			haskellTypes[HaskellTokenTypes.HaskellComment] = typeService.GetClassificationType(PredefinedClassificationTypeNames.Comment);
		}

		public IEnumerable<ITagSpan<ClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			foreach (var tagSpan in this.aggregator.GetTags(spans))
			{
				var tagSpans = tagSpan.Span.GetSpans(spans[0].Snapshot);
				yield return new TagSpan<ClassificationTag>(tagSpans[0], new ClassificationTag(haskellTypes[tagSpan.Tag.Type]));
			}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged
		{
			add { }
			remove { }
		}
	}
}
