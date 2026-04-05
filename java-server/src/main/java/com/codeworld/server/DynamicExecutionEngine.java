package com.codeworld.server;

import javax.tools.*;
import java.io.File;
import java.lang.reflect.Method;
import java.net.URL;
import java.net.URLClassLoader;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.Arrays;
import java.util.List;
import java.util.Locale;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class DynamicExecutionEngine {

    /**
     * Compiles and runs a valid Java class string with a main method.
     * @param javaCode The full class definition
     * @throws CompilationException if compilation fails (carries line-by-line diagnostics)
     * @throws Exception if execution fails
     */
    public static void run(String javaCode) throws Exception {
        // Extract the class name from the code string
        String className = extractClassName(javaCode);
        if (className == null) {
            throw new IllegalArgumentException("Could not find a 'public class <Name>' declaration in the provided code.");
        }

        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();
        if (compiler == null) {
            throw new IllegalStateException("JDK JavaCompiler not found. Please ensure the server is running with a JDK, not just a JRE.");
        }

        // Write the source file to a temporary directory
        Path tempDir = Files.createTempDirectory("codeworld_exec");
        File sourceFile = new File(tempDir.toFile(), className + ".java");
        Files.writeString(sourceFile.toPath(), javaCode);

        // Use DiagnosticCollector to capture specific error messages
        DiagnosticCollector<JavaFileObject> diagnostics = new DiagnosticCollector<>();
        String classpath = System.getProperty("java.class.path");

        try (StandardJavaFileManager fileManager = compiler.getStandardFileManager(diagnostics, Locale.getDefault(), null)) {
            Iterable<? extends JavaFileObject> compilationUnits =
                    fileManager.getJavaFileObjects(sourceFile);

            List<String> options = Arrays.asList("-cp", classpath);
            JavaCompiler.CompilationTask task = compiler.getTask(null, fileManager, diagnostics, options, null, compilationUnits);
            boolean success = task.call();

            if (!success) {
                StringBuilder sb = new StringBuilder();
                for (Diagnostic<? extends JavaFileObject> d : diagnostics.getDiagnostics()) {
                    if (d.getKind() == Diagnostic.Kind.ERROR) {
                        sb.append("Line ").append(d.getLineNumber())
                          .append(": ").append(d.getMessage(Locale.getDefault()))
                          .append("\n");
                    }
                }
                throw new CompilationException(sb.toString().trim());
            }
        }

        // Load the compiled class dynamically
        URLClassLoader classLoader = URLClassLoader.newInstance(new URL[]{tempDir.toUri().toURL()});
        Class<?> cls = Class.forName(className, true, classLoader);

        // Find the main method. Support "Main(String[])" or "main(String[])"
        Method mainMethod = null;
        try {
            mainMethod = cls.getMethod("main", String[].class);
        } catch (NoSuchMethodException e) {
            try {
                mainMethod = cls.getMethod("Main", String[].class);
            } catch (NoSuchMethodException e2) {
                throw new NoSuchMethodException("Could not find public static void main(String[] args) in class " + className);
            }
        }

        // Invoke it!
        String[] emptyArgs = new String[0];
        mainMethod.invoke(null, (Object) emptyArgs);
    }

    private static String extractClassName(String code) {
        Pattern pattern = Pattern.compile("public\\s+class\\s+([A-Za-z0-9_]+)");
        Matcher matcher = pattern.matcher(code);
        if (matcher.find()) {
            return matcher.group(1);
        }
        return null;
    }
}
