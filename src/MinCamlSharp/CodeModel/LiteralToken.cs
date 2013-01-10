using MinCamlSharp.Parser;

namespace MinCamlSharp.CodeModel
{
	public class LiteralToken : Token
	{
		public LiteralTokenType LiteralType { get; private set; }

		public LiteralToken(LiteralTokenType literalType, string sourcePath, BufferPosition position)
			: base(TokenType.Literal, sourcePath, position)
		{
			LiteralType = literalType;
		}
	}
}