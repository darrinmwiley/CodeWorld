using UnityEngine;
using RoslynCSharp;
using RoslynCSharp.Compiler;

public class Example : MonoBehaviour
{

   public AssemblyReferenceAsset[] assemblyReferences;

   void Start()
   {
      // Add assembly references
      /*ScriptDomain domain = ScriptDomain.CreateDomain("MyTestDomain", true);
      foreach (AssemblyReferenceAsset reference in assemblyReferences)
            domain.RoslynCompilerService.ReferenceAssemblies.Add(reference);

      IMetadataReferenceProvider ref1 = domain.LoadAssembly(typeof(Printer).Assembly.GetName(),ScriptSecurityMode.EnsureLoad);
      IMetadataReferenceProvider[] extraRefs = {ref1};
      
      ScriptAssembly assembly = domain.CompileAndLoadFiles(new string[]{"C:\\Users\\Darrin\\Desktop\\RudeObject.cs"}, ScriptSecurityMode.EnsureLoad, extraRefs);
      ScriptType type = assembly.FindType("RudeObject");
      SimpleObject rudeObject = type.CreateInstanceRaw<SimpleObject>(gameObject);
      rudeObject.SetShape(ShapeType.SPHERE);
      rudeObject.SayHello();*/
   }
}