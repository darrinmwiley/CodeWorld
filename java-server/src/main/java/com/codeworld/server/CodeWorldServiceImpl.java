package com.codeworld.server;

import com.codeworld.generated.CodeWorldServiceGrpc;
import com.codeworld.generated.ColorRequest;
import com.codeworld.generated.ObjectStatus;

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
}
