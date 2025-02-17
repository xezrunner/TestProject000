using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

using static CoreSystemFramework.Logging;

namespace CoreSystemFramework {

    public class ConsoleCommandAttribute : Attribute {
        public ConsoleCommandAttribute(params string[] aliases) {
            this.aliases = new string[aliases.Length + 1]; // Index 0 is always the method name.
            aliases.CopyTo(this.aliases, 1);
        }

        public string[] aliases;
        public string   help;
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
                            pushText(LogLevel.Warning, $"   - method {type.Name}::{info.Name}() has [ConsoleCommand] attribute, but is not static. Ignoring!");
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

        // TODO: how do we want to log console-specific "meta" stuff?
        // For instance, if a command throws an exception, should we log that normally?

        const char ARGS_ARRAY_START_TOKEN = '[';
        const char ARGS_ARRAY_END_TOKEN   = ']';
        static (int endIndex, string[] result) parseArrayFromArgs(string[] args, int startIndex = 0) {
            if (args[startIndex][0] != ARGS_ARRAY_START_TOKEN) return (-1, null);
            
            StringBuilder sb = new();

            int endIndex = -1;
            for (int i = startIndex; i < args.Length; ++i) {
                var it = args[i];
                if (it[0] == ARGS_ARRAY_START_TOKEN) it = it[1..];

                sb.Append(it.Trim());

                if (!it.IsEmpty() && it[it.Length-1] == ARGS_ARRAY_END_TOKEN) {
                    endIndex = i;
                    sb.Length -= 1; // Remove trailing END_TOKEN
                    break;
                }
            }

            if (endIndex == -1) return (-1, null);

            args = sb.ToString().Split(',');            
            return (endIndex, args);
        }

        static object attemptStringConversionToDesiredType(string source, Type targetType) {
            try {
                switch (targetType) {
                    case Type t when t == typeof(float): return source.AsFloat();
                    case Type t when t == typeof(int):   return source.AsInt();
                    case Type t when t == typeof(bool): 
                        var conversion = source.AsBool();
                        if (conversion.success) return conversion.result;
                        else                    throw new("Boolean conversion failed");
                }
                // Attempt generic conversion, if none of the above:
                return Convert.ChangeType(source, targetType);
            } catch (Exception ex) {
                logError($"Argument conversion failed - from type string(\"{source}\") to {targetType.Name}: {ex.Message}");
            }

            return null;
        }

        (bool success, object[] result) processArgsForInvocation(ParameterInfo[] functionArgsInfo, string[] args) {
            if (functionArgsInfo == null || args == null) return (false, null);
            
            bool debug = false;
            
            List<object> processedArgs = new(capacity: functionArgsInfo.Length);
            
            for (int i_funcArgs = 0, i_args = 1; i_funcArgs < functionArgsInfo.Length; ++i_funcArgs, ++i_args) {
                // NOTE: we want to detect it like this, since we'd only know the corrent number of args passed once
                // we process array inputs.
                if (processedArgs.Count > functionArgsInfo.Length) {
                    pushText(LogLevel.Error, $"Too many arguments were provided (expected {functionArgsInfo.Length}, got {args.Length})");
                }
                
                var funcArgInfo = functionArgsInfo[i_funcArgs];
                var arg     = (i_args < args.Length) ? args[i_args] : null;

                if (debug) pushText($"{i_funcArgs}: processing argInfo: {funcArgInfo.Name} of type {funcArgInfo.ParameterType} with arg: {arg}"); // TEMP:

                var funcArgType = funcArgInfo.ParameterType;

                if (funcArgType == arg?.GetType()) {
                    processedArgs.Add(arg);
                    continue;
                }

                if (funcArgType.IsArray) {
                    var arraySubtype = funcArgType.GetElementType();
                    if (debug) pushText($"  - this is supposed to be an array! subtype: {arraySubtype}, listing:"); // TEMP:

                    object result = null;
                    var processed = parseArrayFromArgs(args, i_args);
                    if (processed.endIndex != -1) {
                        if (debug) foreach (var it in processed.result) pushText("     - " + it); // TEMP:
                        result = processed.result;
                        i_args = processed.endIndex;
                    }
                    processedArgs.Add(result);
                    continue;
                }

                // Conversion required:
                object toAdd = arg;
                if (arg != null) {
                    toAdd = attemptStringConversionToDesiredType(arg, funcArgType);
                }

                if (toAdd == null) {
                    if (funcArgInfo.HasDefaultValue) toAdd = funcArgInfo.DefaultValue;
                    else {
                        pushText(LogLevel.Error, $"Argument {i_funcArgs} ('{funcArgInfo.Name}') was not passed and no default value is present for it.");
                        return (false, null);
                    }
                }

                processedArgs.Add(toAdd);
            }

            return (true, processedArgs.ToArray());
        }

        (bool success, object result) invokeFunction(ConsoleCommand command, params string[] args) {
            bool debug = false;
            var functionArgsInfo = command.functionArgsInfo;

            var processedArgs = processArgsForInvocation(functionArgsInfo, args);

            if (!processedArgs.success) return (false, null);

            if (debug) {
                pushText("Listing final args:");
                foreach (var arg in processedArgs.result) pushText($"  - {arg}");
            }
            
            var funcResult = command.function(processedArgs.result);
            return (true, funcResult);
        }
    }
    
}