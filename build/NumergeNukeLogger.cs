using Numerge;
class NumergeNukeLogger : INumergeLogger {
    public void Log(NumergeLogLevel level, string message) {
        if (level == NumergeLogLevel.Error)
            Serilog.Log.Error(message);
        else if (level == NumergeLogLevel.Warning)
            Serilog.Log.Warning(message);
        else
            Serilog.Log.Information(message);
    }
}