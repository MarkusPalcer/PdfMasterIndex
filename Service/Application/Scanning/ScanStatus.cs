using AutoInterfaceAttributes;
using Microsoft.AspNetCore.SignalR;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Presentation.v1;

namespace PdfMasterIndex.Service.Application.Scanning;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class ScanStatus(IHubContext<ScanHub> hubContext) : IScanStatus
{
    public bool IsRunning => CurrentStep is not ScanStep.Idle and not ScanStep.Cancelling;

    public ScanStep CurrentStep
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            Notify();
        }
    } = ScanStep.Idle;

    public double CurrentStepProgress
    {
        get;
        set
        {
            if (Math.Abs(field - value) < 0.0001) return;
            field = value;
            Notify();
        }
    } = 0;

    public double CurrentFileProgress
    {
        get;
        set
        {
            if (Math.Abs(field - value) < 0.0001) return;
            field = value;
            Notify();
        }
    } = 0;

    private void Notify()
    {
        _ = hubContext.Clients.All.SendAsync("ScanStatusChanged", this);
    }
}