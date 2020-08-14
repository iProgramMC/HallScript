using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.IO;

namespace HallScript
{
    class Program
    {
        public static List<Function> GetFunctions(string code)
        {
            List<Function> functions = new List<Function>();

            // Each function has a start and an end. The function officially starts
            // with a start_function thing, but it can also prepend arguments and return type.
            Function currentFunction = new Function();
            eFunctionScope currentScope = eFunctionScope.None;
            List<string> lines = code.Split('\n').ToList();
            //Console.WriteLine("[DEBUG] Pass 1: getting functions");
            for (int i = 0; i < lines.Count; i++)
            {
                // Ignore lines that start with a //
                string line = lines[i].TrimStart(' ').TrimEnd(' ');
                if (line.StartsWith("//")) continue;
                if (line.StartsWith("function_args_start"))
                {
                    if (currentScope == eFunctionScope.None)
                    {
                        currentScope = eFunctionScope.Argument;
                        currentFunction.functionArguments.Clear();
                    }
                    else
                    {
                        throw new ParseFailException(i, "Bad function_args_start inside another scope (duplicate?)");
                    }
                }
                else if (line.StartsWith("start_function"))
                {
                    if (currentScope == eFunctionScope.None)
                    {
                        currentScope = eFunctionScope.Main;
                        currentFunction.lines.Clear();
                        // get the name
                        string[] split = line.Split('|');
                        if (split.Length > 2)
                        {
                            throw new ParseFailException(i, "Too many arguments to start_function!");
                        }
                        if (split.Length < 2)
                        {
                            throw new ParseFailException(i, "Name needs to be provided (usage: \"start_function|<your name>\")");
                        }
                        string name = split[1];
                        if (!IsValid.Identifier(name))
                        {
                            throw new ParseFailException(i, "Invalid function name");
                        }
                        currentFunction.name = name;
                        //Console.WriteLine($"[DEBUG] Found function: {name}");
                    }
                    else
                    {
                        throw new ParseFailException(i, "Bad start_function inside another scope (duplicate?)");
                    }
                }
                else if (line.StartsWith("function_args_end"))
                {
                    if (currentScope == eFunctionScope.Argument)
                    {
                        currentScope = eFunctionScope.None;
                    }
                    else
                    {
                        throw new ParseFailException(i, "Bad argument end marker without a scope (duplicate line?)");
                    }
                }
                else if (line.StartsWith("end_function"))
                {
                    if (currentScope == eFunctionScope.Main)
                    {
                        currentScope = eFunctionScope.None;
                        functions.Add(currentFunction);
                        currentFunction = new Function();
                    }
                    else
                    {
                        throw new ParseFailException(i, "Bad function end marker without a scope (duplicate line?)");
                    }
                }
                else
                {
                    switch (currentScope)
                    {
                        case eFunctionScope.Argument:
                            {
                                if (line.StartsWith("func_arg"))
                                {
                                    string[] split = line.Split('|');
                                    if (split.Length > 3)
                                    {
                                        throw new ParseFailException(i, "Too many arguments to return_value_type!");
                                    }
                                    if (split.Length < 2)
                                    {
                                        throw new ParseFailException(i, "Argument type missing");
                                    }
                                    if (split.Length < 3)
                                    {
                                        throw new ParseFailException(i, "Argument name missing");
                                    }
                                    //currentFunction.returnType = Misc.GetTypeFromString(i, split[1]);
                                    currentFunction.functionArguments.Add(new Argument()
                                    {
                                        argName = split[2],
                                        argType = Misc.GetTypeFromString(i, split[1])
                                    });
                                }
                                break;
                            }
                        case eFunctionScope.Main:
                            {
                                if (string.IsNullOrWhiteSpace(line)) continue;
                                currentFunction.lines.Add(line);
                                break;
                            }
                        case eFunctionScope.None:
                            {
                                if (line.StartsWith("return_value_type"))
                                {
                                    if (currentFunction.returnType == QType.Void)
                                    {
                                        string[] split = line.Split('|');
                                        if (split.Length > 2)
                                        {
                                            throw new ParseFailException(i, "Too many arguments to return_value_type!");
                                        }
                                        if (split.Length < 2)
                                        {
                                            throw new ParseFailException(i, "Name needs to be provided (usage: \"return_value_type|<type: string/s/str/bool/boolean/b/integer/int/int32/i32/i>\")");
                                        }
                                        currentFunction.returnType = Misc.GetTypeFromString(i, split[1]);
                                    }
                                }
                                break;
                            }
                    }
                }
            }

            return functions;
        }
        public static Dictionary<string, Variable> GetGlobals(string code)
        {
            Dictionary<string, Variable> vars = new Dictionary<string, Variable>();

            Variable variable = new Variable();
            List<string> lines = code.Split('\n').ToList();
            //Console.WriteLine("[DEBUG] Pass 2: getting globals");
            for (int i = 0; i < lines.Count; i++)
            {
                // Ignore lines that start with a //
                string line = lines[i].TrimStart(' ').TrimEnd(' ');
                if (line.StartsWith("//")) continue;
                if (line.StartsWith("global_var"))
                {
                    // get the name
                    string[] split = line.Split('|');
                    if (split.Length > 4)
                    {
                        throw new ParseFailException(i, "Too many arguments to global_var!");
                    }
                    if (split.Length < 3)
                    {
                        throw new ParseFailException(i, "Invalid usage of global_var (usage: \"global_var|<type, int/string/bool/ri/rs/rb>|<your name>[|<default_value>]\")");
                    }
                    string name = split[2];
                    if (!IsValid.Identifier(name))
                    {
                        throw new ParseFailException(i, "Invalid function name");
                    }
                    if (vars.TryGetValue(name, out _))
                    {
                        throw new ParseFailException(i, "The " + name + " variable is already defined!");
                    }
                    QType qType = Misc.GetTypeFromString(i, split[1]);
                    variable.type = qType;

                    if (split.Length > 3)
                    {
                        variable.obj = Misc.GetObjectFromString(i, split[3], qType, vars);
                    } 
                    else
                    {
                        variable.obj = Misc.GetDefaultObjectFromType(i, qType);
                    }
                    //Console.WriteLine($"[DEBUG] Found global {name} with value {variable.obj}");
                    vars.Add(name, variable);
                    variable = new Variable(); 
                }
            }

            return vars;
        }

        /// <summary>
        /// Parses and executes the code :)
        /// </summary>
        /// <param name="code">This code needs to have all tabs and carriage returns (\r) removed.</param>
        public static int ParseCode(string code)
        {
            List<Function> functions = GetFunctions(code);
            Dictionary<string, Variable> globalVariables = GetGlobals(code);

            //Console.WriteLine("[DEBUG] Pass 3: running program");
            Function func = null;
            foreach(Function f in functions)
            {
                if (f.name.ToLower() == "main")
                {
                    if (func != null)
                    {
                        throw new ParseFailException(-1, "The main function is defined more than once");
                    } 
                    else
                    {
                        func = f;
                    }
                }
            }
            object obj = func.Run(functions, globalVariables, new List<Variable>());
            if (obj is int)
            {
                return (int)obj;
            } 
            else
            {
                return -1;
            }
        }
        public static string ParseFileImports(string filename, string fileContents)
        {
            // look for imports
            string finalText = "";
            string[] lines = fileContents.Replace("\t", "").Replace("\r", "").Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("#import"))
                {
                    string[] split = line.Split('|');
                    if (split.Length != 2)
                    {
                        throw new Exception("#import with too many/too little arguments...");
                    }
                    string file = split[1];
                    string contents = ParseFileImports(Environment.CurrentDirectory + "/" + file, File.ReadAllText(Environment.CurrentDirectory + "/" + file)); // todo: use the same path as the actual file instead!!
                    finalText += contents + "\n";
                }
                else
                {
                    finalText += line + "\n";
                }
            }
            return finalText;
        }
        static void Main(string[] args)
        {
            // Get the file contents
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: hallscript <your_file>.hl");
                return;
            }
            string filename = args[0];
            string fileContents = File.ReadAllText(filename);
            //Console.WriteLine("fileContents: {0}", fileContents);

            // trash the tabs as well, as they are purely optional and aesthetic
            // also carriage return because we use line feeds only!
            int errorCode = -1;
            try
            {
                string newFile = ParseFileImports(filename, fileContents);
                errorCode = ParseCode(newFile.Replace("\t", "").Replace("\r", ""));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString().Split('\n')[0]);
            }


            Console.WriteLine("\n\nThe program exited with error code {0}, press any key to continue...", errorCode);
            Console.ReadKey();
            return;
        }
    }
}
