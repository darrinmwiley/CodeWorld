package com.codeworld.server;

import com.codeworld.generated.ExecuteResponse;
import com.codeworld.generated.PrintIntJob;
import com.codeworld.generated.PrintDoubleJob;
import com.codeworld.generated.PrintBoolJob;
import com.codeworld.generated.PrintStringJob;

public class PrinterProxy implements Printer {

    private final String targetId;

    public PrinterProxy(String targetId) {
        this.targetId = targetId;
    }

    @Override
    public void print(int value) {
        ExecutionContext ctx = ExecutionContext.get();
        if (ctx != null) {
            // Send the typed job for anything listening to int events
            ctx.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                    .setPrintInt(PrintIntJob.newBuilder().setTargetId(targetId).setValue(value).build())
                    .build());
            // Also send as string so the output console can display it
            sendString(ctx, String.valueOf(value));
        }
    }

    @Override
    public void print(double value) {
        ExecutionContext ctx = ExecutionContext.get();
        if (ctx != null) {
            ctx.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                    .setPrintDouble(PrintDoubleJob.newBuilder().setTargetId(targetId).setValue(value).build())
                    .build());
            sendString(ctx, String.valueOf(value));
        }
    }

    @Override
    public void print(boolean value) {
        ExecutionContext ctx = ExecutionContext.get();
        if (ctx != null) {
            ctx.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                    .setPrintBool(PrintBoolJob.newBuilder().setTargetId(targetId).setValue(value).build())
                    .build());
            sendString(ctx, String.valueOf(value));
        }
    }

    @Override
    public void print(String value) {
        ExecutionContext ctx = ExecutionContext.get();
        if (ctx != null) {
            sendString(ctx, value);
        }
    }

    private void sendString(ExecutionContext ctx, String text) {
        ctx.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                .setPrintString(PrintStringJob.newBuilder().setValue(text).build())
                .build());
    }
}
