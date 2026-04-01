using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PdfMasterIndex.Service.Infrastructure.Logging;

public static class HistoryLoggerExtensions
{
    public static ILoggingBuilder AddHistory(this ILoggingBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, HistoryLoggerProvider>());
        return builder;
    }
}
