using System;

namespace MinCamlSharp.Parser
{
	public class ParserException : Exception
	{
		public int LineNumber { get; private set; }

		public ParserException(string message, int lineNumber)
			: base(message)
		{
			LineNumber = lineNumber;
		}
	}
}