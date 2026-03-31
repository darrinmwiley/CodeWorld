package com.codeworld.server;

import com.codeworld.generated.CodeWorldServiceGrpc;
import com.codeworld.generated.ColorRequest;
import com.codeworld.generated.ObjectStatus;
import com.codeworld.generated.ExecuteRequest;
import com.codeworld.generated.ExecuteResponse;
import com.codeworld.generated.PrintIntJob;
import com.codeworld.generated.PrintDoubleJob;
import com.codeworld.generated.PrintBoolJob;

import io.grpc.stub.StreamObserver;

import java.util.Random;
import java.util.concurrent.*;

public class CodeWorldServiceImpl extends CodeWorldServiceGrpc.CodeWorldServiceImplBase {

    private final ScheduledExecutorService scheduler = Executors.newSingleThreadScheduledExecutor();
    private final Random rng = new Random();

    @Override
    public StreamObserver<ObjectStatus> streamObjectData(StreamObserver<ColorRequest> responseObserver) {

        ScheduledFuture<?> ticker = scheduler.scheduleAtFixedRate(() -> {
            ColorRequest color = ColorRequest.newBuilder()
                    .setR(rng.nextFloat())
                    .setG(rng.nextFloat())
                    .setB(rng.nextFloat())
                    .build();
            responseObserver.onNext(color);
        }, 0, 1, TimeUnit.SECONDS);

        return new StreamObserver<>() {
            @Override
            public void onNext(ObjectStatus value) {
                System.out.println("From Unity: " + value.getMessage());
            }

            @Override
            public void onError(Throwable t) {
                System.out.println("Stream error: " + t);
                ticker.cancel(true);
            }

            @Override
            public void onCompleted() {
                System.out.println("Unity disconnected.");
                ticker.cancel(true);
                responseObserver.onCompleted();
            }
        };
    }

    @Override
    public StreamObserver<ExecuteRequest> executeCodeStream(StreamObserver<ExecuteResponse> responseObserver) {
        ExecutionContext context = new ExecutionContext(responseObserver);

        return new StreamObserver<>() {
            @Override
            public void onNext(ExecuteRequest request) {
                switch (request.getPayloadCase()) {
                    case JAVA_CODE:
                        String code = request.getJavaCode();
                        System.out.println("Received Java code to execute:\n" + code);

                        // Run execution in a separate thread so it can block waiting for Unity
                        new Thread(() -> {
                            ExecutionContext.set(context);
                            try {
                                DynamicExecutionEngine.run(code);
                                responseObserver.onNext(ExecuteResponse.newBuilder()
                                        .setExecutionCompleted("Success")
                                        .build());
                            } catch (Exception e) {
                                System.err.println("Execution failed: " + e.getMessage());
                                responseObserver.onNext(ExecuteResponse.newBuilder()
                                        .setExecutionCompleted("Error: " + e.getMessage())
                                        .build());
                            } finally {
                                ExecutionContext.clear();
                            }
                        }).start();
                        break;

                    case FIND_OBJECT_RESULT:
                        com.codeworld.generated.FindObjectResult result = request.getFindObjectResult();
                        System.out.println("Received object pointer from Unity: " + result.getObjectId());
                        context.completeQuery(result.getQueryId(), result.getObjectId());
                        break;

                    case PAYLOAD_NOT_SET:
                        System.err.println("Received empty payload in ExecuteRequest");
                        break;
                }
            }

            @Override
            public void onError(Throwable t) {
                System.out.println("Execution stream error: " + t);
            }

            @Override
            public void onCompleted() {
                System.out.println("Execution stream closed by client.");
                responseObserver.onCompleted();
            }
        };
    }
}
