using AutoInterfaceAttributes;
using PdfMasterIndex.Service.Attributes;

namespace PdfMasterIndex.Service.Application;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class Scanner : IScanner
{
    public ScanStatus Status { get; } = new();
    
    public void Start()
    {
        if (Status.IsRunning)
        {
            throw new InvalidOperationException("Scanner is already scanning.");
        }

        Status.CurrentStep = ScanStatus.Step.ScanForFiles;
    }

    public Task Cancel()
    {
        if (!Status.IsRunning)
        {
            throw new InvalidOperationException("Scanner is not scanning.");
        }

        Status.CurrentStep = ScanStatus.Step.Idle;
        return Task.CompletedTask;
    }
}