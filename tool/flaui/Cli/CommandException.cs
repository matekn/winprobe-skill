namespace Flaui.Cli;

public sealed class CommandException : Exception
{
    public int ExitCode { get; }

    public CommandException(string message, int exitCode = 1) : base(message)
    {
        ExitCode = exitCode;
    }
}
