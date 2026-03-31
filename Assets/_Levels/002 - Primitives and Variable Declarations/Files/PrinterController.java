public class PrinterController
{
    public static void main(String[] args)
    {
        // Pass the Class reference directly in Java:
        Printer printer = GameUtils.findObjectInLevel(Printer.class);
        
        if (printer != null) {
            int i = 42;
            printer.print(i);
        }
    }
}
