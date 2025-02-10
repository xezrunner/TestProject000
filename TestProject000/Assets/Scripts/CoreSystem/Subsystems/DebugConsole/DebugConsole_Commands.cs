using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace CoreSystem {

    public class ConsoleCommandAttribute : Attribute {
        public ConsoleCommandAttribute(params string[] aliases) {
            this.aliases = new string[aliases.Length + 1]; // Index 0 is always the method name.
            aliases.CopyTo(this.aliases, 1);
        }

        public string[] aliases;
        public string   helpText;
        public bool     isCheatCommand;
    }

    public delegate object ConsoleCommandFunction(params object[] args);
    class ConsoleCommand {
        public ConsoleCommand(ConsoleCommandAttribute info, MethodInfo methodInfo) {
            // Assign primary alias:
            if (info.aliases == null) info.aliases    = new string[] { methodInfo.Name };
            else                      info.aliases[0] = methodInfo.Name;
            this.info = info;

            functionArgsInfo   = methodInfo.GetParameters();
            functionReturnType = methodInfo.ReturnType;
            function           = args => methodInfo.Invoke(null, args);
        }
        public ConsoleCommandAttribute info;

        public ParameterInfo[] functionArgsInfo;
        public Type            functionReturnType;
        public ConsoleCommandFunction function;

        public (bool success, object result) invokeFunction(params object[] args) {
            if (args.Length > functionArgsInfo.Length) {
                Debug.LogError($"  - too many arguments: expected {functionArgsInfo.Length}, got {args.Length}");
                return (false, null);
            }
            
            var processedArgs = new List<object>();

            for (int i = 0; i < functionArgsInfo.Length; ++i) {
                var info    = functionArgsInfo[i];

                string argText = (i < args.Length) ? (string)args[i] : null;
                object arg     = (i < args.Length) ? args[i] : null;

                // TODO: named args?
                // if (info.Name)

                // If there are default values defined for an arg we didn't pass, use them instead:
                if (arg == null) {
                    if (info.HasDefaultValue) arg = info.DefaultValue;
                    else {
                        Debug.LogError($"  - argument #{i} ('{info.Name}') was not passed and has no default value.");
                        return (false, null);
                    }
                }
                // NOTE: When [required value type args without default values] are missing, C# will use their canonical default values 
                // if we pass in null in place of the value type args.
                else if (arg != null && info.ParameterType != arg.GetType()) {
                    object converted = null;
                    string conversionError = null;

                    // TODO: lists/arrays
                    try {
                        if      (info.ParameterType == typeof(float)) converted = argText.AsFloat();
                        else if (info.ParameterType == typeof(int))   converted = argText.AsInt();
                        else if (info.ParameterType == typeof(bool)) {
                            var conversion = argText.AsBool();
                            if (conversion.success) converted = conversion.result;
                        }
                        else converted = Convert.ChangeType(arg, info.ParameterType);
                    } catch (Exception e) {
                        // TODO: might want to handle some special args that don't convert on their own?
                        conversionError = e.Message;
                    }

                    if (converted == null) {
                        Debug.LogError($"  - argument conversion failed from: string (\"{argText}\") to: {info.ParameterType}{(conversionError != null ? $" -- {conversionError}" : null)}");
                        return (false, null);
                    }
                    arg = converted;
                }
                processedArgs.Add(arg);
            }

            var result = function(processedArgs.ToArray());
            return (true, result);
        }
    }

    public partial class DebugConsole {
        Dictionary<string, ConsoleCommand> commands = new(capacity: 300);

        public static readonly string[] assemblyPathIgnoreListPathContains = {
            Path.Combine("Unity", "Hub", "Editor"),
        };
        public static readonly string[] assemblyPathIgnoreListFileNameStartsWith = {
            "mscorlib",
            "Unity.", "UnityEngine.", "UnityEditor.", "Mono.", "System.", "nunit"
        };
        
        void registerCommandsFromAssemblies() {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            List<(MethodInfo methodInfo, ConsoleCommandAttribute attribute)> commandMetadatas = new(capacity: 100);
            
            pushText("- Registering console commands and variables...");

            foreach (var assembly in assemblies) {
                var assemblyPath = assembly.ManifestModule.FullyQualifiedName; // TODO: this isn't necessarily always the path
                if (assemblyPath.Contains(assemblyPathIgnoreListPathContains)) continue;
                if (Path.GetFileNameWithoutExtension(assemblyPath).StartsWith(assemblyPathIgnoreListFileNameStartsWith)) {
                    continue;
                }
                
                var types = assembly.GetTypes();
                foreach (var type in types) {
                    var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    foreach (var info in methods) {
                        var attribute = info.GetCustomAttribute<ConsoleCommandAttribute>();
                        if (attribute == null) continue;

                        if (!info.IsStatic) {
                            // TODO: we could do a thing where we'd find instances of Objects/MonoBehaviours and let you choose, as an arg or at invocation.
                            Debug.LogWarning($"   - method {type.Name}::{info.Name}() has [ConsoleCommand] attribute, but is not static. Ignoring!");
                            continue;
                        }

                        pushText($"   - {assembly.ManifestModule.Name}/{type.Name}::" + $"{info.Name}()".bold());
                        commandMetadatas.Add((info, attribute));
                    }
                }
            }

            foreach (var metadata in commandMetadatas) {
                var command = new ConsoleCommand(metadata.attribute, metadata.methodInfo);
                foreach (var alias in metadata.attribute.aliases) commands.Add(alias, command);
            }
        }
    }
    
}