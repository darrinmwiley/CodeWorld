using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

using Grpc.Core;
using Grpc.Net.Client;
using Cysharp.Net.Http;

public class CodeExecutor : MonoBehaviour
{
    [Header("Java server address")]
    public string serverAddress = "http://127.0.0.1:50051";

    [Header("Output Console")]
    [SerializeField] private OutputConsoleController _outputConsole;

    [TextArea(15, 30)]
    public string javaCodeToExecute = @"package com.codeworld.server;

public class MyUserCode {
    public static void Main(Printer printer) {
        printer.print(42);
        printer.print(3.14159);
        printer.print(true);
    }
}";

    private CancellationTokenSource _cts;
    private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
    private CodeWorldService.CodeWorldServiceClient _client;
    private AsyncDuplexStreamingCall<ExecuteRequest, ExecuteResponse> _call;

    private async void Start()
    {
        _cts = new CancellationTokenSource();

        try
        {
            var handler = new YetAnotherHttpHandler() { Http2Only = true };
            var httpClient = new HttpClient(handler);
            var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpClient = httpClient,
                UnsafeUseInsecureChannelCallCredentials = true 
            });

            _client = new CodeWorldService.CodeWorldServiceClient(channel);
            _call = _client.ExecuteCodeStream(cancellationToken: _cts.Token);

            // Start listening for print events
            _ = Task.Run(ReadLoop, _cts.Token);

        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("gRPC connect failed: " + ex);
        }
    }

    public async void ExecuteActiveTab()
    {
        if (_outputConsole != null && _outputConsole.clearOnNewRun)
            _outputConsole.Clear();

        var tabbedConsole = FindObjectOfType<TabbedConsoleWindowController>();
        if (tabbedConsole != null)
        {
            var console = tabbedConsole.GetActiveConsole();
            if (console != null && console.stateManager != null)
            {
                var lines = console.stateManager.ExportLines();
                var sb = new System.Text.StringBuilder();
                foreach (var l in lines) 
                {
                    sb.AppendLine(l.content);
                }
                
                string code = sb.ToString();
                if (string.IsNullOrWhiteSpace(code))
                {
                    _outputConsole?.AppendLine("[Error] Cannot execute an empty script.", isError: true);
                    return;
                }

                await SendCode(code);
                return;
            }
        }

        // Fallback to inspector string if no active tab found
        await SendCode(javaCodeToExecute);
    }

    private void Update()
    {
        while (_mainThread.TryDequeue(out var action))
        {
            action();
        }
    }

    public async Task SendCode(string code)
    {
        if (_call != null)
        {
            UnityEngine.Debug.Log("Sending Java code to Java Server...");
            await _call.RequestStream.WriteAsync(new ExecuteRequest { JavaCode = code });
        }
    }

    private async Task ReadLoop()
    {
        try
        {
            await foreach (var response in _call.ResponseStream.ReadAllAsync(_cts.Token))
            {
                _mainThread.Enqueue(() =>
                {
                    switch (response.EventCase)
                    {
                        case ExecuteResponse.EventOneofCase.PrintInt:
                            DispatchRpc(response.PrintInt.TargetId, "Print", response.PrintInt.Value);
                            break;

                        case ExecuteResponse.EventOneofCase.PrintDouble:
                            DispatchRpc(response.PrintDouble.TargetId, "Print", response.PrintDouble.Value);
                            break;

                        case ExecuteResponse.EventOneofCase.PrintBool:
                            DispatchRpc(response.PrintBool.TargetId, "Print", response.PrintBool.Value);
                            break;

                        case ExecuteResponse.EventOneofCase.PrintString:
                            // Intentionally do not mirror program print output into the console.
                            // We only show compile/runtime status messages there.
                            break;

                        case ExecuteResponse.EventOneofCase.CompileError:
                            _outputConsole?.AppendLine("[Compile Error]\n" + response.CompileError.Message, isError: true);
                            break;

                        case ExecuteResponse.EventOneofCase.RuntimeError:
                            _outputConsole?.AppendLine("[Runtime Error] " + response.RuntimeError.Message, isError: true);
                            break;

                        case ExecuteResponse.EventOneofCase.FindObjectQuery:
                            HandleFindObjectQuery(response.FindObjectQuery);
                            break;

                        case ExecuteResponse.EventOneofCase.ExecutionCompleted:
                            if (response.ExecutionCompleted == "Success")
                                _outputConsole?.AppendLine("compiled and ran successfully", isSuccess: true);
                            else
                                _outputConsole?.AppendLine("[Done] " + response.ExecutionCompleted);
                            UnityEngine.Debug.Log($"[Execution] Completed: {response.ExecutionCompleted}");
                            break;
                    }
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("ReadLoop error: " + ex);
        }
    }

    private async void HandleFindObjectQuery(FindObjectQuery query)
    {
        string foundId = "";
        var objects = FindObjectsOfType<CodeWorldObject>();
        foreach (var obj in objects)
        {
            if (((System.Collections.Generic.List<string>)obj.ImplementedApis).Contains(query.ApiName))
            {
                foundId = obj.ObjectId;
                break;
            }
        }

        try
        {
            await _call.RequestStream.WriteAsync(new ExecuteRequest 
            {
                FindObjectResult = new FindObjectResult 
                {
                    QueryId = query.QueryId,
                    ObjectId = foundId
                }
            });
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("Failed to send FindObjectResult: " + ex);
        }
    }

    private void DispatchRpc(string targetId, string method, object value)
    {
        var objects = FindObjectsOfType<CodeWorldObject>();
        foreach (var obj in objects)
        {
            if (obj.ObjectId == targetId)
            {
                obj.HandleRpcRequest(method, value);
                return;
            }
        }
        UnityEngine.Debug.LogWarning($"[RPC] Target pointer '{targetId}' not found for method '{method}'.");
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
