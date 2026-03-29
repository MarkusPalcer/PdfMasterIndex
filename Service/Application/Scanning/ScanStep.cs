namespace PdfMasterIndex.Service.Application;

public enum ScanStep
{
    Idle,
    Cancelling,
    ScanForFiles,
    ParseFiles
}