namespace CorraStudio.Camera.Events;

public class CameraConnectedEventArgs : EventArgs
{
    public CameraInfo CameraInfo { get; set; }
    public DateTime ConnectedAt { get; set; }
}

public class CameraDisconnectedEventArgs : EventArgs
{
    public string CameraId { get; set; } = string.Empty;
    public DateTime DisconnectedAt { get; set; }
}

public class CameraErrorEventArgs : EventArgs
{
    public string CameraId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

public class LiveViewFrameEventArgs : EventArgs
{
    public byte[] FrameData { get; set; } = Array.Empty<byte>();
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime Timestamp { get; set; }
}
