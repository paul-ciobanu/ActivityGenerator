using System;
using System.IO;
using System.Linq;
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

            string ns = ConstructNamespace(namespaceName);
            string classText = ConstructClass(className);
            string inArgsText = GetArgumentsText(args.Skip(inIndex + 1).Take(inArgsCount).ToArray(), ArgumentType.In);
            string outArgs = GetArgumentsText(args.Skip(outIndex + 1).Take(outArgsCount).ToArray(), ArgumentType.Out);
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

        private static string ConstructClass(string name)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\t[LocalizedDisplayName(nameof(Resources.{name}DisplayName))]");
            sb.AppendLine($"\t[LocalizedDescription(nameof(Resources.{name}Description))]");
            sb.AppendLine($"\tpublic class {name}");
            sb.AppendLine("\t{{");
            sb.Append("{0}");
            sb.Append("{1}");
            sb.Append("\t}}");
            return sb.ToString();
        }

        private static string GetArgumentsText(string[] args, ArgumentType type)
        {
            var sb = new StringBuilder();

            foreach (string arg in args)
            {
                sb.AppendLine(GetArgument(arg, type));
            }

            return sb.ToString();
        }

        private static string GetArgument(string name, ArgumentType type)
        {
            string prefix = type == ArgumentType.In ? "In" : "Out";

            var sb = new StringBuilder();
            sb.AppendLine($"\t\t[LocalizedCategory(nameof(Resources.{prefix}put))]");
            sb.AppendLine($"\t\t[LocalizedDisplayName(nameof(Resources.{name}DisplayName))]");
            sb.AppendLine($"\t\t[LocalizedDescription(nameof(Resources.{name}Description))]");
            sb.AppendLine($"\t\tpublic {prefix}Argument<string> {name} {{ get; set; }}");
            return sb.ToString();
        }

        private static void WriteHelp()
        {
            Console.WriteLine("usage: ActivityGenerator.exe namespace activity [-in [in_args...]] [-out [out_args...]]");
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
