using MinCamlSharp.Parser;

namespace MinCamlSharp.CodeModel
{
	public class StringToken : LiteralToken
	{
		public string Value { get; private set; }

		public StringToken(string value, string sourcePath, BufferPosition position)
			: base(LiteralTokenType.String, sourcePath, position)
		{
			Value = value;
		}

		public override string ToString()
		{
			return "\"" + Value + "\"";
		}
	}
}