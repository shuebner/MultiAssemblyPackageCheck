using Microsoft.Extensions.Logging;

namespace SvSoft.MSBuild.MultiAssemblyPackageCheck;

public static class Logger2
{
    public static void DoLogging(ILogger logger) => Logger.DoLogging(logger);
}
