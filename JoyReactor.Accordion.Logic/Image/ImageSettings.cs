namespace JoyReactor.Accordion.Logic.Image;

public record ImageSettings
{
    public string[] CdnDomainNames { get; set; }
    public int ResizedSize { get; set; }

    public string OnnxModelPath { get; set; }
    public string OnnxModelInputName { get; set; }
    public string OnnxModelOutputName { get; set; }
    public int OnnxModelOutputVectorSize { get; set; }
    public bool OnnxUseCpu { get; set; }
    public bool OnnxUseCuda { get; set; }
    public bool OnnxUseRocm { get; set; }
    public bool OnnxUseOpenVino { get; set; }
    public string OnnxDeviceId { get; set; }
}