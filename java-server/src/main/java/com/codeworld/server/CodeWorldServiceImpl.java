package com.codeworld.server;

import com.codeworld.generated.CodeWorldServiceGrpc;
import com.codeworld.generated.ColorRequest;
import com.codeworld.generated.ObjectStatus;
import com.codeworld.generated.ExecuteRequest;
import com.codeworld.generated.ExecuteResponse;
import com.codeworld.generated.PrintIntJob;
import com.codeworld.generated.PrintDoubleJob;
import com.codeworld.generated.PrintBoolJob;
import com.codeworld.generated.CompileError;
import com.codeworld.generated.RuntimeError;

import io.grpc.stub.StreamObserver;

import java.lang.reflect.InvocationTargetException;
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
                            } catch (CompilationException ce) {
                                System.err.println("Compile error: " + ce.getMessage());
                                responseObserver.onNext(ExecuteResponse.newBuilder()
                                        .setCompileError(CompileError.newBuilder()
                                                .setMessage(ce.getMessage())
                                                .build())
                                        .build());
                            } catch (InvocationTargetException ite) {
                                Throwable cause = ite.getTargetException();
                                String errorMsg = cause.getClass().getSimpleName() + ": " +
                                        (cause.getMessage() != null ? cause.getMessage() : "(no message)");
                                System.err.println("Runtime error: " + errorMsg);
                                responseObserver.onNext(ExecuteResponse.newBuilder()
                                        .setRuntimeError(RuntimeError.newBuilder()
                                                .setMessage(errorMsg)
                                                .build())
                                        .build());
                            } catch (Exception e) {
                                String errorMsg = e.getMessage() != null ? e.getMessage() : e.getClass().getSimpleName();
                                System.err.println("Execution setup error: " + errorMsg);
                                responseObserver.onNext(ExecuteResponse.newBuilder()
                                        .setRuntimeError(RuntimeError.newBuilder()
                                                .setMessage(errorMsg)
                                                .build())
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
