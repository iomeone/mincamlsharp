using System;

namespace MinCamlSharp.Parser
{
	public class ErrorEventArgs : EventArgs
	{
		public string Message { get; private set; }
		public BufferPosition Position { get; private set; }

		public ErrorEventArgs(string message, BufferPosition position)
		{
			Message = message;
			Position = position;
		}
	}
}