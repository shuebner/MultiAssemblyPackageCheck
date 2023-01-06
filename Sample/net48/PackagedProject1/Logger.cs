using Microsoft.Extensions.Logging;

namespace SvSoft.MSBuild.MultiAssemblyPackageCheck;

public static class Logger
{
    public static void DoLogging(ILogger logger) => logger.LogDebug("Hello Log");
}
