namespace PdfMasterIndex.Service.Application.Scanning;

public enum ScanStep
{
    Idle,
    Cancelling,
    ScanForFiles,
    ParseFiles
}