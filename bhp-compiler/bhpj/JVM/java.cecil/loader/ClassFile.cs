﻿
using System;
using System.IO;
using System.Collections.Generic;

namespace javaloader
{
    enum HardError : short
    {
        NoClassDefFoundError,
        IllegalAccessError,
        InstantiationError,
        IncompatibleClassChangeError,
        NoSuchFieldError,
        AbstractMethodError,
        NoSuchMethodError,
        LinkageError,
        // "exceptions" that are wrapped in an IncompatibleClassChangeError
        NoSuchFieldException,
        NoSuchMethodException,
        IllegalAccessException,
        // if an error is added here, it must also be added to MethodAnalyzer.SetHardError()
    }

    [Flags]
    public enum ClassFileParseOptions
    {
        None = 0,
        LocalVariableTable = 1,
        LineNumberTable = 2,
        RelaxedClassNameValidation = 4,
    }

    public class ClassFile
    {
        public ConstantPoolItem[] constantpool;
        // Modifiers is a ushort, so the next four fields combine into two 32 bit slots
        private Modifiers access_flags;
        private ushort this_class;
        private ushort super_class;
        private ushort flags;
        private const ushort FLAG_MASK_MAJORVERSION = 0xFF;
        private const ushort FLAG_MASK_DEPRECATED = 0x100;
        private const ushort FLAG_MASK_INTERNAL = 0x200;
        private const ushort FLAG_MASK_EFFECTIVELY_FINAL = 0x400;
        private const ushort FLAG_HAS_CALLERID = 0x800;
        private ConstantPoolItemClass[] interfaces;
        private Field[] fields;
        private Method[] methods;
        private string sourceFile;
#if !STATIC_COMPILER
        //private string sourcePath;
#endif
        private string ikvmAssembly;
        private InnerClass[] innerClasses;
        private object[] annotations;
        private string signature;
        private string[] enclosingMethod;
        private BootstrapMethod[] bootstrapMethods;

        private static class SupportedVersions
        {
            internal static readonly int Minimum = 45;
            internal static readonly int Maximum = 52;
        }

#if !STATIC_COMPILER
        // This method parses just enough of the class file to obtain its name, it doesn't
        // validate the class file structure, but it may throw a ClassFormatError if it
        // encounters bogus data
        public static string GetClassName(byte[] buf, int offset, int length)
        {
            BigEndianBinaryReader br = new BigEndianBinaryReader(buf, offset, length);
            if (br.ReadUInt32() != 0xCAFEBABE)
            {
                throw new Exception("Bad magic number");
            }
            int minorVersion = br.ReadUInt16();
            int majorVersion = br.ReadUInt16();
            if ((majorVersion & FLAG_MASK_MAJORVERSION) != majorVersion
                || majorVersion < SupportedVersions.Minimum
                || majorVersion > SupportedVersions.Maximum
                || (majorVersion == SupportedVersions.Minimum && minorVersion < 3)
                || (majorVersion == SupportedVersions.Maximum && minorVersion != 0))
            {
                throw new Exception(majorVersion + "." + minorVersion);
            }
            int constantpoolcount = br.ReadUInt16();
            int[] cpclass = new int[constantpoolcount];
            string[] utf8_cp = new string[constantpoolcount];
            for (int i = 1; i < constantpoolcount; i++)
            {
                Constant tag = (Constant)br.ReadByte();
                switch (tag)
                {
                    case Constant.Class:
                        cpclass[i] = br.ReadUInt16();
                        break;
                    case Constant.Double:
                    case Constant.Long:
                        br.Skip(8);
                        i++;
                        break;
                    case Constant.Fieldref:
                    case Constant.InterfaceMethodref:
                    case Constant.Methodref:
                    case Constant.InvokeDynamic:
                    case Constant.NameAndType:
                    case Constant.Float:
                    case Constant.Integer:
                        br.Skip(4);
                        break;
                    case Constant.MethodHandle:
                        br.Skip(3);
                        break;
                    case Constant.String:
                    case Constant.MethodType:
                        br.Skip(2);
                        break;
                    case Constant.Utf8:
                        utf8_cp[i] = br.ReadString();
                        break;
                    default:
                        throw new Exception("Illegal constant pool type 0x" + tag.ToString("X04"));
                }
            }
            br.ReadUInt16(); // access_flags
            try
            {
                return utf8_cp[cpclass[br.ReadUInt16()]].Replace('/', '.');
            }
            catch (Exception x)
            {
                throw new Exception(x.GetType().Name + ":" + x.Message);
            }
        }
#endif // STATIC_COMPILER
        public ClassFile(byte[] buf, int offset, int length)
        {
            try
            {
                BigEndianBinaryReader br = new BigEndianBinaryReader(buf, offset, length);
                if (br.ReadUInt32() != 0xCAFEBABE)
                {
                    throw new Exception(" (Bad magic number)");
                }
                ushort minorVersion = br.ReadUInt16();
                ushort majorVersion = br.ReadUInt16();
                if ((majorVersion & FLAG_MASK_MAJORVERSION) != majorVersion
                    || majorVersion < SupportedVersions.Minimum
                    || majorVersion > SupportedVersions.Maximum
                    || (majorVersion == SupportedVersions.Minimum && minorVersion < 3)
                    || (majorVersion == SupportedVersions.Maximum && minorVersion != 0))
                {
                    throw new Exception(" (" + majorVersion + "." + minorVersion + ")");
                }
                flags = majorVersion;
                int constantpoolcount = br.ReadUInt16();
                constantpool = new ConstantPoolItem[constantpoolcount];
                string[] utf8_cp = new string[constantpoolcount];
                for (int i = 1; i < constantpoolcount; i++)
                {
                    Constant tag = (Constant)br.ReadByte();
                    switch (tag)
                    {
                        case Constant.Class:
                            constantpool[i] = new ConstantPoolItemClass(br);
                            break;
                        case Constant.Double:
                            constantpool[i] = new ConstantPoolItemDouble(br);
                            i++;
                            break;
                        case Constant.Fieldref:
                            constantpool[i] = new ConstantPoolItemFieldref(br);
                            break;
                        case Constant.Float:
                            constantpool[i] = new ConstantPoolItemFloat(br);
                            break;
                        case Constant.Integer:
                            constantpool[i] = new ConstantPoolItemInteger(br);
                            break;
                        case Constant.InterfaceMethodref:
                            constantpool[i] = new ConstantPoolItemInterfaceMethodref(br);
                            break;
                        case Constant.Long:
                            constantpool[i] = new ConstantPoolItemLong(br);
                            i++;
                            break;
                        case Constant.Methodref:
                            constantpool[i] = new ConstantPoolItemMethodref(br);
                            break;
                        case Constant.NameAndType:
                            constantpool[i] = new ConstantPoolItemNameAndType(br);
                            break;
                        case Constant.MethodHandle:
                            if (majorVersion < 51)
                                goto default;
                            constantpool[i] = new ConstantPoolItemMethodHandle(br);
                            break;
                        case Constant.MethodType:
                            if (majorVersion < 51)
                                goto default;
                            constantpool[i] = new ConstantPoolItemMethodType(br);
                            break;
                        case Constant.InvokeDynamic:
                            if (majorVersion < 51)
                                goto default;
                            constantpool[i] = new ConstantPoolItemInvokeDynamic(br);
                            break;
                        case Constant.String:
                            constantpool[i] = new ConstantPoolItemString(br);
                            break;
                        case Constant.Utf8:
                            utf8_cp[i] = br.ReadString();
                            break;
                        default:
                            throw new Exception(" (Illegal constant pool type 0x" + tag.ToString("X04"));
                    }
                }
                for (int i = 1; i < constantpoolcount; i++)
                {
                    if (constantpool[i] != null)
                    {

                        constantpool[i].Resolve(this, utf8_cp);

                    }
                }
                access_flags = (Modifiers)br.ReadUInt16();
                // NOTE although the vmspec says (in 4.1) that interfaces must be marked abstract, earlier versions of
                // javac (JDK 1.1) didn't do this, so the VM doesn't enforce this rule for older class files.
                // NOTE although the vmspec implies (in 4.1) that ACC_SUPER is illegal on interfaces, it doesn't enforce this
                // for older class files.
                // (See http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=6320322)
                if ((IsInterface && IsFinal)
                    || (IsAbstract && IsFinal)
                    || (majorVersion >= 49 && IsAnnotation && !IsInterface)
                    || (majorVersion >= 49 && IsInterface && (!IsAbstract || IsSuper || IsEnum)))
                {
                    throw new Exception(" (Illegal class modifiers 0x" + access_flags.ToString("X04"));
                }
                this_class = br.ReadUInt16();
                ValidateConstantPoolItemClass(this_class);
                super_class = br.ReadUInt16();
                ValidateConstantPoolItemClass(super_class);
                if (IsInterface && (super_class == 0 || this.SuperClass != "java.lang.Object"))
                {
                    throw new Exception(Name + " (Interfaces must have java.lang.Object as superclass)");
                }
                // most checks are already done by ConstantPoolItemClass.Resolve, but since it allows
                // array types, we do need to check for that
                if (this.Name[0] == '[')
                {
                    throw new Exception("Bad name");
                }
                int interfaces_count = br.ReadUInt16();
                interfaces = new ConstantPoolItemClass[interfaces_count];
                for (int i = 0; i < interfaces.Length; i++)
                {
                    int index = br.ReadUInt16();
                    if (index == 0 || index >= constantpool.Length)
                    {
                        throw new Exception(Name + " (Illegal constant pool index)");
                    }
                    ConstantPoolItemClass cpi = constantpool[index] as ConstantPoolItemClass;
                    if (cpi == null)
                    {
                        throw new Exception(Name + " (Interface name has bad constant type)");
                    }
                    interfaces[i] = cpi;
                    for (int j = 0; j < i; j++)
                    {
                        if (Equals(interfaces[j].Name, cpi.Name))
                        {
                            throw new Exception(Name + " (Repetitive interface name)");
                        }
                    }
                }
                int fields_count = br.ReadUInt16();
                fields = new Field[fields_count];
                for (int i = 0; i < fields_count; i++)
                {
                    fields[i] = new Field(this, utf8_cp, br);
                    string name = fields[i].Name;
                    if (!IsValidFieldName(name, majorVersion))
                    {
                        throw new Exception(Name + " (Illegal field name \"" + name + "\")");
                    }
                    for (int j = 0; j < i; j++)
                    {
                        if (Equals(fields[j].Name, name) && Equals(fields[j].Signature, fields[i].Signature))
                        {
                            throw new Exception(Name + " (Repetitive field name/signature)");
                        }
                    }
                }
                int methods_count = br.ReadUInt16();
                methods = new Method[methods_count];
                for (int i = 0; i < methods_count; i++)
                {
                    methods[i] = new Method(this, utf8_cp, br);
                    string name = methods[i].Name;
                    string sig = methods[i].Signature;
                    if (!IsValidMethodName(name, majorVersion))
                    {
                        if (!Equals(name, StringConstants.INIT) && !Equals(name, StringConstants.CLINIT))
                        {
                            throw new Exception(Name + " (Illegal method name \"" + name + "\")");
                        }
                        if (!sig.EndsWith("V"))
                        {
                            throw new Exception(Name + " (Method \"" + name + "\" has illegal signature \"" + sig + "\")");
                        }
                    }
                    for (int j = 0; j < i; j++)
                    {
                        if (Equals(methods[j].Name, name) && Equals(methods[j].Signature, sig))
                        {
                            throw new Exception(Name + " (Repetitive method name/signature)");
                        }
                    }
                }
                int attributes_count = br.ReadUInt16();
                for (int i = 0; i < attributes_count; i++)
                {
                    switch (GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16()))
                    {
                        case "Deprecated":
                            if (br.ReadUInt32() != 0)
                            {
                                throw new Exception("Invalid Deprecated attribute length");
                            }
                            flags |= FLAG_MASK_DEPRECATED;
                            break;
                        case "SourceFile":
                            if (br.ReadUInt32() != 2)
                            {
                                throw new Exception("SourceFile attribute has incorrect length");
                            }
                            sourceFile = GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16());
                            break;
                        case "InnerClasses":
                            {
                                BigEndianBinaryReader rdr = br;
                                uint attribute_length = br.ReadUInt32();
                                ushort count = rdr.ReadUInt16();
                                if (this.MajorVersion >= 49 && attribute_length != 2 + count * (2 + 2 + 2 + 2))
                                {
                                    throw new Exception(this.Name + " (InnerClasses attribute has incorrect length)");
                                }
                                innerClasses = new InnerClass[count];
                                for (int j = 0; j < innerClasses.Length; j++)
                                {
                                    innerClasses[j].innerClass = rdr.ReadUInt16();
                                    innerClasses[j].outerClass = rdr.ReadUInt16();
                                    innerClasses[j].name = rdr.ReadUInt16();
                                    innerClasses[j].accessFlags = (Modifiers)rdr.ReadUInt16();
                                    if (innerClasses[j].innerClass != 0 && !(GetConstantPoolItem(innerClasses[j].innerClass) is ConstantPoolItemClass))
                                    {
                                        throw new Exception(this.Name + " (inner_class_info_index has bad constant pool index)");
                                    }
                                    if (innerClasses[j].outerClass != 0 && !(GetConstantPoolItem(innerClasses[j].outerClass) is ConstantPoolItemClass))
                                    {
                                        throw new Exception(this.Name + " (outer_class_info_index has bad constant pool index)");
                                    }
                                    if (innerClasses[j].name != 0 && utf8_cp[innerClasses[j].name] == null)
                                    {
                                        throw new Exception(this.Name + " (inner class name has bad constant pool index)");
                                    }
                                    if (innerClasses[j].innerClass == innerClasses[j].outerClass)
                                    {
                                        throw new Exception(this.Name + " (Class is both inner and outer class)");
                                    }
                                    if (innerClasses[j].innerClass != 0 && innerClasses[j].outerClass != 0)
                                    {
                                        MarkLinkRequiredConstantPoolItem(innerClasses[j].innerClass);
                                        MarkLinkRequiredConstantPoolItem(innerClasses[j].outerClass);
                                    }
                                }
                                break;
                            }
                        case "Signature":
                            if (majorVersion < 49)
                            {
                                goto default;
                            }
                            if (br.ReadUInt32() != 2)
                            {
                                throw new Exception("Signature attribute has incorrect length");
                            }
                            signature = GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16());
                            break;
                        case "EnclosingMethod":
                            if (majorVersion < 49)
                            {
                                goto default;
                            }
                            if (br.ReadUInt32() != 4)
                            {
                                throw new Exception("EnclosingMethod attribute has incorrect length");
                            }
                            else
                            {
                                int class_index = br.ReadUInt16();
                                int method_index = br.ReadUInt16();
                                if (method_index == 0)
                                {
                                    enclosingMethod = new string[] {
                                        GetConstantPoolClass(class_index),
                                        null,
                                        null
                                                                   };
                                }
                                else
                                {
                                    ConstantPoolItemNameAndType m = (ConstantPoolItemNameAndType)GetConstantPoolItem(method_index);
                                    enclosingMethod = new string[] {
                                        GetConstantPoolClass(class_index),
                                        GetConstantPoolUtf8String(utf8_cp, m.name_index),
                                        GetConstantPoolUtf8String(utf8_cp, m.descriptor_index).Replace('/', '.')
                                                                   };
                                }
                            }
                            break;
                        case "RuntimeVisibleAnnotations":
                            if (majorVersion < 49)
                            {
                                goto default;
                            }
                            annotations = ReadAnnotations(br, this, utf8_cp);
                            break;
						case "RuntimeInvisibleAnnotations":
							if(majorVersion < 49)
							{
								goto default;
							}
							foreach(object[] annot in ReadAnnotations(br, this, utf8_cp))
							{
								if(annot[1].Equals("Likvm/lang/Internal;"))
								{
									this.access_flags &= ~Modifiers.AccessMask;
									flags |= FLAG_MASK_INTERNAL;
								}
							}
							break;

                        case "BootstrapMethods":
                            if (majorVersion < 51)
                            {
                                goto default;
                            }
                            bootstrapMethods = ReadBootstrapMethods(br, this);
                            break;
                        case "IKVM.NET.Assembly":
                            if (br.ReadUInt32() != 2)
                            {
                                throw new Exception("IKVM.NET.Assembly attribute has incorrect length");
                            }
                            ikvmAssembly = GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16());
                            break;
                        default:
                            br.Skip(br.ReadUInt32());
                            break;
                    }
                }
                // validate the invokedynamic entries to point into the bootstrapMethods array
                for (int i = 1; i < constantpoolcount; i++)
                {
                    ConstantPoolItemInvokeDynamic cpi;
                    if (constantpool[i] != null
                        && (cpi = constantpool[i] as ConstantPoolItemInvokeDynamic) != null)
                    {
                        if (bootstrapMethods == null || cpi.BootstrapMethod >= bootstrapMethods.Length)
                        {
                            throw new Exception("Short length on BootstrapMethods in class file");
                        }
                    }
                }
                if (br.Position != offset + length)
                {
                    throw new Exception("Extra bytes at the end of the class file");
                }
            }
            catch (OverflowException)
            {
                throw new Exception("Truncated class file (or section)");
            }
            catch (IndexOutOfRangeException)
            {
                // TODO we should throw more specific errors
                throw new Exception("Unspecified class file format error");
            }
            //		catch(Exception x)
            //		{
            //			Console.WriteLine(x);
            //			FileStream fs = File.Create(inputClassName + ".broken");
            //			fs.Write(buf, offset, length);
            //			fs.Close();
            //			throw;
            //		}
        }

        private void MarkLinkRequiredConstantPoolItem(int index)
        {
            if (index > 0 && index < constantpool.Length && constantpool[index] != null)
            {
                constantpool[index].MarkLinkRequired();
            }
        }

        private static BootstrapMethod[] ReadBootstrapMethods(BigEndianBinaryReader br, ClassFile classFile)
        {
            BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
            ushort count = rdr.ReadUInt16();
            BootstrapMethod[] bsm = new BootstrapMethod[count];
            for (int i = 0; i < bsm.Length; i++)
            {
                ushort bsm_index = rdr.ReadUInt16();
                if (bsm_index >= classFile.constantpool.Length || !(classFile.constantpool[bsm_index] is ConstantPoolItemMethodHandle))
                {
                    throw new Exception(string.Format("bootstrap_method_index {0} has bad constant type in class file {1}", bsm_index, classFile.Name));
                }
                classFile.MarkLinkRequiredConstantPoolItem(bsm_index);
                ushort argument_count = rdr.ReadUInt16();
                ushort[] args = new ushort[argument_count];
                for (int j = 0; j < args.Length; j++)
                {
                    ushort argument_index = rdr.ReadUInt16();
                    if (!classFile.IsValidConstant(argument_index))
                    {
                        throw new Exception(string.Format("argument_index {0} has bad constant type in class file {1}", argument_index, classFile.Name));
                    }
                    classFile.MarkLinkRequiredConstantPoolItem(argument_index);
                    args[j] = argument_index;
                }
                bsm[i] = new BootstrapMethod(bsm_index, args);
            }
            if (!rdr.IsAtEnd)
            {
                throw new Exception(string.Format("Bad length on BootstrapMethods in class file {0}", classFile.Name));
            }
            return bsm;
        }

        private bool IsValidConstant(ushort index)
        {
            if (index < constantpool.Length && constantpool[index] != null)
            {
                try
                {
                    constantpool[index].GetConstantType();
                    return true;
                }
                catch (InvalidOperationException) { }
            }
            return false;
        }

        private static object[] ReadAnnotations(BigEndianBinaryReader br, ClassFile classFile, string[] utf8_cp)
        {
            BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
            ushort num_annotations = rdr.ReadUInt16();
            object[] annotations = new object[num_annotations];
            for (int i = 0; i < annotations.Length; i++)
            {
                annotations[i] = ReadAnnotation(rdr, classFile, utf8_cp);
            }
            if (!rdr.IsAtEnd)
            {
                throw new Exception(string.Format("{0} (RuntimeVisibleAnnotations attribute has wrong length)", classFile.Name));
            }
            return annotations;
        }

        private static object ReadAnnotation(BigEndianBinaryReader rdr, ClassFile classFile, string[] utf8_cp)
        {
            string type = classFile.GetConstantPoolUtf8String(utf8_cp, rdr.ReadUInt16());
            ushort num_element_value_pairs = rdr.ReadUInt16();
            object[] annot = new object[2 + num_element_value_pairs * 2];
            annot[0] = AnnotationDefaultAttribute.TAG_ANNOTATION;
            annot[1] = type;
            for (int i = 0; i < num_element_value_pairs; i++)
            {
                annot[2 + i * 2 + 0] = classFile.GetConstantPoolUtf8String(utf8_cp, rdr.ReadUInt16());
                annot[2 + i * 2 + 1] = ReadAnnotationElementValue(rdr, classFile, utf8_cp);
            }
            return annot;
        }

        private static object ReadAnnotationElementValue(BigEndianBinaryReader rdr, ClassFile classFile, string[] utf8_cp)
        {
            try
            {
                byte tag = rdr.ReadByte();
                switch (tag)
                {
                    case (byte)'Z':
                        return classFile.GetConstantPoolConstantInteger(rdr.ReadUInt16()) != 0;
                    case (byte)'B':
                        return (byte)classFile.GetConstantPoolConstantInteger(rdr.ReadUInt16());
                    case (byte)'C':
                        return (char)classFile.GetConstantPoolConstantInteger(rdr.ReadUInt16());
                    case (byte)'S':
                        return (short)classFile.GetConstantPoolConstantInteger(rdr.ReadUInt16());
                    case (byte)'I':
                        return classFile.GetConstantPoolConstantInteger(rdr.ReadUInt16());
                    case (byte)'F':
                        return classFile.GetConstantPoolConstantFloat(rdr.ReadUInt16());
                    case (byte)'J':
                        return classFile.GetConstantPoolConstantLong(rdr.ReadUInt16());
                    case (byte)'D':
                        return classFile.GetConstantPoolConstantDouble(rdr.ReadUInt16());
                    case (byte)'s':
                        return classFile.GetConstantPoolUtf8String(utf8_cp, rdr.ReadUInt16());
                    case (byte)'e':
                        {
                            ushort type_name_index = rdr.ReadUInt16();
                            ushort const_name_index = rdr.ReadUInt16();
                            return new object[] {
                                            AnnotationDefaultAttribute.TAG_ENUM,
                                            classFile.GetConstantPoolUtf8String(utf8_cp, type_name_index),
                                            classFile.GetConstantPoolUtf8String(utf8_cp, const_name_index)
                                        };
                        }
                    case (byte)'c':
                        return new object[] {
                                            AnnotationDefaultAttribute.TAG_CLASS,
                                            classFile.GetConstantPoolUtf8String(utf8_cp, rdr.ReadUInt16())
                                        };
                    case (byte)'@':
                        return ReadAnnotation(rdr, classFile, utf8_cp);
                    case (byte)'[':
                        {
                            ushort num_values = rdr.ReadUInt16();
                            object[] array = new object[num_values + 1];
                            array[0] = AnnotationDefaultAttribute.TAG_ARRAY;
                            for (int i = 0; i < num_values; i++)
                            {
                                array[i + 1] = ReadAnnotationElementValue(rdr, classFile, utf8_cp);
                            }
                            return array;
                        }
                    default:
                        throw new Exception(string.Format("Invalid tag {0} in annotation element_value", tag));
                }
            }
            catch (NullReferenceException)
            {
            }
            catch (InvalidCastException)
            {
            }
            catch (IndexOutOfRangeException)
            {
            }
            return new object[] { AnnotationDefaultAttribute.TAG_ERROR, "java.lang.IllegalArgumentException", "Wrong type at constant pool index" };
        }

        private void ValidateConstantPoolItemClass(ushort index)
        {
            if (index >= constantpool.Length || !(constantpool[index] is ConstantPoolItemClass))
            {
                throw new Exception(string.Format("(Bad constant pool index #{0})", index));
            }
        }

        private static bool IsValidMethodName(string name, int majorVersion)
        {
            if (name.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < name.Length; i++)
            {
                if (".;/<>".IndexOf(name[i]) != -1)
                {
                    return false;
                }
            }
            return majorVersion >= 49 || IsValidPre49Identifier(name);
        }

        private static bool IsValidFieldName(string name, int majorVersion)
        {
            if (name.Length == 0)
            {
                return false;
            }
            for (int i = 0; i < name.Length; i++)
            {
                if (".;/".IndexOf(name[i]) != -1)
                {
                    return false;
                }
            }
            return majorVersion >= 49 || IsValidPre49Identifier(name);
        }

        private static bool IsValidPre49Identifier(string name)
        {
            if (!Char.IsLetter(name[0]) && "$_".IndexOf(name[0]) == -1)
            {
                return false;
            }
            for (int i = 1; i < name.Length; i++)
            {
                if (!Char.IsLetterOrDigit(name[i]) && "$_".IndexOf(name[i]) == -1)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool IsValidFieldSig(string sig)
        {
            return IsValidFieldSigImpl(sig, 0, sig.Length);
        }

        private static bool IsValidFieldSigImpl(string sig, int start, int end)
        {
            if (start >= end)
            {
                return false;
            }
            switch (sig[start])
            {
                case 'L':
                    return sig.IndexOf(';', start + 1) == end - 1;
                case '[':
                    while (sig[start] == '[')
                    {
                        start++;
                        if (start == end)
                        {
                            return false;
                        }
                    }
                    return IsValidFieldSigImpl(sig, start, end);
                case 'B':
                case 'Z':
                case 'C':
                case 'S':
                case 'I':
                case 'J':
                case 'F':
                case 'D':
                    return start == end - 1;
                default:
                    return false;
            }
        }

        internal static bool IsValidMethodSig(string sig)
        {
            if (sig.Length < 3 || sig[0] != '(')
            {
                return false;
            }
            int end = sig.IndexOf(')');
            if (end == -1)
            {
                return false;
            }
            if (!sig.EndsWith(")V") && !IsValidFieldSigImpl(sig, end + 1, sig.Length))
            {
                return false;
            }
            for (int i = 1; i < end; i++)
            {
                switch (sig[i])
                {
                    case 'B':
                    case 'Z':
                    case 'C':
                    case 'S':
                    case 'I':
                    case 'J':
                    case 'F':
                    case 'D':
                        break;
                    case 'L':
                        i = sig.IndexOf(';', i);
                        break;
                    case '[':
                        while (sig[i] == '[')
                        {
                            i++;
                        }
                        if ("BZCSIJFDL".IndexOf(sig[i]) == -1)
                        {
                            return false;
                        }
                        if (sig[i] == 'L')
                        {
                            i = sig.IndexOf(';', i);
                        }
                        break;
                    default:
                        return false;
                }
                if (i == -1 || i >= end)
                {
                    return false;
                }
            }
            return true;
        }

        internal int MajorVersion
        {
            get
            {
                return flags & FLAG_MASK_MAJORVERSION;
            }
        }

        //lights tag
        //怀疑没用，先删除
        //internal void Link(TypeWrapper thisType)
        //{
        //    for (int i = 1; i < constantpool.Length; i++)
        //    {
        //        if (constantpool[i] != null)
        //        {
        //            constantpool[i].Link(thisType);
        //        }
        //    }
        //}

        internal Modifiers Modifiers
        {
            get
            {
                return access_flags;
            }
        }

        internal bool IsAbstract
        {
            get
            {
                // interfaces are implicitly abstract
                return (access_flags & (Modifiers.Abstract | Modifiers.Interface)) != 0;
            }
        }

        internal bool IsFinal
        {
            get
            {
                return (access_flags & Modifiers.Final) != 0;
            }
        }

        internal bool IsPublic
        {
            get
            {
                return (access_flags & Modifiers.Public) != 0;
            }
        }

        internal bool IsInterface
        {
            get
            {
                return (access_flags & Modifiers.Interface) != 0;
            }
        }

        internal bool IsEnum
        {
            get
            {
                return (access_flags & Modifiers.Enum) != 0;
            }
        }

        internal bool IsAnnotation
        {
            get
            {
                return (access_flags & Modifiers.Annotation) != 0;
            }
        }

        internal bool IsSuper
        {
            get
            {
                return (access_flags & Modifiers.Super) != 0;
            }
        }

        internal void RemoveUnusedFields()
        {
            List<Field> list = new List<Field>();
            foreach (Field f in fields)
            {
                if (f.IsPrivate && f.IsStatic && f.Name != "serialVersionUID" && !IsReferenced(f))
                {
                    ////lights tag
                    //// unused, so we skip it
                    //Tracer.Info(Tracer.Compiler, "Unused field {0}::{1}", this.Name, f.Name);
                }
                else
                {
                    list.Add(f);
                }
            }
            fields = list.ToArray();
        }

        private bool IsReferenced(Field fld)
        {
            foreach (ConstantPoolItem cpi in constantpool)
            {
                ConstantPoolItemFieldref fieldref = cpi as ConstantPoolItemFieldref;
                if (fieldref != null &&
                    fieldref.Class == this.Name &&
                    fieldref.Name == fld.Name &&
                    fieldref.Signature == fld.Signature)
                {
                    return true;
                }
            }
            return false;
        }

        internal ConstantPoolItemFieldref GetFieldref(int index)
        {
            return (ConstantPoolItemFieldref)constantpool[index];
        }

        // this won't throw an exception if index is invalid
        // (used by IsSideEffectFreeStaticInitializer)
        internal ConstantPoolItemFieldref SafeGetFieldref(int index)
        {
            if (index > 0 && index < constantpool.Length)
            {
                return constantpool[index] as ConstantPoolItemFieldref;
            }
            return null;
        }

        // NOTE this returns an MI, because it used for both normal methods and interface methods
        internal ConstantPoolItemMI GetMethodref(int index)
        {
            return (ConstantPoolItemMI)constantpool[index];
        }

        // this won't throw an exception if index is invalid
        // (used by IsAccessBridge)
        internal ConstantPoolItemMI SafeGetMethodref(int index)
        {
            if (index > 0 && index < constantpool.Length)
            {
                return constantpool[index] as ConstantPoolItemMI;
            }
            return null;
        }

        internal ConstantPoolItemInvokeDynamic GetInvokeDynamic(int index)
        {
            return (ConstantPoolItemInvokeDynamic)constantpool[index];
        }

        private ConstantPoolItem GetConstantPoolItem(int index)
        {
            return constantpool[index];
        }

        internal string GetConstantPoolClass(int index)
        {
            return ((ConstantPoolItemClass)constantpool[index]).Name;
        }

        private bool SafeIsConstantPoolClass(int index)
        {
            if (index > 0 && index < constantpool.Length)
            {
                return constantpool[index] as ConstantPoolItemClass != null;
            }
            return false;
        }

        //lights tag 怀疑没用
        //internal TypeWrapper GetConstantPoolClassType(int index)
        //{
        //    return ((ConstantPoolItemClass)constantpool[index]).GetClassType();
        //}

        private string GetConstantPoolUtf8String(string[] utf8_cp, int index)
        {
            string s = utf8_cp[index];
            if (s == null)
            {
                if (this_class == 0)
                {
                    throw new Exception(string.Format("Bad constant pool index #{0}", index));
                }
                else
                {
                    throw new Exception(string.Format("{0} (Bad constant pool index #{1})", this.Name, index));
                }
            }
            return s;
        }

        internal ConstantType GetConstantPoolConstantType(int index)
        {
            return constantpool[index].GetConstantType();
        }

        internal double GetConstantPoolConstantDouble(int index)
        {
            return ((ConstantPoolItemDouble)constantpool[index]).Value;
        }

        internal float GetConstantPoolConstantFloat(int index)
        {
            return ((ConstantPoolItemFloat)constantpool[index]).Value;
        }

        internal int GetConstantPoolConstantInteger(int index)
        {
            return ((ConstantPoolItemInteger)constantpool[index]).Value;
        }

        internal long GetConstantPoolConstantLong(int index)
        {
            return ((ConstantPoolItemLong)constantpool[index]).Value;
        }

        internal string GetConstantPoolConstantString(int index)
        {
            return ((ConstantPoolItemString)constantpool[index]).Value;
        }

        internal ConstantPoolItemMethodHandle GetConstantPoolConstantMethodHandle(int index)
        {
            return (ConstantPoolItemMethodHandle)constantpool[index];
        }

        internal ConstantPoolItemMethodType GetConstantPoolConstantMethodType(int index)
        {
            return (ConstantPoolItemMethodType)constantpool[index];
        }

        public string Name
        {
            get
            {
                return GetConstantPoolClass(this_class);
            }
        }

        public string SuperClass
        {
            get
            {
                return GetConstantPoolClass(super_class);
            }
        }

        public Field[] Fields
        {
            get
            {
                return fields;
            }
        }

        public Method[] Methods
        {
            get
            {
                return methods;
            }
        }

        public ConstantPoolItemClass[] Interfaces
        {
            get
            {
                return interfaces;
            }
        }

        internal string SourceFileAttribute
        {
            get
            {
                return sourceFile;
            }
        }

        internal string SourcePath
        {
#if STATIC_COMPILER
			get { return sourcePath; }
			set { sourcePath = value; }
#else
            get { return sourceFile; }
#endif
        }

        public object[] Annotations
        {
            get
            {
                return annotations;
            }
        }

        internal string GenericSignature
        {
            get
            {
                return signature;
            }
        }

        internal string[] EnclosingMethod
        {
            get
            {
                return enclosingMethod;
            }
        }

        internal string IKVMAssemblyAttribute
        {
            get
            {
                return ikvmAssembly;
            }
        }

        internal bool DeprecatedAttribute
        {
            get
            {
                return (flags & FLAG_MASK_DEPRECATED) != 0;
            }
        }

        internal bool IsInternal
        {
            get
            {
                return (flags & FLAG_MASK_INTERNAL) != 0;
            }
        }

        // for use by ikvmc (to implement the -privatepackage option)
        internal void SetInternal()
        {
            access_flags &= ~Modifiers.AccessMask;
            flags |= FLAG_MASK_INTERNAL;
        }

        internal void SetEffectivelyFinal()
        {
            flags |= FLAG_MASK_EFFECTIVELY_FINAL;
        }

        internal bool IsEffectivelyFinal
        {
            get
            {
                return (flags & FLAG_MASK_EFFECTIVELY_FINAL) != 0;
            }
        }

        internal bool HasInitializedFields
        {
            get
            {
                foreach (Field f in fields)
                {
                    if (f.IsStatic && !f.IsFinal && f.ConstantValue != null)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal BootstrapMethod GetBootstrapMethod(int index)
        {
            return bootstrapMethods[index];
        }

        internal struct BootstrapMethod
        {
            private ushort bsm_index;
            private ushort[] args;

            internal BootstrapMethod(ushort bsm_index, ushort[] args)
            {
                this.bsm_index = bsm_index;
                this.args = args;
            }

            internal int BootstrapMethodIndex
            {
                get { return bsm_index; }
            }

            internal int ArgumentCount
            {
                get { return args.Length; }
            }

            internal int GetArgument(int index)
            {
                return args[index];
            }
        }

        internal struct InnerClass
        {
            internal ushort innerClass;     // ConstantPoolItemClass
            internal ushort outerClass;     // ConstantPoolItemClass
            internal ushort name;           // ConstantPoolItemUtf8
            internal Modifiers accessFlags;
        }

        internal InnerClass[] InnerClasses
        {
            get
            {
                return innerClasses;
            }
        }

        public enum RefKind
        {
            getField = 1,
            getStatic = 2,
            putField = 3,
            putStatic = 4,
            invokeVirtual = 5,
            invokeStatic = 6,
            invokeSpecial = 7,
            newInvokeSpecial = 8,
            invokeInterface = 9
        }

        public enum ConstantType
        {
            Integer,
            Long,
            Float,
            Double,
            String,
            Class,
            MethodHandle,
            MethodType,
        }

        public abstract class ConstantPoolItem
        {
            public virtual void Resolve(ClassFile classFile, string[] utf8_cp)
            {
            }

            //lights tag 怀疑没用
            //internal virtual void Link(TypeWrapper thisType)
            //{
            //}

            public virtual ConstantType GetConstantType()
            {
                throw new InvalidOperationException();
            }

            public virtual void MarkLinkRequired()
            {
            }
        }

        public sealed class ConstantPoolItemClass : ConstantPoolItem
        {
            private ushort name_index;
            private string name;
            //private TypeWrapper typeWrapper;
            private static char[] invalidJava15Characters = { '.', ';' };

            internal ConstantPoolItemClass(BigEndianBinaryReader br)
            {
                name_index = br.ReadUInt16();
            }

            public override void Resolve(ClassFile classFile, string[] utf8_cp)
            {
                name = classFile.GetConstantPoolUtf8String(utf8_cp, name_index);
                if (name.Length > 0)
                {
                    // We don't enforce the strict class name rules in the static compiler, since HotSpot doesn't enforce *any* rules on
                    // class names for the system (and boot) class loader. We still need to enforce the 1.5 restrictions, because we
                    // rely on those invariants.
#if !STATIC_COMPILER
                    if (classFile.MajorVersion < 49)
                    {
                        char prev = name[0];
                        if (Char.IsLetter(prev) || prev == '$' || prev == '_' || prev == '[' || prev == '/')
                        {
                            int skip = 1;
                            int end = name.Length;
                            if (prev == '[')
                            {
                                if (!IsValidFieldSig(name))
                                {
                                    goto barf;
                                }
                                while (name[skip] == '[')
                                {
                                    skip++;
                                }
                                if (name.EndsWith(";"))
                                {
                                    end--;
                                }
                            }
                            for (int i = skip; i < end; i++)
                            {
                                char c = name[i];
                                if (!Char.IsLetterOrDigit(c) && c != '$' && c != '_' && (c != '/' || prev == '/'))
                                {
                                    goto barf;
                                }
                                prev = c;
                            }
                            name = name.Replace('/', '.');
                            return;
                        }
                    }
                    else
#endif
                    {
                        // since 1.5 the restrictions on class names have been greatly reduced
                        int end = name.Length;
                        if (name[0] == '[')
                        {
                            if (!IsValidFieldSig(name))
                            {
                                goto barf;
                            }
                            // the semicolon is only allowed at the end and IsValidFieldSig enforces this,
                            // but since invalidJava15Characters contains the semicolon, we decrement end
                            // to make the following check against invalidJava15Characters ignore the
                            // trailing semicolon.
                            if (name[end - 1] == ';')
                            {
                                end--;
                            }
                        }
                        if (name.IndexOfAny(invalidJava15Characters, 0, end) >= 0)
                        {
                            goto barf;
                        }
                        name = name.Replace('/', '.');
                        return;
                    }
                }
                barf:
                throw new Exception(string.Format("Invalid class name \"{0}\"", name));
            }

            //internal override void MarkLinkRequired()
            //{
            //    typeWrapper = VerifierTypeWrapper.Null;
            //}

            //internal override void Link(TypeWrapper thisType)
            //{
            //    if (typeWrapper == VerifierTypeWrapper.Null)
            //    {
            //        typeWrapper = ClassLoaderWrapper.LoadClassNoThrow(thisType.GetClassLoader(), name);
            //    }
            //}

            public string Name
            {
                get
                {
                    return name;
                }
            }

            //internal TypeWrapper GetClassType()
            //{
            //    return typeWrapper;
            //}

            public override ConstantType GetConstantType()
            {
                return ConstantType.Class;
            }
        }

        public sealed class ConstantPoolItemDouble : ConstantPoolItem
        {
            private double d;

            internal ConstantPoolItemDouble(BigEndianBinaryReader br)
            {
                d = br.ReadDouble();
            }

            public override ConstantType GetConstantType()
            {
                return ConstantType.Double;
            }

            internal double Value
            {
                get
                {
                    return d;
                }
            }
        }

        public abstract class ConstantPoolItemFMI : ConstantPoolItem
        {
            private ushort class_index;
            private ushort name_and_type_index;
            private ConstantPoolItemClass clazz;
            private string name;
            private string descriptor;

            internal ConstantPoolItemFMI(BigEndianBinaryReader br)
            {
                class_index = br.ReadUInt16();
                name_and_type_index = br.ReadUInt16();
            }

            public override void Resolve(ClassFile classFile, string[] utf8_cp)
            {
                ConstantPoolItemNameAndType name_and_type = (ConstantPoolItemNameAndType)classFile.GetConstantPoolItem(name_and_type_index);
                clazz = (ConstantPoolItemClass)classFile.GetConstantPoolItem(class_index);
                // if the constant pool items referred to were strings, GetConstantPoolItem returns null
                if (name_and_type == null || clazz == null)
                {
                    throw new Exception("Bad index in constant pool");
                }
                name = classFile.GetConstantPoolUtf8String(utf8_cp, name_and_type.name_index);
                descriptor = classFile.GetConstantPoolUtf8String(utf8_cp, name_and_type.descriptor_index);
                Validate(name, descriptor, classFile.MajorVersion);
                descriptor = descriptor.Replace('/', '.');
            }

            protected abstract void Validate(string name, string descriptor, int majorVersion);

            public override void MarkLinkRequired()
            {
                clazz.MarkLinkRequired();
            }

            //internal override void Link(TypeWrapper thisType)
            //{
            //    clazz.Link(thisType);
            //}

            public string Name
            {
                get
                {
                    return name;
                }
            }

            public string Signature
            {
                get
                {
                    return descriptor;
                }
            }

            public string Class
            {
                get
                {
                    return clazz.Name;
                }
            }

            //internal TypeWrapper GetClassType()
            //{
            //    return clazz.GetClassType();
            //}

            //internal abstract MemberWrapper GetMember();
        }

        public sealed class ConstantPoolItemFieldref : ConstantPoolItemFMI
        {
            //private FieldWrapper field;
            //private TypeWrapper fieldTypeWrapper;

            internal ConstantPoolItemFieldref(BigEndianBinaryReader br) : base(br)
            {
            }

            protected override void Validate(string name, string descriptor, int majorVersion)
            {
                if (!IsValidFieldSig(descriptor))
                {
                    throw new Exception(string.Format("Invalid field signature \"{0}\"", descriptor));
                }
                if (!IsValidFieldName(name, majorVersion))
                {
                    throw new Exception(string.Format("Invalid field name \"{0}\"", name));
                }
            }

            //internal TypeWrapper GetFieldType()
            //{
            //    return fieldTypeWrapper;
            //}

            //internal override void Link(TypeWrapper thisType)
            //{
            //    base.Link(thisType);
            //    lock (this)
            //    {
            //        if (fieldTypeWrapper != null)
            //        {
            //            return;
            //        }
            //    }
            //    FieldWrapper fw = null;
            //    TypeWrapper wrapper = GetClassType();
            //    if (wrapper == null)
            //    {
            //        return;
            //    }
            //    if (!wrapper.IsUnloadable)
            //    {
            //        fw = wrapper.GetFieldWrapper(Name, Signature);
            //        if (fw != null)
            //        {
            //            fw.Link();
            //        }
            //    }
            //    ClassLoaderWrapper classLoader = thisType.GetClassLoader();
            //    TypeWrapper fld = classLoader.FieldTypeWrapperFromSigNoThrow(this.Signature);
            //    lock (this)
            //    {
            //        if (fieldTypeWrapper == null)
            //        {
            //            fieldTypeWrapper = fld;
            //            field = fw;
            //        }
            //    }
            //}

            //internal FieldWrapper GetField()
            //{
            //    return field;
            //}

            //internal override MemberWrapper GetMember()
            //{
            //    return field;
            //}
        }

        public class ConstantPoolItemMI : ConstantPoolItemFMI
        {
            //private TypeWrapper[] argTypeWrappers;
            //private TypeWrapper retTypeWrapper;
            //protected MethodWrapper method;
            //protected MethodWrapper invokespecialMethod;

            internal ConstantPoolItemMI(BigEndianBinaryReader br) : base(br)
            {
            }

            protected override void Validate(string name, string descriptor, int majorVersion)
            {
                if (!IsValidMethodSig(descriptor))
                {
                    throw new Exception(string.Format("Method {0} has invalid signature {1}", name, descriptor));
                }
                if (!IsValidMethodName(name, majorVersion))
                {
                    if (!Equals(name, StringConstants.INIT))
                    {
                        throw new Exception(string.Format("Invalid method name \"{0}\"", name));
                    }
                    if (!descriptor.EndsWith("V"))
                    {
                        throw new Exception(string.Format("Method {0} has invalid signature {1}", name, descriptor));
                    }
                }
            }

            //internal override void Link(TypeWrapper thisType)
            //{
            //    base.Link(thisType);
            //    lock (this)
            //    {
            //        if (argTypeWrappers != null)
            //        {
            //            return;
            //        }
            //    }
            //    ClassLoaderWrapper classLoader = thisType.GetClassLoader();
            //    TypeWrapper[] args = classLoader.ArgTypeWrapperListFromSigNoThrow(this.Signature);
            //    TypeWrapper ret = classLoader.RetTypeWrapperFromSigNoThrow(this.Signature);
            //    lock (this)
            //    {
            //        if (argTypeWrappers == null)
            //        {
            //            argTypeWrappers = args;
            //            retTypeWrapper = ret;
            //        }
            //    }
            //}

            //internal TypeWrapper[] GetArgTypes()
            //{
            //    return argTypeWrappers;
            //}

            //internal TypeWrapper GetRetType()
            //{
            //    return retTypeWrapper;
            //}

            //internal MethodWrapper GetMethod()
            //{
            //    return method;
            //}

            //internal MethodWrapper GetMethodForInvokespecial()
            //{
            //    return invokespecialMethod != null ? invokespecialMethod : method;
            //}

            //internal override MemberWrapper GetMember()
            //{
            //    return method;
            //}
        }

        public sealed class ConstantPoolItemMethodref : ConstantPoolItemMI
        {
            internal ConstantPoolItemMethodref(BigEndianBinaryReader br) : base(br)
            {
            }

            //internal override void Link(TypeWrapper thisType)
            //{
            //    base.Link(thisType);
            //    TypeWrapper wrapper = GetClassType();
            //    if (wrapper != null && !wrapper.IsUnloadable)
            //    {
            //        method = wrapper.GetMethodWrapper(Name, Signature, !ReferenceEquals(Name, StringConstants.INIT));
            //        if (method != null)
            //        {
            //            method.Link();
            //        }
            //        if (Name != StringConstants.INIT &&
            //            (thisType.Modifiers & (Modifiers.Interface | Modifiers.Super)) == Modifiers.Super &&
            //            thisType != wrapper && thisType.IsSubTypeOf(wrapper))
            //        {
            //            invokespecialMethod = thisType.BaseTypeWrapper.GetMethodWrapper(Name, Signature, true);
            //            if (invokespecialMethod != null)
            //            {
            //                invokespecialMethod.Link();
            //            }
            //        }
            //    }
            //}
        }

        public sealed class ConstantPoolItemInterfaceMethodref : ConstantPoolItemMI
        {
            internal ConstantPoolItemInterfaceMethodref(BigEndianBinaryReader br) : base(br)
            {
            }

            //private static MethodWrapper GetInterfaceMethod(TypeWrapper wrapper, string name, string sig)
            //{
            //    MethodWrapper method = wrapper.GetMethodWrapper(name, sig, false);
            //    if (method != null)
            //    {
            //        return method;
            //    }
            //    TypeWrapper[] interfaces = wrapper.Interfaces;
            //    for (int i = 0; i < interfaces.Length; i++)
            //    {
            //        method = GetInterfaceMethod(interfaces[i], name, sig);
            //        if (method != null)
            //        {
            //            return method;
            //        }
            //    }
            //    return null;
            //}

            //internal override void Link(TypeWrapper thisType)
            //{
            //    base.Link(thisType);
            //    TypeWrapper wrapper = GetClassType();
            //    if (wrapper != null && !wrapper.IsUnloadable)
            //    {
            //        method = GetInterfaceMethod(wrapper, Name, Signature);
            //        if (method == null)
            //        {
            //            // NOTE vmspec 5.4.3.4 clearly states that an interfacemethod may also refer to a method in Object
            //            method = CoreClasses.java.lang.Object.Wrapper.GetMethodWrapper(Name, Signature, false);
            //        }
            //        if (method != null)
            //        {
            //            method.Link();
            //        }
            //    }
            //}
        }

        public sealed class ConstantPoolItemFloat : ConstantPoolItem
        {
            private float v;

            public ConstantPoolItemFloat(BigEndianBinaryReader br)
            {
                v = br.ReadSingle();
            }

            public override ConstantType GetConstantType()
            {
                return ConstantType.Float;
            }

            public float Value
            {
                get
                {
                    return v;
                }
            }
        }

        public sealed class ConstantPoolItemInteger : ConstantPoolItem
        {
            private int v;

            public ConstantPoolItemInteger(BigEndianBinaryReader br)
            {
                v = br.ReadInt32();
            }

            public override ConstantType GetConstantType()
            {
                return ConstantType.Integer;
            }

            public int Value
            {
                get
                {
                    return v;
                }
            }
        }

        public sealed class ConstantPoolItemLong : ConstantPoolItem
        {
            private long l;

            internal ConstantPoolItemLong(BigEndianBinaryReader br)
            {
                l = br.ReadInt64();
            }

            public override ConstantType GetConstantType()
            {
                return ConstantType.Long;
            }

            public long Value
            {
                get
                {
                    return l;
                }
            }
        }

        public sealed class ConstantPoolItemNameAndType : ConstantPoolItem
        {
            internal ushort name_index;
            internal ushort descriptor_index;

            internal ConstantPoolItemNameAndType(BigEndianBinaryReader br)
            {
                name_index = br.ReadUInt16();
                descriptor_index = br.ReadUInt16();
            }

            public override void Resolve(ClassFile classFile, string[] utf8_cp)
            {
                if (classFile.GetConstantPoolUtf8String(utf8_cp, name_index) == null
                    || classFile.GetConstantPoolUtf8String(utf8_cp, descriptor_index) == null)
                {
                    throw new Exception("Illegal constant pool index");
                }
            }
        }

        public sealed class ConstantPoolItemMethodHandle : ConstantPoolItem
        {
            private byte ref_kind;
            private ushort method_index;
            private ConstantPoolItemFMI cpi;

            internal ConstantPoolItemMethodHandle(BigEndianBinaryReader br)
            {
                ref_kind = br.ReadByte();
                method_index = br.ReadUInt16();
            }

            public override void Resolve(ClassFile classFile, string[] utf8_cp)
            {
                switch ((RefKind)ref_kind)
                {
                    case RefKind.getField:
                    case RefKind.getStatic:
                    case RefKind.putField:
                    case RefKind.putStatic:
                        cpi = classFile.GetConstantPoolItem(method_index) as ConstantPoolItemFieldref;
                        break;
                    case RefKind.invokeSpecial:
                    case RefKind.invokeVirtual:
                    case RefKind.invokeStatic:
                    case RefKind.newInvokeSpecial:
                        cpi = classFile.GetConstantPoolItem(method_index) as ConstantPoolItemMethodref;
                        break;
                    case RefKind.invokeInterface:
                        cpi = classFile.GetConstantPoolItem(method_index) as ConstantPoolItemInterfaceMethodref;
                        break;
                }
                if (cpi == null)
                {
                    throw new Exception("Invalid constant pool item MethodHandle");
                }
                if (Equals(cpi.Name, StringConstants.INIT) && Kind != RefKind.newInvokeSpecial)
                {
                    throw new Exception("Bad method name");
                }
            }

            public override void MarkLinkRequired()
            {
                cpi.MarkLinkRequired();
            }

            public string Class
            {
                get { return cpi.Class; }
            }

            public string Name
            {
                get { return cpi.Name; }
            }

            public string Signature
            {
                get { return cpi.Signature; }
            }

            public ConstantPoolItemFMI MemberConstantPoolItem
            {
                get { return cpi; }
            }

            public RefKind Kind
            {
                get { return (RefKind)ref_kind; }
            }

            //internal MemberWrapper Member
            //{
            //    get { return cpi.GetMember(); }
            //}

            //internal TypeWrapper GetClassType()
            //{
            //    return cpi.GetClassType();
            //}

            //internal override void Link(TypeWrapper thisType)
            //{
            //    cpi.Link(thisType);
            //}

            public override ConstantType GetConstantType()
            {
                return ConstantType.MethodHandle;
            }
        }

        public sealed class ConstantPoolItemMethodType : ConstantPoolItem
        {
            private ushort signature_index;
            private string descriptor;
            //private TypeWrapper[] argTypeWrappers;
            //private TypeWrapper retTypeWrapper;

            internal ConstantPoolItemMethodType(BigEndianBinaryReader br)
            {
                signature_index = br.ReadUInt16();
            }

            public override void Resolve(ClassFile classFile, string[] utf8_cp)
            {
                string descriptor = classFile.GetConstantPoolUtf8String(utf8_cp, signature_index);
                if (descriptor == null || !IsValidMethodSig(descriptor))
                {
                    throw new Exception("Invalid MethodType signature");
                }
                this.descriptor = descriptor.Replace('/', '.');
            }

            //internal override void Link(TypeWrapper thisType)
            //{
            //    lock (this)
            //    {
            //        if (argTypeWrappers != null)
            //        {
            //            return;
            //        }
            //    }
            //    ClassLoaderWrapper classLoader = thisType.GetClassLoader();
            //    TypeWrapper[] args = classLoader.ArgTypeWrapperListFromSigNoThrow(descriptor);
            //    TypeWrapper ret = classLoader.RetTypeWrapperFromSigNoThrow(descriptor);
            //    lock (this)
            //    {
            //        if (argTypeWrappers == null)
            //        {
            //            argTypeWrappers = args;
            //            retTypeWrapper = ret;
            //        }
            //    }
            //}

            //internal TypeWrapper[] GetArgTypes()
            //{
            //    return argTypeWrappers;
            //}

            //internal TypeWrapper GetRetType()
            //{
            //    return retTypeWrapper;
            //}

            public override ConstantType GetConstantType()
            {
                return ConstantType.MethodType;
            }
        }

        public sealed class ConstantPoolItemInvokeDynamic : ConstantPoolItem
        {
            private ushort bootstrap_specifier_index;
            private ushort name_and_type_index;
            private string name;
            private string descriptor;
            //private TypeWrapper[] argTypeWrappers;
            //private TypeWrapper retTypeWrapper;

            internal ConstantPoolItemInvokeDynamic(BigEndianBinaryReader br)
            {
                bootstrap_specifier_index = br.ReadUInt16();
                name_and_type_index = br.ReadUInt16();
            }

            public override void Resolve(ClassFile classFile, string[] utf8_cp)
            {
                ConstantPoolItemNameAndType name_and_type = (ConstantPoolItemNameAndType)classFile.GetConstantPoolItem(name_and_type_index);
                // if the constant pool items referred to were strings, GetConstantPoolItem returns null
                if (name_and_type == null)
                {
                    throw new Exception("Bad index in constant pool");
                }
                name = classFile.GetConstantPoolUtf8String(utf8_cp, name_and_type.name_index);
                descriptor = classFile.GetConstantPoolUtf8String(utf8_cp, name_and_type.descriptor_index).Replace('/', '.');
            }

            //internal override void Link(TypeWrapper thisType)
            //{
            //    lock (this)
            //    {
            //        if (argTypeWrappers != null)
            //        {
            //            return;
            //        }
            //    }
            //    ClassLoaderWrapper classLoader = thisType.GetClassLoader();
            //    TypeWrapper[] args = classLoader.ArgTypeWrapperListFromSigNoThrow(descriptor);
            //    TypeWrapper ret = classLoader.RetTypeWrapperFromSigNoThrow(descriptor);
            //    lock (this)
            //    {
            //        if (argTypeWrappers == null)
            //        {
            //            argTypeWrappers = args;
            //            retTypeWrapper = ret;
            //        }
            //    }
            //}

            //internal TypeWrapper[] GetArgTypes()
            //{
            //    return argTypeWrappers;
            //}

            //internal TypeWrapper GetRetType()
            //{
            //    return retTypeWrapper;
            //}

            public string Name
            {
                get { return name; }
            }

            public ushort BootstrapMethod
            {
                get { return bootstrap_specifier_index; }
            }
        }

        public sealed class ConstantPoolItemString : ConstantPoolItem
        {
            private ushort string_index;
            private string s;

            internal ConstantPoolItemString(BigEndianBinaryReader br)
            {
                string_index = br.ReadUInt16();
            }

            public override void Resolve(ClassFile classFile, string[] utf8_cp)
            {
                s = classFile.GetConstantPoolUtf8String(utf8_cp, string_index);
            }

            public override ConstantType GetConstantType()
            {
                return ConstantType.String;
            }

            public string Value
            {
                get
                {
                    return s;
                }
            }
        }

        public enum Constant
        {
            Utf8 = 1,
            Integer = 3,
            Float = 4,
            Long = 5,
            Double = 6,
            Class = 7,
            String = 8,
            Fieldref = 9,
            Methodref = 10,
            InterfaceMethodref = 11,
            NameAndType = 12,
            MethodHandle = 15,
            MethodType = 16,
            InvokeDynamic = 18,
        }

        public abstract class FieldOrMethod
        {
            // Note that Modifiers is a ushort, so it combines nicely with the following ushort field
            protected Modifiers access_flags;
            protected ushort flags;
            private string name;
            private string descriptor;
            protected string signature;
            protected object[] annotations;

            public FieldOrMethod(ClassFile classFile, string[] utf8_cp, BigEndianBinaryReader br)
            {
                access_flags = (Modifiers)br.ReadUInt16();
                name = classFile.GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16());
                descriptor = classFile.GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16());
                ValidateSig(classFile, descriptor);
                descriptor = descriptor.Replace('/', '.');
            }

            protected abstract void ValidateSig(ClassFile classFile, string descriptor);

            public string Name
            {
                get
                {
                    return name;
                }
            }

            public string Signature
            {
                get
                {
                    return descriptor;
                }
            }

            public object[] Annotations
            {
                get
                {
                    return annotations;
                }
            }

            public string GenericSignature
            {
                get
                {
                    return signature;
                }
            }

            public Modifiers Modifiers
            {
                get
                {
                    return (Modifiers)access_flags;
                }
            }

            public bool IsAbstract
            {
                get
                {
                    return (access_flags & Modifiers.Abstract) != 0;
                }
            }

            public bool IsFinal
            {
                get
                {
                    return (access_flags & Modifiers.Final) != 0;
                }
            }

            public bool IsPublic
            {
                get
                {
                    return (access_flags & Modifiers.Public) != 0;
                }
            }

            public bool IsPrivate
            {
                get
                {
                    return (access_flags & Modifiers.Private) != 0;
                }
            }

            public bool IsProtected
            {
                get
                {
                    return (access_flags & Modifiers.Protected) != 0;
                }
            }

            public bool IsStatic
            {
                get
                {
                    return (access_flags & Modifiers.Static) != 0;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return (access_flags & Modifiers.Synchronized) != 0;
                }
            }

            public bool IsVolatile
            {
                get
                {
                    return (access_flags & Modifiers.Volatile) != 0;
                }
            }

            public bool IsTransient
            {
                get
                {
                    return (access_flags & Modifiers.Transient) != 0;
                }
            }

            public bool IsNative
            {
                get
                {
                    return (access_flags & Modifiers.Native) != 0;
                }
            }

            public bool IsEnum
            {
                get
                {
                    return (access_flags & Modifiers.Enum) != 0;
                }
            }

            public bool DeprecatedAttribute
            {
                get
                {
                    return (flags & FLAG_MASK_DEPRECATED) != 0;
                }
            }

            public bool IsInternal
            {
                get
                {
                    return (flags & FLAG_MASK_INTERNAL) != 0;
                }
            }
        }

        public sealed class Field : FieldOrMethod
        {
            private object constantValue;
            private string[] propertyGetterSetter;

            public Field(ClassFile classFile, string[] utf8_cp, BigEndianBinaryReader br) : base(classFile, utf8_cp, br)
            {
                if ((IsPrivate && IsPublic) || (IsPrivate && IsProtected) || (IsPublic && IsProtected)
                    || (IsFinal && IsVolatile)
                    || (classFile.IsInterface && (!IsPublic || !IsStatic || !IsFinal || IsTransient)))
                {
                    throw new Exception(string.Format("{0} (Illegal field modifiers: 0x{1:X})", classFile.Name, access_flags));
                }
                int attributes_count = br.ReadUInt16();
                for (int i = 0; i < attributes_count; i++)
                {
                    switch (classFile.GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16()))
                    {
                        case "Deprecated":
                            if (br.ReadUInt32() != 0)
                            {
                                throw new Exception("Invalid Deprecated attribute length");
                            }
                            flags |= FLAG_MASK_DEPRECATED;
                            break;
                        case "ConstantValue":
                            {
                                if (br.ReadUInt32() != 2)
                                {
                                    throw new Exception("Invalid ConstantValue attribute length");
                                }
                                ushort index = br.ReadUInt16();
                                switch (Signature)
                                {
                                    case "I":
                                        constantValue = classFile.GetConstantPoolConstantInteger(index);
                                        break;
                                    case "S":
                                        constantValue = (short)classFile.GetConstantPoolConstantInteger(index);
                                        break;
                                    case "B":
                                        constantValue = (byte)classFile.GetConstantPoolConstantInteger(index);
                                        break;
                                    case "C":
                                        constantValue = (char)classFile.GetConstantPoolConstantInteger(index);
                                        break;
                                    case "Z":
                                        constantValue = classFile.GetConstantPoolConstantInteger(index) != 0;
                                        break;
                                    case "J":
                                        constantValue = classFile.GetConstantPoolConstantLong(index);
                                        break;
                                    case "F":
                                        constantValue = classFile.GetConstantPoolConstantFloat(index);
                                        break;
                                    case "D":
                                        constantValue = classFile.GetConstantPoolConstantDouble(index);
                                        break;
                                    case "Ljava.lang.String;":
                                        constantValue = classFile.GetConstantPoolConstantString(index);
                                        break;
                                    default:
                                        throw new Exception(string.Format("{0} (Invalid signature for constant)", classFile.Name));
                                }

                                break;
                            }
                        case "Signature":
                            if (classFile.MajorVersion < 49)
                            {
                                goto default;
                            }
                            if (br.ReadUInt32() != 2)
                            {
                                throw new Exception("Signature attribute has incorrect length");
                            }
                            signature = classFile.GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16());
                            break;
                        case "RuntimeVisibleAnnotations":
                            if (classFile.MajorVersion < 49)
                            {
                                goto default;
                            }
                            annotations = ReadAnnotations(br, classFile, utf8_cp);
                            break;
                        case "RuntimeInvisibleAnnotations":
                            if (classFile.MajorVersion < 49)
                            {
                                goto default;
                            }
                            foreach (object[] annot in ReadAnnotations(br, classFile, utf8_cp))
                            {
                                if (annot[1].Equals("Likvm/lang/Property;"))
                                {
                                    DecodePropertyAnnotation(classFile, annot);
                                }
#if STATIC_COMPILER
								else if(annot[1].Equals("Likvm/lang/Internal;"))
								{
									this.access_flags &= ~Modifiers.AccessMask;
									flags |= FLAG_MASK_INTERNAL;
								}
#endif
                            }
                            break;
                        default:
                            br.Skip(br.ReadUInt32());
                            break;
                    }
                }
            }

            private void DecodePropertyAnnotation(ClassFile classFile, object[] annot)
            {
                if (propertyGetterSetter != null)
                {
                    //lights tag
                    //Tracer.Error(Tracer.ClassLoading, "Ignoring duplicate ikvm.lang.Property annotation on {0}.{1}", classFile.Name, this.Name);
                    return;
                }
                propertyGetterSetter = new string[2];
                for (int i = 2; i < annot.Length - 1; i += 2)
                {
                    string value = annot[i + 1] as string;
                    if (value == null)
                    {
                        propertyGetterSetter = null;
                        break;
                    }
                    if (annot[i].Equals("get") && propertyGetterSetter[0] == null)
                    {
                        propertyGetterSetter[0] = value;
                    }
                    else if (annot[i].Equals("set") && propertyGetterSetter[1] == null)
                    {
                        propertyGetterSetter[1] = value;
                    }
                    else
                    {
                        propertyGetterSetter = null;
                        break;
                    }
                }
                if (propertyGetterSetter == null || propertyGetterSetter[0] == null)
                {
                    propertyGetterSetter = null;
                    //lights tag
                    //                    Tracer.Error(Tracer.ClassLoading, "Ignoring malformed ikvm.lang.Property annotation on {0}.{1}", classFile.Name, this.Name);
                    return;
                }
            }

            protected override void ValidateSig(ClassFile classFile, string descriptor)
            {
                if (!IsValidFieldSig(descriptor))
                {
                    throw new Exception(string.Format("{0} (Field \"{1}\" has invalid signature \"{2}\")", classFile.Name, this.Name, descriptor));
                }
            }

            public object ConstantValue
            {
                get
                {
                    return constantValue;
                }
            }

            public bool IsStaticFinalConstant
            {
                get { return (access_flags & (Modifiers.Final | Modifiers.Static)) == (Modifiers.Final | Modifiers.Static) && constantValue != null; }
            }

            public bool IsProperty
            {
                get
                {
                    return propertyGetterSetter != null;
                }
            }

            public string PropertyGetter
            {
                get
                {
                    return propertyGetterSetter[0];
                }
            }

            public string PropertySetter
            {
                get
                {
                    return propertyGetterSetter[1];
                }
            }
        }

        public sealed class Method : FieldOrMethod
        {
            private Code code;
            private string[] exceptions;
            private LowFreqData low;

            public sealed class LowFreqData
            {
                public object annotationDefault;
                public object[][] parameterAnnotations;
#if !STATIC_COMPILER
                public string DllExportName;
                public int DllExportOrdinal;
#endif
            }

            public Method(ClassFile classFile, string[] utf8_cp, BigEndianBinaryReader br) : base(classFile, utf8_cp, br)
            {
                // vmspec 4.6 says that all flags, except ACC_STRICT are ignored on <clinit>
                // however, since Java 7 it does need to be marked static
                if (Equals(Name, StringConstants.CLINIT) && Equals(Signature, StringConstants.SIG_VOID) && (classFile.MajorVersion < 51 || IsStatic))
                {
                    access_flags &= Modifiers.Strictfp;
                    access_flags |= (Modifiers.Static | Modifiers.Private);
                }
                else
                {
                    // LAMESPEC: vmspec 4.6 says that abstract methods can not be strictfp (and this makes sense), but
                    // javac (pre 1.5) is broken and marks abstract methods as strictfp (if you put the strictfp on the class)
                    if ((Equals(Name, StringConstants.INIT) && (IsStatic || IsSynchronized || IsFinal || IsAbstract || IsNative))
                        || (IsPrivate && IsPublic) || (IsPrivate && IsProtected) || (IsPublic && IsProtected)
                        || (IsAbstract && (IsFinal || IsNative || IsPrivate || IsStatic || IsSynchronized))
                        || (classFile.IsInterface && (!IsPublic || !IsAbstract)))
                    {
                        throw new Exception(string.Format("{0} (Illegal method modifiers: 0x{1:X})", classFile.Name, access_flags));
                    }
                }
                int attributes_count = br.ReadUInt16();
                for (int i = 0; i < attributes_count; i++)
                {
                    var id = br.ReadUInt16();
                    var sid = classFile.GetConstantPoolUtf8String(utf8_cp, id);
                    switch (sid)
                    {
                        case "Deprecated":
                            if (br.ReadUInt32() != 0)
                            {
                                throw new Exception("Invalid Deprecated attribute length");
                            }
                            flags |= FLAG_MASK_DEPRECATED;
                            break;
                        case "Code":
                            {
                                if (!code.IsEmpty)
                                {
                                    throw new Exception(string.Format("{0} (Duplicate Code attribute)", classFile.Name));
                                }
                                BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
                                code.Read(classFile, utf8_cp, this, rdr);
                                if (!rdr.IsAtEnd)
                                {
                                    throw new Exception(string.Format("{0} (Code attribute has wrong length)", classFile.Name));
                                }
                                break;
                            }
                        case "Exceptions":
                            {
                                if (exceptions != null)
                                {
                                    throw new Exception(string.Format("{0} (Duplicate Exceptions attribute)", classFile.Name));
                                }
                                BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
                                ushort count = rdr.ReadUInt16();
                                exceptions = new string[count];
                                for (int j = 0; j < count; j++)
                                {
                                    exceptions[j] = classFile.GetConstantPoolClass(rdr.ReadUInt16());
                                }
                                if (!rdr.IsAtEnd)
                                {
                                    throw new Exception(string.Format("{0} (Exceptions attribute has wrong length)", classFile.Name));
                                }
                                break;
                            }
                        case "Signature":
                            if (classFile.MajorVersion < 49)
                            {
                                goto default;
                            }
                            if (br.ReadUInt32() != 2)
                            {
                                throw new Exception("Signature attribute has incorrect length");
                            }
                            signature = classFile.GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16());
                            break;
                        case "RuntimeVisibleAnnotations":
                            if (classFile.MajorVersion < 49)
                            {
                                goto default;
                            }
                            annotations = ReadAnnotations(br, classFile, utf8_cp);
                            break;
                        case "RuntimeInvisibleAnnotations":
                            {
                                if (classFile.MajorVersion < 49)
                                {
                                    goto default;
                                }
                            }
                            annotations = ReadAnnotations(br, classFile, utf8_cp);
                            break;
                        case "RuntimeVisibleParameterAnnotations":
                            {
                                if (classFile.MajorVersion < 49)
                                {
                                    goto default;
                                }
                                if (low == null)
                                {
                                    low = new LowFreqData();
                                }
                                BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
                                byte num_parameters = rdr.ReadByte();
                                low.parameterAnnotations = new object[num_parameters][];
                                for (int j = 0; j < num_parameters; j++)
                                {
                                    ushort num_annotations = rdr.ReadUInt16();
                                    low.parameterAnnotations[j] = new object[num_annotations];
                                    for (int k = 0; k < num_annotations; k++)
                                    {
                                        low.parameterAnnotations[j][k] = ReadAnnotation(rdr, classFile, utf8_cp);
                                    }
                                }
                                if (!rdr.IsAtEnd)
                                {
                                    throw new Exception(string.Format("{0} (RuntimeVisibleParameterAnnotations attribute has wrong length)", classFile.Name));
                                }
                                break;
                            }
                        case "AnnotationDefault":
                            {
                                if (classFile.MajorVersion < 49)
                                {
                                    goto default;
                                }
                                if (low == null)
                                {
                                    low = new LowFreqData();
                                }
                                BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
                                low.annotationDefault = ReadAnnotationElementValue(rdr, classFile, utf8_cp);
                                if (!rdr.IsAtEnd)
                                {
                                    throw new Exception(string.Format("{0} (AnnotationDefault attribute has wrong length)", classFile.Name));
                                }
                                break;
                            }
#if STATIC_COMPILER
						case "RuntimeInvisibleAnnotations":
							if(classFile.MajorVersion < 49)
							{
								goto default;
							}
							foreach(object[] annot in ReadAnnotations(br, classFile, utf8_cp))
							{
								if(annot[1].Equals("Likvm/lang/Internal;"))
								{
									if (classFile.IsInterface)
									{
										StaticCompiler.IssueMessage(Message.InterfaceMethodCantBeInternal, classFile.Name, this.Name, this.Signature);
									}
									else
									{
										this.access_flags &= ~Modifiers.AccessMask;
										flags |= FLAG_MASK_INTERNAL;
									}
								}
								if(annot[1].Equals("Likvm/internal/HasCallerID;"))
								{
									flags |= FLAG_HAS_CALLERID;
								}
								if(annot[1].Equals("Likvm/lang/DllExport;"))
								{
									string name = null;
									int? ordinal = null;
									for (int j = 2; j < annot.Length; j += 2)
									{
										if (annot[j].Equals("name") && annot[j + 1] is string)
										{
											name = (string)annot[j + 1];
										}
										else if (annot[j].Equals("ordinal") && annot[j + 1] is int)
										{
											ordinal = (int)annot[j + 1];
										}
									}
									if (name != null && ordinal != null)
									{
										if (!IsStatic)
										{
											StaticCompiler.IssueMessage(Message.DllExportMustBeStaticMethod, classFile.Name, this.Name, this.Signature);
										}
										else
										{
											if (low == null)
											{
												low = new LowFreqData();
											}
											low.DllExportName = name;
											low.DllExportOrdinal = ordinal.Value;
										}
									}
								}
							}
							break;
#endif
                        default:
                            br.Skip(br.ReadUInt32());
                            break;
                    }
                }
                if (IsAbstract || IsNative)
                {
                    if (!code.IsEmpty)
                    {
                        throw new Exception("Abstract or native method cannot have a Code attribute");
                    }
                }
                else
                {
                    if (code.IsEmpty)
                    {
                        if (Equals(this.Name, StringConstants.CLINIT))
                        {
                            code.verifyError = string.Format(string.Format("Class {0}, method {1} signature {2}: No Code attribute", classFile.Name, this.Name, this.Signature));
                            return;
                        }
                        throw new Exception("Method has no Code attribute");
                    }
                }
            }

            protected override void ValidateSig(ClassFile classFile, string descriptor)
            {
                if (!IsValidMethodSig(descriptor))
                {
                    throw new Exception(string.Format("{0} (Method \"{1}\" has invalid signature \"{2}\")", classFile.Name, this.Name, descriptor));
                }
            }

            public bool IsStrictfp
            {
                get
                {
                    return (access_flags & Modifiers.Strictfp) != 0;
                }
            }

            // Is this the <clinit>()V method?
            public bool IsClassInitializer
            {
                get
                {
                    return Equals(Name, StringConstants.CLINIT) && Equals(Signature, StringConstants.SIG_VOID) && IsStatic;
                }
            }

            public bool IsConstructor
            {
                get
                {
                    return Equals(Name, StringConstants.INIT);
                }
            }

            // for use by ikvmc only
            public bool HasCallerIDAnnotation
            {
                get
                {
                    return (flags & FLAG_HAS_CALLERID) != 0;
                }
            }

            public string[] ExceptionsAttribute
            {
                get
                {
                    return exceptions;
                }
            }

            public object[][] ParameterAnnotations
            {
                get
                {
                    return low == null ? null : low.parameterAnnotations;
                }
            }

            public object AnnotationDefault
            {
                get
                {
                    return low == null ? null : low.annotationDefault;
                }
            }

            public string VerifyError
            {
                get
                {
                    return code.verifyError;
                }
            }

            // maps argument 'slot' (as encoded in the xload/xstore instructions) into the ordinal
            public int[] ArgMap
            {
                get
                {
                    return code.argmap;
                }
            }

            public int MaxStack
            {
                get
                {
                    return code.max_stack;
                }
            }

            public int MaxLocals
            {
                get
                {
                    return code.max_locals;
                }
            }

            public Instruction[] Instructions
            {
                get
                {
                    return code.instructions;
                }
                set
                {
                    code.instructions = value;
                }
            }

            public ExceptionTableEntry[] ExceptionTable
            {
                get
                {
                    return code.exception_table;
                }
                set
                {
                    code.exception_table = value;
                }
            }

            public Dictionary<int,int> LineNumberTableAttribute
            {
                get
                {
                    return code.lineNumberTable;
                }
            }

            public LocalVariableTableEntry[] LocalVariableTableAttribute
            {
                get
                {
                    return code.localVariableTable;
                }
            }

            public bool HasJsr
            {
                get
                {
                    return code.hasJsr;
                }
            }

            public struct Code
            {
                internal bool hasJsr;
                internal string verifyError;
                internal ushort max_stack;
                internal ushort max_locals;
                internal Instruction[] instructions;
                internal ExceptionTableEntry[] exception_table;
                internal int[] argmap;
                public Dictionary<int, int> lineNumberTable;
                internal LocalVariableTableEntry[] localVariableTable;

                internal void Read(ClassFile classFile, string[] utf8_cp, Method method, BigEndianBinaryReader br)
                {
                    max_stack = br.ReadUInt16();
                    max_locals = br.ReadUInt16();
                    uint code_length = br.ReadUInt32();
                    if (code_length > 65535)
                    {
                        throw new Exception(string.Format("{0} (Invalid Code length {1})", classFile.Name, code_length));
                    }
                    Instruction[] instructions = new Instruction[code_length + 1];
                    int basePosition = br.Position;
                    int instructionIndex = 0;
                    try
                    {
                        BigEndianBinaryReader rdr = br.Section(code_length);
                        while (!rdr.IsAtEnd)
                        {
                            instructions[instructionIndex].Read((ushort)(rdr.Position - basePosition), rdr, classFile);
                            hasJsr |= instructions[instructionIndex].NormalizedOpCode == NormalizedByteCode.__jsr;
                            instructionIndex++;
                        }
                        // we add an additional nop instruction to make it easier for consumers of the code array
                        instructions[instructionIndex++].SetTermNop((ushort)(rdr.Position - basePosition));
                    }
                    catch (Exception x)
                    {
                        // any class format errors in the code block are actually verify errors
                        verifyError = x.Message;
                    }
                    this.instructions = new Instruction[instructionIndex];
                    Array.Copy(instructions, 0, this.instructions, 0, instructionIndex);
                    // build the pcIndexMap
                    int[] pcIndexMap = new int[this.instructions[instructionIndex - 1].PC + 1];
                    for (int i = 0; i < pcIndexMap.Length; i++)
                    {
                        pcIndexMap[i] = -1;
                    }
                    for (int i = 0; i < instructionIndex - 1; i++)
                    {
                        pcIndexMap[this.instructions[i].PC] = i;
                    }
                    // convert branch offsets to indexes
                    for (int i = 0; i < instructionIndex - 1; i++)
                    {
                        switch (this.instructions[i].NormalizedOpCode)
                        {
                            case NormalizedByteCode.__ifeq:
                            case NormalizedByteCode.__ifne:
                            case NormalizedByteCode.__iflt:
                            case NormalizedByteCode.__ifge:
                            case NormalizedByteCode.__ifgt:
                            case NormalizedByteCode.__ifle:
                            case NormalizedByteCode.__if_icmpeq:
                            case NormalizedByteCode.__if_icmpne:
                            case NormalizedByteCode.__if_icmplt:
                            case NormalizedByteCode.__if_icmpge:
                            case NormalizedByteCode.__if_icmpgt:
                            case NormalizedByteCode.__if_icmple:
                            case NormalizedByteCode.__if_acmpeq:
                            case NormalizedByteCode.__if_acmpne:
                            case NormalizedByteCode.__ifnull:
                            case NormalizedByteCode.__ifnonnull:
                            case NormalizedByteCode.__goto:
                            case NormalizedByteCode.__jsr:
                                //this.instructions[i].SetTargetIndex(pcIndexMap[this.instructions[i].Arg1 + this.instructions[i].PC]);
                                break;
                            case NormalizedByteCode.__tableswitch:
                            case NormalizedByteCode.__lookupswitch:
                                this.instructions[i].MapSwitchTargets(pcIndexMap);
                                break;
                        }
                    }
                    // read exception table
                    ushort exception_table_length = br.ReadUInt16();
                    exception_table = new ExceptionTableEntry[exception_table_length];
                    for (int i = 0; i < exception_table_length; i++)
                    {
                        ushort start_pc = br.ReadUInt16();
                        ushort end_pc = br.ReadUInt16();
                        ushort handler_pc = br.ReadUInt16();
                        ushort catch_type = br.ReadUInt16();
                        if (start_pc >= end_pc
                            || end_pc > code_length
                            || handler_pc >= code_length
                            || (catch_type != 0 && !classFile.SafeIsConstantPoolClass(catch_type)))
                        {
                            throw new Exception(string.Format("Illegal exception table: {0}.{1}{2}", classFile.Name, method.Name, method.Signature));
                        }
                        classFile.MarkLinkRequiredConstantPoolItem(catch_type);
                        // if start_pc, end_pc or handler_pc is invalid (i.e. doesn't point to the start of an instruction),
                        // the index will be -1 and this will be handled by the verifier
                        int startIndex = pcIndexMap[start_pc];
                        int endIndex;
                        if (end_pc == code_length)
                        {
                            // it is legal for end_pc to point to just after the last instruction,
                            // but since there isn't an entry in our pcIndexMap for that, we have
                            // a special case for this
                            endIndex = instructionIndex - 1;
                        }
                        else
                        {
                            endIndex = pcIndexMap[end_pc];
                        }
                        int handlerIndex = pcIndexMap[handler_pc];
                        exception_table[i] = new ExceptionTableEntry(startIndex, endIndex, handlerIndex, catch_type, i);
                    }
                    ushort attributes_count = br.ReadUInt16();
                    for (int i = 0; i < attributes_count; i++)
                    {
                        switch (classFile.GetConstantPoolUtf8String(utf8_cp, br.ReadUInt16()))
                        {
                            case "LineNumberTable":
                                //if ((options & ClassFileParseOptions.LineNumberTable) != 0)
                                {
                                    BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
                                    int count = rdr.ReadUInt16();
                                    lineNumberTable = new Dictionary<int, int>();
                                    for (int j = 0; j < count; j++)
                                    {
                                        var start_pc = rdr.ReadUInt16();
                                        var line_number = rdr.ReadUInt16();
                                        lineNumberTable[start_pc] = line_number;

                                        if (start_pc >= code_length)
                                        {
                                            throw new Exception(classFile.Name + " (LineNumberTable has invalid pc)");
                                        }
                                    }
                                    if (!rdr.IsAtEnd)
                                    {
                                        throw new Exception(classFile.Name + " (LineNumberTable attribute has wrong length)");
                                    }
                                }
                                //else
                                //{
                                //    br.Skip(br.ReadUInt32());
                                //}
                                break;
                            case "LocalVariableTable":
                                //if ((options & ClassFileParseOptions.LocalVariableTable) != 0)
                                {
                                    BigEndianBinaryReader rdr = br.Section(br.ReadUInt32());
                                    int count = rdr.ReadUInt16();
                                    localVariableTable = new LocalVariableTableEntry[count];
                                    for (int j = 0; j < count; j++)
                                    {
                                        localVariableTable[j].start_pc = rdr.ReadUInt16();
                                        localVariableTable[j].length = rdr.ReadUInt16();
                                        localVariableTable[j].name = classFile.GetConstantPoolUtf8String(utf8_cp, rdr.ReadUInt16());
                                        localVariableTable[j].descriptor = classFile.GetConstantPoolUtf8String(utf8_cp, rdr.ReadUInt16()).Replace('/', '.');
                                        localVariableTable[j].index = rdr.ReadUInt16();
                                    }
                                    // NOTE we're intentionally not checking that we're at the end of the section
                                    // (optional attributes shouldn't cause ClassFormatError)
                                }
                                //else
                                //{
                                //    br.Skip(br.ReadUInt32());
                                //}
                                break;
                            default:
                                br.Skip(br.ReadUInt32());
                                break;
                        }
                    }
                    // build the argmap
                    string sig = method.Signature;
                    List<int> args = new List<int>();
                    int pos = 0;
                    if (!method.IsStatic)
                    {
                        args.Add(pos++);
                    }
                    for (int i = 1; sig[i] != ')'; i++)
                    {
                        args.Add(pos++);
                        switch (sig[i])
                        {
                            case 'L':
                                i = sig.IndexOf(';', i);
                                break;
                            case 'D':
                            case 'J':
                                args.Add(-1);
                                break;
                            case '[':
                                {
                                    while (sig[i] == '[')
                                    {
                                        i++;
                                    }
                                    if (sig[i] == 'L')
                                    {
                                        i = sig.IndexOf(';', i);
                                    }
                                    break;
                                }
                        }
                    }
                    argmap = args.ToArray();
                    if (args.Count > max_locals)
                    {
                        throw new Exception(classFile.Name + " (Arguments can't fit into locals)");
                    }
                }

                internal bool IsEmpty
                {
                    get
                    {
                        return instructions == null;
                    }
                }
            }

            public sealed class ExceptionTableEntry
            {
                internal readonly int startIndex;
                internal readonly int endIndex;
                internal readonly int handlerIndex;
                internal readonly ushort catch_type;
                internal readonly int ordinal;
                internal readonly bool isFinally;

                internal ExceptionTableEntry(int startIndex, int endIndex, int handlerIndex, ushort catch_type, int ordinal)
                    : this(startIndex, endIndex, handlerIndex, catch_type, ordinal, false)
                {
                }

                internal ExceptionTableEntry(int startIndex, int endIndex, int handlerIndex, ushort catch_type, int ordinal, bool isFinally)
                {
                    this.startIndex = startIndex;
                    this.endIndex = endIndex;
                    this.handlerIndex = handlerIndex;
                    this.catch_type = catch_type;
                    this.ordinal = ordinal;
                    this.isFinally = isFinally;
                }
            }

            [Flags]
            public enum InstructionFlags : byte
            {
                Reachable = 1,
                Processed = 2,
                BranchTarget = 4,
            }

            public struct Instruction
            {
                public override string ToString()
                {
                    return normopcode.ToString() + ":" + arg1 + "," + arg2;
                }
                private ushort pc;
                private NormalizedByteCode normopcode;
                private int arg1;
                private short arg2;
                private SwitchEntry[] switch_entries;

                public struct SwitchEntry
                {
                    internal int value;
                    internal int target;
                }

                private void SetHardError(HardError error, int messageId)
                {
                    normopcode = NormalizedByteCode.__static_error;
                    arg2 = (short)error;
                    arg1 = messageId;
                }

                private HardError HardError
                {
                    get
                    {
                        return (HardError)arg2;
                    }
                }

                public int HandlerIndex
                {
                    get { return (ushort)arg2; }
                }

                public int HardErrorMessageId
                {
                    get
                    {
                        return arg1;
                    }
                }

                public void PatchOpCode(NormalizedByteCode bc)
                {
                    this.normopcode = bc;
                }

                public void PatchOpCode(NormalizedByteCode bc, int arg1)
                {
                    this.normopcode = bc;
                    this.arg1 = arg1;
                }

                public void PatchOpCode(NormalizedByteCode bc, int arg1, short arg2)
                {
                    this.normopcode = bc;
                    this.arg1 = arg1;
                    this.arg2 = arg2;
                }

                public void SetPC(int pc)
                {
                    this.pc = (ushort)pc;
                }

                public void SetTargetIndex(int targetIndex)
                {
                    this.arg1 = targetIndex;
                }

                public void SetTermNop(ushort pc)
                {
                    // TODO what happens if we already have exactly the maximum number of instructions?
                    this.pc = pc;
                    this.normopcode = NormalizedByteCode.__nop;
                }

                public void MapSwitchTargets(int[] pcIndexMap)
                {
                    arg1 = pcIndexMap[arg1 + pc];
                    for (int i = 0; i < switch_entries.Length; i++)
                    {
                        switch_entries[i].target = pcIndexMap[switch_entries[i].target + pc];
                    }
                }

                public void Read(ushort pc, BigEndianBinaryReader br, ClassFile classFile)
                {
                    this.pc = pc;
                    ByteCode bc = (ByteCode)br.ReadByte();
                    switch (ByteCodeMetaData.GetMode(bc))
                    {
                        case ByteCodeMode.Simple:
                            break;
                        case ByteCodeMode.Constant_1:
                            arg1 = br.ReadByte();
                            classFile.MarkLinkRequiredConstantPoolItem(arg1);
                            break;
                        case ByteCodeMode.Local_1:
                            arg1 = br.ReadByte();
                            break;
                        case ByteCodeMode.Constant_2:
                            arg1 = br.ReadUInt16();
                            classFile.MarkLinkRequiredConstantPoolItem(arg1);
                            break;
                        case ByteCodeMode.Branch_2:
                            {
                                arg1 = br.ReadInt16();
                            }
                            break;
                        case ByteCodeMode.Branch_4:
                            arg1 = br.ReadInt32();
                            break;
                        case ByteCodeMode.Constant_2_1_1:
                            arg1 = br.ReadUInt16();
                            classFile.MarkLinkRequiredConstantPoolItem(arg1);
                            arg2 = br.ReadByte();
                            if (br.ReadByte() != 0)
                            {
                                throw new Exception("invokeinterface filler must be zero");
                            }
                            break;
                        case ByteCodeMode.Immediate_1:
                            arg1 = br.ReadSByte();
                            break;
                        case ByteCodeMode.Immediate_2:
                            arg1 = br.ReadInt16();
                            break;
                        case ByteCodeMode.Local_1_Immediate_1:
                            arg1 = br.ReadByte();
                            arg2 = br.ReadSByte();
                            break;
                        case ByteCodeMode.Constant_2_Immediate_1:
                            arg1 = br.ReadUInt16();
                            classFile.MarkLinkRequiredConstantPoolItem(arg1);
                            arg2 = br.ReadSByte();
                            break;
                        case ByteCodeMode.Tableswitch:
                            {
                                // skip the padding
                                uint p = pc + 1u;
                                uint align = ((p + 3) & 0x7ffffffc) - p;
                                br.Skip(align);
                                int default_offset = br.ReadInt32();
                                this.arg1 = default_offset;
                                int low = br.ReadInt32();
                                int high = br.ReadInt32();
                                if (low > high || high > 16384L + low)
                                {
                                    throw new Exception("Incorrect tableswitch");
                                }
                                SwitchEntry[] entries = new SwitchEntry[high - low + 1];
                                for (int i = low; i < high; i++)
                                {
                                    entries[i - low].value = i;
                                    entries[i - low].target = br.ReadInt32();
                                }
                                // do the last entry outside the loop, to avoid overflowing "i", if high == int.MaxValue
                                entries[high - low].value = high;
                                entries[high - low].target = br.ReadInt32();
                                this.switch_entries = entries;
                                break;
                            }
                        case ByteCodeMode.Lookupswitch:
                            {
                                // skip the padding
                                uint p = pc + 1u;
                                uint align = ((p + 3) & 0x7ffffffc) - p;
                                br.Skip(align);
                                int default_offset = br.ReadInt32();
                                this.arg1 = default_offset;
                                int count = br.ReadInt32();
                                if (count < 0 || count > 16384)
                                {
                                    throw new Exception("Incorrect lookupswitch");
                                }
                                SwitchEntry[] entries = new SwitchEntry[count];
                                for (int i = 0; i < count; i++)
                                {
                                    entries[i].value = br.ReadInt32();
                                    entries[i].target = br.ReadInt32();
                                }
                                this.switch_entries = entries;
                                break;
                            }
                        case ByteCodeMode.WidePrefix:
                            bc = (ByteCode)br.ReadByte();
                            // NOTE the PC of a wide instruction is actually the PC of the
                            // wide prefix, not the following instruction (vmspec 4.9.2)
                            switch (ByteCodeMetaData.GetWideMode(bc))
                            {
                                case ByteCodeModeWide.Local_2:
                                    arg1 = br.ReadUInt16();
                                    break;
                                case ByteCodeModeWide.Local_2_Immediate_2:
                                    arg1 = br.ReadUInt16();
                                    arg2 = br.ReadInt16();
                                    break;
                                default:
                                    throw new Exception(string.Format("Invalid wide prefix on opcode: {0}", bc));
                            }
                            break;
                        default:
                            throw new Exception(string.Format("Invalid opcode: {0}", bc));
                    }
                    this.normopcode = ByteCodeMetaData.GetNormalizedByteCode(bc);
                    arg1 = ByteCodeMetaData.GetArg(bc, arg1);
                }

                public int PC
                {
                    get
                    {
                        return pc;
                    }
                }

                public NormalizedByteCode NormalizedOpCode
                {
                    get
                    {
                        return normopcode;
                    }
                }

                public int Arg1
                {
                    get
                    {
                        return arg1;
                    }
                }

                public int TargetIndex
                {
                    get
                    {
                        return arg1;
                    }
                    set
                    {
                        arg1 = value;
                    }
                }

                public int Arg2
                {
                    get
                    {
                        return arg2;
                    }
                }

                public int NormalizedArg1
                {
                    get
                    {
                        return arg1;
                    }
                }

                public int DefaultTarget
                {
                    get
                    {
                        return arg1;
                    }
                    set
                    {
                        arg1 = value;
                    }
                }

                public int SwitchEntryCount
                {
                    get
                    {
                        return switch_entries.Length;
                    }
                }

                public int GetSwitchValue(int i)
                {
                    return switch_entries[i].value;
                }

                public int GetSwitchTargetIndex(int i)
                {
                    return switch_entries[i].target;
                }

                public void SetSwitchTargets(int[] targets)
                {
                    SwitchEntry[] newEntries = (SwitchEntry[])switch_entries.Clone();
                    for (int i = 0; i < newEntries.Length; i++)
                    {
                        newEntries[i].target = targets[i];
                    }
                    switch_entries = newEntries;
                }
            }

            public struct LineNumberTableEntry
            {
                public ushort start_pc;
                public ushort line_number;
            }

            public struct LocalVariableTableEntry
            {
                public ushort start_pc;
                public ushort length;
                public string name;
                public string descriptor;
                public ushort index;
            }
        }

        internal Field GetField(string name, string sig)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == name && fields[i].Signature == sig)
                {
                    return fields[i];
                }
            }
            return null;
        }

        internal bool HasSerialVersionUID
        {
            get
            {
                Field field = GetField("serialVersionUID", "J");
                return field != null && field.IsStatic && field.IsFinal;
            }
        }
    }
}
