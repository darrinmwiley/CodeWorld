using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoslynCSharp;
using RoslynCSharp.Compiler;
using System.IO;

public class ButtonController : MonoBehaviour
{

    public ClickListener clickListener;
    public Animator animator;
    public GameObject consoleCharPrefab;
    public GameObject printer;
    public AssemblyReferenceAsset[] assemblyReferences;

    // Start is called before the first frame update
    void Start()
    {
        clickListener.AddClickHandler(OnPress);
    }

    public void OnPress(){
        animator.SetTrigger("press");
        CreateConnectedConsoleChars();
    }

    public void CreateConnectedConsoleChars()
    {
        // Create an empty GameObject to serve as the parent transform
        GameObject parent = new GameObject("ConnectedConsoleChars");

        string toPrint = GetItemToPrint()+"";
        Debug.Log(toPrint);
        // Loop to create four connected consoleChars with text "1234"
        for (int i = 0; i < toPrint.Length; i++)
        {
            GameObject consoleChar = Instantiate(consoleCharPrefab, parent.transform);
            consoleChar.GetComponent<ConsoleCharController>().UpdateText(toPrint[i]+"");
            consoleChar.transform.position = new Vector3(i*.6f,0,0);
            Debug.Log("position is "+consoleChar.transform.position);
            //consoleChar.transform.localScale = new Vector3(.3f,.3f,.3f);
            consoleChar.layer = LayerMask.NameToLayer("Default");
        }

        parent.transform.parent = printer.transform;
        parent.transform.localPosition = new Vector3(.31f,-.36f,.3f);
        parent.transform.localScale = new Vector3(.1f,.1f,.1f);
        parent.transform.localRotation = Quaternion.identity;
        parent.AddComponent<Rigidbody>();
        parent.AddComponent<BoxCollider>();
    }

   int GetItemToPrint()
   {
      // Add assembly references
      ScriptDomain domain = ScriptDomain.CreateDomain("MyTestDomain", true);
      foreach (AssemblyReferenceAsset reference in assemblyReferences)
            domain.RoslynCompilerService.ReferenceAssemblies.Add(reference);

      IMetadataReferenceProvider ref1 = domain.LoadAssembly(typeof(Printer).Assembly.GetName(),ScriptSecurityMode.EnsureLoad);
      IMetadataReferenceProvider[] extraRefs = {ref1};

      //need means of saving and loading files from the local directory
      
      ScriptAssembly assembly = domain.CompileAndLoadFiles(new string[]{Path.Combine(Application.persistentDataPath, "fname.txt")}, ScriptSecurityMode.EnsureLoad, extraRefs);
      ScriptType type = assembly.FindType("PrinterImpl");
      GameObject gameObj = new GameObject("printer impl");
      Printer printer = type.CreateInstanceRaw<Printer>(gameObj);
      return printer.Print();

   }
}
