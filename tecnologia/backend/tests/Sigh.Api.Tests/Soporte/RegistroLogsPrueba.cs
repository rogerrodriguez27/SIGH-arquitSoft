using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Sigh.Api.Tests.Soporte;

/// <summary>Proveedor de logs que guarda en memoria todo lo que escribe la API, incluidas las excepciones.</summary>
public sealed class RegistroLogsPrueba : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _lineas = new();

    public IReadOnlyList<string> Lineas => _lineas.ToList();

    public ILogger CreateLogger(string categoryName) => new Registrador(categoryName, _lineas);

    public void Dispose() { }

    private sealed class Registrador(string categoria, ConcurrentQueue<string> lineas) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            lineas.Enqueue($"{logLevel} {categoria}: {formatter(state, exception)} {exception}");
    }
}
