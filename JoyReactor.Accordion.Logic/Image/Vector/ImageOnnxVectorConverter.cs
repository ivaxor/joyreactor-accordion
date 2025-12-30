using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace JoyReactor.Accordion.Logic.Image.Vector;

public class ImageOnnxVectorConverter(
    InferenceSession inferenceSession,
    IOptions<ImageSettings> settings)
    : IImageOnnxVectorConverter
{
    internal static readonly float[] Mean = [0.48145466f, 0.4578275f, 0.40821073f];
    internal static readonly float[] Std = [0.26862954f, 0.26130258f, 0.27577711f];

    public async Task<float[]> Convert(Image<Rgb24> image, CancellationToken cancellationToken = default)
    {
        var input = ConvertToTensor(image, settings.Value.ResizedSize);
        var output = new float[settings.Value.OnnxModelOutputVectorSize];

        using var inputValue = OrtValue.CreateTensorValueFromMemory(
            OrtMemoryInfo.DefaultInstance,
            input.Buffer,
            [1, 3, settings.Value.ResizedSize, settings.Value.ResizedSize]);

        using var outputValue = OrtValue.CreateTensorValueFromMemory(
            OrtMemoryInfo.DefaultInstance,
            output.AsMemory(),
            [1, settings.Value.OnnxModelOutputVectorSize]);

        await inferenceSession.RunAsync(
            new RunOptions(),
            [settings.Value.OnnxModelInputName],
            [inputValue],
            [settings.Value.OnnxModelOutputName],
            [outputValue]);

        L2Normalize(output);

        return output;
    }

    internal static DenseTensor<float> ConvertToTensor(Image<Rgb24> image, int size)
    {
        var tensor = new DenseTensor<float>([1, 3, size, size]);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < size; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < size; x++)
                {
                    tensor[0, 0, y, x] = (row[x].R / 255f - Mean[0]) / Std[0];
                    tensor[0, 1, y, x] = (row[x].G / 255f - Mean[1]) / Std[1];
                    tensor[0, 2, y, x] = (row[x].B / 255f - Mean[2]) / Std[2];
                }
            }
        });

        return tensor;
    }

    internal static void L2Normalize(float[] vector)
    {
        var sum = 0f;
        for (var i = 0; i < vector.Length; i++)
            sum += vector[i] * vector[i];

        var norm = MathF.Sqrt(sum);
        if (norm < 1e-10f)
            return;

        var invNorm = 1.0f / norm;
        for (var i = 0; i < vector.Length; i++)
            vector[i] *= invNorm;
    }
}

public interface IImageOnnxVectorConverter
{
    Task<float[]> Convert(Image<Rgb24> image, CancellationToken cancellationToken = default);
}