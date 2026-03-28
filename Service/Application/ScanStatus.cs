namespace PdfMasterIndex.Service.Application;

public class ScanStatus
{
    public enum Step
    {
        Idle,
        ScanForFiles,
        ParseFiles
    }
    
    public bool IsRunning => CurrentStep != Step.Idle;
    
    public Step CurrentStep { get; internal set; } = Step.Idle;
    
    public double CurrentStepProgress { get; internal set; } = 0;
    
    public double CurrentFileProgress { get; internal set; } = 0;
}