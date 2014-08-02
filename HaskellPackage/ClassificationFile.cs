using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace HaskellPackage
{
	public enum HaskellTokenTypes
	{
		HaskellType, HaskellText, HaskellNumber, HaskellOperator, HaskellString, HaskellIdentifier, HaskellKeyword, HaskellComment
	}

	internal static class HaskellClassificationDefinition
	{
		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellType")]
		internal static ClassificationTypeDefinition haskellType = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellText")]
		internal static ClassificationTypeDefinition haskellText = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellNumber")]
		internal static ClassificationTypeDefinition haskellNumber = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellOperator")]
		internal static ClassificationTypeDefinition haskellOperator = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellString")]
		internal static ClassificationTypeDefinition haskellString = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellIdentifier")]
		internal static ClassificationTypeDefinition haskellIdentifier = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellKeyword")]
		internal static ClassificationTypeDefinition haskellKeyword = null;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name("haskellComment")]
		internal static ClassificationTypeDefinition haskellComment = null;

	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellType")]
	[Name("haskellType")]
	[UserVisible(true)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellType : ClassificationFormatDefinition
	{
		public HaskellType()
		{
			this.DisplayName = "Haskell Type";
			this.ForegroundColor = HaskellColors.TokenColors["haskellType"];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellText")]
	[Name("haskellText")]
	[UserVisible(false)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellText : ClassificationFormatDefinition
	{
		public HaskellText()
		{
			this.DisplayName = "Haskell Text";
			this.ForegroundColor = HaskellColors.TokenColors["haskellText"];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellNumber")]
	[Name("haskellNumber")]
	[UserVisible(false)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellNumber : ClassificationFormatDefinition
	{
		public HaskellNumber()
		{
			this.DisplayName = "Haskell Number";
			this.ForegroundColor = HaskellColors.TokenColors["haskellNumber"];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellOperator")]
	[Name("haskellOperator")]
	[UserVisible(false)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellOperator : ClassificationFormatDefinition
	{
		public HaskellOperator()
		{
			this.DisplayName = "Haskell Operator";
			this.ForegroundColor = HaskellColors.TokenColors["haskellOperator"];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellString")]
	[Name("haskellString")]
	[UserVisible(false)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellString : ClassificationFormatDefinition
	{
		public HaskellString()
		{
			this.DisplayName = "Haskell String";
			this.ForegroundColor = HaskellColors.TokenColors["haskellString"];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellIdentifier")]
	[Name("haskellIdentifier")]
	[UserVisible(false)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellIdentifier : ClassificationFormatDefinition
	{
		public HaskellIdentifier()
		{
			this.DisplayName = "Haskell Identifier";
			this.ForegroundColor = HaskellColors.TokenColors["haskellIdentifier"];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellKeyword")]
	[Name("haskellKeyword")]
	[UserVisible(false)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellKeyword : ClassificationFormatDefinition
	{
		public HaskellKeyword()
		{
			this.DisplayName = "Haskell Keyword";
			this.ForegroundColor = HaskellColors.TokenColors["haskellKeyword"];
		}
	}

	[Export(typeof(EditorFormatDefinition))]
	[ClassificationType(ClassificationTypeNames = "haskellComment")]
	[Name("haskellComment")]
	[UserVisible(false)]
	[Order(Before = Priority.Default)]
	internal sealed class HaskellComment : ClassificationFormatDefinition
	{
		public HaskellComment()
		{
			this.DisplayName = "Haskell Comment";
			this.ForegroundColor = HaskellColors.TokenColors["haskellComment"];
		}
	}

	internal class HaskellEnumToName
	{
		public static Dictionary<HaskellTokenTypes, string> Mapping = new Dictionary<HaskellTokenTypes, string>
		{
			{ HaskellTokenTypes.HaskellType, "haskellType" },
			{ HaskellTokenTypes.HaskellText, "haskellText" },
			{ HaskellTokenTypes.HaskellNumber, "haskellNumber" },
			{ HaskellTokenTypes.HaskellOperator, "haskellOperator" },
			{ HaskellTokenTypes.HaskellString, "haskellString" },
			{ HaskellTokenTypes.HaskellIdentifier, "haskellIdentifier" },
			{ HaskellTokenTypes.HaskellKeyword, "haskellKeyword" },
			{ HaskellTokenTypes.HaskellComment, "haskellComment" },
		};
	}
}