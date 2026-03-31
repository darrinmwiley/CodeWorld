package com.codeworld.server;

import javax.tools.JavaCompiler;
import javax.tools.ToolProvider;
import java.io.File;
import java.lang.reflect.Method;
import java.net.URL;
import java.net.URLClassLoader;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

public class DynamicExecutionEngine {

    /**
     * Compiles and runs a valid Java class string with a main method.
     * @param javaCode The full class definition (e.g. `public class MyGame { public static void main(String[] args) { ... } }`)
     * @throws Exception if compilation or execution fails
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

        // Compile the source file, making sure current classpath is included so it knows about Printer
        String classpath = System.getProperty("java.class.path");
        int result = compiler.run(null, null, null, "-cp", classpath, sourceFile.getAbsolutePath());
        
        if (result != 0) {
            throw new Exception("Compilation failed. Please check server logs for syntax errors.");
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
