using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace HallScript
{
    public enum QType
    {
        Void,
        Int,
        String,
        Bool,
        // any reference types MUST go below RefInt
        /*RefInt,
        RefString,
        RefBool*/
    }
    class Argument
    {
        public QType argType;
        public string argName;
    }
    enum eFunctionScope
    {
        None,
        Argument,
        Main
    }
    class Function
    {
        public string name = "";
        public List<string> lines = new List<string>();
        public List<Argument> functionArguments = new List<Argument>();
        public QType returnType = QType.Void;

        public object Run(List<Function> functions, Dictionary<string, Variable> globals, List<Variable> parameters)
        {
            if (functionArguments.Count != parameters.Count)
            {
                throw new Exception("Too many/less parameters have been provided");
            }
            Dictionary<string, Variable> locals = new Dictionary<string, Variable>();
            for (int i = 0; i < parameters.Count; i++)
            {
                locals[functionArguments[i].argName] = parameters[i];
                if (parameters[i].type != functionArguments[i].argType)
                {
                    throw new Exception("Invalid type argument at index " + i.ToString() + " (the argument type is " + functionArguments[i].argType.ToString());
                }
            }
            Dictionary<string, int> labels = new Dictionary<string, int>();
            for (int lind = 0; lind < lines.Count; lind++)
            {
                string line = lines[lind];
                string[] tokens = line.Split('|');
                if (tokens.Length >= 1)
                {
                    switch (tokens[0])
                    {
                        case "print":
                            if (tokens.Length == 2)
                            {
                                // Print an object
                                object obj = Misc.GetObjectFromStringAndDetermineType(-1, tokens[1], out _, globals, locals);
                                Console.Write(obj);
                                break;
                            } 
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'print': that is 2");
                            }
                        case "printl":
                            if (tokens.Length == 2)
                            {
                                // Print an object
                                object obj = Misc.GetObjectFromStringAndDetermineType(-1, tokens[1], out _, globals, locals);
                                Console.WriteLine(obj);
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'printl': that is 2");
                            }
                        case "print_fmt":
                        case "printf":
                            if (tokens.Length >= 2)
                            {
                                // Print a string with objects
                                object obj = Misc.GetObjectFromString(-1, tokens[1], QType.String, globals, locals);
                                if (tokens.Length > 2)
                                {
                                    object[] parms = new object[tokens.Length - 2];
                                    for (int i = 2; i < tokens.Length; i++)
                                    {
                                        int j = i - 2;
                                        parms[j] = Misc.GetObjectFromStringAndDetermineType(-1, tokens[i], out _, globals, locals);
                                    }
                                    Console.Write((string)obj, parms);
                                }
                                else
                                {
                                    Console.Write(obj);
                                }
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'printl': that is 2");
                            }
                        case "printl_fmt":
                        case "printlf":
                            if (tokens.Length >= 2)
                            {
                                // Print a string with objects
                                object obj = Misc.GetObjectFromString(-1, tokens[1], QType.String, globals, locals);
                                if (tokens.Length > 2) {
                                    object[] parms = new object[tokens.Length - 2];
                                    for (int i = 2; i < tokens.Length; i++)
                                    {
                                        int j = i - 2;
                                        parms[j] = Misc.GetObjectFromStringAndDetermineType(-1, tokens[i], out _, globals, locals);
                                    }
                                    Console.WriteLine((string)obj, parms);
                                } 
                                else
                                {
                                    Console.WriteLine(obj);
                                }
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'printl': that is 2");
                            }
                        case "return":
                        case "ret":
                            if (tokens.Length == 2)
                            {
                                return Misc.GetObjectFromString(-1, tokens[1], returnType, globals, locals);
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'return': that is 2");
                            }
                        case "def_var":
                        case "dvar":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType theType; object defVal;
                                    name = tokens[2];
                                    theType = Misc.GetTypeFromString(-1, tokens[1]);
                                    if (tokens.Length > 3)
                                    {
                                        defVal = Misc.GetObjectFromString(-1, tokens[3], theType, globals, locals);
                                    } 
                                    else
                                    {
                                        defVal = Misc.GetDefaultObjectFromType(-1, theType);
                                    }
                                    locals.Add(name, new Variable()
                                    {
                                        type = theType,
                                        obj = defVal
                                    });
                                } 
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'def_var', the minimum is 3");
                                }
                                break;
                            }
                        case "read_in":
                        case "getin":
                            {
                                if (tokens.Length == 2)
                                {
                                    Variable v = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    if (v != null)
                                    {
                                        if (v.type != QType.String)
                                        {
                                            throw new Exception("Invalid return value type, the readin return type must be a string");
                                        }
                                        v.obj = Console.ReadLine();
                                    } 
                                    else
                                    {
                                        throw new Exception("Invalid return value variable");
                                    }
                                } 
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'read_in': that is 2");
                                }
                                break;
                            }
                        case "convert_string_to_int":
                        case "cvtstr2int":
                            {
                                if (tokens.Length == 3)
                                {
                                    Variable vIn = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    Variable vOut = Misc.GetVariableFromString(-1, tokens[2], globals, locals);
                                    if (vIn == null)
                                    {
                                        throw new Exception("Invalid input value variable");
                                    }
                                    if (vOut == null)
                                    {
                                        throw new Exception("Invalid output value variable");
                                    }
                                    if (vOut.type != QType.Int)
                                    {
                                        throw new Exception("Invalid output variable type");
                                    }
                                    if (vIn.type != QType.String)
                                    {
                                        throw new Exception("Invalid input variable type");
                                    }
                                    vOut.obj = (object)int.Parse((string)vIn.obj);
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'convert_string_to_int': that is 3");
                                }
                                break;
                            }
                        case "is_equal":
                        case "equ":
                            {
                                if (tokens.Length == 4)
                                {
                                    QType t1, t2;
                                    object v1, v2;
                                    v1 = Misc.GetObjectFromStringAndDetermineType(-1, tokens[1], out t1, globals, locals);
                                    v2 = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out t2, globals, locals);
                                    if (t1 != t2)
                                    {
                                        throw new Exception("The variable types are not the same!");
                                    }
                                    Variable vOut = Misc.GetVariableFromString(-1, tokens[3], globals, locals);
                                    if (vOut.type != QType.Bool)
                                        throw new Exception("The output variable isn't a boolean");
                                    if (t1 == QType.Bool)
                                    {
                                        vOut.obj = ((bool)v1 == (bool)v2);
                                    }
                                    else if (t1 == QType.String)
                                    {
                                        vOut.obj = ((string)v1 == (string)v2);
                                    }
                                    else
                                    {
                                        vOut.obj = ((int)v1 == (int)v2);
                                    }
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'is_equal': that is 4");
                                }
                                break;
                            }
                        case "starts_with":
                            {
                                if (tokens.Length == 4)
                                {
                                    QType t1, t2;
                                    object v1, v2;
                                    v1 = Misc.GetObjectFromStringAndDetermineType(-1, tokens[1], out t1, globals, locals);
                                    v2 = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out t2, globals, locals);
                                    if (t1 != t2)
                                    {
                                        throw new Exception("The variable types are not the same!");
                                    }
                                    Variable vOut = Misc.GetVariableFromString(-1, tokens[3], globals, locals);
                                    if (vOut.type != QType.Bool)
                                        throw new Exception("The output variable isn't a boolean");
                                    if (t1 == QType.String)
                                    {
                                        vOut.obj = (((string)v1).StartsWith((string)v2));
                                    }
                                    else
                                    {
                                        throw new Exception("This isn't a string comparison!");
                                    }
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'starts_with': that is 4");
                                }
                                break;
                            }
                        case "ends_with":
                            {
                                if (tokens.Length == 4)
                                {
                                    QType t1, t2;
                                    object v1, v2;
                                    v1 = Misc.GetObjectFromStringAndDetermineType(-1, tokens[1], out t1, globals, locals);
                                    v2 = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out t2, globals, locals);
                                    if (t1 != t2)
                                    {
                                        throw new Exception("The variable types are not the same!");
                                    }
                                    Variable vOut = Misc.GetVariableFromString(-1, tokens[3], globals, locals);
                                    if (vOut.type != QType.Bool)
                                        throw new Exception("The output variable isn't a boolean");
                                    if (t1 == QType.String)
                                    {
                                        vOut.obj = (((string)v1).EndsWith((string)v2));
                                    }
                                    else
                                    {
                                        throw new Exception("This isn't a string comparison!");
                                    }
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'ends_with': that is 4");
                                }
                                break;
                            }
                        case "concat_str":
                        case "concat":
                            {
                                if (tokens.Length == 3)
                                {
                                    Variable changed = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    if (changed.type != QType.String)
                                    {
                                        throw new Exception("Can't concatenate with a " + changed.type.ToString());
                                    }
                                    QType type; object defVal;
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type, globals, locals);
                                    if (type != QType.String)
                                    {
                                        throw new Exception("The string to concatenate with is not actually a string");
                                    }
                                    changed.obj = ((string)changed.obj + (string)defVal); 
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'concat_str': that is 3");
                                }
                                break;
                            }
                        case "def_label":
                        case "label":
                            if (tokens.Length == 2)
                            {
                                if (labels.TryGetValue(tokens[1], out _))
                                {
                                    throw new Exception("Label " + tokens[1] + " already exists.");
                                }
                                labels[tokens[1]] = lind;
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'def_label': that is 2");
                            }
                            break;
                        case "not":
                            if (tokens.Length == 2)
                            {
                                Variable v = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                if (v.type != QType.Bool)
                                {
                                    throw new Exception("The variable is not a bool");
                                }
                                v.obj = !((bool)v.obj);
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'not': that is 2");
                            }
                        case "call_if":
                            if (tokens.Length >= 3)
                            {
                                bool b = (bool)Misc.GetObjectFromStringAndDetermineType(-1, tokens[1], out _, globals, locals);
                                if (b)
                                {
                                    Function f = null;
                                    List<Variable> paramsPass = new List<Variable>();
                                    foreach(Function func in functions)
                                    {
                                        if (func.name == tokens[2])
                                        {
                                            if (f != null)
                                            {
                                                throw new Exception("Function " + tokens[2] + " is defined twice");
                                            }
                                            else
                                            {
                                                f = func;
                                            }
                                        }
                                    }
                                    if (f != null)
                                    {
                                        // get parameters
                                        int howMany = f.returnType == QType.Void ? 3 : 4;
                                        if (tokens.Length < howMany)
                                        {
                                            throw new Exception("Invalid amount of arguments for 'call_if', in this case they are " + howMany.ToString() + ".");
                                        }
                                        if (tokens.Length > howMany)
                                        {
                                            for(int i = howMany; i < tokens.Length; i++)
                                            {
                                                int j = i - howMany; QType theType; object theObj;
                                                theObj = Misc.GetObjectFromStringAndDetermineType(-1, tokens[i], out theType, globals, locals);
                                                paramsPass.Add(new Variable()
                                                {
                                                    obj = theObj,
                                                    type = theType
                                                });
                                            }
                                        }
                                        object obj = f.Run(functions, globals, paramsPass);
                                        QType type = Misc.GetTypeFromObject(obj);
                                        if (type != QType.Void)
                                        {
                                            // return value!
                                            if (type != f.returnType)
                                            {
                                                throw new Exception("The function returned an invalid return type???");
                                            } 
                                            else
                                            {
                                                Variable v = Misc.GetVariableFromString(-1, tokens[3], globals, locals);
                                                if (v != null)
                                                {
                                                    if (v.type != type)
                                                    {
                                                        throw new Exception("The return variable type is not the same as the return type of the function.");
                                                    }
                                                    v.obj = obj;
                                                } 
                                                else
                                                {
                                                    throw new Exception("There was a weird issue grabbing the return value :/");
                                                }
                                            }
                                        }
                                    } 
                                    else
                                    {
                                        throw new Exception("Function " + tokens[2] + " is not available");
                                    }
                                }
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid amount of arguments for 'call_if', in this case they are 3 or 4.");
                            }
                        case "call":
                            if (tokens.Length >= 2)
                            {
                                Function f = null;
                                List<Variable> paramsPass = new List<Variable>();
                                foreach (Function func in functions)
                                {
                                    if (func.name == tokens[1])
                                    {
                                        if (f != null)
                                        {
                                            throw new Exception("Function " + tokens[1] + " is defined twice");
                                        }
                                        else
                                        {
                                            f = func;
                                        }
                                    }
                                }
                                if (f != null)
                                {
                                    // get parameters
                                    int howMany = f.returnType == QType.Void ? 2 : 3;
                                    if (tokens.Length < howMany)
                                    {
                                        throw new Exception("Invalid amount of arguments for 'call', in this case they are " + howMany.ToString() + ".");
                                    }
                                    if (tokens.Length > howMany)
                                    {
                                        for (int i = howMany; i < tokens.Length; i++)
                                        {
                                            int j = i - howMany; QType theType; object theObj;
                                            theObj = Misc.GetObjectFromStringAndDetermineType(-1, tokens[i], out theType, globals, locals);
                                            paramsPass.Add(new Variable()
                                            {
                                                obj = theObj,
                                                type = theType
                                            });
                                        }
                                    }
                                    object obj = f.Run(functions, globals, paramsPass);
                                    QType type = Misc.GetTypeFromObject(obj);
                                    if (type != QType.Void)
                                    {
                                        // return value!
                                        if (type != f.returnType)
                                        {
                                            throw new Exception("The function returned an invalid return type???");
                                        }
                                        else
                                        {
                                            Variable v = Misc.GetVariableFromString(-1, tokens[2], globals, locals);
                                            if (v != null)
                                            {
                                                if (v.type != type)
                                                {
                                                    throw new Exception("The return variable type is not the same as the return type of the function.");
                                                }
                                                v.obj = obj;
                                            }
                                            else
                                            {
                                                throw new Exception("There was a weird issue grabbing the return value :/");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    throw new Exception("Function " + tokens[1] + " is not available");
                                }
                                
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid amount of arguments for 'call_if', in this case they are 3 or 4.");
                            }
                        case "jump_if":
                            {
                                if (tokens.Length == 3)
                                {
                                    bool b = (bool)Misc.GetObjectFromStringAndDetermineType(-1, tokens[1], out _, globals, locals);
                                    if (b)
                                    {
                                        if (labels.TryGetValue(tokens[2], out int val))
                                        {
                                            lind = val;
                                            continue;
                                        }
                                        else
                                        {
                                            throw new Exception("Label " + tokens[2] + " does not exist.");
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of params for 'jump_if': that is 4");
                                }
                            }
                        case "jump":
                            {
                                if (tokens.Length == 2)
                                {
                                    if (labels.TryGetValue(tokens[1], out int val))
                                    {
                                        lind = val;
                                        continue;
                                    }
                                    else
                                    {
                                        throw new Exception("Label " + tokens[1] + " does not exist.");
                                    }
                                } 
                                else
                                {
                                    throw new Exception("Invalid number of params for 'jump': that is 3");
                                }
                            }
                        case "set_var":
                        case "setv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    var.obj = defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'set_var', the minimum is 3");
                                }
                                break;
                            }
                        case "add_var":
                        case "addv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)var.obj + (int)defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'add_var', the minimum is 3");
                                }
                                break;
                            }
                        case "sub_var":
                        case "subv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)var.obj - (int)defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'sub_var', the minimum is 3");
                                }
                                break;
                            }
                        case "multiply_var":
                        case "mul_var":
                        case "mulv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)var.obj * (int)defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'multiply_var', the minimum is 3");
                                }
                                break;
                            }
                        case "divide_var":
                        case "div_var":
                        case "divv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)var.obj / (int)defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'divide_var', the minimum is 3");
                                }
                                break;
                            }
                        case "power_var":
                        case "pow_var":
                        case "powv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)Math.Pow((int)var.obj, (int)defVal);
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'power_var', the minimum is 3");
                                }
                                break;
                            }
                        case "sqrt_var":
                        case "sqrtv":
                            if (tokens.Length == 2)
                            {
                                Variable v = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                if (v.type != QType.Int)
                                {
                                    throw new Exception("The variable is not an int");
                                }
                                v.obj = (int)Math.Sqrt((int)v.obj);
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'sqrt_var': that is 2");
                            }
                        case "band_var":
                        case "bandv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)var.obj & (int)defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'band_var', the minimum is 3");
                                }
                                break;
                            }
                        case "bor_var":
                        case "borv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)var.obj | (int)defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'bor_var', the minimum is 3");
                                }
                                break;
                            }
                        case "bxor_var":
                        case "bxorv":
                            {
                                if (tokens.Length >= 3)
                                {
                                    string name; QType type2; object defVal;
                                    name = tokens[1];
                                    Variable var = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                    defVal = Misc.GetObjectFromStringAndDetermineType(-1, tokens[2], out type2, globals, locals);
                                    if (type2 != var.type)
                                    {
                                        throw new Exception("Cross-type variable setting is not allowed");
                                    }
                                    if (var.type != QType.Int)
                                    {
                                        throw new Exception("Arithmetic operations can only be done on ints");
                                    }
                                    var.obj = (int)var.obj ^ (int)defVal;
                                }
                                else
                                {
                                    throw new Exception("Invalid number of parameters for 'bxor_var', the minimum is 3");
                                }
                                break;
                            }
                        case "bnot_var":
                        case "bnotv":
                            if (tokens.Length == 2)
                            {
                                Variable v = Misc.GetVariableFromString(-1, tokens[1], globals, locals);
                                if (v.type != QType.Int)
                                {
                                    throw new Exception("The variable is not an int");
                                }
                                v.obj = ~((int)v.obj);
                                break;
                            }
                            else
                            {
                                throw new Exception("Invalid number of parameters for 'bnot_var': that is 2");
                            }
                        default:
                            throw new Exception("Unknown command: " + tokens[0]);
                    }
                }
            }
            return Misc.GetDefaultObjectFromType(-1, returnType);
        }
    }
}
