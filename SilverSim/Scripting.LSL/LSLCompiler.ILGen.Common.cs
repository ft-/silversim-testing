/*

SilverSim is distributed under the terms of the
GNU Affero General Public License v3
with the following clarification and special exception.

Linking this code statically or dynamically with other modules is
making a combined work based on this code. Thus, the terms and
conditions of the GNU Affero General Public License cover the whole
combination.

As a special exception, the copyright holders of this code give you
permission to link this code with independent modules to produce an
executable, regardless of the license terms of these independent
modules, and to copy and distribute the resulting executable under
terms of your choice, provided that you also meet, for each linked
independent module, the terms and conditions of the license of that
module. An independent module is a module which is not derived from
or based on this code. If you modify this code, you may extend
this exception to your version of the code, but you are not
obligated to do so. If you do not wish to do so, delete this
exception statement from your version.

*/

using SilverSim.Scene.Types.Object;
using SilverSim.Scene.Types.Script;
using SilverSim.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;

namespace SilverSim.Scripting.LSL
{
    public partial class LSLCompiler
    {

        class LSLScriptAssembly : IScriptAssembly
        {
            Assembly m_Assembly;
            Type m_Script;
            Dictionary<string, Type> m_StateTypes;
            bool m_ForcedSleep;

            public LSLScriptAssembly(Assembly assembly, Type script, Dictionary<string, Type> stateTypes, bool forcedSleep)
            {
                m_Assembly = assembly;
                m_Script = script;
                m_StateTypes = stateTypes;
                m_ForcedSleep = forcedSleep;
            }

            public ScriptInstance Instantiate(ObjectPart objpart, ObjectPartInventoryItem item)
            {
                Script m_Script = new Script(objpart, item, m_ForcedSleep);
                foreach (KeyValuePair<string, Type> t in m_StateTypes)
                {
                    ConstructorInfo info = t.Value.GetConstructor(new Type[1] { typeof(Script) });
                    object[] param = new object[1];
                    param[0] = m_Script;
                    m_Script.AddState(t.Key, (LSLState)info.Invoke(param));
                }

                return m_Script;
            }
        }

        internal class ILParameterInfo
        {
            public int Position;
            public Type ParameterType;

            public ILParameterInfo(Type type, int position)
            {
                ParameterType = type;
                Position = position;
            }
        }

        internal class ILLabelInfo
        {
            public Label Label;
            public bool IsDefined = false;
            public List<int> UsedInLines = new List<int>();

            public ILLabelInfo(Label label, bool isDefined)
            {
                Label = label;
                IsDefined = isDefined;
            }
        }

        #region LSL Integer Overflow
        /* special functions for converts
         * 
         * Integer Overflow
         * The compiler treats integers outside the range -2147483648 to 2147483647 somewhat strangely. No compile time warning or error is generated. (If the following explanation, doesn't make sense to you don't worry -- just know to avoid using numbers outside the valid range in your script.)

         * - For an integer outside the range -2147483648 to 2147483647, the absolute value of the number is reduced to fall in the range 0 to 4294967295 (0xFFFFFFFF).
         * - This number is then parsed as an unsigned 32 bit integer and cast to the corresponding signed integer.
         * - If the value in the script had a negative sign, the sign of the internal representation is switched.
         * - The net effect is that very large positive numbers get mapped to -1 and very large negative numbers get mapped to 1.
         */

        public static int ConvToInt(double v)
        {
            try
            {
                return (int)v;
            }
            catch
            {
                if (v > 0)
                {
                    try
                    {
                        return (int)((uint)v);
                    }
                    catch
                    {
                        return -1;
                    }
                }
                else
                {
                    try
                    {
                        return (int)-((uint)v);
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }
        }

        public static int ConvToInt(string v)
        {
            if (v.ToLower().StartsWith("0x"))
            {
                try
                {
                    return (int)uint.Parse(v.Substring(2), NumberStyles.HexNumber);
                }
                catch
                {
                    return -1;
                }
            }
            else
            {
                try
                {
                    return int.Parse(v);
                }
                catch
                {
                    try
                    {
                        if (v.StartsWith("-"))
                        {
                            try
                            {
                                return -((int)uint.Parse(v.Substring(1)));
                            }
                            catch
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            try
                            {
                                return (int)uint.Parse(v.Substring(1));
                            }
                            catch
                            {
                                return -1;
                            }
                        }
                    }
                    catch
                    {
                        if (v.StartsWith("-"))
                        {
                            return 1;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                }
            }
        }

        public static int LSL_IntegerMultiply(int a, int b)
        {
#warning implement overflow behaviour for integer multiply
            return a * b;
        }

        public static int LSL_IntegerDivision(int a, int b)
        {
            if (a == -2147483648 && b == -1)
            {
                return -2147483648;
            }
            else
            {
                return a / b;
            }
        }

        public static int LSL_IntegerModulus(int a, int b)
        {
            if (a == -2147483648 && b == -1)
            {
                return 0;
            }
            else
            {
                return a / b;
            }
        }
        #endregion

        #region Preprocessor for concatenated string constants
        void CollapseStringConstants(List<string> args)
        {
            for (int pos = 1; pos < args.Count - 2; ++pos)
            {
                if (args[pos] == "+" && args[pos - 1].StartsWith("\"") && args[pos + 1].StartsWith("\""))
                {
                    args[pos - 1] = args[pos - 1] + args[pos + 1];
                    args.RemoveAt(pos);
                    args.RemoveAt(pos);
                    --pos;
                }
            }
        }
        #endregion

        #region Type validation and string representation
        internal static bool IsValidType(Type t)
        {
            if (t == typeof(string)) return true;
            if (t == typeof(int)) return true;
            if (t == typeof(double)) return true;
            if (t == typeof(LSLKey)) return true;
            if (t == typeof(Quaternion)) return true;
            if (t == typeof(Vector3)) return true;
            if (t == typeof(AnArray)) return true;
            if (t == typeof(void)) return true;
            return false;
        }
        internal static string MapType(Type t)
        {
            if (t == typeof(string)) return "string";
            if (t == typeof(int)) return "integer";
            if (t == typeof(double)) return "float";
            if (t == typeof(LSLKey)) return "key";
            if (t == typeof(Quaternion)) return "rotation";
            if (t == typeof(Vector3)) return "vector";
            if (t == typeof(AnArray)) return "list";
            if (t == typeof(void)) return "void";
            return "???";
        }
        #endregion

        #region Typecasting IL Generator
        internal static void ProcessImplicitCasts(ILGenerator ilgen, Type toType, Type fromType, int lineNumber)
        {
            if (fromType == toType)
            {

            }
            else if (toType == typeof(void))
            {
            }
            else if (fromType == typeof(string) && toType == typeof(LSLKey))
            {

            }
            else if (fromType == typeof(LSLKey) && toType == typeof(string))
            {

            }
            else if (fromType == typeof(int) && toType == typeof(double))
            {

            }
            else if (toType == typeof(AnArray))
            {

            }
            else if (toType == typeof(bool))
            {

            }
            else if(null == fromType)
            {
                throw new CompilerException(lineNumber, "Internal Error! fromType is not set");
            }
            else if (null == toType)
            {
                throw new CompilerException(lineNumber, "Internal Error! toType is not set");
            }
            else if (!IsValidType(fromType))
            {
                throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a LSL compatible type", fromType.FullName));
            }
            else if (!IsValidType(toType))
            {
                throw new CompilerException(lineNumber, string.Format("Internal Error! {0} is not a LSL compatible type", toType.FullName));
            }
            else
            {
                throw new CompilerException(lineNumber, string.Format("Unsupported implicit typecast from {0} to {1}", MapType(fromType), MapType(toType)));
            }
            ProcessCasts(ilgen, toType, fromType, lineNumber);
        }

        internal static void ProcessCasts(ILGenerator ilgen, Type toType, Type fromType, int lineNumber)
        {
            /* value is on stack before */
            if (toType == fromType)
            {
            }
            else if (toType == typeof(void))
            {
                ilgen.Emit(OpCodes.Pop);
            }
            else if (fromType == typeof(void))
            {
                throw new CompilerException(lineNumber, string.Format("function does not return anything"));
            }
            else if (toType == typeof(LSLKey))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Newobj, typeof(LSLKey).GetConstructor(new Type[] { fromType }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("function does not return anything"));
                }
            }
            else if (toType == typeof(string))
            {
                if (fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(string).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(double).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(Vector3))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(Vector3).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(Quaternion))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(Quaternion).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(AnArray))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("ToString", new Type[0]));
                }
                else if (fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetMethod("ToString", new Type[0]));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(int))
            {
                /* yes, we need special handling for conversion of string to integer or float to integer. (see section about Integer Overflow) */
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { fromType }));
                }
                else if (fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLCompiler).GetMethod("ConvToInt", new Type[] { fromType }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(bool))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(LSLKey).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(double))
                {
                    ilgen.Emit(OpCodes.Ldc_R8, 0f);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(AnArray))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetProperty("Count").GetGetMethod());
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(Quaternion))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else if (fromType == typeof(Vector3))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(Vector3).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Ldc_R8, 0f);
                    ilgen.Emit(OpCodes.Ceq);
                    ilgen.Emit(OpCodes.Ldc_I4_0);
                    ilgen.Emit(OpCodes.Ceq);
                }
                else if (fromType == typeof(LSLKey))
                {
                    ilgen.Emit(OpCodes.Call, typeof(LSLKey).GetProperty("IsLSLTrue").GetGetMethod());
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(double))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Callvirt, typeof(string).GetProperty("Length").GetGetMethod());
                    ilgen.Emit(OpCodes.Conv_R8);
                }
                else if (fromType == typeof(int))
                {
                    ilgen.Emit(OpCodes.Conv_R8);
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(Vector3))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Vector3).GetMethod("Parse", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(Quaternion))
            {
                if (fromType == typeof(string))
                {
                    ilgen.Emit(OpCodes.Call, typeof(Quaternion).GetMethod("Parse", new Type[] { typeof(string) }));
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else if (toType == typeof(AnArray))
            {
                if (fromType == typeof(string) || fromType == typeof(int) || fromType == typeof(double))
                {
                    ilgen.BeginScope();
                    LocalBuilder lb = ilgen.DeclareLocal(fromType);
                    ilgen.Emit(OpCodes.Stloc, lb);
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { fromType }));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.EndScope();
                }
                else if (fromType == typeof(Vector3) || fromType == typeof(Quaternion) || fromType == typeof(LSLKey))
                {
                    ilgen.BeginScope();
                    LocalBuilder lb = ilgen.DeclareLocal(fromType);
                    ilgen.Emit(OpCodes.Stloc, lb);
                    ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[0]));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.Emit(OpCodes.Callvirt, typeof(AnArray).GetMethod("Add", new Type[] { typeof(IValue) }));
                    ilgen.Emit(OpCodes.Ldloc, lb);
                    ilgen.EndScope();
                }
                else
                {
                    throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
                }
            }
            else
            {
                throw new CompilerException(lineNumber, string.Format("unsupported typecast from {0} to {1}", MapType(fromType), MapType(toType)));
            }
        }
        #endregion

        #region Variable Access IL Generator
        internal static Type GetVarType(
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            object v)
        {
            if (v is ILParameterInfo)
            {
                return ((ILParameterInfo)v).ParameterType;
            }
            else if (v is LocalBuilder)
            {
                return ((LocalBuilder)v).LocalType;
            }
            else if (v is FieldBuilder)
            {
                return ((FieldBuilder)v).FieldType;
            }
            else if (v is FieldInfo)
            {
                return ((FieldInfo)v).FieldType;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        internal static Type GetVarToStack(
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            object v)
        {
            Type retType;
            if (v is ILParameterInfo)
            {
                ilgen.Emit(OpCodes.Ldarg, ((ILParameterInfo)v).Position);
                retType = ((ILParameterInfo)v).ParameterType;
            }
            else if (v is LocalBuilder)
            {
                ilgen.Emit(OpCodes.Ldloc, (LocalBuilder)v);
                retType = ((LocalBuilder)v).LocalType;
            }
            else if (v is FieldBuilder)
            {
                if ((((FieldBuilder)v).Attributes & FieldAttributes.Static) != 0)
                {
                    ilgen.Emit(OpCodes.Ldsfld, ((FieldBuilder)v));
                }
                else
                {
                    ilgen.Emit(OpCodes.Ldfld, ((FieldBuilder)v));
                }
                retType = ((FieldBuilder)v).FieldType;
            }
            else if (v is FieldInfo)
            {
                if ((((FieldInfo)v).Attributes & FieldAttributes.Static) != 0)
                {
                    ilgen.Emit(OpCodes.Ldsfld, ((FieldInfo)v));
                }
                else
                {
                    ilgen.Emit(OpCodes.Ldfld, ((FieldInfo)v));
                }
                retType = ((FieldInfo)v).FieldType;
            }
            else
            {
                throw new NotImplementedException();
            }
            if (retType == typeof(AnArray))
            {
                /* list has deep copying */
                ilgen.Emit(OpCodes.Newobj, typeof(AnArray).GetConstructor(new Type[] { retType }));
            }
            return retType;
        }

        internal static void SetVarFromStack(
            TypeBuilder scriptTypeBuilder,
            TypeBuilder stateTypeBuilder,
            ILGenerator ilgen,
            object v,
            int lineNumber)
        {
            if (v is ILParameterInfo)
            {
                ilgen.Emit(OpCodes.Starg, ((ILParameterInfo)v).Position);
            }
            else if (v is LocalBuilder)
            {
                ilgen.Emit(OpCodes.Stloc, (LocalBuilder)v);
            }
            else if (v is FieldBuilder)
            {
                if ((((FieldInfo)v).Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, "Setting constants is not allowed");
                }
                ilgen.Emit(OpCodes.Stfld, ((FieldBuilder)v));
            }
            else if (v is FieldInfo)
            {
                if ((((FieldInfo)v).Attributes & FieldAttributes.Static) != 0)
                {
                    throw new CompilerException(lineNumber, "Setting constants is not allowed");
                }
                ilgen.Emit(OpCodes.Stfld, ((FieldInfo)v));
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region Constants collector for IL Generator
        Dictionary<string, object> AddConstants(CompileState compileState, TypeBuilder typeBuilder, ILGenerator ilgen)
        {
            Dictionary<string, object> localVars = new Dictionary<string, object>();
            foreach (IScriptApi api in m_Apis)
            {
                foreach (FieldInfo f in api.GetType().GetFields())
                {
                    System.Attribute attr = System.Attribute.GetCustomAttribute(f, typeof(APILevel));

                    if (attr != null && (f.Attributes & FieldAttributes.Static) != 0)
                    {
                        if ((f.Attributes & FieldAttributes.InitOnly) != 0 || (f.Attributes & FieldAttributes.Literal) != 0)
                        {
                            APILevel apilevel = (APILevel)attr;
                            if ((apilevel.Flags & compileState.AcceptedFlags) != 0)
                            {
                                localVars[f.Name] = f;
                            }
                        }
                        else
                        {
                            m_Log.DebugFormat("Field {0} has unsupported attribute flags {1}", f.Name, f.Attributes.ToString());
                        }
                    }
                }
            }
            return localVars;
        }
        #endregion
    }
}
