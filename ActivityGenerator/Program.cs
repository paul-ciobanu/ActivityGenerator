using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;

namespace ActivityGenerator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (!args.Any() || args[0].Equals("--help"))
            {
                WriteHelp();
                return;
            }

            string namespaceName = args[0];
            string className = args[1];

            int resxIndex = Array.IndexOf(args, "-resx");
            int inIndex = Array.IndexOf(args, "-in");
            int outIndex = Array.IndexOf(args, "-out");

            int inArgsCount = inIndex > 0
                              ? outIndex > 0 ? outIndex - inIndex : args.Length - inIndex
                              : 0;
            inArgsCount--;

            int outArgsCount = outIndex > 0
                ? args.Length - outIndex
                : 0;
            outArgsCount--;

            string resxFilePath = args.Skip(resxIndex + 1).Take(1).ElementAt(0);
            string ns = ConstructNamespace(namespaceName);
            string classText = ConstructClass(className, resxFilePath);
            string inArgsText = GetArgumentsText(args.Skip(inIndex + 1).Take(inArgsCount).ToArray(), ArgumentType.In, resxFilePath);
            string outArgs = GetArgumentsText(args.Skip(outIndex + 1).Take(outArgsCount).ToArray(), ArgumentType.Out, resxFilePath);
            string result = string.Format(ns, string.Format(classText, inArgsText, outArgs));

            File.WriteAllText($"{className}.cs", result);
        }

        private static string ConstructNamespace(string name)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Activities;");
            sb.AppendLine("using System.Diagnostics;");
            sb.AppendLine($"using UiPath.{name}.Activities.Properties;");
            sb.AppendLine();
            sb.AppendLine($"namespace UiPath.{name}.Activities");
            sb.AppendLine("{{");
            sb.AppendLine("{0}");
            sb.AppendLine("}}");
            return sb.ToString();
        }

        private static string ConstructClass(string name, string resxPath)
        {
            string displayNameKey = $"{name}DisplayName";
            string descriptionKey = $"{name}Description";

            AddResourceIfNotPresent(resxPath, displayNameKey, "myvalue", "activity name");
            AddResourceIfNotPresent(resxPath, descriptionKey, "myvalue", null);

            var sb = new StringBuilder();
            sb.AppendLine($"\t[LocalizedDisplayName(nameof(Resources.{displayNameKey}))]");
            sb.AppendLine($"\t[LocalizedDescription(nameof(Resources.{descriptionKey}))]");
            sb.AppendLine($"\tpublic class {name}");
            sb.AppendLine("\t{{");
            sb.Append("{0}");
            sb.Append("{1}");
            sb.Append("\t}}");
            return sb.ToString();
        }

        private static string GetArgumentsText(string[] args, ArgumentType type, string resxPath)
        {
            var sb = new StringBuilder();

            foreach (string arg in args)
            {
                sb.AppendLine(GetArgument(arg, type, resxPath));
            }

            return sb.ToString();
        }

        private static string GetArgument(string name, ArgumentType type, string resxPath)
        {
            string prefix = type == ArgumentType.In ? "In" : "Out";
            string displayNameKey = $"{name}DisplayName";
            string descriptionKey = $"{name}Description";

            AddResourceIfNotPresent(resxPath, displayNameKey, "myvalue", "property name");
            AddResourceIfNotPresent(resxPath, descriptionKey, "myvalue", null);

            var sb = new StringBuilder();
            sb.AppendLine($"\t\t[LocalizedCategory(nameof(Resources.{prefix}put))]");
            sb.AppendLine($"\t\t[LocalizedDisplayName(nameof(Resources.{displayNameKey}))]");
            sb.AppendLine($"\t\t[LocalizedDescription(nameof(Resources.{descriptionKey}))]");
            sb.AppendLine($"\t\tpublic {prefix}Argument<string> {name} {{ get; set; }}");
            return sb.ToString();
        }

        private static void AddResourceIfNotPresent(string resxpath, string key, string value, string comment)
        {
            if (string.IsNullOrWhiteSpace(resxpath))
            {
                return;
            }

            using (ResXResourceWriter writer = new ResXResourceWriter(resxpath))
            using (ResXResourceReader reader = new ResXResourceReader(resxpath))
            {
                reader.UseResXDataNodes = true;
                bool found = false;
                var node = reader.GetEnumerator();
                while (node.MoveNext())
                {
                    ResXDataNode nodeValue = (ResXDataNode)node.Value;
                    writer.AddResource(new ResXDataNode(nodeValue.Name, nodeValue.GetValue((ITypeResolutionService)null))
                    {
                        Comment = nodeValue.Comment
                    });
                    if (node.Key.ToString() == key && !string.IsNullOrWhiteSpace(node.Value?.ToString()))
                    {
                        Console.WriteLine(node.Key.ToString() + " exists");
                        found = true;
                    }
                }

                if (!found)
                {
                    writer.AddResource(new ResXDataNode(key, value) { Comment = comment});
                }
            }
        }

        private static void WriteHelp()
        {
            Console.WriteLine("usage: ActivityGenerator.exe namespace activity [-resx resxpath] [-in [in_args...]] [-out [out_args...]]");
            Console.WriteLine("  generates a file <activity>.cs next to executable, containing the boilerplate code for an activity class");
            Console.WriteLine("  options:");
            Console.WriteLine("    -in:\tlist of input arguments");
            Console.WriteLine("    -out:\tlist of output arguments");
        }

        private enum ArgumentType
        {
            In,
            Out
        }
    }
}
