namespace JoyReactor.Accordion.Logic.Extensions;

public static class GuidExtensions
{
    public static Guid ToGuid(this int value)
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(value).CopyTo(bytes, 0);
        return new Guid(bytes);
    }
}