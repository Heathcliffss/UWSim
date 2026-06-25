using System;
using System.IO.MemoryMappedFiles;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class UnityCameraSharedMemorySender : MonoBehaviour
{
    [Header("Capture")]
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private int width = 835;
    [SerializeField] private int height = 469;
    [SerializeField] private float sendRateHz = 20f;
    [SerializeField] private bool flipVerticalForOpenCV = true;

    [Header("Shared Memory")]
    [SerializeField] private string mappingName = @"Local\BlueROV_Camera_Frame";

    private const int HeaderSize = 64;
    private const int Magic = 0x42524F56; // "BROV"
    private const int Version = 1;
    private const int Channels = 4;       // RGBA

    // Header offsets (little-endian)
    private const long OffMagic = 0;
    private const long OffVersion = 4;
    private const long OffWidth = 8;
    private const long OffHeight = 12;
    private const long OffChannels = 16;
    private const long OffBufferSize = 20;
    private const long OffActiveBuffer = 24;
    private const long OffFrameId = 32;
    private const long OffTimestampUnix = 40;
    private const long OffFlags = 48;

    private MemoryMappedFile mappedFile;
    private MemoryMappedViewAccessor view;
    private RenderTexture captureTexture;
    private RenderTexture previousTargetTexture;

    private byte[][] managedBuffers;
    private int bufferSize;
    private int activeBuffer;
    private long frameId;
    private float nextRequestTime;
    private bool requestInFlight;
    private bool running;

    private void Awake()
    {
        if (sourceCamera == null)
            sourceCamera = GetComponent<Camera>();

        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        sendRateHz = Mathf.Max(1f, sendRateHz);

        bufferSize = checked(width * height * Channels);
        long totalSize = HeaderSize + (2L * bufferSize);

        mappedFile = MemoryMappedFile.CreateOrOpen(
            mappingName,
            totalSize,
            MemoryMappedFileAccess.ReadWrite
        );
        view = mappedFile.CreateViewAccessor(
            0,
            totalSize,
            MemoryMappedFileAccess.ReadWrite
        );

        managedBuffers = new[]
        {
            new byte[bufferSize],
            new byte[bufferSize]
        };

        captureTexture = new RenderTexture(
            width,
            height,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default
        )
        {
            name = "BlueROV_SharedMemory_Capture",
            useMipMap = false,
            autoGenerateMips = false,
            antiAliasing = 1
        };
        captureTexture.Create();

        previousTargetTexture = sourceCamera.targetTexture;
        sourceCamera.targetTexture = captureTexture;

        WriteStaticHeader();

        activeBuffer = 0;
        frameId = 0;
        view.Write(OffActiveBuffer, activeBuffer);
        view.Write(OffFrameId, frameId);
        view.Write(OffTimestampUnix, 0.0);

        running = true;
        nextRequestTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!running || requestInFlight)
            return;

        if (Time.unscaledTime < nextRequestTime)
            return;

        nextRequestTime = Time.unscaledTime + (1f / sendRateHz);
        requestInFlight = true;

        AsyncGPUReadback.Request(
            captureTexture,
            0,
            TextureFormat.RGBA32,
            OnReadbackComplete
        );
    }

    private void OnReadbackComplete(AsyncGPUReadbackRequest request)
    {
        requestInFlight = false;

        if (!running || request.hasError)
        {
            if (request.hasError)
                Debug.LogWarning("BlueROV shared-memory GPU readback failed.");
            return;
        }

        var data = request.GetData<byte>();
        if (data.Length != bufferSize)
        {
            Debug.LogWarning(
                $"Unexpected frame size. Expected {bufferSize}, received {data.Length}."
            );
            return;
        }

        int writeBuffer = 1 - activeBuffer;
        data.CopyTo(managedBuffers[writeBuffer]);

        long imageOffset = HeaderSize + ((long)writeBuffer * bufferSize);
        view.WriteArray(
            imageOffset,
            managedBuffers[writeBuffer],
            0,
            bufferSize
        );

        // Publish metadata only after the complete frame has been written.
        double unixSeconds =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        frameId++;
        view.Write(OffTimestampUnix, unixSeconds);
        view.Write(OffActiveBuffer, writeBuffer);
        view.Write(OffFrameId, frameId); // Commit marker: write last.

        activeBuffer = writeBuffer;
    }

    private void WriteStaticHeader()
    {
        view.Write(OffMagic, Magic);
        view.Write(OffVersion, Version);
        view.Write(OffWidth, width);
        view.Write(OffHeight, height);
        view.Write(OffChannels, Channels);
        view.Write(OffBufferSize, bufferSize);
        view.Write(OffFlags, flipVerticalForOpenCV ? 1 : 0);
    }

    private void OnDestroy()
    {
        running = false;

        if (sourceCamera != null)
            sourceCamera.targetTexture = previousTargetTexture;

        if (captureTexture != null)
        {
            captureTexture.Release();
            Destroy(captureTexture);
        }

        view?.Dispose();
        mappedFile?.Dispose();
    }
}
