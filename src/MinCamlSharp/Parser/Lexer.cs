using System;
using System.Collections.Generic;
using MinCamlSharp.CodeModel;
using MinCamlSharp.Properties;

namespace MinCamlSharp.Parser
{
	public class Lexer : LexerBase
	{
		public Lexer(string path, string text)
			: base(path, text)
		{
			
		}

		public Token[] GetTokens()
		{
			var result = new List<Token>();
			while (!IsEof)
			{
				var next = NextToken();
				if (next.Type != TokenType.Eof)
					result.Add(next);
			}
			return result.ToArray();
		}

		public Token NextToken()
		{
			EatWhiteSpace();
			StartToken();

			if (PeekChar() == '\0')
				return NewToken(TokenType.Eof);

			char c = NextChar();
			switch (c)
			{
				case '{' :
					return NewToken(TokenType.OpenCurly);
				case '}':
					return NewToken(TokenType.CloseCurly);
				case '(':
				{
					char c2 = PeekChar();
					if (c2 == '*')
					{
						NextChar();
						while (!IsEof && (PeekChar() != '*' || PeekChar(1) != ')'))
                            NextChar();
						if (IsEof) {
                            ReportError(Resources.LexerUnexpectedEndOfFileStarCloseParen);
                            return ErrorToken();
                        }
						NextChar();
                        NextChar();
                        
						TakePosition();
						return NextToken();
					}
					return NewToken(TokenType.OpenParen);
				}
				case ')':
					return NewToken(TokenType.CloseParen);
				case '[':
					return NewToken(TokenType.OpenSquare);
				case ']':
					return NewToken(TokenType.CloseSquare);
				case '=':
					return NewToken(TokenType.Equal);
				case ',':
					return NewToken(TokenType.Comma);
				case ':':
					return NewToken(TokenType.Colon);
				case ';':
					return NewToken(TokenType.Semicolon);
				case '-':
					return NewToken(TokenType.Minus);
				case '+':
					return NewToken(TokenType.Plus);
				case '<':
					{
						char c2 = PeekChar();
						if (c2 == '=')
						{
							NextChar();
							return NewToken(TokenType.LessEqual);
						}
						return NewToken(TokenType.Less);
					}
				case '>':
				{
					char c2 = PeekChar();
					if (c2 == '=')
					{
						NextChar();
						return NewToken(TokenType.GreaterEqual);
					}
					return NewToken(TokenType.Greater);
				}
				case '"':
				{
					string value = EatWhile(c2 => c2 != '"');
					NextChar(); //Swallow the end of the string constant
					return new StringToken(value, Path, TakePosition());
				}
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				{
					// Fairly simple rules
					// - If it contains a dot or ends with "f", then it's a float.
					// If it contains an underscore, then it's a shader profile identifier (i.e. 2_0)
					bool containsDot = false;
					bool containsUnderscore = false;
					Value.Length = 0;
					Value.Append(c);
					while (!IsEof && (PeekChar() == '.' || PeekChar() == '_' || char.IsDigit(PeekChar())))
					{
						char c2 = NextChar();
						switch (c2)
						{
							case '.':
								if (containsDot)
								{
									ReportError(Resources.LexerUnexpectedCharacter, c2);
									return ErrorToken();
								}
								containsDot = true;
								break;
							case '_':
								containsUnderscore = true;
								break;
						}
						Value.Append(c2);
					}
					bool floatSuffix = false;
					if (PeekChar() == 'f')
					{
						floatSuffix = true;
						NextChar();
					}
					if (containsUnderscore)
						return new IdentifierToken(Value.ToString(), Path, TakePosition());
					if (containsDot || floatSuffix)
						return new FloatToken(Convert.ToSingle(Value.ToString()), Path, TakePosition());
					return new IntToken(Convert.ToInt32(Value.ToString()), Path, TakePosition());
				}
				default :
					if (IsIdentifierChar(c))
					{
						string identifier = c + EatWhile(IsIdentifierChar);
						if (Keywords.IsKeyword(identifier))
							return new Token(Keywords.GetKeywordType(identifier), Path, TakePosition());
						return new IdentifierToken(identifier, Path, TakePosition());
					}
					ReportError(Resources.LexerUnexpectedCharacter, c);
					return ErrorToken();
			}
		}

		private Token ErrorToken()
		{
			return NewToken(TokenType.Error);
		}

		private Token NewToken(TokenType type)
		{
			return new Token(type, Path, TakePosition());
		}
	}
}