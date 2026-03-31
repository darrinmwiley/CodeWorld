package com.codeworld.server;

import com.codeworld.generated.ExecuteResponse;
import com.codeworld.generated.FindObjectQuery;

import java.util.UUID;
import java.util.concurrent.CompletableFuture;

public class GameUtils {

    @SuppressWarnings("unchecked")
    public static <T> T findObjectInLevel(Class<T> type) {
        ExecutionContext context = ExecutionContext.get();
        if (context == null) {
            System.err.println("GameUtils called outside of an active ExecutionContext!");
            return null;
        }

        String apiName = type.getSimpleName();
        String queryId = UUID.randomUUID().toString();
        CompletableFuture<String> future = context.registerQuery(queryId);

        FindObjectQuery query = FindObjectQuery.newBuilder()
                .setQueryId(queryId)
                .setApiName(apiName)
                .build();

        context.getResponseObserver().onNext(ExecuteResponse.newBuilder()
                .setFindObjectQuery(query)
                .build());

        try {
            // Block execution thread wait for unity to reply
            String objectId = future.get();
            if (objectId == null || objectId.isEmpty()) {
                return null;
            }
            
            if (type == Printer.class) {
                return (T) new PrinterProxy(objectId);
            }

            System.err.println("Unsupported API type: " + apiName);
            return null;

        } catch (Exception e) {
            e.printStackTrace();
            return null;
        }
    }
}
