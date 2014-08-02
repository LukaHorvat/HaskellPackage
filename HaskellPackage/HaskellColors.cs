using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace HaskellPackage
{
	internal class HaskellColors
	{
		public static Dictionary<string, Color> TokenColors = new Dictionary<string, Color>
		{
			{ "haskellText", Colors.White },
			{ "haskellType", Colors.DeepSkyBlue },
			{ "haskellString", Colors.Orange },
			{ "haskellNumber", Colors.Yellow },
			{ "haskellIdentifier", Colors.White },
			{ "haskellOperator", Colors.LightYellow },
			{ "haskellKeyword", Colors.DarkOliveGreen }
		};
	}
}
