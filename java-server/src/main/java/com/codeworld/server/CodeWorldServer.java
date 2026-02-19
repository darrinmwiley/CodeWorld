package com.codeworld.server;

import io.grpc.Server;
import io.grpc.ServerBuilder;

public class CodeWorldServer {
    public static void main(String[] args) throws Exception {
        // Standard ServerBuilder works for h2c by default 
        Server server = ServerBuilder
                .forPort(50051)
                .addService(new CodeWorldServiceImpl())
                .build()
                .start();
        
        System.out.println("Services: " + server.getServices());
        System.out.println("Java gRPC server listening on 50051 (h2c)");
        
        server.awaitTermination();
    }
}