package com.codeworld.server;

import com.codeworld.generated.ExecuteResponse;
import io.grpc.stub.StreamObserver;

import java.util.Map;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ConcurrentHashMap;

public class ExecutionContext {

    private static final ThreadLocal<ExecutionContext> currentContext = new ThreadLocal<>();

    private final StreamObserver<ExecuteResponse> responseObserver;
    private final Map<String, CompletableFuture<String>> pendingQueries = new ConcurrentHashMap<>();

    public ExecutionContext(StreamObserver<ExecuteResponse> responseObserver) {
        this.responseObserver = responseObserver;
    }

    public static void set(ExecutionContext context) {
        currentContext.set(context);
    }

    public static ExecutionContext get() {
        return currentContext.get();
    }

    public static void clear() {
        currentContext.remove();
    }

    public StreamObserver<ExecuteResponse> getResponseObserver() {
        return responseObserver;
    }

    public CompletableFuture<String> registerQuery(String queryId) {
        CompletableFuture<String> future = new CompletableFuture<>();
        pendingQueries.put(queryId, future);
        return future;
    }

    public void completeQuery(String queryId, String result) {
        CompletableFuture<String> future = pendingQueries.remove(queryId);
        if (future != null) {
            future.complete(result);
        }
    }
}
