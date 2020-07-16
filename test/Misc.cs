using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace HallScript
{
    public static class Misc
    {
        public static QType GetTypeFromString(int line, string str)
        {
            switch (str)
            {
                case "bool":
                case "boolean":
                case "b":
                    return QType.Bool;
                case "int":
                case "integer":
                case "int32":
                case "i32":
                case "i":
                    return QType.Int;
                case "string":
                case "str":
                case "s":
                    return QType.String;
                case "void":
                case "v":
                    return QType.Void;/*
                case "ref_int":
                case "ref_integer":
                case "ref_int32":
                case "ref_i32":
                case "ref_i":
                case "ri":
                    return QType.RefInt;
                case "ref_bool":
                case "ref_boolean":
                case "ref_b":
                case "rb":
                    return QType.RefBool;
                case "ref_string":
                case "ref_str":
                case "ref_s":
                case "rs":
                    return QType.RefString;*/
                default:
                    {
                        throw new ParseFailException(line, "Unknown type name " + str + ".");
                    }
            }
        }

        public static Variable GetVariableFromString(int line, string str, Dictionary<string, Variable> globals = null, Dictionary<string, Variable> variables = null)
        {
            if (!IsValid.Identifier(str))
            {
                throw new ParseFailException(line, "Invalid identifier");
            }
            else if (variables != null)
            {
                if (variables.TryGetValue(str, out Variable v))
                {
                    return v;
                }
                else
                {
                    if (globals != null)
                    {
                        if (globals.TryGetValue(str, out Variable v2))
                        {
                            return v2;
                        }
                        else
                        {
                            throw new ParseFailException(line, "Variable " + str + " doesn't exist!");
                        }
                    }
                    else
                    {
                        throw new ParseFailException(line, "Variable " + str + " doesn't exist!");
                    }
                }
            }
            else if (globals != null)
            {
                if (globals.TryGetValue(str, out Variable v2))
                {
                    return v2;
                }
                else
                {
                    throw new ParseFailException(line, "Variable " + str + " doesn't exist!");
                }
            }
            else
            {
                throw new ParseFailException(line, "Variable " + str + " doesn't exist!");
            }
        }
        public static object GetObjectFromString(int line, string str, QType type, Dictionary<string, Variable> globals = null, Dictionary<string, Variable> variables = null)
        {
            if (str.Length == 0)
                throw new ParseFailException(line, "Invalid empty object");
            switch (type)
            {
                case QType.Bool:
                    {
                        if (str == "true") return (object)true;
                        if (str == "false") return (object)false;
                        if (!IsValid.Identifier(str))
                        {
                            throw new ParseFailException(line, "Invalid identifier used as bool");
                        }
                        else
                        {
                            Variable var = GetVariableFromString(line, str, globals, variables);
                            if (var.type == QType.Bool)
                                return var.obj;
                            else
                                throw new ParseFailException(line, "Variable " + str + " is not bool");
                        }
                    }
                case QType.String:
                    {
                        if (str.StartsWith("\"") && str.EndsWith("\""))
                        {
                            return (object)str.Substring(1, str.Length - 2);
                        }
                        else
                        {
                            Variable var = GetVariableFromString(line, str, globals, variables);
                            if (var.type == QType.String)
                                return var.obj;
                            else
                                throw new ParseFailException(line, "Variable " + str + " is not string");
                        }
                    }
                case QType.Int:
                    {
                        if (str[0] <= '9' && str[0] >= '0')
                        {
                            //return (object)int.Parse(str);
                            if (str.Length < 2)
                            {
                                return int.Parse(str);
                            } 
                            else
                            {
                                if (str[0] != '0')
                                {
                                    return int.Parse(str);
                                }
                                else
                                {
                                    if (str[1] == 'x' || str[1] == 'X')
                                    {
                                        // hexadecimal digits are 0-9, a-f/A-F
                                        // we should be able to get away with just a
                                        return int.Parse(str.Substring(2), NumberStyles.HexNumber);
                                    } else { 
                                        // octal representation is 012, which is 10 in decimal
                                        // C would use octal if 0 prefix is used
                                        string octals = str.Substring(1);
                                        int result = 0;
                                        foreach(char c in octals)
                                        {
                                            if (c < '0' || c >= '8')
                                            {
                                                throw new ParseFailException(line, c + " is not a valid digit in octal!");
                                            }
                                            result = result * 8 + (c - '0');
                                        }
                                        return result;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Variable var = GetVariableFromString(line, str, globals, variables);
                            if (var.type == QType.Int)
                                return var.obj;
                            else
                                throw new ParseFailException(line, "Variable " + str + " is not int");
                        }
                    }
            }
            return null;
        }
        public static object GetObjectFromStringAndDetermineType(int line, string str, out QType type, Dictionary<string, Variable> globals = null, Dictionary<string, Variable> variables = null)
        {
            if (str.Length == 0)
                throw new ParseFailException(line, "Invalid empty object");
            if (str == "true")
            {
                type = QType.Bool;
                return (object)true;
            }
            if (str == "false")
            {
                type = QType.Bool;
                return (object)false;
            }
            if (str[0] <= '9' && str[0] >= '0' || str[0] == '-')
            {
                //return (object)int.Parse(str);
                if (str.Length < 2)
                {
                    type = QType.Int;
                    return int.Parse(str);
                }
                else
                {
                    if (str[0] != '0')
                    {
                        type = QType.Int;
                        return int.Parse(str);
                    }
                    else
                    {
                        if (str[1] == 'x' || str[1] == 'X')
                        {
                            // hexadecimal digits are 0-9, a-f/A-F
                            // we should be able to get away with just a
                            type = QType.Int;
                            return int.Parse(str.Substring(2), NumberStyles.HexNumber);
                        }
                        else
                        {
                            // octal representation is 012, which is 10 in decimal
                            // C would use octal if 0 prefix is used
                            string octals = str.Substring(1);
                            int result = 0;
                            foreach (char c in octals)
                            {
                                if (c < '0' || c >= '8')
                                {
                                    throw new ParseFailException(line, c + " is not a valid digit in octal!");
                                }
                                result = result * 8 + (c - '0');
                            }
                            type = QType.Int;
                            return result;
                        }
                    }
                }
            }
            if (str.StartsWith("\"") && str.EndsWith("\""))
            {
                type = QType.String;
                return (object)str.Substring(1, str.Length - 2);
            }
            Variable var = GetVariableFromString(line, str, globals, variables);
            type = var.type;
            return var.obj;
        }
        public static object GetDefaultObjectFromType(int line, QType type)
        {
            switch (type)
            {
                case QType.Bool:
                    {
                        return false;
                    }
                case QType.String:
                    {
                        return "";
                    }
                case QType.Int:
                    {
                        return 0;
                    }
            }
            return null;
        }
        public static QType GetTypeFromObject(object obj)
        {
            if (obj == null) return QType.Void;
            if (obj is bool) return QType.Bool;
            if (obj is int) return QType.Int;
            if (obj is string) return QType.String;
            throw new Exception("Invalid type of object");
        }
    }
}
