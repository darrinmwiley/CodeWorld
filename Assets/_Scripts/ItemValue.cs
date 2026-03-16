using UnityEngine;

public class ItemValue : MonoBehaviour
{
    // Store data as a string (e.g., "true", "42", "3.14")
    public string value; 

    // Helper to get the boolean value
    public bool ToBool()
    {
        bool.TryParse(value, out bool result);
        return result;
    }

    // Helper to get the integer value
    public int ToInt()
    {
        int.TryParse(value, out int result);
        return result;
    }

    // Helper to get the double value
    public double ToDouble()
    {
        double.TryParse(value, out double result);
        return result;
    }
}