using AutoInterfaceAttributes;
using Microsoft.AspNetCore.SignalR;
using PdfMasterIndex.Service.Attributes;
using PdfMasterIndex.Service.Presentation.v1;

namespace PdfMasterIndex.Service.Application;

[AutoInterface]
[Lifetime(ServiceLifetime.Singleton)]
public class ScanStatus(IHubContext<ScanHub> hubContext) : IScanStatus
{
    private ScanStep _currentStep = ScanStep.Idle;
    private double _currentStepProgress = 0;
    private double _currentFileProgress = 0;

    public bool IsRunning => CurrentStep is not ScanStep.Idle and not ScanStep.Cancelling;

    public ScanStep CurrentStep
    {
        get => _currentStep;
        set
        {
            if (_currentStep == value) return;
            _currentStep = value;
            Notify();
        }
    }

    public double CurrentStepProgress
    {
        get => _currentStepProgress;
        set
        {
            if (Math.Abs(_currentStepProgress - value) < 0.0001) return;
            _currentStepProgress = value;
            Notify();
        }
    }

    public double CurrentFileProgress
    {
        get => _currentFileProgress;
        set
        {
            if (Math.Abs(_currentFileProgress - value) < 0.0001) return;
            _currentFileProgress = value;
            Notify();
        }
    }

    private void Notify()
    {
        _ = hubContext.Clients.All.SendAsync("ScanStatusChanged", this);
    }
}