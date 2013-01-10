using System.ComponentModel;
using System.Reflection;
using MinCamlSharp.Parser;

namespace MinCamlSharp.CodeModel
{
	public class Token
	{
		public TokenType Type { get; protected set; }
		public string SourcePath { get; private set; }
		public BufferPosition Position { get; private set; }

		public string Location
		{
			get { return SourcePath + Position; }
		}

		public Token(TokenType type, string sourcePath, BufferPosition position)
		{
			Type = type;
			SourcePath = sourcePath;
			Position = position;
		}

		public override string ToString()
		{
			return GetString(Type);
		}

        public static bool IsKeyword(TokenType type)
		{
			return (type < TokenType.Identifier);
		}

		public static string GetString(TokenType type)
		{
			FieldInfo fi = typeof (TokenType).GetField(type.ToString());
			object[] descriptionAttrs = fi.GetCustomAttributes(typeof (DescriptionAttribute), false);
			if (descriptionAttrs.Length > 0)
			{
				DescriptionAttribute description = (DescriptionAttribute) descriptionAttrs[0];
				return description.Description;
			}
			return type.ToString();
		}
	}
}