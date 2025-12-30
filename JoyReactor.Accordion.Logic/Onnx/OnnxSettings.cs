namespace JoyReactor.Accordion.Logic.Onnx;

public record OnnxSettings
{
    public string ModelPath { get; set; }

    public string InputName { get; set; }
    public int InputSize { get; set; }

    public string OutputName { get; set; }
    public int OutputSize { get; set; }

    public bool UseCpu { get; set; }
    public bool UseCuda { get; set; }
    public bool UseRocm { get; set; }
    public bool UseOpenVino { get; set; }
    public string DeviceId { get; set; }
}