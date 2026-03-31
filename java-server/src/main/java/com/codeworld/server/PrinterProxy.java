package com.codeworld.server;

import com.codeworld.generated.ExecuteResponse;
import com.codeworld.generated.PrintIntJob;
import com.codeworld.generated.PrintDoubleJob;
import com.codeworld.generated.PrintBoolJob;

public class PrinterProxy implements Printer {

    private final String targetId;

    public PrinterProxy(String targetId) {
        this.targetId = targetId;
    }

    @Override
    public void print(int value) {
        ExecutionContext ctx = ExecutionContext.get();
        if (ctx != null) {
            ctx.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                    .setPrintInt(PrintIntJob.newBuilder().setTargetId(targetId).setValue(value).build())
                    .build());
        }
    }

    @Override
    public void print(double value) {
        ExecutionContext ctx = ExecutionContext.get();
        if (ctx != null) {
            ctx.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                    .setPrintDouble(PrintDoubleJob.newBuilder().setTargetId(targetId).setValue(value).build())
                    .build());
        }
    }

    @Override
    public void print(boolean value) {
        ExecutionContext ctx = ExecutionContext.get();
        if (ctx != null) {
            ctx.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                    .setPrintBool(PrintBoolJob.newBuilder().setTargetId(targetId).setValue(value).build())
                    .build());
        }
    }
}
