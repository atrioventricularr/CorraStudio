using System.Drawing;
using System.Drawing.Imaging;

namespace CorraStudio.Rendering.Processors;

public interface IGifProcessor
{
    Task<byte[]> CreateGifAsync(List<byte[]> frames, int frameDelayMs = 500, bool loop = true);
    Task<byte[]> CreateTimelapseGifAsync(List<byte[]> frames, int fps = 10);
    Task<byte[]> AddGifTextOverlayAsync(byte[] gifData, string text, int positionX, int positionY);
    Task<byte[]> OptimizeGifAsync(byte[] gifData, int quality = 85);
}

public class GifProcessor : IGifProcessor
{
    public async Task<byte[]> CreateGifAsync(List<byte[]> frames, int frameDelayMs = 500, bool loop = true)
    {
        // For GIF creation, we'll use System.Drawing.Imaging
        // This is a simplified version
        using var ms = new MemoryStream();
        
        var firstFrame = Image.FromStream(new MemoryStream(frames[0]));
        var encoder = GetEncoder(ImageFormat.Gif);
        var encoderParams = new EncoderParameters(2);
        encoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
        encoderParams.Param[1] = new EncoderParameter(Encoder.FrameDelay, frameDelayMs / 10);
        
        firstFrame.Save(ms, encoder, encoderParams);
        
        for (int i = 1; i < frames.Count; i++)
        {
            encoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);
            using var frame = Image.FromStream(new MemoryStream(frames[i]));
            firstFrame.SaveAdd(frame, encoderParams);
        }
        
        encoderParams.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);
        firstFrame.SaveAdd(encoderParams);
        firstFrame.Dispose();
        
        return ms.ToArray();
    }

    public async Task<byte[]> CreateTimelapseGifAsync(List<byte[]> frames, int fps = 10)
    {
        var frameDelayMs = 1000 / fps;
        return await CreateGifAsync(frames, frameDelayMs, true);
    }

    public async Task<byte[]> AddGifTextOverlayAsync(byte[] gifData, string text, int positionX, int positionY)
    {
        // For GIF text overlay, we'd need to decode and re-encode
        // This is a placeholder
        await Task.CompletedTask;
        return gifData;
    }

    public async Task<byte[]> OptimizeGifAsync(byte[] gifData, int quality = 85)
    {
        // GIF optimization would involve reducing colors, removing duplicate frames
        await Task.CompletedTask;
        return gifData;
    }

    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
        return ImageCodecInfo.GetImageDecoders().First(c => c.FormatID == format.Guid);
    }
}
