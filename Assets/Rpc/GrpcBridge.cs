using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using Grpc.Core;
using Grpc.Net.Client;
using Cysharp.Net.Http; // YetAnotherHttpHandler

public class GrpcBridge : MonoBehaviour
{
    [Header("Assign cube renderer")]
    public Renderer cubeRenderer;

    [Header("Java server address")]
    public string serverAddress = "http://127.0.0.1:50051";

    private CancellationTokenSource _cts;
    private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();

    private CodeWorldService.CodeWorldServiceClient _client;
    private AsyncDuplexStreamingCall<ObjectStatus, ColorRequest> _call;
    private Stopwatch _heartbeatTimer;

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        _heartbeatTimer = Stopwatch.StartNew();

        try
        {
            // Fix 1: Force HTTP/2 for h2c (unencrypted) connections
            var handler = new YetAnotherHttpHandler()
            {
                Http2Only = true
            };
            
            var httpClient = new HttpClient(handler);

            var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpClient = httpClient,
                // Required for older gRPC versions/non-TLS
                UnsafeUseInsecureChannelCallCredentials = true 
            });

            _client = new CodeWorldService.CodeWorldServiceClient(channel);

            _call = _client.StreamObjectData(cancellationToken: _cts.Token);

            _ = Task.Run(ReadLoop, _cts.Token);
            _ = Task.Run(WriteLoop, _cts.Token);

            UnityEngine.Debug.Log("gRPC connected and loops started.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("gRPC connect failed: " + ex);
        }
    }

    private void Update()
    {
        while (_mainThread.TryDequeue(out var a))
            a();
    }

    private async Task ReadLoop()
    {
        try
        {
            await foreach (var color in _call.ResponseStream.ReadAllAsync(_cts.Token))
            {
                float r = Mathf.Clamp01(color.R);
                float g = Mathf.Clamp01(color.G);
                float b = Mathf.Clamp01(color.B);

                _mainThread.Enqueue(() =>
                {
                    if (cubeRenderer != null)
                        cubeRenderer.material.color = new Color(r, g, b, 1f);
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("ReadLoop error: " + ex);
        }
    }

    private async Task WriteLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                // Fix 2: Use Stopwatch instead of Time.time (which is main-thread only)
                double elapsed = _heartbeatTimer.Elapsed.TotalSeconds;

                await _call.RequestStream.WriteAsync(new ObjectStatus
                {
                    Message = $"Unity alive t={elapsed:0.0}"
                });

                await Task.Delay(1000, _cts.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // Note: Use UnityEngine.Debug explicitly if inside Task.Run
            UnityEngine.Debug.LogError("WriteLoop error: " + ex);
        }
    }

    private async void OnDestroy()
    {
        try
        {
            _cts?.Cancel();
            if (_call != null)
            {
                await _call.RequestStream.CompleteAsync();
                _call.Dispose();
            }
        }
        catch { }
    }
}