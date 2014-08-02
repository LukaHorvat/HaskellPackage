using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HaskellPackage
{
	[Export(typeof(ITaggerProvider))]
	[ContentType("haskell")]
	[TagType(typeof(HaskellTokenTag))]
	internal sealed class HaskellTokenTagProvider : ITaggerProvider
	{
		public ITagger<T> CreateTagger<T>(Microsoft.VisualStudio.Text.ITextBuffer buffer) where T : ITag
		{
			return new HaskellTokenTagger(buffer) as ITagger<T>;
		}
	}

	public class HaskellTokenTag : ITag
	{
		public HaskellTokenTypes Type { get; private set; }

		public HaskellTokenTag(HaskellTokenTypes type)
		{
			Type = type;
		}
	}

	internal sealed class HaskellTokenTagger : ITagger<HaskellTokenTag>
	{
		ITextBuffer buffer;

		internal HaskellTokenTagger(ITextBuffer buffer)
		{
			this.buffer = buffer;
		}

		private int offset;
		private string currentLine;

		public IEnumerable<ITagSpan<HaskellTokenTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			int commentCount = 0;
			int intervalStart = 0;
			List<Tuple<int, int>> commentIntervals = new List<Tuple<int, int>>();
			for (var i = 0; i < buffer.CurrentSnapshot.Length - 1; ++i)
			{
				if (buffer.CurrentSnapshot[i] == '{' && buffer.CurrentSnapshot[i + 1] == '-')
				{
					if (commentCount == 0)
					{
						intervalStart = i;
					}
					commentCount++;
				}
				if (buffer.CurrentSnapshot[i] == '-' && buffer.CurrentSnapshot[i + 1] == '}' && commentCount > 0)
				{
					commentCount--;
					if (commentCount == 0)
					{
						commentIntervals.Add(Tuple.Create(intervalStart, i + 2));
					}
				}
			}
			if (commentCount > 0)
			{
				commentIntervals.Add(Tuple.Create(intervalStart, buffer.CurrentSnapshot.Length - 1));
			}
			foreach (var currentSpan in spans)
			{
				var containingLine = currentSpan.Start.GetContainingLine();
				var position = containingLine.Start.Position;
				currentLine = containingLine.GetText();
				offset = 0;
				var match = commentIntervals.Find(interval => interval.Item1 < position && interval.Item2 >= position);
				if (match != null)
				{
					if (containingLine.End.Position <= match.Item2)
					{
						yield return new TagSpan<HaskellTokenTag>(
							new SnapshotSpan(buffer.CurrentSnapshot, position, containingLine.Length),
							new HaskellTokenTag(HaskellTokenTypes.HaskellComment)
						);
						continue;
					}
					else
					{
						offset += match.Item2 - position;
						yield return new TagSpan<HaskellTokenTag>(
							new SnapshotSpan(buffer.CurrentSnapshot, position, match.Item2 - position),
							new HaskellTokenTag(HaskellTokenTypes.HaskellComment)
						);
					}
				}
				while (true)
				{
					while (offset < currentLine.Length && Char.IsWhiteSpace(currentLine[offset])) offset++;
					if (offset >= currentLine.Length) break;
					int tokenStart = offset;
					HaskellTokenTypes type = HaskellTokenTypes.HaskellText;
					string token = "";
					match = commentIntervals.Find(interval => interval.Item1 == position + offset); 
					if (match != null)
					{
						if (containingLine.End.Position <= match.Item2)
						{
							token = currentLine.Substring(offset);
						}
						else
						{
							token = currentLine.Substring(offset, match.Item2 - match.Item1);
						}
						offset += token.Length;
						type = HaskellTokenTypes.HaskellComment;
					}
					else if (Char.IsDigit(currentLine[offset]))
					{
						token = EatNumber();
						type = HaskellTokenTypes.HaskellNumber;
					}
					else if (currentLine[offset] == '"')
					{
						token = EatString();
						type = HaskellTokenTypes.HaskellString;
					}
					else if (currentLine[offset] == '\'')
					{
						token = EatChar();
						type = HaskellTokenTypes.HaskellString;
					}
					else if (offset + 1 < currentLine.Length && currentLine.Substring(offset, 2) == "{-")
					{
						token = EatComment();
						type = HaskellTokenTypes.HaskellComment;
					}
					else if (new[] { '(', ')', '[', ']', '{', '}', ';', ',' }.Contains(currentLine[offset]))
					{
						token = currentLine[offset] + "";
						offset++;
						type = HaskellTokenTypes.HaskellText;
					}
					else if (offset + 1 < currentLine.Length && currentLine.Substring(offset, 2) == "--")
					{
						token = currentLine.Substring(offset);
						offset += token.Length;
						type = HaskellTokenTypes.HaskellComment;
					}
					else if (IsSymbol(currentLine[offset]))
					{
						token = EatOperator();
						type = HaskellTokenTypes.HaskellOperator;
					}
					else if (currentLine[offset] == '`')
					{
						token = EatBackticks();
						type = HaskellTokenTypes.HaskellOperator;
					}
					else if (Char.IsLetter(currentLine[offset]))
					{
						token = EatIdentifier();
						var keywords = @"as case of class data data family data instance default deriving instance do forall foreign hiding if then else import infix infixl, infixr instance let in mdo module newtype proc qualified rec type type family type instance where"
							.Split(' ');
						type =
							keywords.Contains(token) ?
								HaskellTokenTypes.HaskellKeyword :
								(Char.IsUpper(token[0]) ?
									HaskellTokenTypes.HaskellType :
									HaskellTokenTypes.HaskellIdentifier);
					}
					if (token == "")
					{
						token = currentLine.Substring(offset);
						offset += token.Length;
					}
					var tokenSpan = new SnapshotSpan(currentSpan.Snapshot, new Span(tokenStart + position, token.Length));
					yield return new TagSpan<HaskellTokenTag>(tokenSpan, new HaskellTokenTag(type));
				}
			}
		}

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged
		{
			add { }
			remove { }
		}

		private void AppendNext(StringBuilder builder)
		{
			builder.Append(currentLine[offset]);
			offset++;
		}

		private bool IsSymbol(char c)
		{
			return Char.IsSymbol(c) ||
				new[] { '\\', '-', ':', '.', '_', '!', '#', '$', '%', '&', '*', '+', '/', '<', '=', '>', '?', '@', '^', '|', '~' }.Contains(c);
		}

		private string EatNumber()
		{
			var pattern = new Regex("[0-9]+((o|O)[0-7]+|(x|X)[0-9a-f]+|\\.[0-9]+)?((e|E)(\\+|-)?[0-9]+)?");
			var match = pattern.Match(currentLine, offset);
			offset += match.Length;
			return match.Value;
		}

		private string EatString()
		{
			var pattern = new Regex("\"[^\"]*\"");
			var match = pattern.Match(currentLine, offset);
			offset += match.Length;
			return match.Value;
		}

		private string EatChar()
		{
			var pattern = new Regex("'[^\']*'");
			var match = pattern.Match(currentLine, offset);
			offset += match.Length;
			return match.Value;
		}

		private string EatOperator()
		{
			var builder = new StringBuilder();
			while (currentLine.Length > offset && IsSymbol(currentLine[offset]))
			{
				builder.Append(currentLine[offset]);
				offset++;
			}
			return builder.ToString();
		}

		private string EatBackticks()
		{
			var pattern = new Regex("`[a-zA-Z]+`");
			var match = pattern.Match(currentLine, offset);
			offset += match.Length;
			return match.Value;
		}

		private string EatIdentifier()
		{
			var pattern = new Regex("[a-zA-Z0-9_']+");
			var match = pattern.Match(currentLine, offset);
			offset += match.Length;
			return match.Value;
		}

		private string EatComment()
		{
			var pattern = new Regex("{-([^-]|-(?!}))*(-})?");
			var match = pattern.Match(currentLine, offset);
			offset += match.Length;
			return match.Value;
		}
	}
}
