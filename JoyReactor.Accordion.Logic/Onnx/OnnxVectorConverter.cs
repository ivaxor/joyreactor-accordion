using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Runtime.InteropServices;

namespace JoyReactor.Accordion.Logic.Onnx;

public class OnnxVectorConverter(
    InferenceSession inferenceSession,
    IOptions<OnnxSettings> settings)
    : IOnnxVectorConverter
{
    protected static readonly float[] Mean = [0.48145466f, 0.4578275f, 0.40821073f];
    protected static readonly float[] Std = [0.26862954f, 0.26130258f, 0.27577711f];
    protected static readonly RunOptions RunOptions = new();

    public async Task<float[]> ConvertAsync(Image<Rgb24> image)
    {
        var inputSize = settings.Value.InputSize;
        var inputBufferSize = 3 * inputSize * inputSize;
        var outputBufferSize = settings.Value.OutputSize;

        var inputBuffer = ArrayPool<float>.Shared.Rent(inputBufferSize);
        var outputBuffer = ArrayPool<float>.Shared.Rent(outputBufferSize);

        var inputBufferHandle = (GCHandle)default;
        var outputBufferHandle = (GCHandle)default;

        try
        {
            inputBufferHandle = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
            outputBufferHandle = GCHandle.Alloc(outputBuffer, GCHandleType.Pinned);

            FillInputBuffer(image, inputBuffer, inputSize);

            using var inputValue = OrtValue.CreateTensorValueFromMemory(
                OrtMemoryInfo.DefaultInstance,
                inputBuffer.AsMemory(0, inputBufferSize),
                [1, 3, inputSize, inputSize]);

            using var outputValue = OrtValue.CreateTensorValueFromMemory(
                OrtMemoryInfo.DefaultInstance,
                outputBuffer.AsMemory(0, outputBufferSize),
                [1, outputBufferSize]);

            if (settings.Value.UseCpu)
                inferenceSession.Run(
                        RunOptions,
                        [settings.Value.InputName],
                        [inputValue],
                        [settings.Value.OutputName],
                        [outputValue]);
            else
                await inferenceSession.RunAsync(
                    RunOptions,
                    [settings.Value.InputName],
                    [inputValue],
                    [settings.Value.OutputName],
                    [outputValue]);

            L2Normalize(outputBuffer, outputBufferSize);

            return outputBuffer.AsSpan(0, outputBufferSize).ToArray();
        }
        finally
        {
            if (inputBufferHandle.IsAllocated)
                inputBufferHandle.Free();

            if (outputBufferHandle.IsAllocated)
                outputBufferHandle.Free();

            ArrayPool<float>.Shared.Return(inputBuffer);
            ArrayPool<float>.Shared.Return(outputBuffer);
        }
    }

    protected static void FillInputBuffer(Image<Rgb24> image, float[] buffer, int size)
    {
        var channelSize = size * size;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < size; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < size; x++)
                {
                    int offset = y * size + x;
                    buffer[offset] = (row[x].R / 255f - Mean[0]) / Std[0];
                    buffer[offset + channelSize] = (row[x].G / 255f - Mean[1]) / Std[1];
                    buffer[offset + 2 * channelSize] = (row[x].B / 255f - Mean[2]) / Std[2];
                }
            }
        });
    }

    protected static void L2Normalize(float[] buffer, int size)
    {
        var sum = 0f;
        for (var i = 0; i < size; i++)
            sum += buffer[i] * buffer[i];

        var norm = MathF.Sqrt(sum);
        if (norm < 1e-10f)
            return;

        var invNorm = 1.0f / norm;
        for (var i = 0; i < size; i++)
            buffer[i] *= invNorm;
    }
}

public interface IOnnxVectorConverter
{
    Task<float[]> ConvertAsync(Image<Rgb24> image);
}