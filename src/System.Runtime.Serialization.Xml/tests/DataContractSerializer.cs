// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using SerializationTypes;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Xunit;


public static partial class DataContractSerializerTests
{
#if ReflectionOnly
    private static readonly string SerializationOptionSetterName = "set_Option";

    static DataContractSerializerTests()
    {
        if (!PlatformDetection.IsFullFramework)
        {
            MethodInfo method = typeof(DataContractSerializer).GetMethod(SerializationOptionSetterName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.True(method != null, $"No method named {SerializationOptionSetterName}");
            method.Invoke(null, new object[] { 1 });
        }
    }
#endif
    [Fact]
    public static void DCS_DateTimeOffsetAsRoot()
    {
        // Assume that UTC offset doesn't change more often than once in the day 2013-01-02
        // DO NOT USE TimeZoneInfo.Local.BaseUtcOffset !
        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        var objs = new DateTimeOffset[]
        {
            // Adding offsetMinutes so the DateTime component in serialized strings are time-zone independent
            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6).AddMinutes(offsetMinutes)),
            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local).AddMinutes(offsetMinutes)),
            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified).AddMinutes(offsetMinutes)),

            new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc))
        };
        var serializedStrings = new string[]
        {
            string.Format(@"<DateTimeOffset xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System""><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>{0}</OffsetMinutes></DateTimeOffset>", offsetMinutes),
            string.Format(@"<DateTimeOffset xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System""><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>{0}</OffsetMinutes></DateTimeOffset>", offsetMinutes),
            string.Format(@"<DateTimeOffset xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System""><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>{0}</OffsetMinutes></DateTimeOffset>", offsetMinutes),
            @"<DateTimeOffset xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System""><DateTime>2013-01-02T03:04:05.006Z</DateTime><OffsetMinutes>0</OffsetMinutes></DateTimeOffset>",
            @"<DateTimeOffset xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System""><DateTime>0001-01-01T00:00:00Z</DateTime><OffsetMinutes>0</OffsetMinutes></DateTimeOffset>",
            @"<DateTimeOffset xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/System""><DateTime>9999-12-31T23:59:59.9999999Z</DateTime><OffsetMinutes>0</OffsetMinutes></DateTimeOffset>"
        };
        for (int i = 0; i < objs.Length; ++i)
        {
            Assert.StrictEqual(SerializeAndDeserialize<DateTimeOffset>(objs[i], serializedStrings[i]), objs[i]);
        }
    }

    [Fact]
    public static void DCS_BoolAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<bool>(true, @"<boolean xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">true</boolean>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<bool>(false, @"<boolean xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">false</boolean>"), false);
    }

    [Fact]
    public static void DCS_ByteArrayAsRoot()
    {
        Assert.Null(SerializeAndDeserialize<byte[]>(null, @"<base64Binary i:nil=""true"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>"));
        byte[] x = new byte[] { 1, 2 };
        byte[] y = SerializeAndDeserialize<byte[]>(x, @"<base64Binary xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">AQI=</base64Binary>");
        Assert.Equal<byte>(x, y);
    }

    [Fact]
    public static void DCS_CharAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MinValue, @"<char xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">0</char>"), char.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>(char.MaxValue, @"<char xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">65535</char>"), char.MaxValue);
        Assert.StrictEqual(SerializeAndDeserialize<char>('a', @"<char xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">97</char>"), 'a');
        Assert.StrictEqual(SerializeAndDeserialize<char>('ñ', @"<char xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">241</char>"), 'ñ');
        Assert.StrictEqual(SerializeAndDeserialize<char>('漢', @"<char xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">28450</char>"), '漢');
    }

    [Fact]
    public static void DCS_ByteAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<byte>(10, @"<unsignedByte xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">10</unsignedByte>"), 10);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MinValue, @"<unsignedByte xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">0</unsignedByte>"), byte.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<byte>(byte.MaxValue, @"<unsignedByte xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">255</unsignedByte>"), byte.MaxValue);
    }

    [Fact]
    public static void DCS_DateTimeAsRoot()
    {
        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2), @"<dateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">2013-01-02T00:00:00</dateTime>"), new DateTime(2013, 1, 2));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local), string.Format(@"<dateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">2013-01-02T03:04:05.006{0:+;-}{1}</dateTime>", offsetMinutes, new TimeSpan(0, offsetMinutes, 0).ToString(@"hh\:mm"))), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified), @"<dateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">2013-01-02T03:04:05.006</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Unspecified));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc), @"<dateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">2013-01-02T03:04:05.006Z</dateTime>"), new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), @"<dateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">0001-01-01T00:00:00Z</dateTime>"), DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        Assert.StrictEqual(SerializeAndDeserialize<DateTime>(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), @"<dateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">9999-12-31T23:59:59.9999999Z</dateTime>"), DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
    }

    [Fact]
    public static void DCS_DecimalAsRoot()
    {
        foreach (decimal value in new decimal[] { (decimal)-1.2, (decimal)0, (decimal)2.3, decimal.MinValue, decimal.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<decimal>(value, string.Format(@"<decimal xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</decimal>", value.ToString(CultureInfo.InvariantCulture))), value);
        }
    }

    [Fact]
    public static void DCS_DoubleAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<double>(-1.2, @"<double xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">-1.2</double>"), -1.2);
        Assert.StrictEqual(SerializeAndDeserialize<double>(0, @"<double xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">0</double>"), 0);
        Assert.StrictEqual(SerializeAndDeserialize<double>(2.3, @"<double xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">2.3</double>"), 2.3);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MinValue, @"<double xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">-1.7976931348623157E+308</double>"), double.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<double>(double.MaxValue, @"<double xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">1.7976931348623157E+308</double>"), double.MaxValue);
    }

    [Fact]
    public static void DCS_FloatAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)-1.2, @"<float xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">-1.2</float>"), (float)-1.2);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)0, @"<float xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">0</float>"), (float)0);
        Assert.StrictEqual(SerializeAndDeserialize<float>((float)2.3, @"<float xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">2.3</float>"), (float)2.3);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MinValue, @"<float xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">-3.40282347E+38</float>"), float.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<float>(float.MaxValue, @"<float xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">3.40282347E+38</float>"), float.MaxValue);
    }

    [Fact]
    public static void DCS_GuidAsRoot()
    {
        foreach (Guid value in new Guid[] { Guid.NewGuid(), Guid.Empty })
        {
            Assert.StrictEqual(SerializeAndDeserialize<Guid>(value, string.Format(@"<guid xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</guid>", value.ToString())), value);
        }
    }

    [Fact]
    public static void DCS_IntAsRoot()
    {
        foreach (int value in new int[] { -1, 0, 2, int.MinValue, int.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<int>(value, string.Format(@"<int xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</int>", value)), value);
        }
    }

    [Fact]
    public static void DCS_LongAsRoot()
    {
        foreach (long value in new long[] { (long)-1, (long)0, (long)2, long.MinValue, long.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<long>(value, string.Format(@"<long xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</long>", value)), value);
        }
    }

    [Fact]
    public static void DCS_ObjectAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<object>(1, @"<z:anyType i:type=""a:int"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://www.w3.org/2001/XMLSchema"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">1</z:anyType>"), 1);
        Assert.StrictEqual(SerializeAndDeserialize<object>(true, @"<z:anyType i:type=""a:boolean"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://www.w3.org/2001/XMLSchema"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">true</z:anyType>"), true);
        Assert.StrictEqual(SerializeAndDeserialize<object>("abc", @"<z:anyType i:type=""a:string"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://www.w3.org/2001/XMLSchema"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">abc</z:anyType>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<object>(null, @"<z:anyType i:nil=""true"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>"), null);
    }

    [Fact]
    public static void DCS_XmlQualifiedNameAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(new XmlQualifiedName("abc", "def"), @"<z:QName xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""def"">a:abc</z:QName>"), new XmlQualifiedName("abc", "def"));
        Assert.StrictEqual(SerializeAndDeserialize<XmlQualifiedName>(XmlQualifiedName.Empty, @"<z:QName xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/>"), XmlQualifiedName.Empty);
    }

    [Fact]
    public static void DCS_ShortAsRoot()
    {
        foreach (short value in new short[] { (short)-1.2, (short)0, (short)2.3, short.MinValue, short.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<short>(value, string.Format(@"<short xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</short>", value)), value);
        }
    }

    [Fact]
    public static void DCS_SbyteAsRoot()
    {
        foreach (sbyte value in new sbyte[] { (sbyte)3, (sbyte)0, sbyte.MinValue, sbyte.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<sbyte>(value, string.Format(@"<byte xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</byte>", value)), value);
        }
    }

    [Fact]
    public static void DCS_StringAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<string>("abc", @"<string xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">abc</string>"), "abc");
        Assert.StrictEqual(SerializeAndDeserialize<string>("  a b  ", @"<string xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">  a b  </string>"), "  a b  ");
        Assert.StrictEqual(SerializeAndDeserialize<string>(null, @"<string i:nil=""true"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>"), null);
        Assert.StrictEqual(SerializeAndDeserialize<string>("", @"<string xmlns=""http://schemas.microsoft.com/2003/10/Serialization/""/>"), "");
        Assert.StrictEqual(SerializeAndDeserialize<string>(" ", @"<string xmlns=""http://schemas.microsoft.com/2003/10/Serialization/""> </string>"), " ");
        Assert.StrictEqual(SerializeAndDeserialize<string>("Hello World! 漢 ñ", @"<string xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">Hello World! 漢 ñ</string>"), "Hello World! 漢 ñ");
    }

    [Fact]
    public static void DCS_TimeSpanAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(new TimeSpan(1, 2, 3), @"<duration xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">PT1H2M3S</duration>"), new TimeSpan(1, 2, 3));
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.Zero, @"<duration xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">PT0S</duration>"), TimeSpan.Zero);
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.MinValue, @"<duration xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">-P10675199DT2H48M5.4775808S</duration>"), TimeSpan.MinValue);
        Assert.StrictEqual(SerializeAndDeserialize<TimeSpan>(TimeSpan.MaxValue, @"<duration xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</duration>"), TimeSpan.MaxValue);
    }

    [Fact]
    public static void DCS_UintAsRoot()
    {
        foreach (uint value in new uint[] { (uint)3, (uint)0, uint.MinValue, uint.MaxValue })
        {
            Assert.StrictEqual<uint>(SerializeAndDeserialize<uint>(value, string.Format(@"<unsignedInt xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</unsignedInt>", value)), value);
        }
    }

    [Fact]
    public static void DCS_UlongAsRoot()
    {
        foreach (ulong value in new ulong[] { (ulong)3, (ulong)0, ulong.MinValue, ulong.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ulong>(value, string.Format(@"<unsignedLong xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</unsignedLong>", value)), value);
        }
    }

    [Fact]
    public static void DCS_UshortAsRoot()
    {
        foreach (ushort value in new ushort[] { (ushort)3, (ushort)0, ushort.MinValue, ushort.MaxValue })
        {
            Assert.StrictEqual(SerializeAndDeserialize<ushort>(value, string.Format(@"<unsignedShort xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">{0}</unsignedShort>", value)), value);
        }
    }

    [Fact]
    public static void DCS_UriAsRoot()
    {
        Assert.StrictEqual(SerializeAndDeserialize<Uri>(new Uri("http://abc/"), @"<anyURI xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">http://abc/</anyURI>"), new Uri("http://abc/"));
        Assert.StrictEqual(SerializeAndDeserialize<Uri>(new Uri("http://abc/def/x.aspx?p1=12&p2=34"), @"<anyURI xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">http://abc/def/x.aspx?p1=12&amp;p2=34</anyURI>"), new Uri("http://abc/def/x.aspx?p1=12&p2=34"));
    }

    [Fact]
    public static void DCS_ArrayAsRoot()
    {
        SimpleType[] x = new SimpleType[] { new SimpleType { P1 = "abc", P2 = 11 }, new SimpleType { P1 = "def", P2 = 12 } };
        SimpleType[] y = SerializeAndDeserialize<SimpleType[]>(x, @"<ArrayOfSimpleType xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><SimpleType><P1>abc</P1><P2>11</P2></SimpleType><SimpleType><P1>def</P1><P2>12</P2></SimpleType></ArrayOfSimpleType>");

        Utils.Equal<SimpleType>(x, y, (a, b) => { return SimpleType.AreEqual(a, b); });
    }

    [Fact]
    public static void DCS_ArrayAsGetSet()
    {
        TypeWithGetSetArrayMembers x = new TypeWithGetSetArrayMembers
        {
            F1 = new SimpleType[] { new SimpleType { P1 = "ab", P2 = 1 }, new SimpleType { P1 = "cd", P2 = 2 } },
            F2 = new int[] { -1, 3 },
            P1 = new SimpleType[] { new SimpleType { P1 = "ef", P2 = 5 }, new SimpleType { P1 = "gh", P2 = 7 } },
            P2 = new int[] { 11, 12 }
        };
        TypeWithGetSetArrayMembers y = SerializeAndDeserialize<TypeWithGetSetArrayMembers>(x, @"<TypeWithGetSetArrayMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1><SimpleType><P1>ab</P1><P2>1</P2></SimpleType><SimpleType><P1>cd</P1><P2>2</P2></SimpleType></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>-1</a:int><a:int>3</a:int></F2><P1><SimpleType><P1>ef</P1><P2>5</P2></SimpleType><SimpleType><P1>gh</P1><P2>7</P2></SimpleType></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>11</a:int><a:int>12</a:int></P2></TypeWithGetSetArrayMembers>");

        Assert.NotNull(y);
        Utils.Equal<SimpleType>(x.F1, y.F1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal<int>(x.F2, y.F2);
        Utils.Equal<SimpleType>(x.P1, y.P1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal<int>(x.P2, y.P2);
    }

    [Fact]
    public static void DCS_ArrayAsGetOnly()
    {
        TypeWithGetOnlyArrayProperties x = new TypeWithGetOnlyArrayProperties();
        x.P1[0] = new SimpleType { P1 = "ab", P2 = 1 };
        x.P1[1] = new SimpleType { P1 = "cd", P2 = 2 };
        x.P2[0] = -1;
        x.P2[1] = 3;

        TypeWithGetOnlyArrayProperties y = SerializeAndDeserialize<TypeWithGetOnlyArrayProperties>(x, @"<TypeWithGetOnlyArrayProperties xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><P1><SimpleType><P1>ab</P1><P2>1</P2></SimpleType><SimpleType><P1>cd</P1><P2>2</P2></SimpleType></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>-1</a:int><a:int>3</a:int></P2></TypeWithGetOnlyArrayProperties>");

        Assert.NotNull(y);
        Utils.Equal<SimpleType>(x.P1, y.P1, (a, b) => { return SimpleType.AreEqual(a, b); });
        Assert.Equal<int>(x.P2, y.P2);
    }

    [Fact]
    public static void DCS_DictionaryGenericRoot()
    {
        Dictionary<string, int> x = new Dictionary<string, int>();
        x.Add("one", 1);
        x.Add("two", 2);

        Dictionary<string, int> y = SerializeAndDeserialize<Dictionary<string, int>>(x, @"<ArrayOfKeyValueOfstringint xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfstringint><Key>one</Key><Value>1</Value></KeyValueOfstringint><KeyValueOfstringint><Key>two</Key><Value>2</Value></KeyValueOfstringint></ArrayOfKeyValueOfstringint>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True(y["one"] == 1);
        Assert.True(y["two"] == 2);
    }

    [Fact]
    public static void DCS_DictionaryGenericMembers()
    {
        TypeWithDictionaryGenericMembers x = new TypeWithDictionaryGenericMembers
        {
            F1 = new Dictionary<string, int>(),
            F2 = new Dictionary<string, int>(),
            P1 = new Dictionary<string, int>(),
            P2 = new Dictionary<string, int>()
        };
        x.F1.Add("ab", 12);
        x.F1.Add("cd", 15);
        x.F2.Add("ef", 17);
        x.F2.Add("gh", 19);
        x.P1.Add("12", 120);
        x.P1.Add("13", 130);
        x.P2.Add("14", 140);
        x.P2.Add("15", 150);

        x.RO1.Add(true, 't');
        x.RO1.Add(false, 'f');

        x.RO2.Add(true, 'a');
        x.RO2.Add(false, 'b');

        TypeWithDictionaryGenericMembers y = SerializeAndDeserialize<TypeWithDictionaryGenericMembers>(x, @"<TypeWithDictionaryGenericMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfstringint><a:Key>ab</a:Key><a:Value>12</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>cd</a:Key><a:Value>15</a:Value></a:KeyValueOfstringint></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfstringint><a:Key>ef</a:Key><a:Value>17</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>gh</a:Key><a:Value>19</a:Value></a:KeyValueOfstringint></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfstringint><a:Key>12</a:Key><a:Value>120</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>13</a:Key><a:Value>130</a:Value></a:KeyValueOfstringint></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfstringint><a:Key>14</a:Key><a:Value>140</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>15</a:Key><a:Value>150</a:Value></a:KeyValueOfstringint></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfbooleanchar><a:Key>true</a:Key><a:Value>116</a:Value></a:KeyValueOfbooleanchar><a:KeyValueOfbooleanchar><a:Key>false</a:Key><a:Value>102</a:Value></a:KeyValueOfbooleanchar></RO1><RO2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfbooleanchar><a:Key>true</a:Key><a:Value>97</a:Value></a:KeyValueOfbooleanchar><a:KeyValueOfbooleanchar><a:Key>false</a:Key><a:Value>98</a:Value></a:KeyValueOfbooleanchar></RO2></TypeWithDictionaryGenericMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True(y.F1["ab"] == 12);
        Assert.True(y.F1["cd"] == 15);

        Assert.NotNull(y.F2);
        Assert.True(y.F2.Count == 2);
        Assert.True(y.F2["ef"] == 17);
        Assert.True(y.F2["gh"] == 19);

        Assert.NotNull(y.P1);
        Assert.True(y.P1.Count == 2);
        Assert.True(y.P1["12"] == 120);
        Assert.True(y.P1["13"] == 130);

        Assert.NotNull(y.P2);
        Assert.True(y.P2.Count == 2);
        Assert.True(y.P2["14"] == 140);
        Assert.True(y.P2["15"] == 150);

        Assert.NotNull(y.RO1);
        Assert.True(y.RO1.Count == 2);
        Assert.True(y.RO1[true] == 't');
        Assert.True(y.RO1[false] == 'f');

        Assert.NotNull(y.RO2);
        Assert.True(y.RO2.Count == 2);
        Assert.True(y.RO2[true] == 'a');
        Assert.True(y.RO2[false] == 'b');
    }

    [Fact]
    public static void DCS_DictionaryRoot()
    {
        MyDictionary x = new MyDictionary();
        x.Add(1, "one");
        x.Add(2, "two");

        MyDictionary y = SerializeAndDeserialize<MyDictionary>(x, @"<ArrayOfKeyValueOfanyTypeanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">1</Key><Value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">one</Value></KeyValueOfanyTypeanyType><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">2</Key><Value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">two</Value></KeyValueOfanyTypeanyType></ArrayOfKeyValueOfanyTypeanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[1] == "one");
        Assert.True((string)y[2] == "two");
    }

    [Fact]
    public static void DCS_DictionaryMembers()
    {
        TypeWithDictionaryMembers x = new TypeWithDictionaryMembers();

        x.F1 = new MyDictionary();
        x.F1.Add("ab", 12);
        x.F1.Add("cd", 15);

        x.F2 = new MyDictionary();
        x.F2.Add("ef", 17);
        x.F2.Add("gh", 19);

        x.P1 = new MyDictionary();
        x.P1.Add("12", 120);
        x.P1.Add("13", 130);

        x.P2 = new MyDictionary();
        x.P2.Add("14", 140);
        x.P2.Add("15", 150);

        x.RO1.Add(true, 't');
        x.RO1.Add(false, 'f');

        x.RO2.Add(true, 'a');
        x.RO2.Add(false, 'b');

        TypeWithDictionaryMembers y = SerializeAndDeserialize<TypeWithDictionaryMembers>(x, @"<TypeWithDictionaryMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">ab</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">12</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">cd</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">15</a:Value></a:KeyValueOfanyTypeanyType></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">ef</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">17</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">gh</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">19</a:Value></a:KeyValueOfanyTypeanyType></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">12</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">120</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">13</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">130</a:Value></a:KeyValueOfanyTypeanyType></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">14</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">140</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">15</a:Key><a:Value i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">150</a:Value></a:KeyValueOfanyTypeanyType></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</a:Key><a:Value i:type=""b:char"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">116</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">false</a:Key><a:Value i:type=""b:char"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">102</a:Value></a:KeyValueOfanyTypeanyType></RO1><RO2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</a:Key><a:Value i:type=""b:char"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">97</a:Value></a:KeyValueOfanyTypeanyType><a:KeyValueOfanyTypeanyType><a:Key i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">false</a:Key><a:Value i:type=""b:char"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">98</a:Value></a:KeyValueOfanyTypeanyType></RO2></TypeWithDictionaryMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True((int)y.F1["ab"] == 12);
        Assert.True((int)y.F1["cd"] == 15);

        Assert.NotNull(y.F2);
        Assert.True(y.F2.Count == 2);
        Assert.True((int)y.F2["ef"] == 17);
        Assert.True((int)y.F2["gh"] == 19);

        Assert.NotNull(y.P1);
        Assert.True(y.P1.Count == 2);
        Assert.True((int)y.P1["12"] == 120);
        Assert.True((int)y.P1["13"] == 130);

        Assert.NotNull(y.P2);
        Assert.True(y.P2.Count == 2);
        Assert.True((int)y.P2["14"] == 140);
        Assert.True((int)y.P2["15"] == 150);

        Assert.NotNull(y.RO1);
        Assert.True(y.RO1.Count == 2);
        Assert.True((char)y.RO1[true] == 't');
        Assert.True((char)y.RO1[false] == 'f');

        Assert.NotNull(y.RO2);
        Assert.True(y.RO2.Count == 2);
        Assert.True((char)y.RO2[true] == 'a');
        Assert.True((char)y.RO2[false] == 'b');
    }

    [Fact]
    public static void DCS_TypeWithIDictionaryPropertyInitWithConcreteType()
    {
        // Test for Bug 876869 : [Serialization] Concrete type not inferred for DCS
        var dict = new TypeWithIDictionaryPropertyInitWithConcreteType();
        dict.DictionaryProperty.Add("key1", "value1");
        dict.DictionaryProperty.Add("key2", "value2");

        var dict2 = SerializeAndDeserialize<TypeWithIDictionaryPropertyInitWithConcreteType>(dict, @"<TypeWithIDictionaryPropertyInitWithConcreteType xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><DictionaryProperty xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfstringstring><a:Key>key1</a:Key><a:Value>value1</a:Value></a:KeyValueOfstringstring><a:KeyValueOfstringstring><a:Key>key2</a:Key><a:Value>value2</a:Value></a:KeyValueOfstringstring></DictionaryProperty></TypeWithIDictionaryPropertyInitWithConcreteType>");

        Assert.True(dict2 != null && dict2.DictionaryProperty != null);
        Assert.True(dict.DictionaryProperty.Count == dict2.DictionaryProperty.Count);
        foreach (var entry in dict.DictionaryProperty)
        {
            Assert.True(dict2.DictionaryProperty.ContainsKey(entry.Key) && dict2.DictionaryProperty[entry.Key].Equals(dict.DictionaryProperty[entry.Key]));
        }
    }

    [Fact]
    public static void DCS_ListGenericRoot()
    {
        List<string> x = new List<string>();
        x.Add("zero");
        x.Add("one");

        List<string> y = SerializeAndDeserialize<List<string>>(x, @"<ArrayOfstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><string>zero</string><string>one</string></ArrayOfstring>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True(y[0] == "zero");
        Assert.True(y[1] == "one");
    }

    [Fact]
    public static void DCS_ListGenericMembers()
    {
        TypeWithListGenericMembers x = new TypeWithListGenericMembers();

        x.F1 = new List<string>();
        x.F1.Add("zero");
        x.F1.Add("one");

        x.F2 = new List<string>();
        x.F2.Add("abc");
        x.F2.Add("def");

        x.P1 = new List<int>();
        x.P1.Add(10);
        x.P1.Add(20);

        x.P2 = new List<int>();
        x.P2.Add(12);
        x.P2.Add(34);

        x.RO1.Add('a');
        x.RO1.Add('b');

        x.RO2.Add('c');
        x.RO2.Add('d');

        TypeWithListGenericMembers y = SerializeAndDeserialize<TypeWithListGenericMembers>(x, @"<TypeWithListGenericMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>zero</a:string><a:string>one</a:string></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>abc</a:string><a:string>def</a:string></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>10</a:int><a:int>20</a:int></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>12</a:int><a:int>34</a:int></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:char>97</a:char><a:char>98</a:char></RO1><RO2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:char>99</a:char><a:char>100</a:char></RO2></TypeWithListGenericMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True(y.F1[0] == "zero");
        Assert.True(y.F1[1] == "one");

        Assert.NotNull(y.F2);
        Assert.True(y.F2.Count == 2);
        Assert.True(y.F2[0] == "abc");
        Assert.True(y.F2[1] == "def");

        Assert.NotNull(y.P1);
        Assert.True(y.P1.Count == 2);
        Assert.True(y.P1[0] == 10);
        Assert.True(y.P1[1] == 20);

        Assert.NotNull(y.P2);
        Assert.True(y.P2.Count == 2);
        Assert.True(y.P2[0] == 12);
        Assert.True(y.P2[1] == 34);

        Assert.NotNull(y.RO1);
        Assert.True(y.RO1.Count == 2);
        Assert.True(y.RO1[0] == 'a');
        Assert.True(y.RO1[1] == 'b');

        Assert.NotNull(y.RO2);
        Assert.True(y.RO2.Count == 2);
        Assert.True(y.RO2[0] == 'c');
        Assert.True(y.RO2[1] == 'd');
    }

    [Fact]
    public static void DCS_CollectionGenericRoot()
    {
        MyCollection<string> x = new MyCollection<string>("a1", "a2");
        MyCollection<string> y = SerializeAndDeserialize<MyCollection<string>>(x, @"<ArrayOfstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><string>a1</string><string>a2</string></ArrayOfstring>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        foreach (var item in x)
        {
            Assert.True(y.Contains(item));
        }
    }

    [Fact]
    public static void DCS_CollectionGenericMembers()
    {
        TypeWithCollectionGenericMembers x = new TypeWithCollectionGenericMembers
        {
            F1 = new MyCollection<string>("a1", "a2"),
            F2 = new MyCollection<string>("b1", "b2"),
            P1 = new MyCollection<string>("c1", "c2"),
            P2 = new MyCollection<string>("d1", "d2"),
        };
        x.RO1.Add("abc");
        x.RO2.Add("xyz");

        TypeWithCollectionGenericMembers y = SerializeAndDeserialize<TypeWithCollectionGenericMembers>(x, @"<TypeWithCollectionGenericMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>a1</a:string><a:string>a2</a:string></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>b1</a:string><a:string>b2</a:string></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>c1</a:string><a:string>c2</a:string></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>d1</a:string><a:string>d2</a:string></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>abc</a:string></RO1><RO2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>xyz</a:string></RO2></TypeWithCollectionGenericMembers>");
        Assert.NotNull(y);
        Assert.True(y.F1.Count == 2, getCheckFailureMsg("F1"));
        Assert.True(y.F2.Count == 2, getCheckFailureMsg("F2"));
        Assert.True(y.P1.Count == 2, getCheckFailureMsg("P1"));
        Assert.True(y.P2.Count == 2, getCheckFailureMsg("P2"));
        Assert.True(y.RO1.Count == 1, getCheckFailureMsg("RO1"));
        Assert.True(y.RO2.Count == 1, getCheckFailureMsg("RO2"));




        foreach (var item in x.F1)
        {
            Assert.True(y.F1.Contains(item), getCheckFailureMsg("F1"));
        }
        foreach (var item in x.F2)
        {
            Assert.True(y.F2.Contains(item), getCheckFailureMsg("F2"));
        }
        foreach (var item in x.P1)
        {
            Assert.True(y.P1.Contains(item), getCheckFailureMsg("P1"));
        }
        foreach (var item in x.P2)
        {
            Assert.True(y.P2.Contains(item), getCheckFailureMsg("P2"));
        }
        foreach (var item in x.RO1)
        {
            Assert.True(y.RO1.Contains(item), getCheckFailureMsg("RO1"));
        }
        foreach (var item in x.RO2)
        {
            Assert.True(y.RO2.Contains(item), getCheckFailureMsg("RO2"));
        }
    }

    [Fact]
    public static void DCS_ListRoot()
    {
        MyList x = new MyList("a1", "a2");
        MyList y = SerializeAndDeserialize<MyList>(x, @"<ArrayOfanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><anyType i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">a1</anyType><anyType i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">a2</anyType></ArrayOfanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        foreach (var item in x)
        {
            Assert.True(y.Contains(item));
        }
    }

    [Fact]
    public static void DCS_ListMembers()
    {
        TypeWithListMembers x = new TypeWithListMembers
        {
            F1 = new MyList("a1", "a2"),
            F2 = new MyList("b1", "b2"),
            P1 = new MyList("c1", "c2"),
            P2 = new MyList("d1", "d2"),
        };
        x.RO1.Add("abc");
        x.RO2.Add("xyz");

        TypeWithListMembers y = SerializeAndDeserialize<TypeWithListMembers>(x, @"<TypeWithListMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">a1</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">a2</a:anyType></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">b1</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">b2</a:anyType></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">c1</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">c2</a:anyType></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">d1</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">d2</a:anyType></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">abc</a:anyType></RO1><RO2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">xyz</a:anyType></RO2></TypeWithListMembers>");
        Assert.NotNull(y);
        Assert.True(y.F1.Count == 2, getCheckFailureMsg("F1"));
        Assert.True(y.F2.Count == 2, getCheckFailureMsg("F2"));
        Assert.True(y.P1.Count == 2, getCheckFailureMsg("P1"));
        Assert.True(y.P2.Count == 2, getCheckFailureMsg("P2"));
        Assert.True(y.RO1.Count == 1, getCheckFailureMsg("RO1"));
        Assert.True(y.RO2.Count == 1, getCheckFailureMsg("RO2"));

        Assert.True((string)x.F1[0] == (string)y.F1[0], getCheckFailureMsg("F1"));
        Assert.True((string)x.F1[1] == (string)y.F1[1], getCheckFailureMsg("F1"));
        Assert.True((string)x.F2[0] == (string)y.F2[0], getCheckFailureMsg("F2"));
        Assert.True((string)x.F2[1] == (string)y.F2[1], getCheckFailureMsg("F2"));
        Assert.True((string)x.P1[0] == (string)y.P1[0], getCheckFailureMsg("P1"));
        Assert.True((string)x.P1[1] == (string)y.P1[1], getCheckFailureMsg("P1"));
        Assert.True((string)x.P2[0] == (string)y.P2[0], getCheckFailureMsg("P2"));
        Assert.True((string)x.P2[1] == (string)y.P2[1], getCheckFailureMsg("P2"));
        Assert.True((string)x.RO1[0] == (string)y.RO1[0], getCheckFailureMsg("RO1"));
        Assert.True((string)x.RO2[0] == (string)y.RO2[0], getCheckFailureMsg("RO2"));
    }

    [Fact]
    public static void DCS_EnumerableGenericRoot()
    {
        MyEnumerable<string> x = new MyEnumerable<string>("a1", "a2");
        MyEnumerable<string> y = SerializeAndDeserialize<MyEnumerable<string>>(x, @"<ArrayOfstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><string>a1</string><string>a2</string></ArrayOfstring>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);

        string actual = string.Join("", y);
        Assert.StrictEqual(actual, "a1a2");
    }

    [Fact]
    public static void DCS_EnumerableGenericMembers()
    {
        TypeWithEnumerableGenericMembers x = new TypeWithEnumerableGenericMembers
        {
            F1 = new MyEnumerable<string>("a1", "a2"),
            F2 = new MyEnumerable<string>("b1", "b2"),
            P1 = new MyEnumerable<string>("c1", "c2"),
            P2 = new MyEnumerable<string>("d1", "d2")
        };
        x.RO1.Add("abc");

        TypeWithEnumerableGenericMembers y = SerializeAndDeserialize<TypeWithEnumerableGenericMembers>(x, @"<TypeWithEnumerableGenericMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>a1</a:string><a:string>a2</a:string></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>b1</a:string><a:string>b2</a:string></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>c1</a:string><a:string>c2</a:string></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>d1</a:string><a:string>d2</a:string></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>abc</a:string></RO1></TypeWithEnumerableGenericMembers>");

        Assert.NotNull(y);
        Assert.True(y.F1.Count == 2);
        Assert.True(((string[])y.F2).Length == 2);
        Assert.True(y.P1.Count == 2);
        Assert.True(((string[])y.P2).Length == 2);
        Assert.True(y.RO1.Count == 1);
    }

    [Fact]
    public static void DCS_CollectionRoot()
    {
        MyCollection x = new MyCollection('a', 45);
        MyCollection y = SerializeAndDeserialize<MyCollection>(x, @"<ArrayOfanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><anyType i:type=""a:char"" xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/"">97</anyType><anyType i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">45</anyType></ArrayOfanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((char)y[0] == 'a');
        Assert.True((int)y[1] == 45);
    }

    [Fact]
    public static void DCS_CollectionMembers()
    {
        TypeWithCollectionMembers x = new TypeWithCollectionMembers
        {
            F1 = new MyCollection('a', 45),
            F2 = new MyCollection("ab", true),
            P1 = new MyCollection("x", "y"),
            P2 = new MyCollection(false, true)
        };
        x.RO1.Add("abc");

        TypeWithCollectionMembers y = SerializeAndDeserialize<TypeWithCollectionMembers>(x, @"<TypeWithCollectionMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:char"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">97</a:anyType><a:anyType i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">45</a:anyType></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">ab</a:anyType><a:anyType i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</a:anyType></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">x</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">y</a:anyType></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">false</a:anyType><a:anyType i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</a:anyType></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">abc</a:anyType></RO1></TypeWithCollectionMembers>");
        Assert.NotNull(y);

        Assert.NotNull(y.F1);
        Assert.True(y.F1.Count == 2);
        Assert.True((char)y.F1[0] == 'a');
        Assert.True((int)y.F1[1] == 45);

        Assert.NotNull(y.F2);
        Assert.True(((object[])y.F2).Length == 2);
        Assert.True((string)((object[])y.F2)[0] == "ab");
        Assert.True((bool)((object[])y.F2)[1] == true);

        Assert.True(y.P1.Count == 2);
        Assert.True((string)y.P1[0] == "x");
        Assert.True((string)y.P1[1] == "y");

        Assert.True(((object[])y.P2).Length == 2);
        Assert.True((bool)((object[])y.P2)[0] == false);
        Assert.True((bool)((object[])y.P2)[1] == true);

        Assert.True(y.RO1.Count == 1);
        Assert.True((string)y.RO1[0] == "abc");
    }

    [Fact]
    public static void DCS_EnumerableRoot()
    {
        MyEnumerable x = new MyEnumerable("abc", 3);
        MyEnumerable y = SerializeAndDeserialize<MyEnumerable>(x, @"<ArrayOfanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><anyType i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">abc</anyType><anyType i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">3</anyType></ArrayOfanyType>");

        Assert.NotNull(y);
        Assert.True(y.Count == 2);
        Assert.True((string)y[0] == "abc");
        Assert.True((int)y[1] == 3);
    }

    [Fact]
    public static void DCS_EnumerableMembers()
    {
        TypeWithEnumerableMembers x = new TypeWithEnumerableMembers
        {
            F1 = new MyEnumerable('a', 45),
            F2 = new MyEnumerable("ab", true),
            P1 = new MyEnumerable("x", "y"),
            P2 = new MyEnumerable(false, true)
        };
        x.RO1.Add('x');

        TypeWithEnumerableMembers y = SerializeAndDeserialize<TypeWithEnumerableMembers>(x, @"<TypeWithEnumerableMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:char"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">97</a:anyType><a:anyType i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">45</a:anyType></F1><F2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">ab</a:anyType><a:anyType i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</a:anyType></F2><P1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">x</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">y</a:anyType></P1><P2 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">false</a:anyType><a:anyType i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</a:anyType></P2><RO1 xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:char"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">120</a:anyType></RO1></TypeWithEnumerableMembers>");
        Assert.NotNull(y);

        Assert.True(y.F1.Count == 2);
        Assert.True((char)y.F1[0] == 'a');
        Assert.True((int)y.F1[1] == 45);

        Assert.True(((object[])y.F2).Length == 2);
        Assert.True((string)((object[])y.F2)[0] == "ab");
        Assert.True((bool)((object[])y.F2)[1] == true);

        Assert.True(y.P1.Count == 2);
        Assert.True((string)y.P1[0] == "x");
        Assert.True((string)y.P1[1] == "y");

        Assert.True(((object[])y.P2).Length == 2);
        Assert.True((bool)((object[])y.P2)[0] == false);
        Assert.True((bool)((object[])y.P2)[1] == true);

        Assert.True(y.RO1.Count == 1);
        Assert.True((char)y.RO1[0] == 'x');
    }

    [Fact]
    public static void DCS_CustomType()
    {
        MyTypeA x = new MyTypeA
        {
            PropX = new MyTypeC { PropC = 'a', PropB = true },
            PropY = 45,
        };

        MyTypeA y = SerializeAndDeserialize<MyTypeA>(x, @"<MyTypeA xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><P_Col_Array i:nil=""true""/><PropX i:type=""MyTypeC""><PropA i:nil=""true""/><PropC>97</PropC><PropB>true</PropB></PropX><PropY>45</PropY></MyTypeA>");

        Assert.NotNull(y);
        Assert.NotNull(y.PropX);
        Assert.StrictEqual(x.PropX.PropC, y.PropX.PropC);
        Assert.StrictEqual(((MyTypeC)x.PropX).PropB, ((MyTypeC)y.PropX).PropB);
        Assert.StrictEqual(x.PropY, y.PropY);
    }

    [Fact]
    public static void DCS_TypeWithPrivateFieldAndPrivateGetPublicSetProperty()
    {
        TypeWithPrivateFieldAndPrivateGetPublicSetProperty x = new TypeWithPrivateFieldAndPrivateGetPublicSetProperty
        {
            Name = "foo",
        };

        TypeWithPrivateFieldAndPrivateGetPublicSetProperty y = SerializeAndDeserialize<TypeWithPrivateFieldAndPrivateGetPublicSetProperty>(x, @"<TypeWithPrivateFieldAndPrivateGetPublicSetProperty xmlns=""http://schemas.datacontract.org/2004/07/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>foo</Name></TypeWithPrivateFieldAndPrivateGetPublicSetProperty>");
        Assert.Equal(x.GetName(), y.GetName());
    }

    [Fact]
    public static void DCS_DataContractAttribute()
    {
        SerializeAndDeserialize<DCA_1>(new DCA_1 { P1 = "xyz" }, @"<DCA_1 xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>");
        SerializeAndDeserialize<DCA_2>(new DCA_2 { P1 = "xyz" }, @"<abc xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>");
        SerializeAndDeserialize<DCA_3>(new DCA_3 { P1 = "xyz" }, @"<DCA_3 xmlns=""def"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>");
        SerializeAndDeserialize<DCA_4>(new DCA_4 { P1 = "xyz" }, @"<DCA_4 z:Id=""i1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/>");
        SerializeAndDeserialize<DCA_5>(new DCA_5 { P1 = "xyz" }, @"<abc xmlns=""def"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""/>");
    }

    [Fact]
    public static void DCS_DataMemberAttribute()
    {
        SerializeAndDeserialize<DMA_1>(new DMA_1 { P1 = "abc", P2 = 12, P3 = true, P4 = 'a', P5 = 10, MyDataMemberInAnotherNamespace = new MyDataContractClass04_1() { MyDataMember = "Test" }, Order100 = true, OrderMaxValue = false }, @"<DMA_1 xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><MyDataMemberInAnotherNamespace xmlns:a=""http://MyDataContractClass04_1.com/""><a:MyDataMember>Test</a:MyDataMember></MyDataMemberInAnotherNamespace><P1>abc</P1><P4>97</P4><P5>10</P5><xyz>12</xyz><P3>true</P3><Order100>true</Order100><OrderMaxValue>false</OrderMaxValue></DMA_1>");
    }

    [Fact]
    public static void DCS_IgnoreDataMemberAttribute()
    {
        IDMA_1 x = new IDMA_1 { MyDataMember = "MyDataMember", MyIgnoreDataMember = "MyIgnoreDataMember", MyUnsetDataMember = "MyUnsetDataMember" };
        IDMA_1 y = SerializeAndDeserialize<IDMA_1>(x, @"<IDMA_1 xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><MyDataMember>MyDataMember</MyDataMember></IDMA_1>");
        Assert.NotNull(y);
        Assert.StrictEqual(x.MyDataMember, y.MyDataMember);
        Assert.Null(y.MyIgnoreDataMember);
        Assert.Null(y.MyUnsetDataMember);
    }

    [Fact]
    public static void DCS_EnumAsRoot()
    {
        //The approved types for an enum are byte, sbyte, short, ushort, int, uint, long, or ulong.
        Assert.StrictEqual(SerializeAndDeserialize<MyEnum>(MyEnum.Two, @"<MyEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Two</MyEnum>"), MyEnum.Two);
        Assert.StrictEqual(SerializeAndDeserialize<ByteEnum>(ByteEnum.Option1, @"<ByteEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Option1</ByteEnum>"), ByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<SByteEnum>(SByteEnum.Option1, @"<SByteEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Option1</SByteEnum>"), SByteEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ShortEnum>(ShortEnum.Option1, @"<ShortEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Option1</ShortEnum>"), ShortEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<IntEnum>(IntEnum.Option1, @"<IntEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Option1</IntEnum>"), IntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<UIntEnum>(UIntEnum.Option1, @"<UIntEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Option1</UIntEnum>"), UIntEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<LongEnum>(LongEnum.Option1, @"<LongEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Option1</LongEnum>"), LongEnum.Option1);
        Assert.StrictEqual(SerializeAndDeserialize<ULongEnum>(ULongEnum.Option1, @"<ULongEnum xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">Option1</ULongEnum>"), ULongEnum.Option1);
    }

    [Fact]
    public static void DCS_EnumAsMember()
    {
        TypeWithEnumMembers x = new TypeWithEnumMembers { F1 = MyEnum.Three, P1 = MyEnum.Two };
        TypeWithEnumMembers y = SerializeAndDeserialize<TypeWithEnumMembers>(x, @"<TypeWithEnumMembers xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><F1>Three</F1><P1>Two</P1></TypeWithEnumMembers>");

        Assert.NotNull(y);
        Assert.StrictEqual(x.F1, y.F1);
        Assert.StrictEqual(x.P1, y.P1);
    }

    [Fact]
    public static void DCS_DCClassWithEnumAndStruct()
    {
        var x = new DCClassWithEnumAndStruct(true);
        var y = SerializeAndDeserialize<DCClassWithEnumAndStruct>(x, @"<DCClassWithEnumAndStruct xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><MyEnum1>One</MyEnum1><MyStruct><Data>Data</Data></MyStruct></DCClassWithEnumAndStruct>");

        Assert.StrictEqual(x.MyStruct, y.MyStruct);
        Assert.StrictEqual(x.MyEnum1, y.MyEnum1);
    }

    [Fact]
    public static void DCS_SuspensionManager()
    {
        var x = new Dictionary<string, object>();
        var subDictionary = new Dictionary<string, object>();
        subDictionary.Add("subkey1", "subkey1value");
        x.Add("Key1", subDictionary);

        Dictionary<string, object> y = SerializeAndDeserialize<Dictionary<string, object>>(x, @"<ArrayOfKeyValueOfstringanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfstringanyType><Key>Key1</Key><Value i:type=""ArrayOfKeyValueOfstringanyType""><KeyValueOfstringanyType><Key>subkey1</Key><Value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">subkey1value</Value></KeyValueOfstringanyType></Value></KeyValueOfstringanyType></ArrayOfKeyValueOfstringanyType>");
        Assert.NotNull(y);
        Assert.StrictEqual(y.Count, 1);
        Assert.True(y["Key1"] is Dictionary<string, object>);
        Assert.StrictEqual(((y["Key1"] as Dictionary<string, object>)["subkey1"]) as string, "subkey1value");
    }

    [Fact]
    public static void DCS_BuiltInTypes()
    {
        BuiltInTypes x = new BuiltInTypes
        {
            ByteArray = new byte[] { 1, 2 }
        };
        BuiltInTypes y = SerializeAndDeserialize<BuiltInTypes>(x, @"<BuiltInTypes xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ByteArray>AQI=</ByteArray></BuiltInTypes>");

        Assert.NotNull(y);
        Assert.Equal<byte>(x.ByteArray, y.ByteArray);
    }

    [Fact]
    public static void DCS_CircularLink()
    {
        CircularLinkDerived circularLinkDerived = new CircularLinkDerived(true);
        SerializeAndDeserialize<CircularLinkDerived>(circularLinkDerived, @"<CircularLinkDerived z:Id=""i1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Link z:Id=""i2""><Link z:Id=""i3""><Link z:Ref=""i1""/><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink z:Id=""i4""><Link z:Id=""i5""><Link z:Id=""i6"" i:type=""CircularLinkDerived""><Link z:Ref=""i4""/><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink i:nil=""true""/></RandomHangingLink></CircularLinkDerived>");
    }

    [Fact]
    public static void DCS_DataMemberNames()
    {
        var obj = new AppEnvironment()
        {
            ScreenDpi = 440,
            ScreenOrientation = "horizontal"
        };
        var actual = SerializeAndDeserialize(obj, "<AppEnvironment xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><screen_dpi_x0028_x_x003A_y_x0029_>440</screen_dpi_x0028_x_x003A_y_x0029_><screen_x003A_orientation>horizontal</screen_x003A_orientation></AppEnvironment>");
        Assert.StrictEqual(obj.ScreenDpi, actual.ScreenDpi);
        Assert.StrictEqual(obj.ScreenOrientation, actual.ScreenOrientation);
    }

    [Fact]
    public static void DCS_GenericBase()
    {
        var actual = SerializeAndDeserialize<GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>>(new GenericBase2<SimpleBaseDerived, SimpleBaseDerived2>(true), @"<GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2zbP0weY4 z:Id=""i1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><genericData1 z:Id=""i2""><BaseData/><DerivedData/></genericData1><genericData2 z:Id=""i3""><BaseData/><DerivedData/></genericData2></GenericBase2OfSimpleBaseDerivedSimpleBaseDerived2zbP0weY4>");

        Assert.True(actual.genericData1 is SimpleBaseDerived);
        Assert.True(actual.genericData2 is SimpleBaseDerived2);
    }

    [Fact]
    public static void DCS_GenericContainer()
    {
        SerializeAndDeserialize<GenericContainer>(new GenericContainer(true), @"<GenericContainer z:Id=""i1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><GenericData z:Id=""i2"" i:type=""GenericBaseOfSimpleBaseContainervjX03eZJ""><genericData z:Id=""i3"" i:type=""SimpleBaseContainer""><Base1 i:nil=""true""/><Base2 i:nil=""true""/></genericData></GenericData></GenericContainer>");
    }

    [Fact]
    public static void DCS_DictionaryWithVariousKeyValueTypes()
    {
        var x = new DictionaryWithVariousKeyValueTypes(true);

        var y = SerializeAndDeserialize<DictionaryWithVariousKeyValueTypes>(x, @"<DictionaryWithVariousKeyValueTypes xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><WithEnums xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfMyEnumMyEnumzbP0weY4><a:Key>Two</a:Key><a:Value>Three</a:Value></a:KeyValueOfMyEnumMyEnumzbP0weY4><a:KeyValueOfMyEnumMyEnumzbP0weY4><a:Key>One</a:Key><a:Value>One</a:Value></a:KeyValueOfMyEnumMyEnumzbP0weY4></WithEnums><WithNullables xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:Key>-32768</a:Key><a:Value>true</a:Value></a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:Key>0</a:Key><a:Value>false</a:Value></a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P><a:Key>32767</a:Key><a:Value i:nil=""true""/></a:KeyValueOfNullableOfshortNullableOfboolean_ShTDFhl_P></WithNullables><WithStructs xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4><a:Key><value>10</value></a:Key><a:Value><value>12</value></a:Value></a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4><a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4><a:Key><value>2147483647</value></a:Key><a:Value><value>-2147483648</value></a:Value></a:KeyValueOfStructNotSerializableStructNotSerializablezbP0weY4></WithStructs></DictionaryWithVariousKeyValueTypes>");

        Assert.StrictEqual(y.WithEnums[MyEnum.Two], MyEnum.Three);
        Assert.StrictEqual(y.WithEnums[MyEnum.One], MyEnum.One);
        Assert.StrictEqual(y.WithStructs[new StructNotSerializable() { value = 10 }], new StructNotSerializable() { value = 12 });
        Assert.StrictEqual(y.WithStructs[new StructNotSerializable() { value = int.MaxValue }], new StructNotSerializable() { value = int.MinValue });
        Assert.StrictEqual(y.WithNullables[Int16.MinValue], true);
        Assert.StrictEqual(y.WithNullables[0], false);
        Assert.StrictEqual(y.WithNullables[Int16.MaxValue], null);
    }

    [Fact]
    public static void DCS_TypesWithArrayOfOtherTypes()
    {
        var x = new TypeHasArrayOfASerializedAsB(true);
        var y = SerializeAndDeserialize<TypeHasArrayOfASerializedAsB>(x, @"<TypeHasArrayOfASerializedAsB xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Items><TypeA><Name>typeAValue</Name></TypeA><TypeA><Name>typeBValue</Name></TypeA></Items></TypeHasArrayOfASerializedAsB>");

        Assert.StrictEqual(x.Items[0].Name, y.Items[0].Name);
        Assert.StrictEqual(x.Items[1].Name, y.Items[1].Name);
    }

    [Fact]
    public static void DCS_WithDuplicateNames()
    {
        var x = new WithDuplicateNames(true);
        var y = SerializeAndDeserialize<WithDuplicateNames>(x, @"<WithDuplicateNames xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ClassA1 xmlns:a=""http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns1""><a:Name>Hello World! 漢 ñ</a:Name></ClassA1><ClassA2 xmlns:a=""http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns2""><a:Nombre/></ClassA2><EnumA1>two</EnumA1><EnumA2>dos</EnumA2><StructA1 xmlns:a=""http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns1""><a:Text/></StructA1><StructA2 xmlns:a=""http://schemas.datacontract.org/2004/07/DuplicateTypeNamesTest.ns2""><a:Texto/></StructA2></WithDuplicateNames>");

        Assert.StrictEqual(x.ClassA1.Name, y.ClassA1.Name);
        Assert.StrictEqual(x.StructA1, y.StructA1);
        Assert.StrictEqual(x.EnumA1, y.EnumA1);
        Assert.StrictEqual(x.EnumA2, y.EnumA2);
        Assert.StrictEqual(x.StructA2, y.StructA2);
    }

    [Fact]
    public static void DCS_XElementAsRoot()
    {
        var original = new XElement("ElementName1");
        original.SetAttributeValue(XName.Get("Attribute1"), "AttributeValue1");
        original.SetValue("Value1");
        var actual = SerializeAndDeserialize<XElement>(original, @"<ElementName1 Attribute1=""AttributeValue1"">Value1</ElementName1>");

        VerifyXElementObject(original, actual);
    }

    [Fact]
    public static void DCS_WithXElement()
    {
        var original = new WithXElement(true);
        var actual = SerializeAndDeserialize<WithXElement>(original, @"<WithXElement xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><e><ElementName1 Attribute1=""AttributeValue1"" xmlns="""">Value1</ElementName1></e></WithXElement>");

        VerifyXElementObject(original.e, actual.e);
    }

    private static void VerifyXElementObject(XElement x1, XElement x2, bool checkFirstAttribute = true)
    {
        Assert.StrictEqual(x1.Value, x2.Value);
        Assert.StrictEqual(x1.Name, x2.Name);
        if (checkFirstAttribute)
        {
            Assert.StrictEqual(x1.FirstAttribute.Name, x2.FirstAttribute.Name);
            Assert.StrictEqual(x1.FirstAttribute.Value, x2.FirstAttribute.Value);
        }
    }

    [Fact]
    public static void DCS_WithXElementWithNestedXElement()
    {
        var original = new WithXElementWithNestedXElement(true);
        var actual = SerializeAndDeserialize<WithXElementWithNestedXElement>(original, @"<WithXElementWithNestedXElement xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><e1><ElementName1 Attribute1=""AttributeValue1"" xmlns=""""><ElementName2 Attribute2=""AttributeValue2"">Value2</ElementName2></ElementName1></e1></WithXElementWithNestedXElement>");

        VerifyXElementObject(original.e1, actual.e1);
        VerifyXElementObject((XElement)original.e1.FirstNode, (XElement)actual.e1.FirstNode);
    }

    [Fact]
    public static void DCS_WithArrayOfXElement()
    {
        var original = new WithArrayOfXElement(true);
        var actual = SerializeAndDeserialize<WithArrayOfXElement>(original, @"<WithArrayOfXElement xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><a xmlns:a=""http://schemas.datacontract.org/2004/07/System.Xml.Linq""><a:XElement><item xmlns=""http://p.com/"">item0</item></a:XElement><a:XElement><item xmlns=""http://p.com/"">item1</item></a:XElement><a:XElement><item xmlns=""http://p.com/"">item2</item></a:XElement></a></WithArrayOfXElement>");

        Assert.StrictEqual(original.a.Length, actual.a.Length);
        VerifyXElementObject(original.a[0], actual.a[0], checkFirstAttribute: false);
        VerifyXElementObject(original.a[1], actual.a[1], checkFirstAttribute: false);
        VerifyXElementObject(original.a[2], actual.a[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void DCS_WithListOfXElement()
    {
        var original = new WithListOfXElement(true);
        var actual = SerializeAndDeserialize<WithListOfXElement>(original, @"<WithListOfXElement xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><list xmlns:a=""http://schemas.datacontract.org/2004/07/System.Xml.Linq""><a:XElement><item xmlns=""http://p.com/"">item0</item></a:XElement><a:XElement><item xmlns=""http://p.com/"">item1</item></a:XElement><a:XElement><item xmlns=""http://p.com/"">item2</item></a:XElement></list></WithListOfXElement>");

        Assert.StrictEqual(original.list.Count, actual.list.Count);
        VerifyXElementObject(original.list[0], actual.list[0], checkFirstAttribute: false);
        VerifyXElementObject(original.list[1], actual.list[1], checkFirstAttribute: false);
        VerifyXElementObject(original.list[2], actual.list[2], checkFirstAttribute: false);
    }

    [Fact]
    public static void DCS_DerivedTypeWithDifferentOverrides()
    {
        var x = new DerivedTypeWithDifferentOverrides() { Name1 = "Name1", Name2 = "Name2", Name3 = "Name3", Name4 = "Name4", Name5 = "Name5" };
        var y = SerializeAndDeserialize<DerivedTypeWithDifferentOverrides>(x, @"<DerivedTypeWithDifferentOverrides xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name1>Name1</Name1><Name2 i:nil=""true""/><Name3 i:nil=""true""/><Name4 i:nil=""true""/><Name5 i:nil=""true""/><Name2>Name2</Name2><Name3>Name3</Name3><Name5>Name5</Name5></DerivedTypeWithDifferentOverrides>");

        Assert.StrictEqual(x.Name1, y.Name1);
        Assert.StrictEqual(x.Name2, y.Name2);
        Assert.StrictEqual(x.Name3, y.Name3);
        Assert.Null(y.Name4);
        Assert.StrictEqual(x.Name5, y.Name5);
    }

    [Fact]
    public static void DCS_TypeNamesWithSpecialCharacters()
    {
        var x = new __TypeNameWithSpecialCharacters漢ñ() { PropertyNameWithSpecialCharacters漢ñ = "Test" };
        var y = SerializeAndDeserialize<__TypeNameWithSpecialCharacters漢ñ>(x, @"<__TypeNameWithSpecialCharacters漢ñ xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><PropertyNameWithSpecialCharacters漢ñ>Test</PropertyNameWithSpecialCharacters漢ñ></__TypeNameWithSpecialCharacters漢ñ>");

        Assert.StrictEqual(x.PropertyNameWithSpecialCharacters漢ñ, y.PropertyNameWithSpecialCharacters漢ñ);
    }

    [Fact]
    public static void DCS_JaggedArrayAsRoot()
    {
        int[][] jaggedIntegerArray = new int[][] { new int[] { 1, 3, 5, 7, 9 }, new int[] { 0, 2, 4, 6 }, new int[] { 11, 22 } };
        var actualJaggedIntegerArray = SerializeAndDeserialize<int[][]>(jaggedIntegerArray, @"<ArrayOfArrayOfint xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ArrayOfint><int>1</int><int>3</int><int>5</int><int>7</int><int>9</int></ArrayOfint><ArrayOfint><int>0</int><int>2</int><int>4</int><int>6</int></ArrayOfint><ArrayOfint><int>11</int><int>22</int></ArrayOfint></ArrayOfArrayOfint>");

        Assert.Equal<int>(jaggedIntegerArray[0], actualJaggedIntegerArray[0]);
        Assert.Equal<int>(jaggedIntegerArray[1], actualJaggedIntegerArray[1]);
        Assert.Equal<int>(jaggedIntegerArray[2], actualJaggedIntegerArray[2]);

        string[][] jaggedStringArray = new string[][] { new string[] { "1", "3", "5", "7", "9" }, new string[] { "0", "2", "4", "6" }, new string[] { "11", "22" } };
        var actualJaggedStringArray = SerializeAndDeserialize<string[][]>(jaggedStringArray, @"<ArrayOfArrayOfstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ArrayOfstring><string>1</string><string>3</string><string>5</string><string>7</string><string>9</string></ArrayOfstring><ArrayOfstring><string>0</string><string>2</string><string>4</string><string>6</string></ArrayOfstring><ArrayOfstring><string>11</string><string>22</string></ArrayOfstring></ArrayOfArrayOfstring>");

        Assert.Equal<string>(jaggedStringArray[0], actualJaggedStringArray[0]);
        Assert.Equal<string>(jaggedStringArray[1], actualJaggedStringArray[1]);
        Assert.Equal<string>(jaggedStringArray[2], actualJaggedStringArray[2]);

        object[] objectArray = new object[] { 1, 1.0F, 1.0, "string", Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"), new DateTime(2013, 1, 2) };
        var actualObjectArray = SerializeAndDeserialize<object[]>(objectArray, @"<ArrayOfanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><anyType i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">1</anyType><anyType i:type=""a:float"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">1</anyType><anyType i:type=""a:double"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">1</anyType><anyType i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">string</anyType><anyType i:type=""a:guid"" xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/"">2054fd3e-e118-476a-9962-1a882be51860</anyType><anyType i:type=""a:dateTime"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">2013-01-02T00:00:00</anyType></ArrayOfanyType>");

        Assert.True(1 == (int)actualObjectArray[0]);
        Assert.True(1.0F == (float)actualObjectArray[1]);
        Assert.True(1.0 == (double)actualObjectArray[2]);
        Assert.True("string" == (string)actualObjectArray[3]);
        Assert.True(Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860") == (Guid)actualObjectArray[4]);
        Assert.True(new DateTime(2013, 1, 2) == (DateTime)actualObjectArray[5]);

        int[][][] jaggedIntegerArray2 = new int[][][] { new int[][] { new int[] { 1 }, new int[] { 3 } }, new int[][] { new int[] { 0 } }, new int[][] { new int[] { } } };
        var actualJaggedIntegerArray2 = SerializeAndDeserialize<int[][][]>(jaggedIntegerArray2, @"<ArrayOfArrayOfArrayOfint xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ArrayOfArrayOfint><ArrayOfint><int>1</int></ArrayOfint><ArrayOfint><int>3</int></ArrayOfint></ArrayOfArrayOfint><ArrayOfArrayOfint><ArrayOfint><int>0</int></ArrayOfint></ArrayOfArrayOfint><ArrayOfArrayOfint><ArrayOfint/></ArrayOfArrayOfint></ArrayOfArrayOfArrayOfint>");

        Assert.True(actualJaggedIntegerArray2.Length == 3);
        Assert.True(actualJaggedIntegerArray2[0][0][0] == 1);
        Assert.True(actualJaggedIntegerArray2[0][1][0] == 3);
        Assert.True(actualJaggedIntegerArray2[1][0][0] == 0);
        Assert.True(actualJaggedIntegerArray2[2][0].Length == 0);
    }

    [Fact]
    public static void DCS_MyDataContractResolver()
    {
        var myresolver = new MyResolver();
        var settings = new DataContractSerializerSettings() { DataContractResolver = myresolver, KnownTypes = new Type[] { typeof(MyOtherType) } };
        var input = new MyType() { Value = new MyOtherType() { Str = "Hello World" } };
        var output = SerializeAndDeserialize<MyType>(input, @"<MyType xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Value i:type=""MyOtherType""><Str>Hello World</Str></Value></MyType>", settings);

        Assert.True(myresolver.ResolveNameInvoked, "myresolver.ResolveNameInvoked is false");
        Assert.True(myresolver.TryResolveTypeInvoked, "myresolver.TryResolveTypeInvoked is false");
        Assert.True(input.OnSerializingMethodInvoked, "input.OnSerializingMethodInvoked is false");
        Assert.True(input.OnSerializedMethodInvoked, "input.OnSerializedMethodInvoked is false");
        Assert.True(output.OnDeserializingMethodInvoked, "output.OnDeserializingMethodInvoked is false");
        Assert.True(output.OnDeserializedMethodInvoked, "output.OnDeserializedMethodInvoked is false");
    }

    [Fact]
    public static void DCS_WriteObject_Use_DataContractResolver()
    {
        var settings = new DataContractSerializerSettings() { DataContractResolver = null, KnownTypes = new Type[] { typeof(MyOtherType) } };
        var dcs = new DataContractSerializer(typeof(MyType), settings);

        var value = new MyType() { Value = new MyOtherType() { Str = "Hello World" } };
        using (var ms = new MemoryStream())
        {
            var myresolver = new MyResolver();
            var xmlWriter = XmlDictionaryWriter.CreateTextWriter(ms);
            dcs.WriteObject(xmlWriter, value, myresolver);

            xmlWriter.Flush();
            ms.Position = 0;

            Assert.True(myresolver.ResolveNameInvoked, "myresolver.ResolveNameInvoked was false");
            Assert.True(myresolver.TryResolveTypeInvoked, "myresolver.TryResolveTypeInvoked was false");

            ms.Position = 0;
            myresolver = new MyResolver();
            var xmlReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max);
            MyType deserialized = (MyType)dcs.ReadObject(xmlReader, false, myresolver);

            Assert.NotNull(deserialized);
            Assert.True(deserialized.Value is MyOtherType, "deserialized.Value was not of MyOtherType.");
            Assert.Equal(((MyOtherType)value.Value).Str, ((MyOtherType)deserialized.Value).Str);

            Assert.True(myresolver.ResolveNameInvoked, "myresolver.ResolveNameInvoked was false");
        }
    }

    [Fact]
    public static void DCS_DataContractResolver_Property()
    {
        var myresolver = new MyResolver();
        var settings = new DataContractSerializerSettings() { DataContractResolver = myresolver };
        var dcs = new DataContractSerializer(typeof(MyType), settings);
        Assert.Equal(myresolver, dcs.DataContractResolver);
    }

    [Fact]
    public static void DCS_EnumerableStruct()
    {
        var original = new EnumerableStruct();
        original.Add("a");
        original.Add("b");

        var actual = SerializeAndDeserialize<EnumerableStruct>(original, @"<ArrayOfstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><string i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">a</string><string i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">b</string></ArrayOfstring>");

        Assert.Equal((IEnumerable<string>)actual, (IEnumerable<string>)original);
    }

    [Fact]
    public static void DCS_EnumerableCollection()
    {
        var original = new EnumerableCollection();
        original.Add(new DateTime(100, DateTimeKind.Utc));
        original.Add(new DateTime(200, DateTimeKind.Utc));
        original.Add(new DateTime(300, DateTimeKind.Utc));

        var actual = SerializeAndDeserialize<EnumerableCollection>(original, @"<ArrayOfdateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><dateTime i:type=""a:dateTime"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00.00001Z</dateTime><dateTime i:type=""a:dateTime"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00.00002Z</dateTime><dateTime i:type=""a:dateTime"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00.00003Z</dateTime></ArrayOfdateTime>");

        Assert.Equal((IEnumerable<DateTime>)actual, (IEnumerable<DateTime>)original);
    }

    [Fact]
    public static void DCS_BaseClassAndDerivedClassWithSameProperty()
    {
        var value = new DerivedClassWithSameProperty() { DateTimeProperty = new DateTime(100), IntProperty = 5, StringProperty = "TestString", ListProperty = new List<string>() };
        value.ListProperty.AddRange(new string[] { "one", "two", "three" });
        var actual = SerializeAndDeserialize<DerivedClassWithSameProperty>(value, @"<DerivedClassWithSameProperty xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><DateTimeProperty>0001-01-01T00:00:00</DateTimeProperty><IntProperty>0</IntProperty><ListProperty i:nil=""true"" xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/><StringProperty i:nil=""true""/><DateTimeProperty>0001-01-01T00:00:00.00001</DateTimeProperty><IntProperty>5</IntProperty><ListProperty xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>one</a:string><a:string>two</a:string><a:string>three</a:string></ListProperty><StringProperty>TestString</StringProperty></DerivedClassWithSameProperty>");

        Assert.StrictEqual(value.DateTimeProperty, actual.DateTimeProperty);
        Assert.StrictEqual(value.IntProperty, actual.IntProperty);
        Assert.StrictEqual(value.StringProperty, actual.StringProperty);
        Assert.NotNull(actual.ListProperty);
        Assert.True(value.ListProperty.Count == actual.ListProperty.Count);
        Assert.StrictEqual("one", actual.ListProperty[0]);
        Assert.StrictEqual("two", actual.ListProperty[1]);
        Assert.StrictEqual("three", actual.ListProperty[2]);
    }

    [Fact]
    public static void DCS_ContainsLinkedList()
    {
        var value = new ContainsLinkedList(true);

        SerializeAndDeserialize<ContainsLinkedList>(value, @"<ContainsLinkedList xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Data><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef><SimpleDCWithRef><Data><Data>23:59:59</Data></Data><RefData><Data>23:59:59</Data></RefData></SimpleDCWithRef></Data></ContainsLinkedList>");
    }

    [Fact]
    public static void DCS_SimpleCollectionDataContract()
    {
        var value = new SimpleCDC(true);
        var actual = SerializeAndDeserialize<SimpleCDC>(value, @"<SimpleCDC xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Item>One</Item><Item>Two</Item><Item>Three</Item></SimpleCDC>");

        Assert.True(actual.Count == 3);
        Assert.True(actual.Contains("One"));
        Assert.True(actual.Contains("Two"));
        Assert.True(actual.Contains("Three"));
    }

    [Fact]
    public static void DCS_MyDerivedCollectionContainer()
    {
        var value = new MyDerivedCollectionContainer();
        value.Items.AddLast("One");
        value.Items.AddLast("Two");
        value.Items.AddLast("Three");
        SerializeAndDeserialize<MyDerivedCollectionContainer>(value, @"<MyDerivedCollectionContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Items><string>One</string><string>Two</string><string>Three</string></Items></MyDerivedCollectionContainer>");
    }

    [Fact]
    public static void DCS_EnumFlags()
    {
        EnumFlags value1 = EnumFlags.One | EnumFlags.Four;
        var value2 = SerializeAndDeserialize<EnumFlags>(value1, @"<EnumFlags xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"">One Four</EnumFlags>");
        Assert.StrictEqual(value1, value2);
    }

    [Fact]
    public static void DCS_SerializeClassThatImplementsInteface()
    {
        ClassImplementsInterface value = new ClassImplementsInterface() { ClassID = "ClassID", DisplayName = "DisplayName", Id = "Id", IsLoaded = true };
        var actual = SerializeAndDeserialize<ClassImplementsInterface>(value, @"<ClassImplementsInterface xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><DisplayName>DisplayName</DisplayName><Id>Id</Id></ClassImplementsInterface>");


        Assert.StrictEqual(value.DisplayName, actual.DisplayName);
        Assert.StrictEqual(value.Id, actual.Id);
    }

    [Fact]
    public static void DCS_Nullables()
    {
        // Arrange
        var baseline = @"<WithNullables xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Optional>Option1</Optional><OptionalInt>42</OptionalInt><Optionull i:nil=""true""/><OptionullInt i:nil=""true""/><Struct1><A>1</A><B>2</B></Struct1><Struct2 i:nil=""true""/></WithNullables>";

        var item = new WithNullables()
        {
            Optional = IntEnum.Option1,
            OptionalInt = 42,
            Struct1 = new SomeStruct { A = 1, B = 2 }
        };

        // Act
        var actual = SerializeAndDeserialize(item, baseline);

        // Assert
        Assert.StrictEqual(item.OptionalInt, actual.OptionalInt);
        Assert.StrictEqual(item.Optional, actual.Optional);
        Assert.StrictEqual(item.Optionull, actual.Optionull);
        Assert.StrictEqual(item.OptionullInt, actual.OptionullInt);
        Assert.Null(actual.Struct2);
        Assert.StrictEqual(item.Struct1.Value.A, actual.Struct1.Value.A);
        Assert.StrictEqual(item.Struct1.Value.B, actual.Struct1.Value.B);
    }

    [Fact]
    public static void DCS_SimpleStructWithProperties()
    {
        SimpleStructWithProperties x = new SimpleStructWithProperties() { Num = 1, Text = "Foo" };
        var y = SerializeAndDeserialize(x, "<SimpleStructWithProperties xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Num>1</Num><Text>Foo</Text></SimpleStructWithProperties>");

        Assert.True(x.Num == y.Num, "x.Num != y.Num");
        Assert.True(x.Text == y.Text, "x.Text != y.Text");
    }

    [Fact]
    public static void DCS_InternalTypeSerialization()
    {
        var value = new InternalType() { InternalProperty = 12 };
        var deserializedValue = SerializeAndDeserialize<InternalType>(value, @"<InternalType xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><InternalProperty>12</InternalProperty><PrivateProperty>100</PrivateProperty></InternalType>");
        Assert.StrictEqual(deserializedValue.InternalProperty, value.InternalProperty);
        Assert.StrictEqual(deserializedValue.GetPrivatePropertyValue(), value.GetPrivatePropertyValue());
    }

    [Fact]
    public static void DCS_PrivateTypeSerialization()
    {
        var value = new PrivateType();
        var deserializedValue = SerializeAndDeserialize<PrivateType>(value, @"<DataContractSerializerTests.PrivateType xmlns=""http://schemas.datacontract.org/2004/07/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><InternalProperty>1</InternalProperty><PrivateProperty>2</PrivateProperty></DataContractSerializerTests.PrivateType>");
        Assert.StrictEqual(deserializedValue.GetInternalPropertyValue(), value.GetInternalPropertyValue());
        Assert.StrictEqual(deserializedValue.GetPrivatePropertyValue(), value.GetPrivatePropertyValue());
    }

    #region private type has to be in with in the class
    [DataContract]
    private class PrivateType
    {
        public PrivateType()
        {
            InternalProperty = 1;
            PrivateProperty = 2;
        }

        [DataMember]
        internal int InternalProperty { get; set; }

        [DataMember]
        private int PrivateProperty { get; set; }

        public int GetInternalPropertyValue()
        {
            return InternalProperty;
        }

        public int GetPrivatePropertyValue()
        {
            return PrivateProperty;
        }
    }
    #endregion

    [Fact]
    public static void DCS_RootNameAndNamespaceThroughConstructorAsString()
    {
        //Constructor# 3
        var obj = new MyOtherType() { Str = "Hello" };
        Func<DataContractSerializer> serializerFactory = () => new DataContractSerializer(typeof(MyOtherType), "ChangedRoot", "http://changedNamespace");
        string baselineXml = @"<ChangedRoot xmlns=""http://changedNamespace"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><a:Str>Hello</a:Str></ChangedRoot>";
        var result = SerializeAndDeserialize(obj, baselineXml, serializerFactory: serializerFactory);
        Assert.StrictEqual(result.Str, "Hello");
    }

    [Fact]
    public static void DCS_RootNameAndNamespaceThroughConstructorAsXmlDictionary()
    {
        //Constructor# 4
        var xmlDictionary = new XmlDictionary();
        var obj = new MyOtherType() { Str = "Hello" };
        Func<DataContractSerializer> serializerFactory = () => new DataContractSerializer(typeof(MyOtherType), xmlDictionary.Add("ChangedRoot"), xmlDictionary.Add("http://changedNamespace"));
        string baselineXml = @"<ChangedRoot xmlns=""http://changedNamespace"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><a:Str>Hello</a:Str></ChangedRoot>";
        var result = SerializeAndDeserialize(obj, baselineXml, serializerFactory: serializerFactory);
        Assert.StrictEqual(result.Str, "Hello");
    }

    [Fact]
    public static void DCS_KnownTypesThroughConstructor()
    {
        //Constructor# 5
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
            @"<KnownTypesThroughConstructor xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><EnumValue i:type=""MyEnum"">One</EnumValue><SimpleTypeValue i:type=""SimpleKnownTypeValue""><StrProperty>PropertyValue</StrProperty></SimpleTypeValue></KnownTypesThroughConstructor>",
            null, () => { return new DataContractSerializer(typeof(KnownTypesThroughConstructor), new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_DuplicatedKnownTypesWithAdapterThroughConstructor()
    {
        //Constructor# 5  
        DateTimeOffset dto = new DateTimeOffset(new DateTime(2015, 11, 11), new TimeSpan(0, 0, 0));
        var value = new KnownTypesThroughConstructor() { EnumValue = dto, SimpleTypeValue = dto };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value, 
            @"<KnownTypesThroughConstructor xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><EnumValue i:type=""a:DateTimeOffset"" xmlns:a=""http://schemas.datacontract.org/2004/07/System""><a:DateTime>2015-11-11T00:00:00Z</a:DateTime><a:OffsetMinutes>0</a:OffsetMinutes></EnumValue><SimpleTypeValue i:type=""a:DateTimeOffset"" xmlns:a=""http://schemas.datacontract.org/2004/07/System""><a:DateTime>2015-11-11T00:00:00Z</a:DateTime><a:OffsetMinutes>0</a:OffsetMinutes></SimpleTypeValue></KnownTypesThroughConstructor>",
            null, () => { return new DataContractSerializer(typeof(KnownTypesThroughConstructor), new Type[] { typeof(DateTimeOffset), typeof(DateTimeOffset) }); });

        Assert.StrictEqual((DateTimeOffset)value.EnumValue, (DateTimeOffset)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is DateTimeOffset);
        Assert.StrictEqual((DateTimeOffset)actual.SimpleTypeValue, (DateTimeOffset)actual.SimpleTypeValue);
    }

    [Fact]
    public static void DCS_KnownTypesThroughSettings()
    {
        //Constructor# 2.1
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
            @"<KnownTypesThroughConstructor xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><EnumValue i:type=""MyEnum"">One</EnumValue><SimpleTypeValue i:type=""SimpleKnownTypeValue""><StrProperty>PropertyValue</StrProperty></SimpleTypeValue></KnownTypesThroughConstructor>",
            new DataContractSerializerSettings() { KnownTypes = new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) } });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_RootNameNamespaceAndKnownTypesThroughConstructorAsStrings()
    {
        //Constructor# 6
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
            @"<ChangedRoot xmlns=""http://changedNamespace"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><a:EnumValue i:type=""a:MyEnum"">One</a:EnumValue><a:SimpleTypeValue i:type=""a:SimpleKnownTypeValue""><a:StrProperty>PropertyValue</a:StrProperty></a:SimpleTypeValue></ChangedRoot>",
            null, () => { return new DataContractSerializer(typeof(KnownTypesThroughConstructor), "ChangedRoot", "http://changedNamespace", new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_RootNameNamespaceAndKnownTypesThroughConstructorAsXmlDictionary()
    {
        //Constructor# 7
        var xmlDictionary = new XmlDictionary();
        var value = new KnownTypesThroughConstructor() { EnumValue = MyEnum.One, SimpleTypeValue = new SimpleKnownTypeValue() { StrProperty = "PropertyValue" } };
        var actual = SerializeAndDeserialize<KnownTypesThroughConstructor>(value,
            @"<ChangedRoot xmlns=""http://changedNamespace"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><a:EnumValue i:type=""a:MyEnum"">One</a:EnumValue><a:SimpleTypeValue i:type=""a:SimpleKnownTypeValue""><a:StrProperty>PropertyValue</a:StrProperty></a:SimpleTypeValue></ChangedRoot>",
            null, () => { return new DataContractSerializer(typeof(KnownTypesThroughConstructor), xmlDictionary.Add("ChangedRoot"), xmlDictionary.Add("http://changedNamespace"), new Type[] { typeof(MyEnum), typeof(SimpleKnownTypeValue) }); });

        Assert.StrictEqual((MyEnum)value.EnumValue, (MyEnum)actual.EnumValue);
        Assert.True(actual.SimpleTypeValue is SimpleKnownTypeValue);
        Assert.StrictEqual(((SimpleKnownTypeValue)actual.SimpleTypeValue).StrProperty, "PropertyValue");
    }

    [Fact]
    public static void DCS_ExceptionObject()
    {
        var value = new ArgumentException("Test Exception");
        var actual = SerializeAndDeserialize<ArgumentException>(value, @"<ArgumentException xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:x=""http://www.w3.org/2001/XMLSchema""><ClassName i:type=""x:string"" xmlns="""">System.ArgumentException</ClassName><Message i:type=""x:string"" xmlns="""">Test Exception</Message><Data i:nil=""true"" xmlns=""""/><InnerException i:nil=""true"" xmlns=""""/><HelpURL i:nil=""true"" xmlns=""""/><StackTraceString i:nil=""true"" xmlns=""""/><RemoteStackTraceString i:nil=""true"" xmlns=""""/><RemoteStackIndex i:type=""x:int"" xmlns="""">0</RemoteStackIndex><ExceptionMethod i:nil=""true"" xmlns=""""/><HResult i:type=""x:int"" xmlns="""">-2147024809</HResult><Source i:nil=""true"" xmlns=""""/><WatsonBuckets i:nil=""true"" xmlns=""""/><ParamName i:nil=""true"" xmlns=""""/></ArgumentException>");

        Assert.StrictEqual(value.Message, actual.Message);
        Assert.StrictEqual(value.ParamName, actual.ParamName);
        Assert.StrictEqual(value.Source, actual.Source);
        Assert.StrictEqual(value.StackTrace, actual.StackTrace);
        Assert.StrictEqual(value.HResult, actual.HResult);
        Assert.StrictEqual(value.HelpLink, actual.HelpLink);
    }

    [Fact]
    public static void DCS_ArgumentExceptionObject()
    {
        var value = new ArgumentException("Test Exception", "paramName");
        var actual = SerializeAndDeserialize<ArgumentException>(value, @"<ArgumentException xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:x=""http://www.w3.org/2001/XMLSchema""><ClassName i:type=""x:string"" xmlns="""">System.ArgumentException</ClassName><Message i:type=""x:string"" xmlns="""">Test Exception</Message><Data i:nil=""true"" xmlns=""""/><InnerException i:nil=""true"" xmlns=""""/><HelpURL i:nil=""true"" xmlns=""""/><StackTraceString i:nil=""true"" xmlns=""""/><RemoteStackTraceString i:nil=""true"" xmlns=""""/><RemoteStackIndex i:type=""x:int"" xmlns="""">0</RemoteStackIndex><ExceptionMethod i:nil=""true"" xmlns=""""/><HResult i:type=""x:int"" xmlns="""">-2147024809</HResult><Source i:nil=""true"" xmlns=""""/><WatsonBuckets i:nil=""true"" xmlns=""""/><ParamName i:type=""x:string"" xmlns="""">paramName</ParamName></ArgumentException>");

        Assert.StrictEqual(value.Message, actual.Message);
        Assert.StrictEqual(value.ParamName, actual.ParamName);
        Assert.StrictEqual(value.Source, actual.Source);
        Assert.StrictEqual(value.StackTrace, actual.StackTrace);
        Assert.StrictEqual(value.HResult, actual.HResult);
        Assert.StrictEqual(value.HelpLink, actual.HelpLink);
    }

    [Fact]
    public static void DCS_ExceptionMessageWithSpecialChars()
    {
        var value = new ArgumentException("Test Exception<>&'\"");
        var actual = SerializeAndDeserialize<ArgumentException>(value, @"<ArgumentException xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:x=""http://www.w3.org/2001/XMLSchema""><ClassName i:type=""x:string"" xmlns="""">System.ArgumentException</ClassName><Message i:type=""x:string"" xmlns="""">Test Exception&lt;&gt;&amp;'""</Message><Data i:nil=""true"" xmlns=""""/><InnerException i:nil=""true"" xmlns=""""/><HelpURL i:nil=""true"" xmlns=""""/><StackTraceString i:nil=""true"" xmlns=""""/><RemoteStackTraceString i:nil=""true"" xmlns=""""/><RemoteStackIndex i:type=""x:int"" xmlns="""">0</RemoteStackIndex><ExceptionMethod i:nil=""true"" xmlns=""""/><HResult i:type=""x:int"" xmlns="""">-2147024809</HResult><Source i:nil=""true"" xmlns=""""/><WatsonBuckets i:nil=""true"" xmlns=""""/><ParamName i:nil=""true"" xmlns=""""/></ArgumentException>");

        Assert.StrictEqual(value.Message, actual.Message);
        Assert.StrictEqual(value.ParamName, actual.ParamName);
        Assert.StrictEqual(value.Source, actual.Source);
        Assert.StrictEqual(value.StackTrace, actual.StackTrace);
        Assert.StrictEqual(value.HResult, actual.HResult);
        Assert.StrictEqual(value.HelpLink, actual.HelpLink);
    }

    [Fact]
    public static void DCS_InnerExceptionMessageWithSpecialChars()
    {
        var value = new Exception("", new Exception("Test Exception<>&'\""));
        var actual = SerializeAndDeserialize<Exception>(value, @"<Exception xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:x=""http://www.w3.org/2001/XMLSchema""><ClassName i:type=""x:string"" xmlns="""">System.Exception</ClassName><Message i:type=""x:string"" xmlns=""""/><Data i:nil=""true"" xmlns=""""/><InnerException i:type=""a:Exception"" xmlns="""" xmlns:a=""http://schemas.datacontract.org/2004/07/System""><ClassName i:type=""x:string"">System.Exception</ClassName><Message i:type=""x:string"">Test Exception&lt;&gt;&amp;'""</Message><Data i:nil=""true""/><InnerException i:nil=""true""/><HelpURL i:nil=""true""/><StackTraceString i:nil=""true""/><RemoteStackTraceString i:nil=""true""/><RemoteStackIndex i:type=""x:int"">0</RemoteStackIndex><ExceptionMethod i:nil=""true""/><HResult i:type=""x:int"">-2146233088</HResult><Source i:nil=""true""/><WatsonBuckets i:nil=""true""/></InnerException><HelpURL i:nil=""true"" xmlns=""""/><StackTraceString i:nil=""true"" xmlns=""""/><RemoteStackTraceString i:nil=""true"" xmlns=""""/><RemoteStackIndex i:type=""x:int"" xmlns="""">0</RemoteStackIndex><ExceptionMethod i:nil=""true"" xmlns=""""/><HResult i:type=""x:int"" xmlns="""">-2146233088</HResult><Source i:nil=""true"" xmlns=""""/><WatsonBuckets i:nil=""true"" xmlns=""""/></Exception>");

        Assert.StrictEqual(value.Message, actual.Message);
        Assert.StrictEqual(value.Source, actual.Source);
        Assert.StrictEqual(value.StackTrace, actual.StackTrace);
        Assert.StrictEqual(value.HResult, actual.HResult);
        Assert.StrictEqual(value.HelpLink, actual.HelpLink);

        Assert.StrictEqual(value.InnerException.Message, actual.InnerException.Message);
        Assert.StrictEqual(value.InnerException.Source, actual.InnerException.Source);
        Assert.StrictEqual(value.InnerException.StackTrace, actual.InnerException.StackTrace);
        Assert.StrictEqual(value.InnerException.HResult, actual.InnerException.HResult);
        Assert.StrictEqual(value.InnerException.HelpLink, actual.InnerException.HelpLink);
    }

    [Fact]
    public static void DCS_TypeWithUriTypeProperty()
    {
        var value = new TypeWithUriTypeProperty() { ConfigUri = new Uri("http://www.bing.com") };

        var actual = SerializeAndDeserialize(value, @"<TypeWithUriTypeProperty xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ConfigUri>http://www.bing.com/</ConfigUri></TypeWithUriTypeProperty>");

        Assert.StrictEqual(value.ConfigUri, actual.ConfigUri);
    }

    [Fact]
    public static void DCS_TypeWithDatetimeOffsetTypeProperty()
    {
        var value = new TypeWithDateTimeOffsetTypeProperty() { ModifiedTime = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc)) };
        var actual = SerializeAndDeserialize(value, @"<TypeWithDateTimeOffsetTypeProperty xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ModifiedTime xmlns:a=""http://schemas.datacontract.org/2004/07/System""><a:DateTime>2013-01-02T03:04:05.006Z</a:DateTime><a:OffsetMinutes>0</a:OffsetMinutes></ModifiedTime></TypeWithDateTimeOffsetTypeProperty>");
        Assert.StrictEqual(value.ModifiedTime, actual.ModifiedTime);

        // Assume that UTC offset doesn't change more often than once in the day 2013-01-02
        // DO NOT USE TimeZoneInfo.Local.BaseUtcOffset !
        var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(new DateTime(2013, 1, 2)).TotalMinutes;
        // Adding offsetMinutes to ModifiedTime property so the DateTime component in serialized strings are time-zone independent
        value = new TypeWithDateTimeOffsetTypeProperty() { ModifiedTime = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6).AddMinutes(offsetMinutes)) };
        actual = SerializeAndDeserialize(value, string.Format(@"<TypeWithDateTimeOffsetTypeProperty xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ModifiedTime xmlns:a=""http://schemas.datacontract.org/2004/07/System""><a:DateTime>2013-01-02T03:04:05.006Z</a:DateTime><a:OffsetMinutes>{0}</a:OffsetMinutes></ModifiedTime></TypeWithDateTimeOffsetTypeProperty>", offsetMinutes));
        Assert.StrictEqual(value.ModifiedTime, actual.ModifiedTime);

        value = new TypeWithDateTimeOffsetTypeProperty() { ModifiedTime = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Local).AddMinutes(offsetMinutes)) };
        actual = SerializeAndDeserialize(value, string.Format(@"<TypeWithDateTimeOffsetTypeProperty xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ModifiedTime xmlns:a=""http://schemas.datacontract.org/2004/07/System""><a:DateTime>2013-01-02T03:04:05.006Z</a:DateTime><a:OffsetMinutes>{0}</a:OffsetMinutes></ModifiedTime></TypeWithDateTimeOffsetTypeProperty>", offsetMinutes));
        Assert.StrictEqual(value.ModifiedTime, actual.ModifiedTime);
    }

    [Fact]
    public static void DCS_Tuple()
    {
        DCS_Tuple1();
        DCS_Tuple2();
        DCS_Tuple3();
        DCS_Tuple4();
        DCS_Tuple5();
        DCS_Tuple6();
        DCS_Tuple7();
        DCS_Tuple8();
    }

    private static void DCS_Tuple1()
    {
        Tuple<int> value = new Tuple<int>(1);
        var deserializedValue = SerializeAndDeserialize<Tuple<int>>(value, @"<TupleOfint xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1></TupleOfint>");
        Assert.StrictEqual<Tuple<int>>(value, deserializedValue);
    }

    private static void DCS_Tuple2()
    {
        Tuple<int, int> value = new Tuple<int, int>(1, 2);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int>>(value, @"<TupleOfintint xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1><m_Item2>2</m_Item2></TupleOfintint>");
        Assert.StrictEqual<Tuple<int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple3()
    {
        Tuple<int, int, int> value = new Tuple<int, int, int>(1, 2, 3);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int>>(value, @"<TupleOfintintint xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3></TupleOfintintint>");
        Assert.StrictEqual<Tuple<int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple4()
    {
        Tuple<int, int, int, int> value = new Tuple<int, int, int, int>(1, 2, 3, 4);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int>>(value, @"<TupleOfintintintint xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4></TupleOfintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple5()
    {
        Tuple<int, int, int, int, int> value = new Tuple<int, int, int, int, int>(1, 2, 3, 4, 5);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int>>(value, @"<TupleOfintintintintint xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5></TupleOfintintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple6()
    {
        Tuple<int, int, int, int, int, int> value = new Tuple<int, int, int, int, int, int>(1, 2, 3, 4, 5, 6);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int>>(value, @"<TupleOfintintintintintint xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5><m_Item6>6</m_Item6></TupleOfintintintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple7()
    {
        Tuple<int, int, int, int, int, int, int> value = new Tuple<int, int, int, int, int, int, int>(1, 2, 3, 4, 5, 6, 7);
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int, int>>(value, @"<TupleOfintintintintintintint xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5><m_Item6>6</m_Item6><m_Item7>7</m_Item7></TupleOfintintintintintintint>");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int, int>>(value, deserializedValue);
    }

    private static void DCS_Tuple8()
    {
        Tuple<int, int, int, int, int, int, int, Tuple<int>> value = new Tuple<int, int, int, int, int, int, int, Tuple<int>>(1, 2, 3, 4, 5, 6, 7, new Tuple<int>(8));
        var deserializedValue = SerializeAndDeserialize<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(value, @"<TupleOfintintintintintintintTupleOfintcd6ORBnm xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><m_Item1>1</m_Item1><m_Item2>2</m_Item2><m_Item3>3</m_Item3><m_Item4>4</m_Item4><m_Item5>5</m_Item5><m_Item6>6</m_Item6><m_Item7>7</m_Item7><m_Rest><m_Item1>8</m_Item1></m_Rest></TupleOfintintintintintintintTupleOfintcd6ORBnm>");
        Assert.StrictEqual<Tuple<int, int, int, int, int, int, int, Tuple<int>>>(value, deserializedValue);
    }

    [Fact]
    public static void DCS_GenericQueue()
    {
        Queue<int> value = new Queue<int>();
        value.Enqueue(1);
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Queue<int>>(value, @"<QueueOfint xmlns=""http://schemas.datacontract.org/2004/07/System.Collections.Generic"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_array xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>1</a:int><a:int>0</a:int><a:int>0</a:int><a:int>0</a:int></_array><_head>0</_head><_size>1</_size><_tail>1</_tail><_version>2</_version></QueueOfint>");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
    }

    [Fact]
    public static void DCS_GenericStack()
    {
        var value = new Stack<int>();
        value.Push(123);
        value.Push(456);
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Stack<int>>(value, @"<StackOfint xmlns=""http://schemas.datacontract.org/2004/07/System.Collections.Generic"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_array xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:int>123</a:int><a:int>456</a:int><a:int>0</a:int><a:int>0</a:int></_array><_size>2</_size><_version>2</_version></StackOfint>");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
        Assert.StrictEqual(a1[1], a2[1]);
    }

    [Fact]
    public static void DCS_Queue()
    {
        var value = new Queue();
        value.Enqueue(123);
        value.Enqueue("Foo");
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Queue>(value, @"<Queue xmlns=""http://schemas.datacontract.org/2004/07/System.Collections"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_array xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">123</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">Foo</a:anyType><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/></_array><_growFactor>200</_growFactor><_head>0</_head><_size>2</_size><_tail>2</_tail><_version>2</_version></Queue>");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
    }

    [Fact]
    public static void DCS_Stack()
    {
        var value = new Stack();
        value.Push(123);
        value.Push("Foo");
        object syncRoot = ((ICollection)value).SyncRoot;
        var deserializedValue = SerializeAndDeserialize<Stack>(value, @"<Stack xmlns=""http://schemas.datacontract.org/2004/07/System.Collections"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_array xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">123</a:anyType><a:anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">Foo</a:anyType><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/><a:anyType i:nil=""true""/></_array><_size>2</_size><_version>2</_version></Stack>");
        var a1 = value.ToArray();
        var a2 = deserializedValue.ToArray();
        Assert.StrictEqual(a1.Length, a2.Length);
        Assert.StrictEqual(a1[0], a2[0]);
        Assert.StrictEqual(a1[1], a2[1]);
    }

    [Fact]
    public static void DCS_SortedList()
    {
        var value = new SortedList();
        value.Add(456, "Foo");
        value.Add(123, "Bar");
        var deserializedValue = SerializeAndDeserialize<SortedList>(value, @"<ArrayOfKeyValueOfanyTypeanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">123</Key><Value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">Bar</Value></KeyValueOfanyTypeanyType><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">456</Key><Value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">Foo</Value></KeyValueOfanyTypeanyType></ArrayOfKeyValueOfanyTypeanyType>");
        Assert.StrictEqual(value.Count, deserializedValue.Count);
        Assert.StrictEqual(value[0], deserializedValue[0]);
        Assert.StrictEqual(value[1], deserializedValue[1]);
    }

    [Fact]
    public static void DCS_SystemVersion()
    {
        Version value = new Version(1, 2, 3, 4);
        var deserializedValue = SerializeAndDeserialize<Version>(value,
            @"<Version xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_Build>3</_Build><_Major>1</_Major><_Minor>2</_Minor><_Revision>4</_Revision></Version>");
        Assert.StrictEqual(value.Major, deserializedValue.Major);
        Assert.StrictEqual(value.Minor, deserializedValue.Minor);
        Assert.StrictEqual(value.Build, deserializedValue.Build);
        Assert.StrictEqual(value.Revision, deserializedValue.Revision);
    }

    [Fact]
    public static void DCS_TypeWithCommonTypeProperties()
    {
        TypeWithCommonTypeProperties value = new TypeWithCommonTypeProperties { Ts = new TimeSpan(1, 1, 1), Id = new Guid("ad948f1e-9ba9-44c8-8e2e-b6ba969ec987") };
        var deserializedValue = SerializeAndDeserialize<TypeWithCommonTypeProperties>(value, @"<TypeWithCommonTypeProperties xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Id>ad948f1e-9ba9-44c8-8e2e-b6ba969ec987</Id><Ts>PT1H1M1S</Ts></TypeWithCommonTypeProperties>");
        Assert.StrictEqual<TypeWithCommonTypeProperties>(value, deserializedValue);
    }

#if uapaot
    [Fact]
    public static void DCS_TypeWithTypeProperty()
    {
        TypeWithTypeProperty value = new TypeWithTypeProperty { Id = 123, Name = "Jon Doe" };
        var deserializedValue = SerializeAndDeserialize<TypeWithTypeProperty>(value, @"<TypeWithTypeProperty xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Id>123</Id><Name>Jon Doe</Name><Type i:nil=""true"" xmlns:a=""http://schemas.datacontract.org/2004/07/System""/></TypeWithTypeProperty>");
        Assert.StrictEqual(value.Id, deserializedValue.Id);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
        Assert.StrictEqual(value.Type, deserializedValue.Type);
    }
#endif

    [Fact]
    public static void DCS_TypeWithExplicitIEnumerableImplementation()
    {
        TypeWithExplicitIEnumerableImplementation value = new TypeWithExplicitIEnumerableImplementation { };
        value.Add("Foo");
        value.Add("Bar");
        var deserializedValue = SerializeAndDeserialize<TypeWithExplicitIEnumerableImplementation>(value, @"<ArrayOfanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><anyType i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">Foo</anyType><anyType i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">Bar</anyType></ArrayOfanyType>");
        Assert.StrictEqual(2, deserializedValue.Count);
        IEnumerator enumerator = ((IEnumerable)deserializedValue).GetEnumerator();
        enumerator.MoveNext();
        Assert.StrictEqual("Foo", (string)enumerator.Current);
        enumerator.MoveNext();
        Assert.StrictEqual("Bar", (string)enumerator.Current);
    }

    [Fact]
    public static void DCS_TypeWithGenericDictionaryAsKnownType()
    {
        TypeWithGenericDictionaryAsKnownType value = new TypeWithGenericDictionaryAsKnownType { };
        value.Foo.Add(10, new Level() { Name = "Foo", LevelNo = 1 });
        value.Foo.Add(20, new Level() { Name = "Bar", LevelNo = 2 });
        var deserializedValue = SerializeAndDeserialize<TypeWithGenericDictionaryAsKnownType>(value, @"<TypeWithGenericDictionaryAsKnownType xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Foo xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfintLevelQk4Xq8_SP><a:Key>10</a:Key><a:Value><LevelNo>1</LevelNo><Name>Foo</Name></a:Value></a:KeyValueOfintLevelQk4Xq8_SP><a:KeyValueOfintLevelQk4Xq8_SP><a:Key>20</a:Key><a:Value><LevelNo>2</LevelNo><Name>Bar</Name></a:Value></a:KeyValueOfintLevelQk4Xq8_SP></Foo></TypeWithGenericDictionaryAsKnownType>");

        Assert.StrictEqual(2, deserializedValue.Foo.Count);
        Assert.StrictEqual("Foo", deserializedValue.Foo[10].Name);
        Assert.StrictEqual(1, deserializedValue.Foo[10].LevelNo);
        Assert.StrictEqual("Bar", deserializedValue.Foo[20].Name);
        Assert.StrictEqual(2, deserializedValue.Foo[20].LevelNo);
    }

    [Fact]
    public static void DCS_TypeWithKnownTypeAttributeAndInterfaceMember()
    {
        TypeWithKnownTypeAttributeAndInterfaceMember value = new TypeWithKnownTypeAttributeAndInterfaceMember();
        value.HeadLine = new NewsArticle() { Title = "Foo News" };
        var deserializedValue = SerializeAndDeserialize<TypeWithKnownTypeAttributeAndInterfaceMember>(value, @"<TypeWithKnownTypeAttributeAndInterfaceMember xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><HeadLine i:type=""NewsArticle""><Category>News</Category><Title>Foo News</Title></HeadLine></TypeWithKnownTypeAttributeAndInterfaceMember>");

        Assert.StrictEqual("News", deserializedValue.HeadLine.Category);
        Assert.StrictEqual("Foo News", deserializedValue.HeadLine.Title);
    }

    [Fact]
    public static void DCS_TypeWithKnownTypeAttributeAndListOfInterfaceMember()
    {
        TypeWithKnownTypeAttributeAndListOfInterfaceMember value = new TypeWithKnownTypeAttributeAndListOfInterfaceMember();
        value.Articles = new List<IArticle>() { new SummaryArticle() { Title = "Bar Summary" } };
        var deserializedValue = SerializeAndDeserialize<TypeWithKnownTypeAttributeAndListOfInterfaceMember>(value, @"<TypeWithKnownTypeAttributeAndListOfInterfaceMember xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Articles xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:anyType i:type=""SummaryArticle""><Category>Summary</Category><Title>Bar Summary</Title></a:anyType></Articles></TypeWithKnownTypeAttributeAndListOfInterfaceMember>");

        Assert.StrictEqual(1, deserializedValue.Articles.Count);
        Assert.StrictEqual("Summary", deserializedValue.Articles[0].Category);
        Assert.StrictEqual("Bar Summary", deserializedValue.Articles[0].Title);
    }

    /*
     * Begin tests of the InvalidDataContract generated for illegal types
     */

    [Fact]
    public static void DCS_InvalidDataContract_Write_And_Read_Empty_Collection_Of_Invalid_Type_Succeeds()
    {
        // Collections of invalid types can be serialized and deserialized if they are empty.
        // This is consistent with .Net
        List<Invalid_Class_No_Parameterless_Ctor> list = new List<Invalid_Class_No_Parameterless_Ctor>();
        MemoryStream ms = new MemoryStream();
        DataContractSerializer dcs = new DataContractSerializer(list.GetType());
        dcs.WriteObject(ms, list);
        ms.Seek(0L, SeekOrigin.Begin);
        List<Invalid_Class_No_Parameterless_Ctor> list2 = (List<Invalid_Class_No_Parameterless_Ctor>)dcs.ReadObject(ms);
        Assert.True(list2.Count == 0, String.Format("Unexpected length {0}", list.Count));
    }

    [Fact]
    public static void DCS_InvalidDataContract_Write_NonEmpty_Collection_Of_Invalid_Type_Throws()
    {
        // Non-empty collections of invalid types throw
        // This is consistent with .Net
        Invalid_Class_No_Parameterless_Ctor c = new Invalid_Class_No_Parameterless_Ctor("test");
        List<Invalid_Class_No_Parameterless_Ctor> list = new List<Invalid_Class_No_Parameterless_Ctor>();
        list.Add(c);
        DataContractSerializer dcs = new DataContractSerializer(list.GetType());

        MemoryStream ms = new MemoryStream();
        Assert.Throws<InvalidDataContractException>(() =>
        {
            dcs.WriteObject(ms, c);
        });
    }

    /*
     * End tests of the InvalidDataContract generated for illegal types
     */

    [Fact]
    public static void DCS_DerivedTypeWithBaseTypeWithDataMember()
    {
        DerivedTypeWithDataMemberInBaseType value = new DerivedTypeWithDataMemberInBaseType() { EmbeddedDataMember = new TypeAsEmbeddedDataMember { Name = "Foo" } };
        var deserializedValue = SerializeAndDeserialize<DerivedTypeWithDataMemberInBaseType>(value, @"<DerivedTypeWithDataMemberInBaseType xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><EmbeddedDataMember><Name>Foo</Name></EmbeddedDataMember></DerivedTypeWithDataMemberInBaseType>");

        Assert.StrictEqual("Foo", deserializedValue.EmbeddedDataMember.Name);
    }

    [Fact]
    public static void DCS_PocoDerivedTypeWithBaseTypeWithDataMember()
    {
        PocoDerivedTypeWithDataMemberInBaseType value = new PocoDerivedTypeWithDataMemberInBaseType() { EmbeddedDataMember = new PocoTypeAsEmbeddedDataMember { Name = "Foo" } };
        var deserializedValue = SerializeAndDeserialize<PocoDerivedTypeWithDataMemberInBaseType>(value, @"<PocoDerivedTypeWithDataMemberInBaseType xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><EmbeddedDataMember><Name>Foo</Name></EmbeddedDataMember></PocoDerivedTypeWithDataMemberInBaseType>");

        Assert.StrictEqual("Foo", deserializedValue.EmbeddedDataMember.Name);
    }

    [Fact]
    public static void DCS_ClassImplementingIXmlSerialiable()
    {
        ClassImplementingIXmlSerialiable value = new ClassImplementingIXmlSerialiable() { StringValue = "Foo" };
        var deserializedValue = SerializeAndDeserialize<ClassImplementingIXmlSerialiable>(value, @"<ClassImplementingIXmlSerialiable StringValue=""Foo"" BoolValue=""True"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes""/>");
        Assert.StrictEqual(value.StringValue, deserializedValue.StringValue);
    }

    [Fact]
    public static void DCS_TypeWithNestedGenericClassImplementingIXmlSerialiable()
    {
        TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiable<bool> value = new TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiable<bool>() { StringValue = "Foo" };
        var deserializedValue = SerializeAndDeserialize<TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiable<bool>>(value, @"<TypeWithNestedGenericClassImplementingIXmlSerialiable.NestedGenericClassImplementingIXmlSerialiableOfbooleanRvdAXEcW StringValue=""Foo"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes""/>");
        Assert.StrictEqual(value.StringValue, deserializedValue.StringValue);
    }

    [Fact]
    public static void DCS_GenericTypeWithNestedGenerics()
    {
        GenericTypeWithNestedGenerics<int>.InnerGeneric<double> value = new GenericTypeWithNestedGenerics<int>.InnerGeneric<double>()
        {
            data1 = 123,
            data2 = 4.56
        };
        var deserializedValue = SerializeAndDeserialize<GenericTypeWithNestedGenerics<int>.InnerGeneric<double>>(value, @"<GenericTypeWithNestedGenerics.InnerGenericOfintdouble2LMUf4bh xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><data1>123</data1><data2>4.56</data2></GenericTypeWithNestedGenerics.InnerGenericOfintdouble2LMUf4bh>");
        Assert.StrictEqual(value.data1, deserializedValue.data1);
        Assert.StrictEqual(value.data2, deserializedValue.data2);
    }

    [Fact]
    public static void DCS_DuplicatedKeyDateTimeOffset()
    {
        DateTimeOffset value = new DateTimeOffset(new DateTime(2013, 1, 2, 3, 4, 5, 6, DateTimeKind.Utc).AddMinutes(7));
        var deserializedValue = SerializeAndDeserialize<DateTimeOffset>(value, @"<DateTimeOffset xmlns=""http://schemas.datacontract.org/2004/07/System"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><DateTime>2013-01-02T03:11:05.006Z</DateTime><OffsetMinutes>0</OffsetMinutes></DateTimeOffset>");

        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(DateTimeOffset));
        MemoryStream stream = new MemoryStream();
        dcjs.WriteObject(stream, value);
    }

    [Fact]
    public static void DCS_DuplicatedKeyXmlQualifiedName()
    {
        XmlQualifiedName qname = new XmlQualifiedName("abc", "def");
        TypeWithXmlQualifiedName value = new TypeWithXmlQualifiedName() { Value = qname };
        TypeWithXmlQualifiedName deserialized = SerializeAndDeserialize<TypeWithXmlQualifiedName>(value, @"<TypeWithXmlQualifiedName xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><q:Value xmlns:q=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:a=""def"">a:abc</q:Value></TypeWithXmlQualifiedName>");
        Assert.StrictEqual(value.Value, deserialized.Value);
    }

    [Fact]
    public static void DCS_DeserializeTypeWithInnerInvalidDataContract()
    {
        DataContractSerializer dcs = new DataContractSerializer(typeof(TypeWithPropertyWithoutDefaultCtor));
        string xmlString = @"<TypeWithPropertyWithoutDefaultCtor xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>Foo</Name></TypeWithPropertyWithoutDefaultCtor>";
        MemoryStream ms = new MemoryStream();
        StreamWriter sw = new StreamWriter(ms);
        sw.Write(xmlString);
        sw.Flush();
        ms.Seek(0, SeekOrigin.Begin);

        TypeWithPropertyWithoutDefaultCtor deserializedValue = (TypeWithPropertyWithoutDefaultCtor)dcs.ReadObject(ms);
        Assert.StrictEqual("Foo", deserializedValue.Name);
        Assert.StrictEqual(null, deserializedValue.MemberWithInvalidDataContract);
    }

    [Fact]
    public static void DCS_ReadOnlyCollection()
    {
        List<string> list = new List<string>() { "Foo", "Bar" };
        ReadOnlyCollection<string> value = new ReadOnlyCollection<string>(list);
        var deserializedValue = SerializeAndDeserialize<ReadOnlyCollection<string>>(value, @"<ReadOnlyCollectionOfstring xmlns=""http://schemas.datacontract.org/2004/07/System.Collections.ObjectModel"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><list xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>Foo</a:string><a:string>Bar</a:string></list></ReadOnlyCollectionOfstring>");
        Assert.StrictEqual(value.Count, deserializedValue.Count);
        Assert.StrictEqual(value[0], deserializedValue[0]);
        Assert.StrictEqual(value[1], deserializedValue[1]);
    }

    [Fact]
    public static void DCS_ReadOnlyDictionary()
    {
        var dict = new Dictionary<string, int>();
        dict["Foo"] = 1;
        dict["Bar"] = 2;
        ReadOnlyDictionary<string, int> value = new ReadOnlyDictionary<string, int>(dict);
        var deserializedValue = SerializeAndDeserialize(value, @"<ReadOnlyDictionaryOfstringint xmlns=""http://schemas.datacontract.org/2004/07/System.Collections.ObjectModel"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_dictionary xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:KeyValueOfstringint><a:Key>Foo</a:Key><a:Value>1</a:Value></a:KeyValueOfstringint><a:KeyValueOfstringint><a:Key>Bar</a:Key><a:Value>2</a:Value></a:KeyValueOfstringint></_dictionary></ReadOnlyDictionaryOfstringint>");

        Assert.StrictEqual(value.Count, deserializedValue.Count);
        Assert.StrictEqual(value["Foo"], deserializedValue["Foo"]);
        Assert.StrictEqual(value["Bar"], deserializedValue["Bar"]);
    }

    [Fact]
    public static void DCS_KeyValuePair()
    {
        var value = new KeyValuePair<string, object>("FooKey", "FooValue");
        var deserializedValue = SerializeAndDeserialize<KeyValuePair<string, object>>(value, @"<KeyValuePairOfstringanyType xmlns=""http://schemas.datacontract.org/2004/07/System.Collections.Generic"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><key>FooKey</key><value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">FooValue</value></KeyValuePairOfstringanyType>");

        Assert.StrictEqual(value.Key, deserializedValue.Key);
        Assert.StrictEqual(value.Value, deserializedValue.Value);
    }

    [Fact]
    public static void DCS_ConcurrentDictionary()
    {
        var value = new ConcurrentDictionary<string, int>();
        value["one"] = 1;
        value["two"] = 2;
        var deserializedValue = SerializeAndDeserialize<ConcurrentDictionary<string, int>>(value, @"<ArrayOfKeyValueOfstringint xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfstringint><Key>one</Key><Value>1</Value></KeyValueOfstringint><KeyValueOfstringint><Key>two</Key><Value>2</Value></KeyValueOfstringint></ArrayOfKeyValueOfstringint>", null, null, true);

        Assert.NotNull(deserializedValue);
        Assert.True(deserializedValue.Count == 2);
        Assert.True(deserializedValue["one"] == 1);
        Assert.True(deserializedValue["two"] == 2);
    }

    [Fact]
    public static void DCS_DataContractWithDotInName()
    {
        DataContractWithDotInName value = new DataContractWithDotInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithDotInName>(value, @"<DCWith.InName xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>Foo</Name></DCWith.InName>");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCS_DataContractWithMinusSignInName()
    {
        DataContractWithMinusSignInName value = new DataContractWithMinusSignInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithMinusSignInName>(value, @"<DCWith-InName xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>Foo</Name></DCWith-InName>");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCS_DataContractWithOperatorsInName()
    {
        DataContractWithOperatorsInName value = new DataContractWithOperatorsInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithOperatorsInName>(value, @"<DCWith_x007B__x007D__x005B__x005D__x0028__x0029_._x002C__x003A__x003B__x002B_-_x002A__x002F__x0025__x0026__x007C__x005E__x0021__x007E__x003D__x003C__x003E__x003F__x002B__x002B_--_x0026__x0026__x007C__x007C__x003C__x003C__x003E__x003E__x003D__x003D__x0021__x003D__x003C__x003D__x003E__x003D__x002B__x003D_-_x003D__x002A__x003D__x002F__x003D__x0025__x003D__x0026__x003D__x007C__x003D__x005E__x003D__x003C__x003C__x003D__x003E__x003E__x003D_-_x003E_InName xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>Foo</Name></DCWith_x007B__x007D__x005B__x005D__x0028__x0029_._x002C__x003A__x003B__x002B_-_x002A__x002F__x0025__x0026__x007C__x005E__x0021__x007E__x003D__x003C__x003E__x003F__x002B__x002B_--_x0026__x0026__x007C__x007C__x003C__x003C__x003E__x003E__x003D__x003D__x0021__x003D__x003C__x003D__x003E__x003D__x002B__x003D_-_x003D__x002A__x003D__x002F__x003D__x0025__x003D__x0026__x003D__x007C__x003D__x005E__x003D__x003C__x003C__x003D__x003E__x003E__x003D_-_x003E_InName>");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCS_DataContractWithOtherSymbolsInName()
    {
        DataContractWithOtherSymbolsInName value = new DataContractWithOtherSymbolsInName() { Name = "Foo" };
        var deserializedValue = SerializeAndDeserialize<DataContractWithOtherSymbolsInName>(value, @"<DCWith_x0060__x0040__x0023__x0024__x0027__x0022__x0020__x0009_InName xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>Foo</Name></DCWith_x0060__x0040__x0023__x0024__x0027__x0022__x0020__x0009_InName>");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value.Name, deserializedValue.Name);
    }

    [Fact]
    public static void DCS_CollectionDataContractWithCustomKeyName()
    {
        CollectionDataContractWithCustomKeyName value = new CollectionDataContractWithCustomKeyName();
        value.Add(100, 123);
        value.Add(200, 456);
        var deserializedValue = SerializeAndDeserialize<CollectionDataContractWithCustomKeyName>(value, @"<MyHeaders xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><MyHeader><MyKey>100</MyKey><MyValue>123</MyValue></MyHeader><MyHeader><MyKey>200</MyKey><MyValue>456</MyValue></MyHeader></MyHeaders>");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value[100], deserializedValue[100]);
        Assert.StrictEqual(value[200], deserializedValue[200]);
    }

    [Fact]
    public static void DCS_CollectionDataContractWithCustomKeyNameDuplicate()
    {
        CollectionDataContractWithCustomKeyNameDuplicate value = new CollectionDataContractWithCustomKeyNameDuplicate();
        value.Add(100, 123);
        value.Add(200, 456);
        var deserializedValue = SerializeAndDeserialize<CollectionDataContractWithCustomKeyNameDuplicate>(value, @"<MyHeaders2 xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><MyHeader2><MyKey2>100</MyKey2><MyValue2>123</MyValue2></MyHeader2><MyHeader2><MyKey2>200</MyKey2><MyValue2>456</MyValue2></MyHeader2></MyHeaders2>");

        Assert.NotNull(deserializedValue);
        Assert.StrictEqual(value[100], deserializedValue[100]);
        Assert.StrictEqual(value[200], deserializedValue[200]);
    }

    [Fact]
    public static void DCS_TypeWithCollectionWithoutDefaultConstructor()
    {
        TypeWithCollectionWithoutDefaultConstructor value = new TypeWithCollectionWithoutDefaultConstructor();
        value.CollectionProperty.Add("Foo");
        value.CollectionProperty.Add("Bar");
        var deserializedValue = SerializeAndDeserialize<TypeWithCollectionWithoutDefaultConstructor>(value, @"<TypeWithCollectionWithoutDefaultConstructor xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><CollectionProperty xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>Foo</a:string><a:string>Bar</a:string></CollectionProperty></TypeWithCollectionWithoutDefaultConstructor>");

        Assert.NotNull(deserializedValue);
        Assert.NotNull(deserializedValue.CollectionProperty);
        Assert.StrictEqual(value.CollectionProperty.Count, deserializedValue.CollectionProperty.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.CollectionProperty, deserializedValue.CollectionProperty));
    }

    [Fact]
    public static void DCS_DeserializeEmptyString()
    {
        var serializer = new DataContractSerializer(typeof(object));
        bool exceptionThrown = false;
        try
        {
            serializer.ReadObject(new MemoryStream());
        }
        catch (Exception e)
        {
            Type expectedExceptionType = typeof(XmlException);
            Type actualExceptionType = e.GetType();
            if (!actualExceptionType.Equals(expectedExceptionType))
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("The actual exception was not of the expected type.");
                messageBuilder.AppendLine($"Expected exception type: {expectedExceptionType.FullName}, {expectedExceptionType.GetTypeInfo().Assembly.FullName}");
                messageBuilder.AppendLine($"Actual exception type: {actualExceptionType.FullName}, {actualExceptionType.GetTypeInfo().Assembly.FullName}");
                messageBuilder.AppendLine($"The type of {nameof(expectedExceptionType)} was: {expectedExceptionType.GetType()}");
                messageBuilder.AppendLine($"The type of {nameof(actualExceptionType)} was: {actualExceptionType.GetType()}");
                Assert.True(false, messageBuilder.ToString());
            }

            exceptionThrown = true;
        }

        Assert.True(exceptionThrown, "An expected exception was not thrown.");
    }

    [Theory]
    [MemberData(nameof(XmlDictionaryReaderQuotasData))]
    public static void DCS_XmlDictionaryQuotas(XmlDictionaryReaderQuotas quotas, bool shouldSucceed)
    {
        var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><TypeWithTypeWithIntAndStringPropertyProperty><ObjectProperty><SampleInt>10</SampleInt><SampleString>Sample string</SampleString></ObjectProperty></TypeWithTypeWithIntAndStringPropertyProperty>";
        var content = new MemoryStream(Encoding.UTF8.GetBytes(input));
        using (var reader = XmlDictionaryReader.CreateTextReader(content, Encoding.UTF8, quotas, onClose: null))
        {
            var serializer = new DataContractSerializer(typeof(TypeWithTypeWithIntAndStringPropertyProperty), new DataContractSerializerSettings());
            if (shouldSucceed)
            {
                var deserializedObject = (TypeWithTypeWithIntAndStringPropertyProperty)serializer.ReadObject(reader);
                Assert.StrictEqual(10, deserializedObject.ObjectProperty.SampleInt);
                Assert.StrictEqual("Sample string", deserializedObject.ObjectProperty.SampleString);
            }
            else
            {
                Assert.Throws<SerializationException>(() => { serializer.ReadObject(reader); });
            }
        }
    }

    public static IEnumerable<object[]> XmlDictionaryReaderQuotasData
    {
        get
        {
            return new[]
            {
                new object[] { new XmlDictionaryReaderQuotas(), true },
                new object[] { new XmlDictionaryReaderQuotas() { MaxDepth = 1}, false },
                new object[] { new XmlDictionaryReaderQuotas() { MaxStringContentLength = 1}, false }
            };
        }
    }

    [Fact]
    public static void DCS_CollectionInterfaceGetOnlyCollection()
    {
        var obj = new TypeWithCollectionInterfaceGetOnlyCollection(new List<string>() { "item1", "item2", "item3" });
        var deserializedObj = SerializeAndDeserialize(obj, @"<TypeWithCollectionInterfaceGetOnlyCollection xmlns=""http://schemas.datacontract.org/2004/07/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Items xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>item1</a:string><a:string>item2</a:string><a:string>item3</a:string></Items></TypeWithCollectionInterfaceGetOnlyCollection>");
        Assert.Equal(obj.Items, deserializedObj.Items);
    }

    [Fact]
    public static void DCS_EnumerableInterfaceGetOnlyCollection()
    {
        // Expect exception in deserialization process
        Assert.Throws<InvalidDataContractException>(() => {
            var obj = new TypeWithEnumerableInterfaceGetOnlyCollection(new List<string>() { "item1", "item2", "item3" });
            SerializeAndDeserialize(obj, @"<TypeWithEnumerableInterfaceGetOnlyCollection xmlns=""http://schemas.datacontract.org/2004/07/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Items xmlns:a=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><a:string>item1</a:string><a:string>item2</a:string><a:string>item3</a:string></Items></TypeWithEnumerableInterfaceGetOnlyCollection>");
        });
    }

    [Fact]
    public static void DCS_RecursiveCollection()
    {
        Assert.Throws<InvalidDataContractException>(() =>
        {
            (new DataContractSerializer(typeof (RecursiveCollection))).WriteObject(new MemoryStream(), new RecursiveCollection());
        });
    }

    [Fact]
    public static void DCS_XmlElementAsRoot()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement expected = xDoc.CreateElement("Element");
        expected.InnerText = "Element innertext";
        var actual = SerializeAndDeserialize(expected,
@"<Element>Element innertext</Element>");
        Assert.NotNull(actual);
        Assert.StrictEqual(expected.InnerText, actual.InnerText);
    }

    [Fact]
    public static void DCS_TypeWithXmlElementProperty()
    {
        XmlDocument xDoc = new XmlDocument();
        xDoc.LoadXml(@"<html></html>");
        XmlElement productElement = xDoc.CreateElement("Product");
        productElement.InnerText = "Product innertext";
        XmlElement categoryElement = xDoc.CreateElement("Category");
        categoryElement.InnerText = "Category innertext";
        var expected = new TypeWithXmlElementProperty() { Elements = new[] { productElement, categoryElement } };
        var actual = SerializeAndDeserialize(expected,
@"<TypeWithXmlElementProperty xmlns=""http://schemas.datacontract.org/2004/07/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Elements xmlns:a=""http://schemas.datacontract.org/2004/07/System.Xml""><a:XmlElement><Product xmlns="""">Product innertext</Product></a:XmlElement><a:XmlElement><Category xmlns="""">Category innertext</Category></a:XmlElement></Elements></TypeWithXmlElementProperty>");
        Assert.StrictEqual(expected.Elements.Length, actual.Elements.Length);
        for (int i = 0; i < expected.Elements.Length; ++i)
        {
            Assert.StrictEqual(expected.Elements[i].InnerText, actual.Elements[i].InnerText);
        }
    }

    [Fact]
    public static void DCS_ArrayOfSimpleType_PreserveObjectReferences_True()
    {
        var x = new SimpleType[3];
        var simpleObject1 = new SimpleType() { P1 = "simpleObject1", P2 = 1 };
        var simpleObject2 = new SimpleType() { P1 = "simpleObject2", P2 = 2 };
        x[0] = simpleObject1;
        x[1] = simpleObject1;
        x[2] = simpleObject2;

        var settings = new DataContractSerializerSettings
        {
            PreserveObjectReferences = true,
        };

        var y = SerializeAndDeserialize(x,
            baseline: "<ArrayOfSimpleType z:Id=\"1\" z:Size=\"3\" xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><SimpleType z:Id=\"2\"><P1 z:Id=\"3\">simpleObject1</P1><P2>1</P2></SimpleType><SimpleType z:Ref=\"2\" i:nil=\"true\"/><SimpleType z:Id=\"4\"><P1 z:Id=\"5\">simpleObject2</P1><P2>2</P2></SimpleType></ArrayOfSimpleType>",
            settings: settings);

        Assert.True(x.Length == y.Length, "x.Length != y.Length");
        Assert.True(x[0].P1 == y[0].P1, "x[0].P1 != y[0].P1");
        Assert.True(x[0].P2 == y[0].P2, "x[0].P2 != y[0].P2");
        Assert.True(y[0] == y[1], "y[0] and y[1] should point to the same object, but they pointed to different objects.");

        Assert.True(x[2].P1 == y[2].P1, "x[2].P1 != y[2].P1");
        Assert.True(x[2].P2 == y[2].P2, "x[2].P2 != y[2].P2");
    }

    [Fact]
    public static void DCS_ArrayOfSimpleType_PreserveObjectReferences_False()
    {
        var x = new SimpleType[3];
        var simpleObject1 = new SimpleType() { P1 = "simpleObject1", P2 = 1 };
        var simpleObject2 = new SimpleType() { P1 = "simpleObject2", P2 = 2 };
        x[0] = simpleObject1;
        x[1] = simpleObject1;
        x[2] = simpleObject2;

        var settings = new DataContractSerializerSettings
        {
            PreserveObjectReferences = false,
        };

        var y = SerializeAndDeserialize(x,
            baseline: "<ArrayOfSimpleType xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><SimpleType><P1>simpleObject1</P1><P2>1</P2></SimpleType><SimpleType><P1>simpleObject1</P1><P2>1</P2></SimpleType><SimpleType><P1>simpleObject2</P1><P2>2</P2></SimpleType></ArrayOfSimpleType>",
            settings: settings);

        Assert.True(x.Length == y.Length, "x.Length != y.Length");
        Assert.True(x[0].P1 == y[0].P1, "x[0].P1 != y[0].P1");
        Assert.True(x[0].P2 == y[0].P2, "x[0].P2 != y[0].P2");
        Assert.True(x[1].P1 == y[1].P1, "x[1].P1 != y[1].P1");
        Assert.True(x[1].P2 == y[1].P2, "x[1].P2 != y[1].P2");
        Assert.True(y[0] != y[1], "y[0] and y[1] should point to different objects, but they pointed to the same object.");

        Assert.True(x[2].P1 == y[2].P1, "x[2].P1 != y[2].P1");
        Assert.True(x[2].P2 == y[2].P2, "x[2].P2 != y[2].P2");
    }

    [Fact]
    public static void DCS_CircularTypes_PreserveObjectReferences_True()
    {
        var root = new TypeWithListOfReferenceChildren();
        var typeOfReferenceChildA = new TypeOfReferenceChild { Root = root, Name = "A" };
        var typeOfReferenceChildB = new TypeOfReferenceChild { Root = root, Name = "B" };
        root.Children = new List<TypeOfReferenceChild> {
                typeOfReferenceChildA,
                typeOfReferenceChildB,
                typeOfReferenceChildA,
        };

        var settings = new DataContractSerializerSettings
        {
            PreserveObjectReferences = true,
        };

        var root2 = SerializeAndDeserialize(root,
            baseline: "<TypeWithListOfReferenceChildren z:Id=\"1\" xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Children z:Id=\"2\" z:Size=\"3\"><TypeOfReferenceChild z:Id=\"3\"><Name z:Id=\"4\">A</Name><Root z:Ref=\"1\" i:nil=\"true\"/></TypeOfReferenceChild><TypeOfReferenceChild z:Id=\"5\"><Name z:Id=\"6\">B</Name><Root z:Ref=\"1\" i:nil=\"true\"/></TypeOfReferenceChild><TypeOfReferenceChild z:Ref=\"3\" i:nil=\"true\"/></Children></TypeWithListOfReferenceChildren>",
            settings: settings);

        Assert.True(3 == root2.Children.Count, $"root2.Children.Count was expected to be {2}, but the actual value was {root2.Children.Count}");
        Assert.True(root.Children[0].Name == root2.Children[0].Name, "root.Children[0].Name != root2.Children[0].Name");
        Assert.True(root.Children[1].Name == root2.Children[1].Name, "root.Children[1].Name != root2.Children[1].Name");
        Assert.True(root2 == root2.Children[0].Root, "root2 != root2.Children[0].Root");
        Assert.True(root2 == root2.Children[1].Root, "root2 != root2.Children[1].Root");

        Assert.True(root2.Children[0] == root2.Children[2], "root2.Children[0] != root2.Children[2]");
    }

    [Fact]
    public static void DCS_CircularTypes_PreserveObjectReferences_False()
    {
        var root = new TypeWithListOfReferenceChildren();
        var typeOfReferenceChildA = new TypeOfReferenceChild { Root = root, Name = "A" };
        var typeOfReferenceChildB = new TypeOfReferenceChild { Root = root, Name = "B" };
        root.Children = new List<TypeOfReferenceChild> {
                typeOfReferenceChildA,
                typeOfReferenceChildB,
                typeOfReferenceChildA,
        };

        var settings = new DataContractSerializerSettings
        {
            PreserveObjectReferences = false,
        };

        var root2 = SerializeAndDeserialize(root,
            baseline: "<TypeWithListOfReferenceChildren xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Children><TypeOfReferenceChild z:Id=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Name>A</Name><Root><Children><TypeOfReferenceChild z:Ref=\"i1\"/><TypeOfReferenceChild z:Id=\"i2\"><Name>B</Name><Root><Children><TypeOfReferenceChild z:Ref=\"i1\"/><TypeOfReferenceChild z:Ref=\"i2\"/><TypeOfReferenceChild z:Ref=\"i1\"/></Children></Root></TypeOfReferenceChild><TypeOfReferenceChild z:Ref=\"i1\"/></Children></Root></TypeOfReferenceChild><TypeOfReferenceChild z:Ref=\"i2\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><TypeOfReferenceChild z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></Children></TypeWithListOfReferenceChildren>",
            settings: settings);

        Assert.True(3 == root2.Children.Count, $"root2.Children.Count was expected to be {2}, but the actual value was {root2.Children.Count}");
        Assert.True(root.Children[0].Name == root2.Children[0].Name, "root.Children[0].Name != root2.Children[0].Name");
        Assert.True(root.Children[1].Name == root2.Children[1].Name, "root.Children[1].Name != root2.Children[1].Name");
        Assert.True(root2 != root2.Children[0].Root, "root2 == root2.Children[0].Root");
        Assert.True(root2 != root2.Children[1].Root, "root2 == root2.Children[1].Root");
        Assert.True(root2.Children[0].Root != root2.Children[1].Root, "root2.Children[0].Root == root2.Children[1].Root");
        Assert.True(root2.Children[0] == root2.Children[2], "root2.Children[0] != root2.Children[2]");
    }

    [Fact]
    public static void DCS_TypeWithPrimitiveProperties()
    {
        TypeWithPrimitiveProperties x = new TypeWithPrimitiveProperties { P1 = "abc", P2 = 11 };
        TypeWithPrimitiveProperties y = SerializeAndDeserialize<TypeWithPrimitiveProperties>(x, @"<TypeWithPrimitiveProperties xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><P1>abc</P1><P2>11</P2></TypeWithPrimitiveProperties>");
        Assert.StrictEqual(x.P1, y.P1);
        Assert.StrictEqual(x.P2, y.P2);
    }

    [Fact]
    public static void DCS_TypeWithPrimitiveFields()
    {
        TypeWithPrimitiveFields x = new TypeWithPrimitiveFields { P1 = "abc", P2 = 11 };
        TypeWithPrimitiveFields y = SerializeAndDeserialize<TypeWithPrimitiveFields>(x, @"<TypeWithPrimitiveFields xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><P1>abc</P1><P2>11</P2></TypeWithPrimitiveFields>");
        Assert.StrictEqual(x.P1, y.P1);
        Assert.StrictEqual(x.P2, y.P2);
    }

    [Fact]
    public static void DCS_TypeWithAllPrimitiveProperties()
    {
        TypeWithAllPrimitiveProperties x = new TypeWithAllPrimitiveProperties
        {
            BooleanMember = true,
            //ByteArrayMember = new byte[] { 1, 2, 3, 4 },
            CharMember = 'C',
            DateTimeMember = new DateTime(2016, 7, 8, 9, 10, 11),
            DecimalMember = new decimal(123, 456, 789, true, 0),
            DoubleMember = 123.456,
            FloatMember = 456.789f,
            GuidMember = Guid.Parse("2054fd3e-e118-476a-9962-1a882be51860"),
            //public byte[] HexBinaryMember 
            StringMember = "abc",
            IntMember = 123
        };
        TypeWithAllPrimitiveProperties y = SerializeAndDeserialize<TypeWithAllPrimitiveProperties>(x, @"<TypeWithAllPrimitiveProperties xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><BooleanMember>true</BooleanMember><CharMember>67</CharMember><DateTimeMember>2016-07-08T09:10:11</DateTimeMember><DecimalMember>-14554481076115341312123</DecimalMember><DoubleMember>123.456</DoubleMember><FloatMember>456.789</FloatMember><GuidMember>2054fd3e-e118-476a-9962-1a882be51860</GuidMember><IntMember>123</IntMember><StringMember>abc</StringMember></TypeWithAllPrimitiveProperties>");
        Assert.StrictEqual(x.BooleanMember, y.BooleanMember);
        //Assert.StrictEqual(x.ByteArrayMember, y.ByteArrayMember);
        Assert.StrictEqual(x.CharMember, y.CharMember);
        Assert.StrictEqual(x.DateTimeMember, y.DateTimeMember);
        Assert.StrictEqual(x.DecimalMember, y.DecimalMember);
        Assert.StrictEqual(x.DoubleMember, y.DoubleMember);
        Assert.StrictEqual(x.FloatMember, y.FloatMember);
        Assert.StrictEqual(x.GuidMember, y.GuidMember);
        Assert.StrictEqual(x.StringMember, y.StringMember);
        Assert.StrictEqual(x.IntMember, y.IntMember);
    }

#region Array of primitive types

    [Fact]
    public static void DCS_ArrayOfBoolean()
    {
        var value = new bool[] { true, false, true };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfboolean xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><boolean>true</boolean><boolean>false</boolean><boolean>true</boolean></ArrayOfboolean>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfDateTime()
    {
        var value = new DateTime[] { new DateTime(2000, 1, 2, 3, 4, 5, DateTimeKind.Utc), new DateTime(2011, 2, 3, 4, 5, 6, DateTimeKind.Utc) };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfdateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><dateTime>2000-01-02T03:04:05Z</dateTime><dateTime>2011-02-03T04:05:06Z</dateTime></ArrayOfdateTime>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfDecimal()
    {
        var value = new decimal[] { new decimal(1, 2, 3, false, 1), new decimal(4, 5, 6, true, 2) };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfdecimal xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><decimal>5534023222971858944.1</decimal><decimal>-1106804644637321461.80</decimal></ArrayOfdecimal>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfInt32()
    {
        var value = new int[] { 123, int.MaxValue, int.MinValue };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfint xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><int>123</int><int>2147483647</int><int>-2147483648</int></ArrayOfint>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfInt64()
    {
        var value = new long[] { 123, long.MaxValue, long.MinValue };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOflong xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><long>123</long><long>9223372036854775807</long><long>-9223372036854775808</long></ArrayOflong>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfSingle()
    {
        var value = new float[] { 1.23f, 4.56f, 7.89f };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOffloat xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><float>1.23</float><float>4.56</float><float>7.89</float></ArrayOffloat>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfDouble()
    {
        var value = new double[] { 1.23, 4.56, 7.89 };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfdouble xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><double>1.23</double><double>4.56</double><double>7.89</double></ArrayOfdouble>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfString()
    {
        var value = new string[] { "abc", "def", "xyz" };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><string>abc</string><string>def</string><string>xyz</string></ArrayOfstring>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfTypeWithPrimitiveProperties()
    {
        var value = new TypeWithPrimitiveProperties[]
        {
            new TypeWithPrimitiveProperties() { P1 = "abc" , P2 = 123 },
            new TypeWithPrimitiveProperties() { P1 = "def" , P2 = 456 },
        };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfTypeWithPrimitiveProperties xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><TypeWithPrimitiveProperties><P1>abc</P1><P2>123</P2></TypeWithPrimitiveProperties><TypeWithPrimitiveProperties><P1>def</P1><P2>456</P2></TypeWithPrimitiveProperties></ArrayOfTypeWithPrimitiveProperties>");
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_ArrayOfSimpleType()
    {
        // Intentionally set count to 64 to test array resizing functionality during de-serialization.
        int count = 64;
        var value = new SimpleType[count];
        for (int i = 0; i < count; i++)
        {
            value[i] = new SimpleType() { P1 = i.ToString(), P2 = i };
        }

        var deserialized = SerializeAndDeserialize(value, baseline: null, skipStringCompare: true);
        Assert.StrictEqual(value.Length, deserialized.Length);
        Assert.StrictEqual(0, deserialized[0].P2);
        Assert.StrictEqual(1, deserialized[1].P2);
        Assert.StrictEqual(count-1, deserialized[count-1].P2);
    }

    [Fact]
    public static void DCS_TypeWithEmitDefaultValueFalse()
    {
        var value = new TypeWithEmitDefaultValueFalse();

        var actual = SerializeAndDeserialize(value, "<TypeWithEmitDefaultValueFalse xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"/>");

        Assert.NotNull(actual);
        Assert.Equal(value.Name, actual.Name);
        Assert.Equal(value.ID, actual.ID);
    }

#endregion

#region Collection

    [Fact]
    public static void DCS_GenericICollectionOfBoolean()
    {
        var value = new TypeImplementsGenericICollection<bool>() { true, false, true };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfboolean xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><boolean>true</boolean><boolean>false</boolean><boolean>true</boolean></ArrayOfboolean>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    //[Fact]
    // This also fails without using reflection based fallback
    public static void DCS_GenericICollectionOfDateTime()
    {
        var value = new TypeImplementsGenericICollection<DateTime>() { new DateTime(2000, 1, 2, 3, 4, 5), new DateTime(2011, 2, 3, 4, 5, 6) };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfdateTime xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><dateTime>2000-01-02T03:04:05-08:00</dateTime><dateTime>2011-02-03T04:05:06-08:00</dateTime></ArrayOfdateTime>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_GenericICollectionOfDecimal()
    {
        var value = new TypeImplementsGenericICollection<decimal>() { new decimal(1, 2, 3, false, 1), new decimal(4, 5, 6, true, 2) };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfdecimal xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><decimal>5534023222971858944.1</decimal><decimal>-1106804644637321461.80</decimal></ArrayOfdecimal>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_GenericICollectionOfInt32()
    {
        TypeImplementsGenericICollection<int> x = new TypeImplementsGenericICollection<int>(123, int.MaxValue, int.MinValue);
        TypeImplementsGenericICollection<int> y = SerializeAndDeserialize(x, @"<ArrayOfint xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><int>123</int><int>2147483647</int><int>-2147483648</int></ArrayOfint>");

        Assert.NotNull(y);
        Assert.StrictEqual(x.Count, y.Count);
        Assert.True(x.SequenceEqual(y));
    }

    [Fact]
    public static void DCS_GenericICollectionOfInt64()
    {
        var value = new TypeImplementsGenericICollection<long>() { 123, long.MaxValue, long.MinValue };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOflong xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><long>123</long><long>9223372036854775807</long><long>-9223372036854775808</long></ArrayOflong>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_GenericICollectionOfSingle()
    {
        var value = new TypeImplementsGenericICollection<float>() { 1.23f, 4.56f, 7.89f };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOffloat xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><float>1.23</float><float>4.56</float><float>7.89</float></ArrayOffloat>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_GenericICollectionOfDouble()
    {
        var value = new TypeImplementsGenericICollection<double>() { 1.23, 4.56, 7.89 };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfdouble xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><double>1.23</double><double>4.56</double><double>7.89</double></ArrayOfdouble>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_GenericICollectionOfString()
    {
        TypeImplementsGenericICollection<string> value = new TypeImplementsGenericICollection<string>("a1", "a2");
        TypeImplementsGenericICollection<string> deserialized = SerializeAndDeserialize(value, @"<ArrayOfstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><string>a1</string><string>a2</string></ArrayOfstring>");

        Assert.NotNull(deserialized);
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.True(value.SequenceEqual(deserialized));
    }

    [Fact]
    public static void DCS_GenericICollectionOfTypeWithPrimitiveProperties()
    {
        var value = new TypeImplementsGenericICollection<TypeWithPrimitiveProperties>()
        {
            new TypeWithPrimitiveProperties() { P1 = "abc" , P2 = 123 },
            new TypeWithPrimitiveProperties() { P1 = "def" , P2 = 456 },
        };
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfTypeWithPrimitiveProperties xmlns=""http://schemas.datacontract.org/2004/07/SerializationTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><TypeWithPrimitiveProperties><P1>abc</P1><P2>123</P2></TypeWithPrimitiveProperties><TypeWithPrimitiveProperties><P1>def</P1><P2>456</P2></TypeWithPrimitiveProperties></ArrayOfTypeWithPrimitiveProperties>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value, deserialized));
    }

    [Fact]
    public static void DCS_CollectionOfTypeWithNonDefaultNamcespace()
    {
        var value = new CollectionOfTypeWithNonDefaultNamcespace();
        value.Add(new TypeWithNonDefaultNamcespace() { Name = "foo" });

        var actual = SerializeAndDeserialize(value, "<CollectionOfTypeWithNonDefaultNamcespace xmlns=\"CollectionNamespace\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:a=\"ItemTypeNamespace\"><TypeWithNonDefaultNamcespace><a:Name>foo</a:Name></TypeWithNonDefaultNamcespace></CollectionOfTypeWithNonDefaultNamcespace>");
        Assert.NotNull(actual);
        Assert.NotNull(actual[0]);
        Assert.Equal(value[0].Name, actual[0].Name);
    }

#endregion

#region Generic Dictionary

    [Fact]
    public static void DCS_GenericDictionaryOfInt32Boolean()
    {
        var value = new Dictionary<int, bool>();
        value.Add(123, true);
        value.Add(456, false);
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfKeyValueOfintboolean xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfintboolean><Key>123</Key><Value>true</Value></KeyValueOfintboolean><KeyValueOfintboolean><Key>456</Key><Value>false</Value></KeyValueOfintboolean></ArrayOfKeyValueOfintboolean>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.ToArray(), deserialized.ToArray()));
    }

    [Fact]
    public static void DCS_GenericDictionaryOfInt32String()
    {
        var value = new Dictionary<int, string>();
        value.Add(123, "abc");
        value.Add(456, "def");
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfKeyValueOfintstring xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfintstring><Key>123</Key><Value>abc</Value></KeyValueOfintstring><KeyValueOfintstring><Key>456</Key><Value>def</Value></KeyValueOfintstring></ArrayOfKeyValueOfintstring>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.ToArray(), deserialized.ToArray()));
    }

    [Fact]
    public static void DCS_GenericDictionaryOfStringInt32()
    {
        var value = new Dictionary<string, int>();
        value.Add("abc", 123);
        value.Add("def", 456);
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfKeyValueOfstringint xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfstringint><Key>abc</Key><Value>123</Value></KeyValueOfstringint><KeyValueOfstringint><Key>def</Key><Value>456</Value></KeyValueOfstringint></ArrayOfKeyValueOfstringint>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.ToArray(), deserialized.ToArray()));
    }

#endregion

#region Non-Generic Dictionary

    [Fact]
    public static void DCS_NonGenericDictionaryOfInt32Boolean()
    {
        var value = new MyNonGenericDictionary();
        value.Add(123, true);
        value.Add(456, false);
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfKeyValueOfanyTypeanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">123</Key><Value i:type=""a:boolean"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">true</Value></KeyValueOfanyTypeanyType><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">456</Key><Value i:type=""a:boolean"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">false</Value></KeyValueOfanyTypeanyType></ArrayOfKeyValueOfanyTypeanyType>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Keys.Cast<int>().ToArray(), deserialized.Keys.Cast<int>().ToArray()));
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Values.Cast<bool>().ToArray(), deserialized.Values.Cast<bool>().ToArray()));
    }

    [Fact]
    public static void DCS_NonGenericDictionaryOfInt32String()
    {
        var value = new MyNonGenericDictionary();
        value.Add(123, "abc");
        value.Add(456, "def");
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfKeyValueOfanyTypeanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">123</Key><Value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">abc</Value></KeyValueOfanyTypeanyType><KeyValueOfanyTypeanyType><Key i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">456</Key><Value i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">def</Value></KeyValueOfanyTypeanyType></ArrayOfKeyValueOfanyTypeanyType>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Keys.Cast<int>().ToArray(), deserialized.Keys.Cast<int>().ToArray()));
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Values.Cast<string>().ToArray(), deserialized.Values.Cast<string>().ToArray()));
    }

    [Fact]
    public static void DCS_NonGenericDictionaryOfStringInt32()
    {
        var value = new MyNonGenericDictionary();
        value.Add("abc", 123);
        value.Add("def", 456);
        var deserialized = SerializeAndDeserialize(value, @"<ArrayOfKeyValueOfanyTypeanyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><KeyValueOfanyTypeanyType><Key i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">abc</Key><Value i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">123</Value></KeyValueOfanyTypeanyType><KeyValueOfanyTypeanyType><Key i:type=""a:string"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">def</Key><Value i:type=""a:int"" xmlns:a=""http://www.w3.org/2001/XMLSchema"">456</Value></KeyValueOfanyTypeanyType></ArrayOfKeyValueOfanyTypeanyType>");
        Assert.StrictEqual(value.Count, deserialized.Count);
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Keys.Cast<string>().ToArray(), deserialized.Keys.Cast<string>().ToArray()));
        Assert.StrictEqual(true, Enumerable.SequenceEqual(value.Values.Cast<int>().ToArray(), deserialized.Values.Cast<int>().ToArray()));
    }

#endregion

    [Fact]
    public static void DCS_BasicRoundTripResolveDTOTypes()
    {
        ObjectContainer instance = new ObjectContainer(new DTOContainer());
        Func<DataContractSerializer> serializerfunc = () =>
        {
            var settings = new DataContractSerializerSettings()
            {
                DataContractResolver = new DTOResolver()
            };

            var serializer = new DataContractSerializer(typeof(ObjectContainer), settings);
            return serializer;
        };

        string expectedxmlstring = "<ObjectContainer xmlns =\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:DTOContainer\" xmlns:a=\"http://www.default.com\"><nDTO i:type=\"a:DTO\"><DateTime xmlns=\"http://schemas.datacontract.org/2004/07/System\">9999-12-31T23:59:59.9999999Z</DateTime><OffsetMinutes xmlns=\"http://schemas.datacontract.org/2004/07/System\">0</OffsetMinutes></nDTO></_data></ObjectContainer>";
        ObjectContainer deserialized = SerializeAndDeserialize(instance, expectedxmlstring, null, serializerfunc, false);
        Assert.Equal(DateTimeOffset.MaxValue, ((DTOContainer)deserialized.Data).nDTO);
    }

    [Fact]
    public static void DCS_ExtensionDataObjectTest()
    {
        var p2 = new PersonV2();
        p2.Name = "Elizabeth";
        p2.ID = 2006;

        // Serialize the PersonV2 object
        var ser = new DataContractSerializer(typeof(PersonV2));
        var ms1 = new MemoryStream();
        ser.WriteObject(ms1, p2);

        // Verify the payload
        ms1.Position = 0;
        string actualOutput1 = new StreamReader(ms1).ReadToEnd();
        string baseline1 = "<Person xmlns=\"http://www.msn.com/employees\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Name>Elizabeth</Name><ID>2006</ID></Person>";

        Utils.CompareResult result = Utils.Compare(baseline1, actualOutput1);
        Assert.True(result.Equal, $"{nameof(actualOutput1)} was not as expected: {Environment.NewLine}Expected: {baseline1}{Environment.NewLine}Actual: {actualOutput1}");

        // Deserialize the payload into a Person instance.
        ms1.Position = 0;
        var ser2 = new DataContractSerializer(typeof(Person));
        XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(ms1, new XmlDictionaryReaderQuotas());
        var p1 = (Person)ser2.ReadObject(reader, false);

        Assert.True(p1 != null, $"Variable {nameof(p1)} was null.");
        Assert.True(p1.ExtensionData != null, $"{nameof(p1.ExtensionData)} was null.");
        Assert.Equal(p2.Name, p1.Name);

        // Serialize the Person instance
        var ms2 = new MemoryStream();
        ser2.WriteObject(ms2, p1);

        // Verify the payload
        ms2.Position = 0;
        string actualOutput2 = new StreamReader(ms2).ReadToEnd();
        string baseline2 = "<Person xmlns=\"http://www.msn.com/employees\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Name>Elizabeth</Name><ID>2006</ID></Person>";

        Utils.CompareResult result2 = Utils.Compare(baseline2, actualOutput2);
        Assert.True(result2.Equal, $"{nameof(actualOutput2)} was not as expected: {Environment.NewLine}Expected: {baseline2}{Environment.NewLine}Actual: {actualOutput2}");
    }

    [Fact]
    public static void DCS_XPathQueryGeneratorTest()
    {
        Type t = typeof(Order);
        MemberInfo[] mi = t.GetMember("Product");
        MemberInfo[] mi2 = t.GetMember("Value");
        MemberInfo[] mi3 = t.GetMember("Quantity");
        Assert.Equal("/xg0:Order/xg0:productName", GenerateaAndGetXPath(t, mi));
        Assert.Equal("/xg0:Order/xg0:cost", GenerateaAndGetXPath(t, mi2));
        Assert.Equal("/xg0:Order/xg0:quantity", GenerateaAndGetXPath(t, mi3));
        Type t2 = typeof(Line);
        MemberInfo[] mi4 = t2.GetMember("Items");
        Assert.Equal("/xg0:Line/xg0:Items", GenerateaAndGetXPath(t2, mi4));
    }
    static string GenerateaAndGetXPath(Type t, MemberInfo[] mi)
    {
        // Create a new name table and name space manager.
        NameTable nt = new NameTable();
        XmlNamespaceManager xname = new XmlNamespaceManager(nt);
        // Generate the query and print it.
        return XPathQueryGenerator.CreateFromDataContractSerializer(
            t, mi, out xname);
    }

    [Fact]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework has an implementation and does not throw InvalidOperationException")]
    public static void XsdDataContractExporterTest()
    {
        XsdDataContractExporter exporter = new XsdDataContractExporter();
        Assert.Throws<PlatformNotSupportedException>(() => exporter.CanExport(typeof(Employee)));
        Assert.Throws<PlatformNotSupportedException>(() => exporter.Export(typeof(Employee)));
    }

    [Fact]
    public static void DCS_MyISerializableType()
    {
        var value = new MyISerializableType();
        value.StringValue = "test string";

        var actual = SerializeAndDeserialize(value, "<MyISerializableType xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:x=\"http://www.w3.org/2001/XMLSchema\"><_stringValue i:type=\"x:string\" xmlns=\"\">test string</_stringValue></MyISerializableType>");

        Assert.NotNull(actual);
        Assert.Equal(value.StringValue, actual.StringValue);
    }

    [Fact]
    public static void DCS_TypeWithNonSerializedField()
    {
        var value = new TypeWithSerializableAttributeAndNonSerializedField();
        value.Member1 = 11;
        value.Member2 = "22";
        value.SetMember3(33);
        value.Member4 = "44";

        var actual = SerializeAndDeserialize(
            value,
            "<TypeWithSerializableAttributeAndNonSerializedField xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Member1>11</Member1><_member2>22</_member2><_member3>33</_member3></TypeWithSerializableAttributeAndNonSerializedField>",
            skipStringCompare: false);
        Assert.NotNull(actual);
        Assert.Equal(value.Member1, actual.Member1);
        Assert.Equal(value.Member2, actual.Member2);
        Assert.Equal(value.Member3, actual.Member3);
        Assert.Null(actual.Member4);
    }

    [Fact]
    public static void DCS_TypeWithOptionalField()
    {
        var value = new TypeWithOptionalField();
        value.Member1 = 11;
        value.Member2 = 22;

        var actual = SerializeAndDeserialize(value, "<TypeWithOptionalField xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Member1>11</Member1><Member2>22</Member2></TypeWithOptionalField>");
        Assert.NotNull(actual);
        Assert.Equal(value.Member1, actual.Member1);
        Assert.Equal(value.Member2, actual.Member2);

        int member1Value = 11;
        string payloadMissingOptionalField = $"<TypeWithOptionalField xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Member1>{member1Value}</Member1></TypeWithOptionalField>";
        var deserialized = DeserializeString<TypeWithOptionalField>(payloadMissingOptionalField);
        Assert.Equal(member1Value, deserialized.Member1);
        Assert.Equal(0, deserialized.Member2);
    }

    [Fact]
    public static void DCS_SerializableEnumWithNonSerializedValue()
    {
        var value1 = new TypeWithSerializableEnum();
        value1.EnumField = SerializableEnumWithNonSerializedValue.One;
        var actual1 = SerializeAndDeserialize(value1, "<TypeWithSerializableEnum xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><EnumField>One</EnumField></TypeWithSerializableEnum>");
        Assert.NotNull(actual1);
        Assert.Equal(value1.EnumField, actual1.EnumField);

        var value2 = new TypeWithSerializableEnum();
        value2.EnumField = SerializableEnumWithNonSerializedValue.Two;
        Assert.Throws<SerializationException>(() => SerializeAndDeserialize(value2, ""));
    }

    [Fact]
    public static void DCS_SquareWithDeserializationCallback()
    {
        var value = new SquareWithDeserializationCallback(2);
        var actual = SerializeAndDeserialize(value, "<SquareWithDeserializationCallback xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><Edge>2</Edge></SquareWithDeserializationCallback>");
        Assert.NotNull(actual);
        Assert.Equal(value.Area, actual.Area);
    }

    [Fact]
    public static void DCS_TypeWithDelegate()
    {
        var value = new TypeWithDelegate();
        value.IntProperty = 3;
        var actual = SerializeAndDeserialize(value, "<TypeWithDelegate xmlns=\"http://schemas.datacontract.org/2004/07/\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:x=\"http://www.w3.org/2001/XMLSchema\"><IntValue i:type=\"x:int\" xmlns=\"\">3</IntValue></TypeWithDelegate>");
        Assert.NotNull(actual);
        Assert.Null(actual.DelegateProperty);
        Assert.Equal(value.IntProperty, actual.IntProperty);
    }

    #region DesktopTest

    [Fact]
    public static void DCS_ResolveNameReturnsEmptyNamespace()
    {
        SerializationTestTypes.EmptyNsContainer instance = new SerializationTestTypes.EmptyNsContainer(new SerializationTestTypes.EmptyNSAddress());
        var settings = new DataContractSerializerSettings() { MaxItemsInObjectGraph = int.MaxValue, IgnoreExtensionDataObject = false, PreserveObjectReferences = false };
        string baseline1 = @"<EmptyNsContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>P1</Name><address i:type=""EmptyNSAddress"" xmlns=""""><street>downing street</street></address></EmptyNsContainer>";
        var result = SerializeAndDeserialize(instance, baseline1, settings);
        Assert.True(result.address == null, "Address not null");

        settings = new DataContractSerializerSettings() { DataContractResolver = new SerializationTestTypes.EmptyNamespaceResolver(), MaxItemsInObjectGraph = int.MaxValue, IgnoreExtensionDataObject = false, PreserveObjectReferences = false };
        result = SerializeAndDeserialize(instance, baseline1, settings);
        Assert.True(result.address == null, "Address not null");

        instance = new SerializationTestTypes.EmptyNsContainer(new SerializationTestTypes.UknownEmptyNSAddress());        
        string baseline2 = @"<EmptyNsContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Name>P1</Name><address i:type=""AddressFoo"" xmlns=""""><street>downing street</street></address></EmptyNsContainer>";
        result = SerializeAndDeserialize(instance, baseline2, settings);
        Assert.True(result.address == null, "Address not null");       
    }

    [Fact]
    public static void DCS_ResolveDatacontractBaseType()
    {
        SerializationTestTypes.Customer customerInstance = new SerializationTestTypes.PreferredCustomerProxy();
        Type customerBaseType = customerInstance.GetType().BaseType;
        var settings = new DataContractSerializerSettings() { DataContractResolver = new SerializationTestTypes.ProxyDataContractResolver(), MaxItemsInObjectGraph = int.MaxValue, IgnoreExtensionDataObject = false, PreserveObjectReferences = true };
        string baseline1 = @"<Customer z:Id=""1"" i:type=""PreferredCustomer"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Name i:nil=""true""/><VipInfo i:nil=""true""/></Customer>";
        object result = SerializeAndDeserialize(customerInstance, baseline1, settings);
        Assert.Equal(customerBaseType, result.GetType());

        settings = new DataContractSerializerSettings() { DataContractResolver = new SerializationTestTypes.ProxyDataContractResolver(), MaxItemsInObjectGraph = int.MaxValue, IgnoreExtensionDataObject = false, PreserveObjectReferences = false };
        string baseline2 = @"<Customer z:Id=""i1"" i:type=""PreferredCustomer"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Name i:nil=""true""/><VipInfo i:nil=""true""/></Customer>";
        result = SerializeAndDeserialize(customerInstance, baseline2, settings);
        Assert.Equal(customerBaseType, result.GetType());
    }

    /// <summary>
    /// Roundtrips a Datacontract type  which contains Primitive types assigned to member of type object. 
    /// Resolver is plugged in and resolves the primitive types. Verify resolver called during ser and deser
    /// </summary>
    [Fact]
    public static void DCS_BasicRoundTripResolvePrimitiveTypes()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.PrimitiveTypeResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = false
        };

        string baseline = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:PrimitiveContainer_foo"" xmlns:a=""http://www.default.com""><a i:type=""a:Boolean_foo"">false</a><array1><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/></array1><b i:type=""a:Byte_foo"">255</b><c i:type=""a:Byte_foo"">0</c><d i:type=""a:Char_foo"">65535</d><e i:type=""a:Decimal_foo"">79228162514264337593543950335</e><f i:type=""a:Decimal_foo"">-1</f><f5 i:type=""a:DateTime_foo"">9999-12-31T23:59:59.9999999</f5><g i:type=""a:Decimal_foo"">-79228162514264337593543950335</g><guidData i:type=""a:Guid_foo"">4bc848b1-a541-40bf-8aa9-dd6ccb6d0e56</guidData><h i:type=""a:Decimal_foo"">1</h><i i:type=""a:Decimal_foo"">0</i><j i:type=""a:Decimal_foo"">0</j><k i:type=""a:Double_foo"">0</k><l i:type=""a:Double_foo"">4.94065645841247E-324</l><lDTO xmlns:b=""http://schemas.datacontract.org/2004/07/System""/><m i:type=""a:Double_foo"">1.7976931348623157E+308</m><n i:type=""a:Double_foo"">-1.7976931348623157E+308</n><nDTO i:type=""a:DateTimeOffset_foo""><DateTime xmlns=""http://schemas.datacontract.org/2004/07/System"">9999-12-31T23:59:59.9999999Z</DateTime><OffsetMinutes xmlns=""http://schemas.datacontract.org/2004/07/System"">0</OffsetMinutes></nDTO><o i:type=""a:Double_foo"">NaN</o><obj/><p i:type=""a:Double_foo"">-INF</p><q i:type=""a:Double_foo"">INF</q><r i:type=""a:Single_foo"">0</r><s i:type=""a:Single_foo"">1.401298E-45</s><strData i:nil=""true""/><t i:type=""a:Single_foo"">-3.40282347E+38</t><timeSpan i:type=""a:TimeSpan_foo"">P10675199DT2H48M5.4775807S</timeSpan><u i:type=""a:Single_foo"">3.40282347E+38</u><uri>http://www.microsoft.com/</uri><v i:type=""a:Single_foo"">NaN</v><w i:type=""a:Single_foo"">-INF</w><x i:type=""a:Single_foo"">INF</x><xmlQualifiedName i:type=""a:XmlQualifiedName_foo"" xmlns:b=""http://www.microsoft.com"">b:WCF</xmlQualifiedName><y i:type=""a:Int32_foo"">0</y><z i:type=""a:Int32_foo"">2147483647</z><z1 i:type=""a:Int32_foo"">-2147483648</z1><z2 i:type=""a:Int64_foo"">0</z2><z3 i:type=""a:Int64_foo"">9223372036854775807</z3><z4 i:type=""a:Int64_foo"">-9223372036854775808</z4><z5/><z6 i:type=""a:SByte_foo"">0</z6><z7 i:type=""a:SByte_foo"">127</z7><z8 i:type=""a:SByte_foo"">-128</z8><z9 i:type=""a:Int16_foo"">0</z9><z91 i:type=""a:Int16_foo"">32767</z91><z92 i:type=""a:Int16_foo"">-32768</z92><z93 i:type=""a:String_foo"">abc</z93><z94 i:type=""a:UInt16_foo"">0</z94><z95 i:type=""a:UInt16_foo"">65535</z95><z96 i:type=""a:UInt16_foo"">0</z96><z97 i:type=""a:UInt32_foo"">0</z97><z98 i:type=""a:UInt32_foo"">4294967295</z98><z99 i:type=""a:UInt32_foo"">0</z99><z990 i:type=""a:UInt64_foo"">0</z990><z991 i:type=""a:UInt64_foo"">18446744073709551615</z991><z992 i:type=""a:UInt64_foo"">0</z992><z993>AQIDBA==</z993></_data><_data2 i:type=""a:PrimitiveContainer_foo"" xmlns:a=""http://www.default.com""><a i:type=""a:Boolean_foo"">false</a><array1><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/></array1><b i:type=""a:Byte_foo"">255</b><c i:type=""a:Byte_foo"">0</c><d i:type=""a:Char_foo"">65535</d><e i:type=""a:Decimal_foo"">79228162514264337593543950335</e><f i:type=""a:Decimal_foo"">-1</f><f5 i:type=""a:DateTime_foo"">9999-12-31T23:59:59.9999999</f5><g i:type=""a:Decimal_foo"">-79228162514264337593543950335</g><guidData i:type=""a:Guid_foo"">4bc848b1-a541-40bf-8aa9-dd6ccb6d0e56</guidData><h i:type=""a:Decimal_foo"">1</h><i i:type=""a:Decimal_foo"">0</i><j i:type=""a:Decimal_foo"">0</j><k i:type=""a:Double_foo"">0</k><l i:type=""a:Double_foo"">4.94065645841247E-324</l><lDTO xmlns:b=""http://schemas.datacontract.org/2004/07/System""/><m i:type=""a:Double_foo"">1.7976931348623157E+308</m><n i:type=""a:Double_foo"">-1.7976931348623157E+308</n><nDTO i:type=""a:DateTimeOffset_foo""><DateTime xmlns=""http://schemas.datacontract.org/2004/07/System"">9999-12-31T23:59:59.9999999Z</DateTime><OffsetMinutes xmlns=""http://schemas.datacontract.org/2004/07/System"">0</OffsetMinutes></nDTO><o i:type=""a:Double_foo"">NaN</o><obj/><p i:type=""a:Double_foo"">-INF</p><q i:type=""a:Double_foo"">INF</q><r i:type=""a:Single_foo"">0</r><s i:type=""a:Single_foo"">1.401298E-45</s><strData i:nil=""true""/><t i:type=""a:Single_foo"">-3.40282347E+38</t><timeSpan i:type=""a:TimeSpan_foo"">P10675199DT2H48M5.4775807S</timeSpan><u i:type=""a:Single_foo"">3.40282347E+38</u><uri>http://www.microsoft.com/</uri><v i:type=""a:Single_foo"">NaN</v><w i:type=""a:Single_foo"">-INF</w><x i:type=""a:Single_foo"">INF</x><xmlQualifiedName i:type=""a:XmlQualifiedName_foo"" xmlns:b=""http://www.microsoft.com"">b:WCF</xmlQualifiedName><y i:type=""a:Int32_foo"">0</y><z i:type=""a:Int32_foo"">2147483647</z><z1 i:type=""a:Int32_foo"">-2147483648</z1><z2 i:type=""a:Int64_foo"">0</z2><z3 i:type=""a:Int64_foo"">9223372036854775807</z3><z4 i:type=""a:Int64_foo"">-9223372036854775808</z4><z5/><z6 i:type=""a:SByte_foo"">0</z6><z7 i:type=""a:SByte_foo"">127</z7><z8 i:type=""a:SByte_foo"">-128</z8><z9 i:type=""a:Int16_foo"">0</z9><z91 i:type=""a:Int16_foo"">32767</z91><z92 i:type=""a:Int16_foo"">-32768</z92><z93 i:type=""a:String_foo"">abc</z93><z94 i:type=""a:UInt16_foo"">0</z94><z95 i:type=""a:UInt16_foo"">65535</z95><z96 i:type=""a:UInt16_foo"">0</z96><z97 i:type=""a:UInt32_foo"">0</z97><z98 i:type=""a:UInt32_foo"">4294967295</z98><z99 i:type=""a:UInt32_foo"">0</z99><z990 i:type=""a:UInt64_foo"">0</z990><z991 i:type=""a:UInt64_foo"">18446744073709551615</z991><z992 i:type=""a:UInt64_foo"">0</z992><z993>AQIDBA==</z993></_data2></ObjectContainer>";
        var value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PrimitiveContainer());

        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        // Throw Exception when verification failed
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);
    }

    /// <summary>
    /// Roundtrip Datacontract types  which contains members of type enum and struct.
    /// Some enums are resolved by Resolver and others by the KT attribute.
    /// Enum and struct members are of base enum type and ValueTyperespecitively
    /// </summary>
    [Fact]
    public static void DCS_BasicRoundTripResolveEnumStructTypes()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.PrimitiveTypeResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = false
        };

        string baseline = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:EnumStructContainer"" xmlns:a=""http://www.default.com""><enumArrayData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:anyType i:type=""a:1munemy"">red</b:anyType><b:anyType i:type=""a:1munemy"">black</b:anyType><b:anyType i:type=""a:1munemy"">blue</b:anyType><b:anyType i:type=""a:1"">Autumn</b:anyType><b:anyType i:type=""a:2"">Spring</b:anyType></enumArrayData><p1 i:type=""a:VT_foo""><b>10</b></p1><p2 i:type=""a:NotSer_foo""><a>0</a></p2><p3 i:type=""a:MyStruct_foo""><globName i:nil=""true""/><value>0</value></p3></_data><_data2 i:type=""a:EnumStructContainer"" xmlns:a=""http://www.default.com""><enumArrayData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:anyType i:type=""a:1munemy"">red</b:anyType><b:anyType i:type=""a:1munemy"">black</b:anyType><b:anyType i:type=""a:1munemy"">blue</b:anyType><b:anyType i:type=""a:1"">Autumn</b:anyType><b:anyType i:type=""a:2"">Spring</b:anyType></enumArrayData><p1 i:type=""a:VT_foo""><b>10</b></p1><p2 i:type=""a:NotSer_foo""><a>0</a></p2><p3 i:type=""a:MyStruct_foo""><globName i:nil=""true""/><value>0</value></p3></_data2></ObjectContainer>";
        var value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.EnumStructContainer());

        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);
    }

    [Fact]
    public static void DCS_BasicRoundtripDCRVariation1()
    {
        SerializationTestTypes.DCRVariations dcrVariationsGoing = new SerializationTestTypes.DCRVariations();
        dcrVariationsGoing.unknownType1 = new SerializationTestTypes.Person();
        dcrVariationsGoing.unknownType2 = new SerializationTestTypes.SimpleDC();
        var setting1 = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver_Ser(),            
            PreserveObjectReferences = true
        };
        var setting2 = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver_DeSer(),            
            PreserveObjectReferences = true
        };
        var dcs1 = new DataContractSerializer(typeof(SerializationTestTypes.CustomClass), setting1);
        var dcs2 = new DataContractSerializer(typeof(SerializationTestTypes.CustomClass), setting2);
        string baseline = @"<CustomClass z:Id=""1"" i:type=""a:SerializationTestTypes.DCRVariations"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/""><unknownType1 z:Id=""2"" i:type=""a:SerializationTestTypes.Person""><Age>0</Age><Name i:nil=""true""/></unknownType1><unknownType2 z:Id=""3"" i:type=""a:SerializationTestTypes.SimpleDC""><Data i:nil=""true""/></unknownType2></CustomClass>";

        MemoryStream ms = new MemoryStream();
        dcs1.WriteObject(ms, dcrVariationsGoing);
        CompareBaseline(baseline, ms);
        ms.Position = 0;
        var dcrVariationsReturning = dcs2.ReadObject(ms);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(dcrVariationsGoing, dcrVariationsReturning);
    }

    [Fact]
    public static void DCS_BasicRoundtripDCRVariation2()
    {
        SerializationTestTypes.DCRVariations dcrVariationsGoing = new SerializationTestTypes.DCRVariations();
        dcrVariationsGoing.unknownType1 = new SerializationTestTypes.Person();
        dcrVariationsGoing.unknownType2 = new SerializationTestTypes.SimpleDC();
        var dcr1 = new SerializationTestTypes.SimpleResolver_Ser();
        var dcr2 = new SerializationTestTypes.SimpleResolver_DeSer();                
        var setting = new DataContractSerializerSettings()
        {
            PreserveObjectReferences = true
        };
        var dcs = new DataContractSerializer(typeof(SerializationTestTypes.DCRVariations), setting);
        string baseline = @"<DCRVariations z:Id=""1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><unknownType1 z:Id=""2"" i:type=""a:SerializationTestTypes.Person"" xmlns:a=""http://schemas.datacontract.org/2004/07/""><Age>0</Age><Name i:nil=""true""/></unknownType1><unknownType2 z:Id=""3"" i:type=""a:SerializationTestTypes.SimpleDC"" xmlns:a=""http://schemas.datacontract.org/2004/07/""><Data i:nil=""true""/></unknownType2></DCRVariations>";

        MemoryStream ms = new MemoryStream();
        var xmlWriter = XmlDictionaryWriter.CreateTextWriter(ms);
        dcs.WriteObject(xmlWriter, dcrVariationsGoing, dcr1);
        xmlWriter.Flush();
        CompareBaseline(baseline, ms);        
        ms.Position = 0;
        var xmlReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max);
        var dcrVariationsReturning = dcs.ReadObject(xmlReader, false, dcr2);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(dcrVariationsGoing, dcrVariationsReturning);
    }

    [Fact]
    public static void DCS_BasicRoundtripDCRVariation3()
    {
        SerializationTestTypes.DCRVariations dcrVariationsGoing = new SerializationTestTypes.DCRVariations();
        dcrVariationsGoing.unknownType1 = new SerializationTestTypes.Person();
        dcrVariationsGoing.unknownType2 = new SerializationTestTypes.SimpleDC();
        var dcr1 = new SerializationTestTypes.SimpleResolver_Ser();
        var dcr2 = new SerializationTestTypes.SimpleResolver_DeSer();
        var setting = new DataContractSerializerSettings()
        {
            DataContractResolver = dcr2,
            PreserveObjectReferences = true
        };
        var dcs = new DataContractSerializer(typeof(SerializationTestTypes.DCRVariations), setting);
        string baseline = @"<DCRVariations z:Id=""1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><unknownType1 z:Id=""2"" i:type=""a:SerializationTestTypes.Person"" xmlns:a=""http://schemas.datacontract.org/2004/07/""><Age>0</Age><Name i:nil=""true""/></unknownType1><unknownType2 z:Id=""3"" i:type=""a:SerializationTestTypes.SimpleDC"" xmlns:a=""http://schemas.datacontract.org/2004/07/""><Data i:nil=""true""/></unknownType2></DCRVariations>";

        MemoryStream ms = new MemoryStream();
        var xmlWriter = XmlDictionaryWriter.CreateTextWriter(ms);
        dcs.WriteObject(xmlWriter, dcrVariationsGoing, dcr1);
        xmlWriter.Flush();
        CompareBaseline(baseline, ms);        
        ms.Position = 0;
        var xmlReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max);
        var dcrVariationsReturning = dcs.ReadObject(xmlReader, false);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(dcrVariationsGoing, dcrVariationsReturning);
    }

    [Fact]
    public static void DCS_BasicRoundtripDCRVariation4()
    {
        SerializationTestTypes.DCRVariations dcrVariationsGoing = new SerializationTestTypes.DCRVariations();
        dcrVariationsGoing.unknownType1 = new SerializationTestTypes.Person();
        dcrVariationsGoing.unknownType2 = new SerializationTestTypes.SimpleDC();
        var dcr1 = new SerializationTestTypes.SimpleResolver_Ser();
        var dcr2 = new SerializationTestTypes.SimpleResolver_DeSer();
        var setting = new DataContractSerializerSettings()
        {
            DataContractResolver = dcr1,
            PreserveObjectReferences = true
        };
        var dcs = new DataContractSerializer(typeof(SerializationTestTypes.DCRVariations), setting);
        string baseline = @"<DCRVariations z:Id=""1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><unknownType1 z:Id=""2"" i:type=""a:SerializationTestTypes.Person"" xmlns:a=""http://schemas.datacontract.org/2004/07/""><Age>0</Age><Name i:nil=""true""/></unknownType1><unknownType2 z:Id=""3"" i:type=""a:SerializationTestTypes.SimpleDC"" xmlns:a=""http://schemas.datacontract.org/2004/07/""><Data i:nil=""true""/></unknownType2></DCRVariations>";

        MemoryStream ms = new MemoryStream();
        var xmlWriter = XmlDictionaryWriter.CreateTextWriter(ms);
        dcs.WriteObject(xmlWriter, dcrVariationsGoing);
        xmlWriter.Flush();
        CompareBaseline(baseline, ms);        
        ms.Position = 0;
        var xmlReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max);
        var dcrVariationsReturning = dcs.ReadObject(xmlReader, false, dcr2);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(dcrVariationsGoing, dcrVariationsReturning);
    }

    private static void CompareBaseline(string baseline, MemoryStream ms)
    {
        ms.Position = 0;
        string actualOutput = new StreamReader(ms).ReadToEnd();
        var result = Utils.Compare(baseline, actualOutput);
        Assert.True(result.Equal, string.Format("{1}{0}Test failed.{0}Expected: {2}{0}Actual: {3}",
                Environment.NewLine, result.ErrorMessage, baseline, actualOutput));
    }

    [Fact]
    public static void DCS_BasicRoundTripPOCOWithIgnoreDM()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.POCOTypeResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = false
        };

        string baseline = @"<POCOObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><Data i:type=""a:EmptyDCType"" xmlns:a=""http://www.Default.com""/></POCOObjectContainer>";
        var value = new SerializationTestTypes.POCOObjectContainer();
        value.NonSerializedData = new SerializationTestTypes.Person();
        
        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);

        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);
    }

    [Fact]
    public static void DCS_BasicRoundtripDCRVerifyWireformatScenarios()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.WireFormatVerificationResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = true
        };
        string typeName = typeof(SerializationTestTypes.Employee).FullName;
        string typeNamespace = typeof(SerializationTestTypes.Employee).Assembly.FullName;

        string baseline1 = $@"<Wireformat1 z:Id=""1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><alpha z:Id=""2""><person z:Id=""3""><Age>0</Age><Name i:nil=""true""/></person></alpha><beta z:Id=""4""><unknown1 z:Id=""5"" i:type=""CharClass""><c>0</c><c1>65535</c1><c2>0</c2><c3>99</c3></unknown1></beta><charlie z:Id=""6""><unknown2 z:Id=""7"" i:type=""a:{typeName}***"" xmlns:a=""{typeNamespace}***""><dateHired xmlns=""NonExistNamespace"">0001-01-01T00:00:00</dateHired><individual i:nil=""true"" xmlns=""NonExistNamespace"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/><salary xmlns=""NonExistNamespace"">0</salary></unknown2></charlie></Wireformat1>";
        var value1 = new SerializationTestTypes.Wireformat1();
        var actual1 = SerializeAndDeserialize(value1, baseline1, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value1, actual1);

        string baseline2 = $@"<Wireformat2 z:Id=""1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><beta1 z:Id=""2""><unknown1 z:Id=""3"" i:type=""CharClass""><c>0</c><c1>65535</c1><c2>0</c2><c3>99</c3></unknown1></beta1><beta2 z:Id=""4""><unknown1 z:Id=""5"" i:type=""CharClass""><c>0</c><c1>65535</c1><c2>0</c2><c3>99</c3></unknown1></beta2><charlie z:Id=""6""><unknown2 z:Id=""7"" i:type=""a:{typeName}***"" xmlns:a=""{typeNamespace}***""><dateHired xmlns=""NonExistNamespace"">0001-01-01T00:00:00</dateHired><individual i:nil=""true"" xmlns=""NonExistNamespace"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/><salary xmlns=""NonExistNamespace"">0</salary></unknown2></charlie></Wireformat2>";
        var value2 = new SerializationTestTypes.Wireformat2();
        var actual2 = SerializeAndDeserialize(value2, baseline2, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value2, actual2);

        string baseline3 = $@"<Wireformat3 z:Id=""1"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><beta z:Id=""2""><unknown1 z:Id=""3"" i:type=""CharClass""><c>0</c><c1>65535</c1><c2>0</c2><c3>99</c3></unknown1></beta><charlie1 z:Id=""4""><unknown2 z:Id=""5"" i:type=""a:{typeName}***"" xmlns:a=""{typeNamespace}***""><dateHired xmlns=""NonExistNamespace"">0001-01-01T00:00:00</dateHired><individual i:nil=""true"" xmlns=""NonExistNamespace"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/><salary xmlns=""NonExistNamespace"">0</salary></unknown2></charlie1><charlie2 z:Id=""6""><unknown2 z:Id=""7"" i:type=""a:{typeName}***"" xmlns:a=""{typeNamespace}***""><dateHired xmlns=""NonExistNamespace"">0001-01-01T00:00:00</dateHired><individual i:nil=""true"" xmlns=""NonExistNamespace"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/><salary xmlns=""NonExistNamespace"">0</salary></unknown2></charlie2></Wireformat3>";
        var value3 = new SerializationTestTypes.Wireformat3();
        var actual3 = SerializeAndDeserialize(value3, baseline3, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value3, actual3);
    }

    [Fact]
    public static void DCS_BasicRoundtripDCRDelegates()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.VerySimpleResolver(),
        };

        string coreAssemblyName = typeof(System.Delegate).Assembly.FullName;
        string assemblyName = typeof(DelegateClass).Assembly.FullName;
        string baseline = $@"<DelegateClass xmlns=""http://schemas.datacontract.org/2004/07/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><container i:type=""a:Del"" z:FactoryType=""b:System.DelegateSerializationHolder"" xmlns:a=""{assemblyName}"" xmlns:b=""{coreAssemblyName}"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Delegate i:type=""b:System.DelegateSerializationHolder+DelegateEntry"" xmlns=""""><assembly xmlns=""http://schemas.datacontract.org/2004/07/System"">{assemblyName}</assembly><delegateEntry i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/System""/><methodName xmlns=""http://schemas.datacontract.org/2004/07/System"">TestingTheDelegate</methodName><target i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/System""/><targetTypeAssembly xmlns=""http://schemas.datacontract.org/2004/07/System"">{assemblyName}</targetTypeAssembly><targetTypeName xmlns=""http://schemas.datacontract.org/2004/07/System"">DelegateClass</targetTypeName><type xmlns=""http://schemas.datacontract.org/2004/07/System"">Del</type></Delegate><method0 i:type=""b:System.Reflection.RuntimeMethodInfo"" z:FactoryType=""b:System.Reflection.MemberInfoSerializationHolder"" xmlns=""""><Name i:type=""c:string"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">TestingTheDelegate</Name><AssemblyName i:type=""c:string"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">{assemblyName}</AssemblyName><ClassName i:type=""c:string"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">DelegateClass</ClassName><Signature i:type=""c:string"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">Void TestingTheDelegate(People)</Signature><Signature2 i:type=""c:string"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">System.Void TestingTheDelegate(People)</Signature2><MemberType i:type=""c:int"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">8</MemberType><GenericArguments i:nil=""true""/></method0></container></DelegateClass>";
        var value = new DelegateClass();
        Del handle = DelegateClass.TestingTheDelegate;
        value.container = handle;
        People people = new People();
        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        ((Del)actual.container).Invoke(people);
        Assert.NotNull(actual);
        Assert.NotNull(actual.container);
        Assert.Equal(DelegateClass.delegateVariable, "Verifying the Delegate Test");
    }

    [Fact]
    public static void DCS_ResolveNameVariationTest()
    {
        SerializationTestTypes.ObjectContainer instance = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.UserTypeContainer());
        var setting = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.UserTypeToPrimitiveTypeResolver()           
        };
        string baseline = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:UserType"" xmlns:a=""http://www.default.com""><unknownData i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema""><id>10000</id></unknownData></_data><_data2 i:type=""a:UserType"" xmlns:a=""http://www.default.com""><unknownData i:type=""b:int"" xmlns:b=""http://www.w3.org/2001/XMLSchema""><id>10000</id></unknownData></_data2></ObjectContainer>";

        var result = SerializeAndDeserialize(instance, baseline, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(instance, result);
    }

    [Fact]
    public static void DCS_BasicPerSerializerRoundTripAndCompare_IObjectRef()
    {
        var setting = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver()
        };

        //DCExplicitInterfaceIObjRef
        string baseline1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DCExplicitInterfaceIObjRef***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCExplicitInterfaceIObjRef***""><data z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data z:Ref=""i1""/></data></_data><_data2 i:type=""a:SerializationTestTypes.DCExplicitInterfaceIObjRef***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCExplicitInterfaceIObjRef***""><data z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueDCExplicitInterfaceIObjRef = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCExplicitInterfaceIObjRef(true));
        var resultDCExplicitInterfaceIObjRef = SerializeAndDeserialize(valueDCExplicitInterfaceIObjRef, baseline1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCExplicitInterfaceIObjRef, resultDCExplicitInterfaceIObjRef);
        valueDCExplicitInterfaceIObjRef.Data.Equals(resultDCExplicitInterfaceIObjRef.Data);

        //DCIObjRef
        string baseline2 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DCIObjRef***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCIObjRef***""><data i:nil=""true""/></_data><_data2 i:type=""a:SerializationTestTypes.DCIObjRef***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCIObjRef***""><data i:nil=""true""/></_data2></ObjectContainer>";
        var valueDCIObjRef = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCIObjRef(true));
        var resutDCIObjRef = SerializeAndDeserialize(valueDCIObjRef, baseline2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCIObjRef, resutDCIObjRef);

        //SerExplicitInterfaceIObjRefReturnsPrivate
        string baseline3 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SerExplicitInterfaceIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerExplicitInterfaceIObjRefReturnsPrivate***""/><_data2 i:type=""a:SerializationTestTypes.SerExplicitInterfaceIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerExplicitInterfaceIObjRefReturnsPrivate***""/></ObjectContainer>";
        var valueSerExplicitInterfaceIObjRefReturnsPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerExplicitInterfaceIObjRefReturnsPrivate());
        var resutSerExplicitInterfaceIObjRefReturnsPrivate = SerializeAndDeserialize(valueSerExplicitInterfaceIObjRefReturnsPrivate, baseline3, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSerExplicitInterfaceIObjRefReturnsPrivate, resutSerExplicitInterfaceIObjRefReturnsPrivate);

        //SerIObjRefReturnsPrivate
        string baseline4 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SerIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerIObjRefReturnsPrivate***""/><_data2 i:type=""a:SerializationTestTypes.SerIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerIObjRefReturnsPrivate***""/></ObjectContainer>";
        var valueSerIObjRefReturnsPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerIObjRefReturnsPrivate());
        var resutSerIObjRefReturnsPrivate = SerializeAndDeserialize(valueSerIObjRefReturnsPrivate, baseline4, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSerIObjRefReturnsPrivate, resutSerIObjRefReturnsPrivate);

        //DCExplicitInterfaceIObjRefReturnsPrivate
        string baseline5 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DCExplicitInterfaceIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCExplicitInterfaceIObjRefReturnsPrivate***""><_data z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></_data></_data><_data2 i:type=""a:SerializationTestTypes.DCExplicitInterfaceIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCExplicitInterfaceIObjRefReturnsPrivate***""><_data z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueDCExplicitInterfaceIObjRefReturnsPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCExplicitInterfaceIObjRefReturnsPrivate());
        var resutDCExplicitInterfaceIObjRefReturnsPrivate = SerializeAndDeserialize(valueDCExplicitInterfaceIObjRefReturnsPrivate, baseline5, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCExplicitInterfaceIObjRefReturnsPrivate, resutDCExplicitInterfaceIObjRefReturnsPrivate);

        //DCIObjRefReturnsPrivate
        string baseline6 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DCIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCIObjRefReturnsPrivate***""><_data z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></_data></_data><_data2 i:type=""a:SerializationTestTypes.DCIObjRefReturnsPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCIObjRefReturnsPrivate***""><_data z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueDCIObjRefReturnsPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCIObjRefReturnsPrivate());
        var resutDCIObjRefReturnsPrivate = SerializeAndDeserialize(valueDCIObjRefReturnsPrivate, baseline6, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCIObjRefReturnsPrivate, resutDCIObjRefReturnsPrivate);
    }

    [Fact]
    public static void DCS_BasicPerSerializerRoundTripAndCompare_SampleTypes()
    {
        var setting = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver()
        };
        string assemblyName = typeof(DataContractSerializerTests).Assembly.FullName;
        
        //TypeNotFound
        string baselineTypeNotFound = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.TypeNotFound***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.TypeNotFound***""/><_data2 i:type=""a:SerializationTestTypes.TypeNotFound***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.TypeNotFound***""/></ObjectContainer>";
        var valueTypeNotFound = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TypeNotFound());
        var resultTypeNotFound = SerializeAndDeserialize(valueTypeNotFound, baselineTypeNotFound, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueTypeNotFound, resultTypeNotFound);

        //EmptyDCType
        string baselineEmptyDCType = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.EmptyDCType***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.EmptyDCType***""/><_data2 i:type=""a:SerializationTestTypes.EmptyDCType***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.EmptyDCType***""/></ObjectContainer>";
        var valueEmptyDCType = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.EmptyDCType());
        var resultEmptyDCType = SerializeAndDeserialize(valueEmptyDCType, baselineEmptyDCType, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueEmptyDCType, resultEmptyDCType);

        //ObjectContainer
        string baselineObjectContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.ObjectContainer***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.ObjectContainer***""><_data i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</_data><_data2 i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</_data2></_data><_data2 i:type=""a:SerializationTestTypes.ObjectContainer***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.ObjectContainer***""><_data i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</_data><_data2 i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</_data2></_data2></ObjectContainer>";
        var valueObjectContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ObjectContainer(true));
        var resultObjectContainer = SerializeAndDeserialize(valueObjectContainer, baselineObjectContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueObjectContainer, resultObjectContainer);

        //POCOObjectContainer
        string baselinePOCOObjectContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.POCOObjectContainer***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.POCOObjectContainer***""><Data i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</Data></_data><_data2 i:type=""a:SerializationTestTypes.POCOObjectContainer***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.POCOObjectContainer***""><Data i:type=""b:boolean"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">true</Data></_data2></ObjectContainer>";
        var valuePOCOObjectContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.POCOObjectContainer(true));
        var resultPOCOObjectContainer = SerializeAndDeserialize(valuePOCOObjectContainer, baselinePOCOObjectContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePOCOObjectContainer, resultPOCOObjectContainer);

        //CircularLink
        string baselineCircularLink = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CircularLink***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CircularLink***""><Link z:Id=""i2""><Link z:Id=""i3""><Link z:Ref=""i1""/><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink z:Id=""i4""><Link z:Id=""i5""><Link z:Id=""i6"" i:type=""b:SerializationTestTypes.CircularLinkDerived***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CircularLinkDerived***""><Link z:Ref=""i4""/><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink i:nil=""true""/></Link><RandomHangingLink i:nil=""true""/></RandomHangingLink></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCircularLink = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CircularLink(true));
        var resultCircularLink = SerializeAndDeserialize(valueCircularLink, baselineCircularLink, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCircularLink, resultCircularLink);

        //CircularLinkDerived
        string baselineCircularLinkDerived = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CircularLinkDerived***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CircularLinkDerived***""><Link i:nil=""true""/><RandomHangingLink i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCircularLinkDerived = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CircularLinkDerived(true));
        var resultCircularLinkDerived = SerializeAndDeserialize(valueCircularLinkDerived, baselineCircularLinkDerived, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCircularLinkDerived, resultCircularLinkDerived);

        //KT1Base
        string baselineKT1Base = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.KT1Base***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT1Base***""><BData z:Id=""i2"" i:type=""b:SerializationTestTypes.KT1Derived***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT1Derived***""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></BData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueKT1Base = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.KT1Base(true));
        var resultKT1Base = SerializeAndDeserialize(valueKT1Base, baselineKT1Base, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueKT1Base, resultKT1Base);

        //KT1Derived
        string baselineKT1Derived = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.KT1Derived***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT1Derived***""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueKT1Derived = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.KT1Derived());
        var resultKT1Derived = SerializeAndDeserialize(valueKT1Derived, baselineKT1Derived, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueKT1Derived, resultKT1Derived);

        //KT2Base
        string baselineKT2Base = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.KT2Base***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT2Base***""><BData z:Id=""i2"" i:type=""b:SerializationTestTypes.KT2Derived***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT2Derived***""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></BData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueKT2Base = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.KT2Base(true));
        var resultKT2Base = SerializeAndDeserialize(valueKT2Base, baselineKT2Base, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueKT2Base, resultKT2Base);

        //KT3BaseKTMReturnsPrivateType
        string baselineKT3BaseKTMReturnsPrivateType = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.KT3BaseKTMReturnsPrivateType***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT3BaseKTMReturnsPrivateType***""><BData z:Id=""i2"" i:type=""KT3DerivedPrivate""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></BData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueKT3BaseKTMReturnsPrivateType = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.KT3BaseKTMReturnsPrivateType(true));
        var resultKT3BaseKTMReturnsPrivateType = SerializeAndDeserialize(valueKT3BaseKTMReturnsPrivateType, baselineKT3BaseKTMReturnsPrivateType, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueKT3BaseKTMReturnsPrivateType, resultKT3BaseKTMReturnsPrivateType);

        //KT2Derived
        string baselineKT2Derived = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.KT2Derived***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT2Derived***""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueKT2Derived = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.KT2Derived());
        var resultKT2Derived = SerializeAndDeserialize(valueKT2Derived, baselineKT2Derived, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueKT2Derived, resultKT2Derived);

        //CB1
        string baselineCB1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CB1***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CB1***""><anyType z:Id=""i2"" i:type=""b:SerializationTestTypes.PublicDC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></anyType><anyType z:Id=""i3"" i:type=""b:SerializationTestTypes.PublicDCClassPublicDM***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM***""><Data i:nil=""true""/></anyType><anyType z:Id=""i4"" i:type=""b:SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***""><_data/></anyType></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCB1 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CB1(true));
        var resultCB1 = SerializeAndDeserialize(valueCB1, baselineCB1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCB1, resultCB1);

        //ArrayListWithCDCFilledPublicTypes
        string baselineArrayListWithCDCFilledPublicTypes = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.ArrayListWithCDCFilledPublicTypes***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.ArrayListWithCDCFilledPublicTypes***""><List xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:anyType z:Id=""i2"" i:type=""c:SerializationTestTypes.PublicDC***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:anyType><b:anyType z:Id=""i3"" i:type=""c:SerializationTestTypes.PublicDCClassPublicDM***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM***""><Data>No change</Data></b:anyType></List></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueArrayListWithCDCFilledPublicTypes = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ArrayListWithCDCFilledPublicTypes(true));
        var resultArrayListWithCDCFilledPublicTypes = SerializeAndDeserialize(valueArrayListWithCDCFilledPublicTypes, baselineArrayListWithCDCFilledPublicTypes, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueArrayListWithCDCFilledPublicTypes, resultArrayListWithCDCFilledPublicTypes);

        ////not support
        ////ArrayListWithCDCFilledWithMixedTypes
        //string baselineArrayListWithCDCFilledWithMixedTypes = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.ArrayListWithCDCFilledWithMixedTypes***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.ArrayListWithCDCFilledWithMixedTypes***""><List xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:anyType z:Id=""i2"" i:type=""c:SerializationTestTypes.PublicDC***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:anyType><b:anyType z:Id=""i3"" i:type=""c:SerializationTestTypes.PublicDCClassPublicDM***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM***""><Data>No change</Data></b:anyType><b:anyType z:Id=""i4"" i:type=""c:SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***""><_data/></b:anyType><b:anyType z:Id=""i5"" i:type=""PrivateDCClassPublicDM_DerivedDCClassPrivate""><Data>Data</Data></b:anyType><b:anyType z:Id=""i6"" i:type=""PrivateDCClassPrivateDM""><_data>No change</_data></b:anyType><b:anyType z:Id=""i7"" i:type=""PrivateCallBackSample_IDeserializationCallback""><Data>Data</Data></b:anyType><b:anyType z:Id=""i8"" i:type=""PrivateCallBackSample_OnDeserialized""><Data>Data</Data></b:anyType><b:anyType z:Id=""i9"" i:type=""PrivateCallBackSample_OnSerialized""><Data>Data</Data></b:anyType><b:anyType i:type=""PrivateDCStruct""><Data>2147483647</Data></b:anyType><b:anyType i:type=""c:SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"">68656C6C6F20776F726C64</b:anyType><b:anyType i:type=""PrivateIXmlSerializables"">68656C6C6F20776F726C64</b:anyType><b:anyType z:Id=""i10"" i:type=""c:SerializationTestTypes.Derived_Override_Prop_GetPrivate_Private***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived_Override_Prop_GetPrivate_Private***""><Data>No change</Data><Data>No change</Data></b:anyType><b:anyType i:type=""c:SerializationTestTypes.DerivedFromPriC***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedFromPriC***""><a>0</a><b i:nil=""true""/><c>100</c><d>100</d></b:anyType></List></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        //var valueArrayListWithCDCFilledWithMixedTypes = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ArrayListWithCDCFilledWithMixedTypes(true));
        //var resultArrayListWithCDCFilledWithMixedTypes = SerializeAndDeserialize(valueArrayListWithCDCFilledWithMixedTypes, baselineArrayListWithCDCFilledWithMixedTypes, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valueArrayListWithCDCFilledWithMixedTypes, resultArrayListWithCDCFilledWithMixedTypes);

        //CollectionBaseWithCDCFilledPublicTypes
        string baselineCollectionBaseWithCDCFilledPublicTypes = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CollectionBaseWithCDCFilledPublicTypes***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CollectionBaseWithCDCFilledPublicTypes***""><anyType z:Id=""i2"" i:type=""b:SerializationTestTypes.PublicDC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></anyType><anyType z:Id=""i3"" i:type=""b:SerializationTestTypes.PublicDCClassPublicDM***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM***""><Data>No change</Data></anyType></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCollectionBaseWithCDCFilledPublicTypes = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CollectionBaseWithCDCFilledPublicTypes(true));
        var resultCollectionBaseWithCDCFilledPublicTypes = SerializeAndDeserialize(valueCollectionBaseWithCDCFilledPublicTypes, baselineCollectionBaseWithCDCFilledPublicTypes, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCollectionBaseWithCDCFilledPublicTypes, resultCollectionBaseWithCDCFilledPublicTypes);

        ////not support
        ////CollectionBaseWithCDCFilledWithMixedTypes
        //string baselineCollectionBaseWithCDCFilledWithMixedTypes = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CollectionBaseWithCDCFilledWithMixedTypes***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CollectionBaseWithCDCFilledWithMixedTypes***""><anyType z:Id=""i2"" i:type=""b:SerializationTestTypes.PublicDC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></anyType><anyType z:Id=""i3"" i:type=""b:SerializationTestTypes.PublicDCClassPublicDM***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM***""><Data>No change</Data></anyType><anyType z:Id=""i4"" i:type=""b:SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***""><_data/></anyType><anyType z:Id=""i5"" i:type=""PrivateDCClassPublicDM_DerivedDCClassPrivate""><Data>Data</Data></anyType><anyType z:Id=""i6"" i:type=""PrivateDCClassPrivateDM""><_data>No change</_data></anyType><anyType z:Id=""i7"" i:type=""PrivateCallBackSample_IDeserializationCallback""><Data>Data</Data></anyType><anyType z:Id=""i8"" i:type=""PrivateCallBackSample_OnDeserialized""><Data>Data</Data></anyType><anyType z:Id=""i9"" i:type=""PrivateCallBackSample_OnSerialized""><Data>Data</Data></anyType><anyType i:type=""PrivateDCStruct""><Data>2147483647</Data></anyType><anyType i:type=""b:SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"">68656C6C6F20776F726C64</anyType><anyType i:type=""PrivateIXmlSerializables"">68656C6C6F20776F726C64</anyType><anyType z:Id=""i10"" i:type=""b:SerializationTestTypes.Derived_Override_Prop_GetPrivate_Private***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived_Override_Prop_GetPrivate_Private***""><Data>No change</Data><Data>No change</Data></anyType><anyType i:type=""b:SerializationTestTypes.DerivedFromPriC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedFromPriC***""><a>0</a><b i:nil=""true""/><c>100</c><d>100</d></anyType></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        //var valueCollectionBaseWithCDCFilledWithMixedTypes = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CollectionBaseWithCDCFilledWithMixedTypes(true));
        //var resultCollectionBaseWithCDCFilledWithMixedTypes = SerializeAndDeserialize(valueCollectionBaseWithCDCFilledWithMixedTypes, baselineCollectionBaseWithCDCFilledWithMixedTypes, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCollectionBaseWithCDCFilledWithMixedTypes, resultCollectionBaseWithCDCFilledWithMixedTypes);

        //DCHashtableContainerPublic
        string baselineDCHashtableContainerPublic = "";
        var valueDCHashtableContainerPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCHashtableContainerPublic(true));
        var resultDCHashtableContainerPublic = SerializeAndDeserialize(valueDCHashtableContainerPublic, baselineDCHashtableContainerPublic, setting, skipStringCompare: true);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCHashtableContainerPublic, resultDCHashtableContainerPublic);

        ////not support
        ////DCHashtableContainerMixedTypes
        //string baselineDCHashtableContainerMixedTypes = "";
        //var valueDCHashtableContainerMixedTypes = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCHashtableContainerMixedTypes(true));
        //var resultDCHashtableContainerMixedTypes = SerializeAndDeserialize(valueDCHashtableContainerMixedTypes, baselineDCHashtableContainerMixedTypes, setting, skipStringCompare:true);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCHashtableContainerMixedTypes, resultDCHashtableContainerMixedTypes);

        //CustomGenericContainerPrivateType1
        string baselineCustomGenericContainerPrivateType1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPrivateType1***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPrivateType1***""><_data1 z:Id=""i2""><t z:Id=""i3""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></t></_data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPrivateType1 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPrivateType1());
        var resultCustomGenericContainerPrivateType1 = SerializeAndDeserialize(valueCustomGenericContainerPrivateType1, baselineCustomGenericContainerPrivateType1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPrivateType1, resultCustomGenericContainerPrivateType1);

        //CustomGenericContainerPrivateType2
        string baselineCustomGenericContainerPrivateType2 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPrivateType2***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPrivateType2***""><_data1 z:Id=""i2""><k z:Id=""i3""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></k><t z:Id=""i4""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></t></_data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPrivateType2 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPrivateType2());
        var resultCustomGenericContainerPrivateType2 = SerializeAndDeserialize(valueCustomGenericContainerPrivateType2, baselineCustomGenericContainerPrivateType2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPrivateType2, resultCustomGenericContainerPrivateType2);

        //CustomGenericContainerPrivateType3
        string baselineCustomGenericContainerPrivateType3 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPrivateType3***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPrivateType3***""><_data1 z:Id=""i2""><k z:Id=""i3""><Data i:nil=""true""/></k><t z:Id=""i4""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></t></_data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPrivateType3 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPrivateType3());
        var resultCustomGenericContainerPrivateType3 = SerializeAndDeserialize(valueCustomGenericContainerPrivateType3, baselineCustomGenericContainerPrivateType3, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPrivateType3, resultCustomGenericContainerPrivateType3);

        //CustomGenericContainerPrivateType4
        string baselineCustomGenericContainerPrivateType4 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPrivateType4***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPrivateType4***""><_data1 z:Id=""i2""><k z:Id=""i3""><_data/></k><t z:Id=""i4""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></t></_data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPrivateType4 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPrivateType4());
        var resultCustomGenericContainerPrivateType4 = SerializeAndDeserialize(valueCustomGenericContainerPrivateType4, baselineCustomGenericContainerPrivateType4, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPrivateType4, resultCustomGenericContainerPrivateType4);

        //CustomGenericContainerPublicType1
        string baselineCustomGenericContainerPublicType1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPublicType1***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPublicType1***""><data1 z:Id=""i2""><t z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></t></data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPublicType1 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPublicType1());
        var resultCustomGenericContainerPublicType1 = SerializeAndDeserialize(valueCustomGenericContainerPublicType1, baselineCustomGenericContainerPublicType1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPublicType1, resultCustomGenericContainerPublicType1);

        //CustomGenericContainerPublicType2
        string baselineCustomGenericContainerPublicType2 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPublicType2***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPublicType2***""><data1 z:Id=""i2""><k z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></k><t z:Id=""i4""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></t></data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPublicType2 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPublicType2());
        var resultCustomGenericContainerPublicType2 = SerializeAndDeserialize(valueCustomGenericContainerPublicType2, baselineCustomGenericContainerPublicType2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPublicType2, resultCustomGenericContainerPublicType2);

        //CustomGenericContainerPublicType3
        string baselineCustomGenericContainerPublicType3 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPublicType3***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPublicType3***""><data1 z:Id=""i2""><k z:Id=""i3""><Data i:nil=""true""/></k><t z:Id=""i4""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></t></data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPublicType3 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPublicType3());
        var resultCustomGenericContainerPublicType3 = SerializeAndDeserialize(valueCustomGenericContainerPublicType3, baselineCustomGenericContainerPublicType3, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPublicType3, resultCustomGenericContainerPublicType3);

        //CustomGenericContainerPublicType4
        string baselineCustomGenericContainerPublicType4 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGenericContainerPublicType4***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGenericContainerPublicType4***""><data1 z:Id=""i2""><k z:Id=""i3""><Data i:nil=""true""/></k><t z:Id=""i4""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></t></data1></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGenericContainerPublicType4 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGenericContainerPublicType4());
        var resultCustomGenericContainerPublicType4 = SerializeAndDeserialize(valueCustomGenericContainerPublicType4, baselineCustomGenericContainerPublicType4, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGenericContainerPublicType4, resultCustomGenericContainerPublicType4);

        //CustomGeneric1
        string baselineCustomGeneric1 = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGeneric1`1[[SerializationTestTypes.KT1Base, {assemblyName}]]***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGeneric1`1[[SerializationTestTypes.KT1Base, {assemblyName}]]***""><t z:Id=""i2""><BData z:Id=""i3"" i:type=""b:SerializationTestTypes.KT1Derived***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT1Derived***""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></BData></t></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var customGeneric1 = new SerializationTestTypes.CustomGeneric1<SerializationTestTypes.KT1Base>();
        customGeneric1.t = new SerializationTestTypes.KT1Base(true);
        var valueCustomGeneric1 = new SerializationTestTypes.ObjectContainer(customGeneric1);
        var resultCustomGeneric1 = SerializeAndDeserialize(valueCustomGeneric1, baselineCustomGeneric1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGeneric1, resultCustomGeneric1);

        //CustomGeneric2
        string baselineCustomGeneric2 = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGeneric2`2[[SerializationTestTypes.KT1Base, {assemblyName}],[SerializationTestTypes.NonDCPerson, {assemblyName}]]***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGeneric2`2[[SerializationTestTypes.KT1Base, {assemblyName}],[SerializationTestTypes.NonDCPerson, {assemblyName}]]***""><k><Age>20</Age><Name>jeff</Name></k><t z:Id=""i2""><BData z:Id=""i3"" i:type=""b:SerializationTestTypes.KT1Derived***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT1Derived***""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></BData></t></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var customGeneric2 = new SerializationTestTypes.CustomGeneric2<SerializationTestTypes.KT1Base, SerializationTestTypes.NonDCPerson>();
        customGeneric2.t = new SerializationTestTypes.KT1Base(true);
        var valueCustomGeneric2 = new SerializationTestTypes.ObjectContainer(customGeneric2);
        var resultCustomGeneric2 = SerializeAndDeserialize(valueCustomGeneric2, baselineCustomGeneric2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGeneric2, resultCustomGeneric2);

        //GenericContainer
        string baselineGenericContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.GenericContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.GenericContainer***""><GenericData z:Id=""i2"" i:type=""GenericBaseOfSimpleBaseContainermrfXJLu8""><genericData z:Id=""i3"" i:type=""b:SerializationTestTypes.SimpleBaseContainer***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleBaseContainer***""><Base1 i:nil=""true""/><Base2 i:nil=""true""/></genericData></GenericData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueGenericContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.GenericContainer(true));
        var resultGenericContainer = SerializeAndDeserialize(valueGenericContainer, baselineGenericContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueGenericContainer, resultGenericContainer);

        //GenericBase
        string baselineGenericBase = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.GenericBase`1[[SerializationTestTypes.NonDCPerson, {assemblyName}]]***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.GenericBase`1[[SerializationTestTypes.NonDCPerson, {assemblyName}]]***""><genericData i:type=""b:SerializationTestTypes.NonDCPerson***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NonDCPerson***""><Age>20</Age><Name>jeff</Name></genericData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueGenericBase = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.GenericBase<SerializationTestTypes.NonDCPerson>());
        var resultGenericBase = SerializeAndDeserialize(valueGenericBase, baselineGenericBase, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueGenericBase, resultGenericBase);

        //GenericBase2
        string baselineGenericBase2 = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.GenericBase2`2[[SerializationTestTypes.KT1Base, {assemblyName}],[SerializationTestTypes.NonDCPerson, {assemblyName}]]***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.GenericBase2`2[[SerializationTestTypes.KT1Base, {assemblyName}],[SerializationTestTypes.NonDCPerson, {assemblyName}]]***""><genericData1 z:Id=""i2""><BData z:Id=""i3"" i:type=""b:SerializationTestTypes.KT1Derived***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.KT1Derived***""><BData i:nil=""true""/><DData>1/1/0001 12:00:00 AM +00:00</DData></BData></genericData1><genericData2><Age>20</Age><Name>jeff</Name></genericData2></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var genericBase2 = new SerializationTestTypes.GenericBase2<SerializationTestTypes.KT1Base, SerializationTestTypes.NonDCPerson>();
        genericBase2.genericData1 = new SerializationTestTypes.KT1Base(true);
        var valueGenericBase2 = new SerializationTestTypes.ObjectContainer(genericBase2);
        var resultGenericBase2 = SerializeAndDeserialize(valueGenericBase2, baselineGenericBase2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueGenericBase2, resultGenericBase2);

        //SimpleBase
        string baselineSimpleBase = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SimpleBase***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleBase***""><BaseData/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSimpleBase = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleBase());
        var resultSimpleBase = SerializeAndDeserialize(valueSimpleBase, baselineSimpleBase, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSimpleBase, resultSimpleBase);

        //SimpleBaseDerived
        string baselineSimpleBaseDerived = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SimpleBaseDerived***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleBaseDerived***""><BaseData/><DerivedData/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSimpleBaseDerived = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleBaseDerived());
        var resultSimpleBaseDerived = SerializeAndDeserialize(valueSimpleBaseDerived, baselineSimpleBaseDerived, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSimpleBaseDerived, resultSimpleBaseDerived);

        //SimpleBaseDerived2
        string baselineSimpleBaseDerived2 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SimpleBaseDerived2***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleBaseDerived2***""><BaseData/><DerivedData/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSimpleBaseDerived2 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleBaseDerived2());
        var resultSimpleBaseDerived2 = SerializeAndDeserialize(valueSimpleBaseDerived2, baselineSimpleBaseDerived2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSimpleBaseDerived2, resultSimpleBaseDerived2);

        //SimpleBaseContainer
        string baselineSimpleBaseContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SimpleBaseContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleBaseContainer***""><Base1 z:Id=""i2"" i:type=""b:SerializationTestTypes.SimpleBaseDerived***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleBaseDerived***""><BaseData/><DerivedData/></Base1><Base2 z:Id=""i3"" i:type=""b:SerializationTestTypes.SimpleBaseDerived2***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleBaseDerived2***""><BaseData/><DerivedData/></Base2></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSimpleBaseContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleBaseContainer(true));
        var resultSimpleBaseContainer = SerializeAndDeserialize(valueSimpleBaseContainer, baselineSimpleBaseContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSimpleBaseContainer, resultSimpleBaseContainer);

        //DCListPrivateTContainer2
        string baselineDCListPrivateTContainer2 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCListPrivateTContainer2***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCListPrivateTContainer2***""><ListData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:anyType z:Id=""i2"" i:type=""PrivateDC""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:anyType></ListData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCListPrivateTContainer2 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCListPrivateTContainer2());
        var resultDCListPrivateTContainer2 = SerializeAndDeserialize(valueDCListPrivateTContainer2, baselineDCListPrivateTContainer2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCListPrivateTContainer2, resultDCListPrivateTContainer2);

        //DCListPrivateTContainer
        string baselineDCListPrivateTContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCListPrivateTContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCListPrivateTContainer***""><_listData><PrivateDC z:Id=""i2""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></PrivateDC></_listData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCListPrivateTContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCListPrivateTContainer());
        var resultDCListPrivateTContainer = SerializeAndDeserialize(valueDCListPrivateTContainer, baselineDCListPrivateTContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCListPrivateTContainer, resultDCListPrivateTContainer);

        //DCListPublicTContainer
        string baselineDCListPublicTContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCListPublicTContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCListPublicTContainer***""><ListData><PublicDC z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></PublicDC></ListData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCListPublicTContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCListPublicTContainer());
        var resultDCListPublicTContainer = SerializeAndDeserialize(valueDCListPublicTContainer, baselineDCListPublicTContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCListPublicTContainer, resultDCListPublicTContainer);

        //DCListMixedTContainer
        string baselineDCListMixedTContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCListMixedTContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCListMixedTContainer***""><_listData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:anyType z:Id=""i2"" i:type=""c:SerializationTestTypes.PublicDC***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:anyType><b:anyType z:Id=""i3"" i:type=""PrivateDC""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:anyType></_listData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCListMixedTContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCListMixedTContainer());
        var resultDCListMixedTContainer = SerializeAndDeserialize(valueDCListMixedTContainer, baselineDCListMixedTContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCListMixedTContainer, resultDCListMixedTContainer);

        //SampleListImplicitWithDC
        string baselineSampleListImplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleListImplicitWithDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListImplicitWithDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType><anyType z:Id=""i1"" i:type=""b:SerializationTestTypes.SimpleDCWithRef***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDCWithRef***""><Data z:Id=""i2""><Data>11:59:59 PM</Data></Data><RefData z:Ref=""i2""/></anyType></_data><_data2 i:type=""a:SerializationTestTypes.SampleListImplicitWithDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListImplicitWithDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType><anyType z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueSampleListImplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListImplicitWithDC(true));
        var resultSampleListImplicitWithDC = SerializeAndDeserialize(valueSampleListImplicitWithDC, baselineSampleListImplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListImplicitWithDC, resultSampleListImplicitWithDC);

        //SampleListImplicitWithoutDC
        string baselineSampleListImplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleListImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListImplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">899288c9-8bee-41c1-a6d4-13c477ec1b29</anyType></_data><_data2 i:type=""a:SerializationTestTypes.SampleListImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListImplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">899288c9-8bee-41c1-a6d4-13c477ec1b29</anyType></_data2></ObjectContainer>";
        var valueSampleListImplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListImplicitWithoutDC(true));
        var resultSampleListImplicitWithoutDC = SerializeAndDeserialize(valueSampleListImplicitWithoutDC, baselineSampleListImplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListImplicitWithoutDC, resultSampleListImplicitWithoutDC);

        //SampleListImplicitWithCDC
        string baselineSampleListImplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListImplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListImplicitWithCDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListImplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListImplicitWithCDC(true));
        var resultSampleListImplicitWithCDC = SerializeAndDeserialize(valueSampleListImplicitWithCDC, baselineSampleListImplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListImplicitWithCDC, resultSampleListImplicitWithCDC);

        //SampleListExplicitWithDC
        string baselineSampleListExplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListExplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListExplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListExplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListExplicitWithDC(true));
        var resultSampleListExplicitWithDC = SerializeAndDeserialize(valueSampleListExplicitWithDC, baselineSampleListExplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListExplicitWithDC, resultSampleListExplicitWithDC);

        //SampleListExplicitWithoutDC
        string baselineSampleListExplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleListExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListExplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data><_data2 i:type=""a:SerializationTestTypes.SampleListExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListExplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data2></ObjectContainer>";
        var valueSampleListExplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListExplicitWithoutDC(true));
        var resultSampleListExplicitWithoutDC = SerializeAndDeserialize(valueSampleListExplicitWithoutDC, baselineSampleListExplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListExplicitWithoutDC, resultSampleListExplicitWithoutDC);

        //SampleListExplicitWithCDC
        string baselineSampleListExplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListExplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListExplicitWithCDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListExplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListExplicitWithCDC(true));
        var resultSampleListExplicitWithCDC = SerializeAndDeserialize(valueSampleListExplicitWithCDC, baselineSampleListExplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListExplicitWithCDC, resultSampleListExplicitWithCDC);

        //SampleListExplicitWithCDCContainsPrivateDC
        string baselineSampleListExplicitWithCDCContainsPrivateDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListExplicitWithCDCContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListExplicitWithCDCContainsPrivateDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item><Item z:Id=""i2"" i:type=""b:PrivateDC"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""><b:Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</b:Data></Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListExplicitWithCDCContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListExplicitWithCDCContainsPrivateDC(true));
        var resultSampleListExplicitWithCDCContainsPrivateDC = SerializeAndDeserialize(valueSampleListExplicitWithCDCContainsPrivateDC, baselineSampleListExplicitWithCDCContainsPrivateDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListExplicitWithCDCContainsPrivateDC, resultSampleListExplicitWithCDCContainsPrivateDC);

        //SampleListTImplicitWithDC
        string baselineSampleListTImplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListTImplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTImplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListTImplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTImplicitWithDC(true));
        var resultSampleListTImplicitWithDC = SerializeAndDeserialize(valueSampleListTImplicitWithDC, baselineSampleListTImplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTImplicitWithDC, resultSampleListTImplicitWithDC);

        //SampleListTImplicitWithoutDC
        string baselineSampleListTImplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleListTImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTImplicitWithoutDC***""><DC z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Id=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data><_data2 i:type=""a:SerializationTestTypes.SampleListTImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTImplicitWithoutDC***""><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueSampleListTImplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTImplicitWithoutDC(true));
        var resultSampleListTImplicitWithoutDC = SerializeAndDeserialize(valueSampleListTImplicitWithoutDC, baselineSampleListTImplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTImplicitWithoutDC, resultSampleListTImplicitWithoutDC);

        //SampleListTImplicitWithCDC
        string baselineSampleListTImplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListTImplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTImplicitWithCDC***""><Item z:Id=""i2"" xmlns=""Test""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Id=""i3"" xmlns=""Test""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListTImplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTImplicitWithCDC(true));
        var resultSampleListTImplicitWithCDC = SerializeAndDeserialize(valueSampleListTImplicitWithCDC, baselineSampleListTImplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTImplicitWithCDC, resultSampleListTImplicitWithCDC);

        //SampleListTExplicitWithDC
        string baselineSampleListTExplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListTExplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTExplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListTExplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTExplicitWithDC(true));
        var resultSampleListTExplicitWithDC = SerializeAndDeserialize(valueSampleListTExplicitWithDC, baselineSampleListTExplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTExplicitWithDC, resultSampleListTExplicitWithDC);

        //SampleListTExplicitWithoutDC
        string baselineSampleListTExplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleListTExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTExplicitWithoutDC***""><DC z:Id=""i1"" i:type=""b:SerializationTestTypes.DC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Id=""i2"" i:type=""b:SerializationTestTypes.DC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data><_data2 i:type=""a:SerializationTestTypes.SampleListTExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTExplicitWithoutDC***""><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueSampleListTExplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTExplicitWithoutDC(true));
        var resultSampleListTExplicitWithoutDC = SerializeAndDeserialize(valueSampleListTExplicitWithoutDC, baselineSampleListTExplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTExplicitWithoutDC, resultSampleListTExplicitWithoutDC);

        //SampleListTExplicitWithCDC
        string baselineSampleListTExplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListTExplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTExplicitWithCDC***""><Item z:Id=""i2"" i:type=""b:SerializationTestTypes.DC***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Id=""i3"" i:type=""b:SerializationTestTypes.DC***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListTExplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTExplicitWithCDC(true));
        var resultSampleListTExplicitWithCDC = SerializeAndDeserialize(valueSampleListTExplicitWithCDC, baselineSampleListTExplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTExplicitWithCDC, resultSampleListTExplicitWithCDC);

        //SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM
        string baselineSampleListTExplicitWithCDCContainsPublicDCClassPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM***""><Item z:Id=""i2"" i:type=""b:SerializationTestTypes.PublicDCClassPrivateDM***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM***""><_data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Id=""i3"" i:type=""b:SerializationTestTypes.PublicDCClassPrivateDM***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM***""><_data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListTExplicitWithCDCContainsPublicDCClassPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTExplicitWithCDCContainsPublicDCClassPrivateDM(true));
        var resultSampleListTExplicitWithCDCContainsPublicDCClassPrivateDM = SerializeAndDeserialize(valueSampleListTExplicitWithCDCContainsPublicDCClassPrivateDM, baselineSampleListTExplicitWithCDCContainsPublicDCClassPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTExplicitWithCDCContainsPublicDCClassPrivateDM, resultSampleListTExplicitWithCDCContainsPublicDCClassPrivateDM);

        //SampleListTExplicitWithCDCContainsPrivateDC
        string baselineSampleListTExplicitWithCDCContainsPrivateDC = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleListTExplicitWithCDCContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleListTExplicitWithCDCContainsPrivateDC***""><Item z:Id=""i2"" i:type=""b:SerializationTestTypes.PrivateDC"" xmlns=""Test"" xmlns:b=""{assemblyName}""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></Item><Item z:Id=""i3"" i:type=""b:SerializationTestTypes.PrivateDC"" xmlns=""Test"" xmlns:b=""{assemblyName}""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleListTExplicitWithCDCContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleListTExplicitWithCDCContainsPrivateDC(true));
        var resultSampleListTExplicitWithCDCContainsPrivateDC = SerializeAndDeserialize(valueSampleListTExplicitWithCDCContainsPrivateDC, baselineSampleListTExplicitWithCDCContainsPrivateDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleListTExplicitWithCDCContainsPrivateDC, resultSampleListTExplicitWithCDCContainsPrivateDC);

        //SampleICollectionTImplicitWithDC
        string baselineSampleICollectionTImplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionTImplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTImplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionTImplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionTImplicitWithDC(true));
        var resultSampleICollectionTImplicitWithDC = SerializeAndDeserialize(valueSampleICollectionTImplicitWithDC, baselineSampleICollectionTImplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionTImplicitWithDC, resultSampleICollectionTImplicitWithDC);

        //SampleICollectionTImplicitWithoutDC
        string baselineSampleICollectionTImplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleICollectionTImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTImplicitWithoutDC***""><DC z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Id=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data><_data2 i:type=""a:SerializationTestTypes.SampleICollectionTImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTImplicitWithoutDC***""><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueSampleICollectionTImplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionTImplicitWithoutDC(true));
        var resultSampleICollectionTImplicitWithoutDC = SerializeAndDeserialize(valueSampleICollectionTImplicitWithoutDC, baselineSampleICollectionTImplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionTImplicitWithoutDC, resultSampleICollectionTImplicitWithoutDC);

        //SampleICollectionTImplicitWithCDC
        string baselineSampleICollectionTImplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionTImplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTImplicitWithCDC***""><Item z:Id=""i2"" xmlns=""Test""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Id=""i3"" xmlns=""Test""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionTImplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionTImplicitWithCDC(true));
        var resultSampleICollectionTImplicitWithCDC = SerializeAndDeserialize(valueSampleICollectionTImplicitWithCDC, baselineSampleICollectionTImplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionTImplicitWithCDC, resultSampleICollectionTImplicitWithCDC);

        //SampleICollectionTExplicitWithDC
        string baselineSampleICollectionTExplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionTExplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTExplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionTExplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionTExplicitWithDC(true));
        var resultSampleICollectionTExplicitWithDC = SerializeAndDeserialize(valueSampleICollectionTExplicitWithDC, baselineSampleICollectionTExplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionTExplicitWithDC, resultSampleICollectionTExplicitWithDC);
        
        //???
        //SampleICollectionTExplicitWithoutDC
        string baselineSampleICollectionTExplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleICollectionTExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTExplicitWithoutDC***""><DC z:Id=""i1"" i:type=""b:SerializationTestTypes.DC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Id=""i2"" i:type=""b:SerializationTestTypes.DC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data><_data2 i:type=""a:SerializationTestTypes.SampleICollectionTExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTExplicitWithoutDC***""><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueSampleICollectionTExplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionTExplicitWithoutDC(true));
        var resultSampleICollectionTExplicitWithoutDC = SerializeAndDeserialize(valueSampleICollectionTExplicitWithoutDC, baselineSampleICollectionTExplicitWithoutDC, setting, skipStringCompare: true);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionTExplicitWithoutDC, resultSampleICollectionTExplicitWithoutDC);

        //???
        //SampleICollectionTExplicitWithCDC
        string baselineSampleICollectionTExplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionTExplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTExplicitWithCDC***""><Item z:Id=""i2"" i:type=""b:SerializationTestTypes.DC***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Id=""i3"" i:type=""b:SerializationTestTypes.DC***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionTExplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionTExplicitWithCDC(true));
        var resultSampleICollectionTExplicitWithCDC = SerializeAndDeserialize(valueSampleICollectionTExplicitWithCDC, baselineSampleICollectionTExplicitWithCDC, setting, skipStringCompare: true);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionTExplicitWithCDC, resultSampleICollectionTExplicitWithCDC);

        //???
        //SampleICollectionTExplicitWithCDCContainsPrivateDC
        string baselineSampleICollectionTExplicitWithCDCContainsPrivateDC = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionTExplicitWithCDCContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionTExplicitWithCDCContainsPrivateDC***""><Item z:Id=""i2"" i:type=""b:SerializationTestTypes.PrivateDC"" xmlns=""Test"" xmlns:b=""{assemblyName}""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></Item><Item z:Id=""i3"" i:type=""b:SerializationTestTypes.PrivateDC"" xmlns=""Test"" xmlns:b=""{assemblyName}""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionTExplicitWithCDCContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionTExplicitWithCDCContainsPrivateDC(true));
        var resultSampleICollectionTExplicitWithCDCContainsPrivateDC = SerializeAndDeserialize(valueSampleICollectionTExplicitWithCDCContainsPrivateDC, baselineSampleICollectionTExplicitWithCDCContainsPrivateDC, setting, skipStringCompare: true);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionTExplicitWithCDCContainsPrivateDC, resultSampleICollectionTExplicitWithCDCContainsPrivateDC);

        //SampleIEnumerableTImplicitWithDC
        string baselineSampleIEnumerableTImplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableTImplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTImplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableTImplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableTImplicitWithDC(true));
        var resultSampleIEnumerableTImplicitWithDC = SerializeAndDeserialize(valueSampleIEnumerableTImplicitWithDC, baselineSampleIEnumerableTImplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableTImplicitWithDC, resultSampleIEnumerableTImplicitWithDC);

        //SampleIEnumerableTImplicitWithoutDC
        string baselineSampleIEnumerableTImplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleIEnumerableTImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTImplicitWithoutDC***""><DC z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Id=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data><_data2 i:type=""a:SerializationTestTypes.SampleIEnumerableTImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTImplicitWithoutDC***""><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueSampleIEnumerableTImplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableTImplicitWithoutDC(true));
        var resultSampleIEnumerableTImplicitWithoutDC = SerializeAndDeserialize(valueSampleIEnumerableTImplicitWithoutDC, baselineSampleIEnumerableTImplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableTImplicitWithoutDC, resultSampleIEnumerableTImplicitWithoutDC);

        //SampleIEnumerableTImplicitWithCDC
        string baselineSampleIEnumerableTImplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableTImplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTImplicitWithCDC***""><Item z:Id=""i2"" xmlns=""Test""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Id=""i3"" xmlns=""Test""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableTImplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableTImplicitWithCDC(true));
        var resultSampleIEnumerableTImplicitWithCDC = SerializeAndDeserialize(valueSampleIEnumerableTImplicitWithCDC, baselineSampleIEnumerableTImplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableTImplicitWithCDC, resultSampleIEnumerableTImplicitWithCDC);

        //SampleIEnumerableTExplicitWithDC
        string baselineSampleIEnumerableTExplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableTExplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTExplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableTExplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableTExplicitWithDC(true));
        var resultSampleIEnumerableTExplicitWithDC = SerializeAndDeserialize(valueSampleIEnumerableTExplicitWithDC, baselineSampleIEnumerableTExplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableTExplicitWithDC, resultSampleIEnumerableTExplicitWithDC);

        //SampleIEnumerableTExplicitWithoutDC
        string baselineSampleIEnumerableTExplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleIEnumerableTExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTExplicitWithoutDC***""><DC z:Id=""i1"" i:type=""b:SerializationTestTypes.DC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Id=""i2"" i:type=""b:SerializationTestTypes.DC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></DC><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data><_data2 i:type=""a:SerializationTestTypes.SampleIEnumerableTExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTExplicitWithoutDC***""><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><DC z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></_data2></ObjectContainer>";
        var valueSampleIEnumerableTExplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableTExplicitWithoutDC(true));
        var resultSampleIEnumerableTExplicitWithoutDC = SerializeAndDeserialize(valueSampleIEnumerableTExplicitWithoutDC, baselineSampleIEnumerableTExplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableTExplicitWithoutDC, resultSampleIEnumerableTExplicitWithoutDC);

        //SampleIEnumerableTExplicitWithCDC
        string baselineSampleIEnumerableTExplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableTExplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTExplicitWithCDC***""><Item z:Id=""i2"" i:type=""b:SerializationTestTypes.DC***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Id=""i3"" i:type=""b:SerializationTestTypes.DC***"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">Monday, January 1, 0001</Data><Next i:nil=""true"" xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableTExplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableTExplicitWithCDC(true));
        var resultSampleIEnumerableTExplicitWithCDC = SerializeAndDeserialize(valueSampleIEnumerableTExplicitWithCDC, baselineSampleIEnumerableTExplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableTExplicitWithCDC, resultSampleIEnumerableTExplicitWithCDC);

        //SampleIEnumerableTExplicitWithCDCContainsPrivateDC
        string baselineSampleIEnumerableTExplicitWithCDCContainsPrivateDC = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableTExplicitWithCDCContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableTExplicitWithCDCContainsPrivateDC***""><Item z:Id=""i2"" i:type=""b:SerializationTestTypes.PrivateDC"" xmlns=""Test"" xmlns:b=""{assemblyName}""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></Item><Item z:Id=""i3"" i:type=""b:SerializationTestTypes.PrivateDC"" xmlns=""Test"" xmlns:b=""{assemblyName}""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></Item><Item z:Ref=""i2"" xmlns=""Test""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableTExplicitWithCDCContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableTExplicitWithCDCContainsPrivateDC(true));
        var resultSampleIEnumerableTExplicitWithCDCContainsPrivateDC = SerializeAndDeserialize(valueSampleIEnumerableTExplicitWithCDCContainsPrivateDC, baselineSampleIEnumerableTExplicitWithCDCContainsPrivateDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableTExplicitWithCDCContainsPrivateDC, resultSampleIEnumerableTExplicitWithCDCContainsPrivateDC);

        //SampleICollectionImplicitWithDC
        string baselineSampleICollectionImplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionImplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionImplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionImplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionImplicitWithDC(true));
        var resultSampleICollectionImplicitWithDC = SerializeAndDeserialize(valueSampleICollectionImplicitWithDC, baselineSampleICollectionImplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionImplicitWithDC, resultSampleICollectionImplicitWithDC);

        //SampleICollectionImplicitWithoutDC
        string baselineSampleICollectionImplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleICollectionImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionImplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data><_data2 i:type=""a:SerializationTestTypes.SampleICollectionImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionImplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data2></ObjectContainer>";
        var valueSampleICollectionImplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionImplicitWithoutDC(true));
        var resultSampleICollectionImplicitWithoutDC = SerializeAndDeserialize(valueSampleICollectionImplicitWithoutDC, baselineSampleICollectionImplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionImplicitWithoutDC, resultSampleICollectionImplicitWithoutDC);

        //SampleICollectionImplicitWithCDC
        string baselineSampleICollectionImplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionImplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionImplicitWithCDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionImplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionImplicitWithCDC(true));
        var resultSampleICollectionImplicitWithCDC = SerializeAndDeserialize(valueSampleICollectionImplicitWithCDC, baselineSampleICollectionImplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionImplicitWithCDC, resultSampleICollectionImplicitWithCDC);

        //SampleICollectionExplicitWithDC
        string baselineSampleICollectionExplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionExplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionExplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionExplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionExplicitWithDC(true));
        var resultSampleICollectionExplicitWithDC = SerializeAndDeserialize(valueSampleICollectionExplicitWithDC, baselineSampleICollectionExplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionExplicitWithDC, resultSampleICollectionExplicitWithDC);

        //SampleICollectionExplicitWithoutDC
        string baselineSampleICollectionExplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleICollectionExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionExplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data><_data2 i:type=""a:SerializationTestTypes.SampleICollectionExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionExplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data2></ObjectContainer>";
        var valueSampleICollectionExplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionExplicitWithoutDC(true));
        var resultSampleICollectionExplicitWithoutDC = SerializeAndDeserialize(valueSampleICollectionExplicitWithoutDC, baselineSampleICollectionExplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionExplicitWithoutDC, resultSampleICollectionExplicitWithoutDC);

        //SampleICollectionExplicitWithCDC
        string baselineSampleICollectionExplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionExplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionExplicitWithCDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionExplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionExplicitWithCDC(true));
        var resultSampleICollectionExplicitWithCDC = SerializeAndDeserialize(valueSampleICollectionExplicitWithCDC, baselineSampleICollectionExplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionExplicitWithCDC, resultSampleICollectionExplicitWithCDC);

        //SampleICollectionExplicitWithCDCContainsPrivateDC
        string baselineSampleICollectionExplicitWithCDCContainsPrivateDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleICollectionExplicitWithCDCContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleICollectionExplicitWithCDCContainsPrivateDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item><Item z:Id=""i2"" i:type=""b:PrivateDC"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""><b:Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</b:Data></Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleICollectionExplicitWithCDCContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleICollectionExplicitWithCDCContainsPrivateDC(true));
        var resultSampleICollectionExplicitWithCDCContainsPrivateDC = SerializeAndDeserialize(valueSampleICollectionExplicitWithCDCContainsPrivateDC, baselineSampleICollectionExplicitWithCDCContainsPrivateDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleICollectionExplicitWithCDCContainsPrivateDC, resultSampleICollectionExplicitWithCDCContainsPrivateDC);

        //SampleIEnumerableImplicitWithDC
        string baselineSampleIEnumerableImplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableImplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableImplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableImplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableImplicitWithDC(true));
        var resultSampleIEnumerableImplicitWithDC = SerializeAndDeserialize(valueSampleIEnumerableImplicitWithDC, baselineSampleIEnumerableImplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableImplicitWithDC, resultSampleIEnumerableImplicitWithDC);

        //SampleIEnumerableImplicitWithoutDC
        string baselineSampleIEnumerableImplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleIEnumerableImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableImplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data><_data2 i:type=""a:SerializationTestTypes.SampleIEnumerableImplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableImplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data2></ObjectContainer>";
        var valueSampleIEnumerableImplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableImplicitWithoutDC(true));
        var resultSampleIEnumerableImplicitWithoutDC = SerializeAndDeserialize(valueSampleIEnumerableImplicitWithoutDC, baselineSampleIEnumerableImplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableImplicitWithoutDC, resultSampleIEnumerableImplicitWithoutDC);

        //SampleIEnumerableImplicitWithCDC
        string baselineSampleIEnumerableImplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableImplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableImplicitWithCDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableImplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableImplicitWithCDC(true));
        var resultSampleIEnumerableImplicitWithCDC = SerializeAndDeserialize(valueSampleIEnumerableImplicitWithCDC, baselineSampleIEnumerableImplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableImplicitWithCDC, resultSampleIEnumerableImplicitWithCDC);

        //SampleIEnumerableExplicitWithDC
        string baselineSampleIEnumerableExplicitWithDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableExplicitWithDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableExplicitWithDC***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableExplicitWithDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableExplicitWithDC(true));
        var resultSampleIEnumerableExplicitWithDC = SerializeAndDeserialize(valueSampleIEnumerableExplicitWithDC, baselineSampleIEnumerableExplicitWithDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableExplicitWithDC, resultSampleIEnumerableExplicitWithDC);

        //SampleIEnumerableExplicitWithoutDC
        string baselineSampleIEnumerableExplicitWithoutDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SampleIEnumerableExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableExplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data><_data2 i:type=""a:SerializationTestTypes.SampleIEnumerableExplicitWithoutDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableExplicitWithoutDC***""><anyType i:type=""b:dateTime"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</anyType><anyType i:type=""b:duration"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">P10675199DT2H48M5.4775807S</anyType><anyType i:type=""b:string"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</anyType><anyType i:type=""b:double"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</anyType><anyType i:type=""b:guid"" xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</anyType></_data2></ObjectContainer>";
        var valueSampleIEnumerableExplicitWithoutDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableExplicitWithoutDC(true));
        var resultSampleIEnumerableExplicitWithoutDC = SerializeAndDeserialize(valueSampleIEnumerableExplicitWithoutDC, baselineSampleIEnumerableExplicitWithoutDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableExplicitWithoutDC, resultSampleIEnumerableExplicitWithoutDC);

        //SampleIEnumerableExplicitWithCDC
        string baselineSampleIEnumerableExplicitWithCDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableExplicitWithCDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableExplicitWithCDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableExplicitWithCDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableExplicitWithCDC(true));
        var resultSampleIEnumerableExplicitWithCDC = SerializeAndDeserialize(valueSampleIEnumerableExplicitWithCDC, baselineSampleIEnumerableExplicitWithCDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableExplicitWithCDC, resultSampleIEnumerableExplicitWithCDC);

        //SampleIEnumerableExplicitWithCDCContainsPrivateDC
        string baselineSampleIEnumerableExplicitWithCDCContainsPrivateDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.SampleIEnumerableExplicitWithCDCContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SampleIEnumerableExplicitWithCDCContainsPrivateDC***""><Item i:type=""b:dateTime"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">0001-01-01T00:00:00</Item><Item i:type=""z:duration"" xmlns=""Test"">P10675199DT2H48M5.4775807S</Item><Item i:type=""b:string"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema""/><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">1.7976931348623157E+308</Item><Item i:type=""b:double"" xmlns=""Test"" xmlns:b=""http://www.w3.org/2001/XMLSchema"">-INF</Item><Item i:type=""z:guid"" xmlns=""Test"">0c9e174e-cdd8-4b68-a70d-aaeb26c7deeb</Item><Item z:Id=""i2"" i:type=""b:PrivateDC"" xmlns=""Test"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""><b:Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</b:Data></Item></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueSampleIEnumerableExplicitWithCDCContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SampleIEnumerableExplicitWithCDCContainsPrivateDC(true));
        var resultSampleIEnumerableExplicitWithCDCContainsPrivateDC = SerializeAndDeserialize(valueSampleIEnumerableExplicitWithCDCContainsPrivateDC, baselineSampleIEnumerableExplicitWithCDCContainsPrivateDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSampleIEnumerableExplicitWithCDCContainsPrivateDC, resultSampleIEnumerableExplicitWithCDCContainsPrivateDC);

        //MyIDictionaryContainsPublicDC
        string baselineMyIDictionaryContainsPublicDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.MyIDictionaryContainsPublicDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyIDictionaryContainsPublicDC***""><DictItem xmlns=""MyDictNS1""><DictKey z:Id=""i2"" i:type=""b:SerializationTestTypes.PublicDC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></DictKey><DictValue z:Id=""i3"" i:type=""b:SerializationTestTypes.PublicDC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></DictValue></DictItem></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueMyIDictionaryContainsPublicDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.MyIDictionaryContainsPublicDC(true));
        var resultMyIDictionaryContainsPublicDC = SerializeAndDeserialize(valueMyIDictionaryContainsPublicDC, baselineMyIDictionaryContainsPublicDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueMyIDictionaryContainsPublicDC, resultMyIDictionaryContainsPublicDC);

        //MyIDictionaryContainsPublicDCExplicit
        string baselineMyIDictionaryContainsPublicDCExplicit = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.MyIDictionaryContainsPublicDCExplicit***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyIDictionaryContainsPublicDCExplicit***""><DictItem xmlns=""MyDictNS1""><DictKey z:Id=""i2"" i:type=""b:SerializationTestTypes.PublicDC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></DictKey><DictValue z:Id=""i3"" i:type=""b:SerializationTestTypes.PublicDC***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"">55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></DictValue></DictItem></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueMyIDictionaryContainsPublicDCExplicit = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.MyIDictionaryContainsPublicDCExplicit(true));
        var resultMyIDictionaryContainsPublicDCExplicit = SerializeAndDeserialize(valueMyIDictionaryContainsPublicDCExplicit, baselineMyIDictionaryContainsPublicDCExplicit, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueMyIDictionaryContainsPublicDCExplicit, resultMyIDictionaryContainsPublicDCExplicit);

        //MyIDictionaryContainsPrivateDC
        string baselineMyIDictionaryContainsPrivateDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.MyIDictionaryContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyIDictionaryContainsPrivateDC***""><DictItem xmlns=""MyDictNS2""><DictKey z:Id=""i2"" i:type=""b:PrivateDC"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""><b:Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</b:Data></DictKey><DictValue z:Id=""i3"" i:type=""b:PrivateDC"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""><b:Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</b:Data></DictValue></DictItem><DictItem xmlns=""MyDictNS2""><DictKey z:Id=""i4"" i:type=""b:SerializationTestTypes.PublicDCClassPrivateDM***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM***""><_data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></DictKey><DictValue z:Id=""i5"" i:type=""b:SerializationTestTypes.PublicDCClassPrivateDM***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM***""><_data xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""/></DictValue></DictItem></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueMyIDictionaryContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.MyIDictionaryContainsPrivateDC(true));
        var resultMyIDictionaryContainsPrivateDC = SerializeAndDeserialize(valueMyIDictionaryContainsPrivateDC, baselineMyIDictionaryContainsPrivateDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueMyIDictionaryContainsPrivateDC, resultMyIDictionaryContainsPrivateDC);

        //MyGenericIDictionaryKVContainsPublicDC
        string baselineMyGenericIDictionaryKVContainsPublicDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDC***""><KeyValueOfPublicDCPublicDCzETuxydO><Key z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></Key><Value z:Id=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></Value></KeyValueOfPublicDCPublicDCzETuxydO></_data><_data2 i:type=""a:SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDC***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDC***""><KeyValueOfPublicDCPublicDCzETuxydO><Key z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><Value z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></KeyValueOfPublicDCPublicDCzETuxydO></_data2></ObjectContainer>";
        var valueMyGenericIDictionaryKVContainsPublicDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDC(true));
        var resultMyGenericIDictionaryKVContainsPublicDC = SerializeAndDeserialize(valueMyGenericIDictionaryKVContainsPublicDC, baselineMyGenericIDictionaryKVContainsPublicDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueMyGenericIDictionaryKVContainsPublicDC, resultMyGenericIDictionaryKVContainsPublicDC);

        //MyGenericIDictionaryKVContainsPublicDCExplicit
        string baselineMyGenericIDictionaryKVContainsPublicDCExplicit = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDCExplicit***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDCExplicit***""><KeyValueOfPublicDCPublicDCzETuxydO><Key z:Id=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></Key><Value z:Id=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></Value></KeyValueOfPublicDCPublicDCzETuxydO></_data><_data2 i:type=""a:SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDCExplicit***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDCExplicit***""><KeyValueOfPublicDCPublicDCzETuxydO><Key z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/><Value z:Ref=""i2"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></KeyValueOfPublicDCPublicDCzETuxydO></_data2></ObjectContainer>";
        var valueMyGenericIDictionaryKVContainsPublicDCExplicit = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.MyGenericIDictionaryKVContainsPublicDCExplicit(true));
        var resultMyGenericIDictionaryKVContainsPublicDCExplicit = SerializeAndDeserialize(valueMyGenericIDictionaryKVContainsPublicDCExplicit, baselineMyGenericIDictionaryKVContainsPublicDCExplicit, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueMyGenericIDictionaryKVContainsPublicDCExplicit, resultMyGenericIDictionaryKVContainsPublicDCExplicit);

        //MyGenericIDictionaryKVContainsPrivateDC
        string baselineMyGenericIDictionaryKVContainsPrivateDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.MyGenericIDictionaryKVContainsPrivateDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyGenericIDictionaryKVContainsPrivateDC***""><DictItem xmlns=""MyDictNS""><DictKey z:Id=""i2"" i:type=""b:PrivateDC"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""><b:Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</b:Data></DictKey><DictValue z:Id=""i3"" i:type=""b:PrivateDC"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes""><b:Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</b:Data></DictValue></DictItem></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueMyGenericIDictionaryKVContainsPrivateDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.MyGenericIDictionaryKVContainsPrivateDC(true));
        var resultMyGenericIDictionaryKVContainsPrivateDC = SerializeAndDeserialize(valueMyGenericIDictionaryKVContainsPrivateDC, baselineMyGenericIDictionaryKVContainsPrivateDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueMyGenericIDictionaryKVContainsPrivateDC, resultMyGenericIDictionaryKVContainsPrivateDC);

        //DCDictionaryPrivateKTContainer
        string baselineDCDictionaryPrivateKTContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryPrivateKTContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryPrivateKTContainer***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPrivateDCPrivateDCzETuxydO><b:Key z:Id=""i2""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Key><b:Value z:Id=""i3""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Value></b:KeyValueOfPrivateDCPrivateDCzETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryPrivateKTContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryPrivateKTContainer());
        var resultDCDictionaryPrivateKTContainer = SerializeAndDeserialize(valueDCDictionaryPrivateKTContainer, baselineDCDictionaryPrivateKTContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryPrivateKTContainer, resultDCDictionaryPrivateKTContainer);

        //DCDictionaryPublicKTContainer
        string baselineDCDictionaryPublicKTContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryPublicKTContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryPublicKTContainer***""><DictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCPublicDCzETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPublicDCPublicDCzETuxydO></DictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryPublicKTContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryPublicKTContainer());
        var resultDCDictionaryPublicKTContainer = SerializeAndDeserialize(valueDCDictionaryPublicKTContainer, baselineDCDictionaryPublicKTContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryPublicKTContainer, resultDCDictionaryPublicKTContainer);

        //DCDictionaryMixedKTContainer1
        string baselineDCDictionaryMixedKTContainer1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer1***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer1***""><DictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfanyTypeanyType><b:Key z:Id=""i2"" i:type=""PrivateDC""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Key><b:Value z:Id=""i3"" i:type=""c:SerializationTestTypes.PublicDC***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfanyTypeanyType><b:KeyValueOfanyTypeanyType><b:Key z:Id=""i4"" i:type=""c:SerializationTestTypes.PublicDC***"" xmlns:c=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i5"" i:type=""PrivateDC""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Value></b:KeyValueOfanyTypeanyType></DictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer1 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer1());
        var resultDCDictionaryMixedKTContainer1 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer1, baselineDCDictionaryMixedKTContainer1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer1, resultDCDictionaryMixedKTContainer1);

        //DCDictionaryMixedKTContainer2
        string baselineDCDictionaryMixedKTContainer2 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer2***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer2***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCPrivateDCzETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Value></b:KeyValueOfPublicDCPrivateDCzETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer2 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer2());
        var resultDCDictionaryMixedKTContainer2 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer2, baselineDCDictionaryMixedKTContainer2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer2, resultDCDictionaryMixedKTContainer2);

        //DCDictionaryMixedKTContainer3
        string baselineDCDictionaryMixedKTContainer3 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer3***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer3***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPrivateDCPublicDCzETuxydO><b:Key z:Id=""i2""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPrivateDCPublicDCzETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer3 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer3());
        var resultDCDictionaryMixedKTContainer3 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer3, baselineDCDictionaryMixedKTContainer3, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer3, resultDCDictionaryMixedKTContainer3);

        //DCDictionaryMixedKTContainer4
        string baselineDCDictionaryMixedKTContainer4 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer4***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer4***""><DictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPublicPublicDCzETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPublicDCDerivedPublicPublicDCzETuxydO></DictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer4 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer4());
        var resultDCDictionaryMixedKTContainer4 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer4, baselineDCDictionaryMixedKTContainer4, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer4, resultDCDictionaryMixedKTContainer4);

        //DCDictionaryMixedKTContainer5
        string baselineDCDictionaryMixedKTContainer5 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer5***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer5***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPrivatePublicDCzETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPublicDCDerivedPrivatePublicDCzETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer5 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer5());
        var resultDCDictionaryMixedKTContainer5 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer5, baselineDCDictionaryMixedKTContainer5, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer5, resultDCDictionaryMixedKTContainer5);

        //DCDictionaryMixedKTContainer6
        string baselineDCDictionaryMixedKTContainer6 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer6***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer6***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPublicPrivateDCzETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Value></b:KeyValueOfPublicDCDerivedPublicPrivateDCzETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer6 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer6());
        var resultDCDictionaryMixedKTContainer6 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer6, baselineDCDictionaryMixedKTContainer6, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer6, resultDCDictionaryMixedKTContainer6);

        //DCDictionaryMixedKTContainer7
        string baselineDCDictionaryMixedKTContainer7 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer7***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer7***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPrivatePrivateDCzETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>7b4ac88f-972b-43e5-8f6a-5ae64480eaad</Data></b:Value></b:KeyValueOfPublicDCDerivedPrivatePrivateDCzETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer7 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer7());
        var resultDCDictionaryMixedKTContainer7 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer7, baselineDCDictionaryMixedKTContainer7, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer7, resultDCDictionaryMixedKTContainer7);

        //DCDictionaryMixedKTContainer8
        string baselineDCDictionaryMixedKTContainer8 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer8***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer8***""><DictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPublicPublicDCDerivedPubliczETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPublicDCDerivedPublicPublicDCDerivedPubliczETuxydO></DictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer8 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer8());
        var resultDCDictionaryMixedKTContainer8 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer8, baselineDCDictionaryMixedKTContainer8, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer8, resultDCDictionaryMixedKTContainer8);

        //DCDictionaryMixedKTContainer9
        string baselineDCDictionaryMixedKTContainer9 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer9***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer9***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPrivatePublicDCDerivedPrivatezETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPublicDCDerivedPrivatePublicDCDerivedPrivatezETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer9 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer9());
        var resultDCDictionaryMixedKTContainer9 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer9, baselineDCDictionaryMixedKTContainer9, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer9, resultDCDictionaryMixedKTContainer9);

        //DCDictionaryMixedKTContainer10
        string baselineDCDictionaryMixedKTContainer10 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer10***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer10***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPublicPublicDCDerivedPrivatezETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPublicDCDerivedPublicPublicDCDerivedPrivatezETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer10 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer10());
        var resultDCDictionaryMixedKTContainer10 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer10, baselineDCDictionaryMixedKTContainer10, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer10, resultDCDictionaryMixedKTContainer10);

        //DCDictionaryMixedKTContainer11
        string baselineDCDictionaryMixedKTContainer11 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer11***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer11***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCDerivedPrivatePublicDCDerivedPubliczETuxydO><b:Key z:Id=""i2""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Key><b:Value z:Id=""i3""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></b:Value></b:KeyValueOfPublicDCDerivedPrivatePublicDCDerivedPubliczETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer11 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer11());
        var resultDCDictionaryMixedKTContainer11 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer11, baselineDCDictionaryMixedKTContainer11, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer11, resultDCDictionaryMixedKTContainer11);

        //DCDictionaryMixedKTContainer12
        string baselineDCDictionaryMixedKTContainer12 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer12***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer12***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfPublicDCClassPrivateDMPublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDMzETuxydO><b:Key z:Id=""i2""><_data/></b:Key><b:Value z:Id=""i3""><Data i:nil=""true""/><DerivedData2 i:nil=""true""/><_derivedData1/></b:Value></b:KeyValueOfPublicDCClassPrivateDMPublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDMzETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer12 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer12());
        var resultDCDictionaryMixedKTContainer12 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer12, baselineDCDictionaryMixedKTContainer12, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer12, resultDCDictionaryMixedKTContainer12);

        //DCDictionaryMixedKTContainer13
        string baselineDCDictionaryMixedKTContainer13 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer13***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer13***""><_dictData xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfKT1BaseKT2BasezETuxydO><b:Key z:Id=""i2""><BData i:nil=""true""/></b:Key><b:Value z:Id=""i3""><BData i:nil=""true""/></b:Value></b:KeyValueOfKT1BaseKT2BasezETuxydO></_dictData></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer13 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer13());
        var resultDCDictionaryMixedKTContainer13 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer13, baselineDCDictionaryMixedKTContainer13, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer13, resultDCDictionaryMixedKTContainer13);

        //DCDictionaryMixedKTContainer14
        string baselineDCDictionaryMixedKTContainer14 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCDictionaryMixedKTContainer14***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCDictionaryMixedKTContainer14***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCDictionaryMixedKTContainer14 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCDictionaryMixedKTContainer14());
        var resultDCDictionaryMixedKTContainer14 = SerializeAndDeserialize(valueDCDictionaryMixedKTContainer14, baselineDCDictionaryMixedKTContainer14, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCDictionaryMixedKTContainer14, resultDCDictionaryMixedKTContainer14);

        //PublicDC
        string baselinePublicDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDC());
        var resultPublicDC = SerializeAndDeserialize(valuePublicDC, baselinePublicDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDC, resultPublicDC);

        //PublicDCDerivedPublic
        string baselinePublicDCDerivedPublic = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCDerivedPublic***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCDerivedPublic***""><Data>55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCDerivedPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCDerivedPublic());
        var resultPublicDCDerivedPublic = SerializeAndDeserialize(valuePublicDCDerivedPublic, baselinePublicDCDerivedPublic, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCDerivedPublic, resultPublicDCDerivedPublic);

        //DC
        string baselineDC = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DC***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC***""><Data>Monday, January 1, 0001</Data><Next i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDC = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC());
        var resultDC = SerializeAndDeserialize(valueDC, baselineDC, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC, resultDC);

        //DCWithReadOnlyField
        string baselineDCWithReadOnlyField = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCWithReadOnlyField***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCWithReadOnlyField***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCWithReadOnlyField = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCWithReadOnlyField());
        var resultDCWithReadOnlyField = SerializeAndDeserialize(valueDCWithReadOnlyField, baselineDCWithReadOnlyField, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCWithReadOnlyField, resultDCWithReadOnlyField);

        //// not support
        ////IReadWriteXmlWriteBinHex_EqualityDefined
        //string baselineIReadWriteXmlWriteBinHex_EqualityDefined = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.IReadWriteXmlWriteBinHex_EqualityDefined***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.IReadWriteXmlWriteBinHex_EqualityDefined***"">68656C6C6F20776F726C64</_data><_data2 i:type=""a:SerializationTestTypes.IReadWriteXmlWriteBinHex_EqualityDefined***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.IReadWriteXmlWriteBinHex_EqualityDefined***"">68656C6C6F20776F726C64</_data2></ObjectContainer>";
        //var valueIReadWriteXmlWriteBinHex_EqualityDefined = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.IReadWriteXmlWriteBinHex_EqualityDefined());
        //var resultIReadWriteXmlWriteBinHex_EqualityDefined = SerializeAndDeserialize(valueIReadWriteXmlWriteBinHex_EqualityDefined, baselineIReadWriteXmlWriteBinHex_EqualityDefined, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valueIReadWriteXmlWriteBinHex_EqualityDefined, resultIReadWriteXmlWriteBinHex_EqualityDefined);

        ////not support
        ////PrivateDefaultCtorIXmlSerializables
        //string baselinePrivateDefaultCtorIXmlSerializables = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"">68656C6C6F20776F726C64</_data><_data2 i:type=""a:SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PrivateDefaultCtorIXmlSerializables***"">68656C6C6F20776F726C64</_data2></ObjectContainer>";
        //var valuePrivateDefaultCtorIXmlSerializables = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PrivateDefaultCtorIXmlSerializables(true));
        //var resultPrivateDefaultCtorIXmlSerializables = SerializeAndDeserialize(valuePrivateDefaultCtorIXmlSerializables, baselinePrivateDefaultCtorIXmlSerializables, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePrivateDefaultCtorIXmlSerializables, resultPrivateDefaultCtorIXmlSerializables);

        ////not support
        ////PublicIXmlSerializablesWithPublicSchemaProvider
        //string baselinePublicIXmlSerializablesWithPublicSchemaProvider = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.PublicIXmlSerializablesWithPublicSchemaProvider***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicIXmlSerializablesWithPublicSchemaProvider***"">68656C6C6F20776F726C64</_data><_data2 i:type=""a:SerializationTestTypes.PublicIXmlSerializablesWithPublicSchemaProvider***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicIXmlSerializablesWithPublicSchemaProvider***"">68656C6C6F20776F726C64</_data2></ObjectContainer>";
        //var valuePublicIXmlSerializablesWithPublicSchemaProvider = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicIXmlSerializablesWithPublicSchemaProvider());
        //var resultPublicIXmlSerializablesWithPublicSchemaProvider = SerializeAndDeserialize(valuePublicIXmlSerializablesWithPublicSchemaProvider, baselinePublicIXmlSerializablesWithPublicSchemaProvider, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicIXmlSerializablesWithPublicSchemaProvider, resultPublicIXmlSerializablesWithPublicSchemaProvider);

        ////not support
        ////PublicExplicitIXmlSerializablesWithPublicSchemaProvider
        //string baselinePublicExplicitIXmlSerializablesWithPublicSchemaProvider = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.PublicExplicitIXmlSerializablesWithPublicSchemaProvider***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicExplicitIXmlSerializablesWithPublicSchemaProvider***"">68656C6C6F20776F726C64</_data><_data2 i:type=""a:SerializationTestTypes.PublicExplicitIXmlSerializablesWithPublicSchemaProvider***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicExplicitIXmlSerializablesWithPublicSchemaProvider***"">68656C6C6F20776F726C64</_data2></ObjectContainer>";
        //var valuePublicExplicitIXmlSerializablesWithPublicSchemaProvider = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicExplicitIXmlSerializablesWithPublicSchemaProvider());
        //var resultPublicExplicitIXmlSerializablesWithPublicSchemaProvider = SerializeAndDeserialize(valuePublicExplicitIXmlSerializablesWithPublicSchemaProvider, baselinePublicExplicitIXmlSerializablesWithPublicSchemaProvider, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicExplicitIXmlSerializablesWithPublicSchemaProvider, resultPublicExplicitIXmlSerializablesWithPublicSchemaProvider);

        ////not support
        ////PublicIXmlSerializablesWithPrivateSchemaProvider
        //string baselinePublicIXmlSerializablesWithPrivateSchemaProvider = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.PublicIXmlSerializablesWithPrivateSchemaProvider***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicIXmlSerializablesWithPrivateSchemaProvider***"">68656C6C6F20776F726C64</_data><_data2 i:type=""a:SerializationTestTypes.PublicIXmlSerializablesWithPrivateSchemaProvider***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicIXmlSerializablesWithPrivateSchemaProvider***"">68656C6C6F20776F726C64</_data2></ObjectContainer>";
        //var valuePublicIXmlSerializablesWithPrivateSchemaProvider = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicIXmlSerializablesWithPrivateSchemaProvider());
        //var resultPublicIXmlSerializablesWithPrivateSchemaProvider = SerializeAndDeserialize(valuePublicIXmlSerializablesWithPrivateSchemaProvider, baselinePublicIXmlSerializablesWithPrivateSchemaProvider, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicIXmlSerializablesWithPrivateSchemaProvider, resultPublicIXmlSerializablesWithPrivateSchemaProvider);

        //PublicDCClassPublicDM
        string baselinePublicDCClassPublicDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCClassPublicDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM***""><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCClassPublicDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCClassPublicDM(true));
        var resultPublicDCClassPublicDM = SerializeAndDeserialize(valuePublicDCClassPublicDM, baselinePublicDCClassPublicDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCClassPublicDM, resultPublicDCClassPublicDM);

        //PublicDCClassPrivateDM
        string baselinePublicDCClassPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCClassPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM***""><_data>No change</_data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCClassPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCClassPrivateDM(true));
        var resultPublicDCClassPrivateDM = SerializeAndDeserialize(valuePublicDCClassPrivateDM, baselinePublicDCClassPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCClassPrivateDM, resultPublicDCClassPrivateDM);

        //PublicDCClassInternalDM
        string baselinePublicDCClassInternalDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCClassInternalDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassInternalDM***""><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCClassInternalDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCClassInternalDM(true));
        var resultPublicDCClassInternalDM = SerializeAndDeserialize(valuePublicDCClassInternalDM, baselinePublicDCClassInternalDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCClassInternalDM, resultPublicDCClassInternalDM);

        //PublicDCClassMixedDM
        string baselinePublicDCClassMixedDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCClassMixedDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassMixedDM***""><Data1>No change</Data1><Data3/><_data2/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCClassMixedDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCClassMixedDM(true));
        var resultPublicDCClassMixedDM = SerializeAndDeserialize(valuePublicDCClassMixedDM, baselinePublicDCClassMixedDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCClassMixedDM, resultPublicDCClassMixedDM);

        //PublicDCClassPublicDM_DerivedDCClassPublic
        string baselinePublicDCClassPublicDM_DerivedDCClassPublic = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCClassPublicDM_DerivedDCClassPublic***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM_DerivedDCClassPublic***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCClassPublicDM_DerivedDCClassPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCClassPublicDM_DerivedDCClassPublic());
        var resultPublicDCClassPublicDM_DerivedDCClassPublic = SerializeAndDeserialize(valuePublicDCClassPublicDM_DerivedDCClassPublic, baselinePublicDCClassPublicDM_DerivedDCClassPublic, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCClassPublicDM_DerivedDCClassPublic, resultPublicDCClassPublicDM_DerivedDCClassPublic);

        //PublicDCClassPrivateDM_DerivedDCClassPublic
        string baselinePublicDCClassPrivateDM_DerivedDCClassPublic = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic***""><_data/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCClassPrivateDM_DerivedDCClassPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCClassPrivateDM_DerivedDCClassPublic());
        var resultPublicDCClassPrivateDM_DerivedDCClassPublic = SerializeAndDeserialize(valuePublicDCClassPrivateDM_DerivedDCClassPublic, baselinePublicDCClassPrivateDM_DerivedDCClassPublic, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCClassPrivateDM_DerivedDCClassPublic, resultPublicDCClassPrivateDM_DerivedDCClassPublic);

        //PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM
        string baselinePublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM***""><Data i:nil=""true""/><DerivedData2 i:nil=""true""/><_derivedData1/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM());
        var resultPublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM = SerializeAndDeserialize(valuePublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM, baselinePublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM, resultPublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM);

        //Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM
        string baselineProp_PublicDCClassPublicDM_PublicDCClassPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM***""><Data z:Id=""i2""><_data/></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassPublicDM_PublicDCClassPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassPublicDM_PublicDCClassPrivateDM(true));
        var resultProp_PublicDCClassPublicDM_PublicDCClassPrivateDM = SerializeAndDeserialize(valueProp_PublicDCClassPublicDM_PublicDCClassPrivateDM, baselineProp_PublicDCClassPublicDM_PublicDCClassPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassPublicDM_PublicDCClassPrivateDM, resultProp_PublicDCClassPublicDM_PublicDCClassPrivateDM);

        //Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM
        string baselineProp_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM***""><Data z:Id=""i2""><_data/></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM(true));
        var resultProp_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM = SerializeAndDeserialize(valueProp_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM, baselineProp_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM, resultProp_SetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM);

        //Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM
        string baselineProp_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM***""><Data z:Id=""i2""><_data/></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM(true));
        var resultProp_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM = SerializeAndDeserialize(valueProp_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM, baselineProp_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM, resultProp_GetPrivate_PublicDCClassPublicDM_PublicDCClassPrivateDM);

        //Prop_PublicDCClassPublicDM
        string baselineProp_PublicDCClassPublicDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassPublicDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassPublicDM***""><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassPublicDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassPublicDM(true));
        var resultProp_PublicDCClassPublicDM = SerializeAndDeserialize(valueProp_PublicDCClassPublicDM, baselineProp_PublicDCClassPublicDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassPublicDM, resultProp_PublicDCClassPublicDM);

        //Prop_PublicDCClassPrivateDM
        string baselineProp_PublicDCClassPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassPrivateDM***""><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassPrivateDM(true));
        var resultProp_PublicDCClassPrivateDM = SerializeAndDeserialize(valueProp_PublicDCClassPrivateDM, baselineProp_PublicDCClassPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassPrivateDM, resultProp_PublicDCClassPrivateDM);

        //Prop_PublicDCClassInternalDM
        string baselineProp_PublicDCClassInternalDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassInternalDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassInternalDM***""><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassInternalDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassInternalDM(true));
        var resultProp_PublicDCClassInternalDM = SerializeAndDeserialize(valueProp_PublicDCClassInternalDM, baselineProp_PublicDCClassInternalDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassInternalDM, resultProp_PublicDCClassInternalDM);

        //Prop_PublicDCClassMixedDM
        string baselineProp_PublicDCClassMixedDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassMixedDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassMixedDM***""><Data1>No change</Data1><Data2 i:nil=""true""/><Data3 i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassMixedDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassMixedDM(true));
        var resultProp_PublicDCClassMixedDM = SerializeAndDeserialize(valueProp_PublicDCClassMixedDM, baselineProp_PublicDCClassMixedDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassMixedDM, resultProp_PublicDCClassMixedDM);

        //Prop_PublicDCClassPublicDM_DerivedDCClassPublic
        string baselineProp_PublicDCClassPublicDM_DerivedDCClassPublic = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassPublicDM_DerivedDCClassPublic***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassPublicDM_DerivedDCClassPublic***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassPublicDM_DerivedDCClassPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassPublicDM_DerivedDCClassPublic());
        var resultProp_PublicDCClassPublicDM_DerivedDCClassPublic = SerializeAndDeserialize(valueProp_PublicDCClassPublicDM_DerivedDCClassPublic, baselineProp_PublicDCClassPublicDM_DerivedDCClassPublic, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassPublicDM_DerivedDCClassPublic, resultProp_PublicDCClassPublicDM_DerivedDCClassPublic);

        //Prop_PublicDCClassPrivateDM_DerivedDCClassPublic
        string baselineProp_PublicDCClassPrivateDM_DerivedDCClassPublic = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassPrivateDM_DerivedDCClassPublic***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassPrivateDM_DerivedDCClassPublic***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassPrivateDM_DerivedDCClassPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassPrivateDM_DerivedDCClassPublic());
        var resultProp_PublicDCClassPrivateDM_DerivedDCClassPublic = SerializeAndDeserialize(valueProp_PublicDCClassPrivateDM_DerivedDCClassPublic, baselineProp_PublicDCClassPrivateDM_DerivedDCClassPublic, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassPrivateDM_DerivedDCClassPublic, resultProp_PublicDCClassPrivateDM_DerivedDCClassPublic);

        //Prop_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM
        string baselineProp_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM***""><Data i:nil=""true""/><DerivedData2 i:nil=""true""/><_derivedData1/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM());
        var resultProp_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM = SerializeAndDeserialize(valueProp_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM, baselineProp_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM, resultProp_PublicDCClassPublicDM_DerivedDCClassPublicContainsPrivateDM);

        //Prop_SetPrivate_PublicDCClassPublicDM
        string baselineProp_SetPrivate_PublicDCClassPublicDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_SetPrivate_PublicDCClassPublicDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_SetPrivate_PublicDCClassPublicDM***""><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_SetPrivate_PublicDCClassPublicDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_SetPrivate_PublicDCClassPublicDM(true));
        var resultProp_SetPrivate_PublicDCClassPublicDM = SerializeAndDeserialize(valueProp_SetPrivate_PublicDCClassPublicDM, baselineProp_SetPrivate_PublicDCClassPublicDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_SetPrivate_PublicDCClassPublicDM, resultProp_SetPrivate_PublicDCClassPublicDM);

        //Prop_GetPrivate_PublicDCClassPublicDM
        string baselineProp_GetPrivate_PublicDCClassPublicDM = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Prop_GetPrivate_PublicDCClassPublicDM***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Prop_GetPrivate_PublicDCClassPublicDM***""><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueProp_GetPrivate_PublicDCClassPublicDM = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Prop_GetPrivate_PublicDCClassPublicDM(true));
        var resultProp_GetPrivate_PublicDCClassPublicDM = SerializeAndDeserialize(valueProp_GetPrivate_PublicDCClassPublicDM, baselineProp_GetPrivate_PublicDCClassPublicDM, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueProp_GetPrivate_PublicDCClassPublicDM, resultProp_GetPrivate_PublicDCClassPublicDM);

        //Derived_Override_Prop_All_Public
        string baselineDerived_Override_Prop_All_Public = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Derived_Override_Prop_All_Public***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived_Override_Prop_All_Public***""><Data>No change</Data><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDerived_Override_Prop_All_Public = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived_Override_Prop_All_Public(true));
        var resultDerived_Override_Prop_All_Public = SerializeAndDeserialize(valueDerived_Override_Prop_All_Public, baselineDerived_Override_Prop_All_Public, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDerived_Override_Prop_All_Public, resultDerived_Override_Prop_All_Public);

        //Derived_Override_Prop_Private
        string baselineDerived_Override_Prop_Private = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Derived_Override_Prop_Private***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived_Override_Prop_Private***""><Data>No change</Data><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDerived_Override_Prop_Private = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived_Override_Prop_Private(true));
        var resultDerived_Override_Prop_Private = SerializeAndDeserialize(valueDerived_Override_Prop_Private, baselineDerived_Override_Prop_Private, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDerived_Override_Prop_Private, resultDerived_Override_Prop_Private);

        //Derived_Override_Prop_GetPrivate_All_Public
        string baselineDerived_Override_Prop_GetPrivate_All_Public = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Derived_Override_Prop_GetPrivate_All_Public***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived_Override_Prop_GetPrivate_All_Public***""><Data>No change</Data><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDerived_Override_Prop_GetPrivate_All_Public = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived_Override_Prop_GetPrivate_All_Public(true));
        var resultDerived_Override_Prop_GetPrivate_All_Public = SerializeAndDeserialize(valueDerived_Override_Prop_GetPrivate_All_Public, baselineDerived_Override_Prop_GetPrivate_All_Public, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDerived_Override_Prop_GetPrivate_All_Public, resultDerived_Override_Prop_GetPrivate_All_Public);

        //Derived_Override_Prop_GetPrivate_Private
        string baselineDerived_Override_Prop_GetPrivate_Private = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Derived_Override_Prop_GetPrivate_Private***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived_Override_Prop_GetPrivate_Private***""><Data>No change</Data><Data>No change</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDerived_Override_Prop_GetPrivate_Private = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived_Override_Prop_GetPrivate_Private(true));
        var resultDerived_Override_Prop_GetPrivate_Private = SerializeAndDeserialize(valueDerived_Override_Prop_GetPrivate_Private, baselineDerived_Override_Prop_GetPrivate_Private, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDerived_Override_Prop_GetPrivate_Private, resultDerived_Override_Prop_GetPrivate_Private);

        //DC1_Version1
        string baselineDC1_Version1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DC1_Version1***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC1_Version1***""/><_data2 i:type=""a:SerializationTestTypes.DC1_Version1***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC1_Version1***""/></ObjectContainer>";
        var valueDC1_Version1 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC1_Version1());
        var resultDC1_Version1 = SerializeAndDeserialize(valueDC1_Version1, baselineDC1_Version1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC1_Version1, resultDC1_Version1);

        //DC2_Version1
        string baselineDC2_Version1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DC2_Version1***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC2_Version1***""><Data i:nil=""true""/></_data><_data2 i:type=""a:SerializationTestTypes.DC2_Version1***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC2_Version1***""><Data i:nil=""true""/></_data2></ObjectContainer>";
        var valueDC2_Version1 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC2_Version1());
        var resultDC2_Version1 = SerializeAndDeserialize(valueDC2_Version1, baselineDC2_Version1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC2_Version1, resultDC2_Version1);

        //DC2_Version4
        string baselineDC2_Version4 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DC2_Version4***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC2_Version4***""><_data/></_data><_data2 i:type=""a:SerializationTestTypes.DC2_Version4***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC2_Version4***""><_data/></_data2></ObjectContainer>";
        var valueDC2_Version4 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC2_Version4());
        var resultDC2_Version4 = SerializeAndDeserialize(valueDC2_Version4, baselineDC2_Version4, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC2_Version4, resultDC2_Version4);

        //DC2_Version5
        string baselineDC2_Version5 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DC2_Version5***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC2_Version5***""><Data i:nil=""true""/></_data><_data2 i:type=""a:SerializationTestTypes.DC2_Version5***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC2_Version5***""><Data i:nil=""true""/></_data2></ObjectContainer>";
        var valueDC2_Version5 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC2_Version5());
        var resultDC2_Version5 = SerializeAndDeserialize(valueDC2_Version5, baselineDC2_Version5, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC2_Version5, resultDC2_Version5);

        //DC3_Version1
        string baselineDC3_Version1 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DC3_Version1***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC3_Version1***""><Data1 i:nil=""true""/></_data><_data2 i:type=""a:SerializationTestTypes.DC3_Version1***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC3_Version1***""><Data1 i:nil=""true""/></_data2></ObjectContainer>";
        var valueDC3_Version1 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC3_Version1());
        var resultDC3_Version1 = SerializeAndDeserialize(valueDC3_Version1, baselineDC3_Version1, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC3_Version1, resultDC3_Version1);

        //DC3_Version2
        string baselineDC3_Version2 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DC3_Version2***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC3_Version2***""/><_data2 i:type=""a:SerializationTestTypes.DC3_Version2***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC3_Version2***""/></ObjectContainer>";
        var valueDC3_Version2 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC3_Version2());
        var resultDC3_Version2 = SerializeAndDeserialize(valueDC3_Version2, baselineDC3_Version2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC3_Version2, resultDC3_Version2);

        //DC3_Version3
        string baselineDC3_Version3 = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DC3_Version3***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC3_Version3***""/><_data2 i:type=""a:SerializationTestTypes.DC3_Version3***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DC3_Version3***""/></ObjectContainer>";
        var valueDC3_Version3 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DC3_Version3());
        var resultDC3_Version3 = SerializeAndDeserialize(valueDC3_Version3, baselineDC3_Version3, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDC3_Version3, resultDC3_Version3);

        //CallBackSample_OnSerializing_Public
        string baselineCallBackSample_OnSerializing_Public = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnSerializing_Public***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnSerializing_Public***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnSerializing_Public = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnSerializing_Public());
        var resultCallBackSample_OnSerializing_Public = SerializeAndDeserialize(valueCallBackSample_OnSerializing_Public, baselineCallBackSample_OnSerializing_Public, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnSerializing_Public, resultCallBackSample_OnSerializing_Public);

        //CallBackSample_OnSerialized_Public
        string baselineCallBackSample_OnSerialized_Public = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnSerialized_Public***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnSerialized_Public***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnSerialized_Public = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnSerialized_Public());
        var resultCallBackSample_OnSerialized_Public = SerializeAndDeserialize(valueCallBackSample_OnSerialized_Public, baselineCallBackSample_OnSerialized_Public, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnSerialized_Public, resultCallBackSample_OnSerialized_Public);

        //CallBackSample_OnDeserializing_Public
        string baselineCallBackSample_OnDeserializing_Public = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnDeserializing_Public***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnDeserializing_Public***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnDeserializing_Public = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnDeserializing_Public());
        var resultCallBackSample_OnDeserializing_Public = SerializeAndDeserialize(valueCallBackSample_OnDeserializing_Public, baselineCallBackSample_OnDeserializing_Public, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnDeserializing_Public, resultCallBackSample_OnDeserializing_Public);

        //CallBackSample_OnDeserialized_Public
        string baselineCallBackSample_OnDeserialized_Public = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnDeserialized_Public***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnDeserialized_Public***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnDeserialized_Public = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnDeserialized_Public());
        var resultCallBackSample_OnDeserialized_Public = SerializeAndDeserialize(valueCallBackSample_OnDeserialized_Public, baselineCallBackSample_OnDeserialized_Public, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnDeserialized_Public, resultCallBackSample_OnDeserialized_Public);

        //CallBackSample_OnSerializing
        string baselineCallBackSample_OnSerializing = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnSerializing***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnSerializing***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnSerializing = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnSerializing());
        var resultCallBackSample_OnSerializing = SerializeAndDeserialize(valueCallBackSample_OnSerializing, baselineCallBackSample_OnSerializing, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnSerializing, resultCallBackSample_OnSerializing);

        //CallBackSample_OnSerialized
        string baselineCallBackSample_OnSerialized = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnSerialized***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnSerialized***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnSerialized = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnSerialized());
        var resultCallBackSample_OnSerialized = SerializeAndDeserialize(valueCallBackSample_OnSerialized, baselineCallBackSample_OnSerialized, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnSerialized, resultCallBackSample_OnSerialized);

        //CallBackSample_OnDeserializing
        string baselineCallBackSample_OnDeserializing = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnDeserializing***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnDeserializing***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnDeserializing = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnDeserializing());
        var resultCallBackSample_OnDeserializing = SerializeAndDeserialize(valueCallBackSample_OnDeserializing, baselineCallBackSample_OnDeserializing, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnDeserializing, resultCallBackSample_OnDeserializing);

        //CallBackSample_OnDeserialized
        string baselineCallBackSample_OnDeserialized = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnDeserialized***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnDeserialized***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnDeserialized = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnDeserialized());
        var resultCallBackSample_OnDeserialized = SerializeAndDeserialize(valueCallBackSample_OnDeserialized, baselineCallBackSample_OnDeserialized, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnDeserialized, resultCallBackSample_OnDeserialized);

        //CallBackSample_IDeserializationCallback
        string baselineCallBackSample_IDeserializationCallback = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_IDeserializationCallback***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_IDeserializationCallback***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_IDeserializationCallback = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_IDeserializationCallback());
        var resultCallBackSample_IDeserializationCallback = SerializeAndDeserialize(valueCallBackSample_IDeserializationCallback, baselineCallBackSample_IDeserializationCallback, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_IDeserializationCallback, resultCallBackSample_IDeserializationCallback);

        //CallBackSample_IDeserializationCallback_Explicit
        string baselineCallBackSample_IDeserializationCallback_Explicit = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_IDeserializationCallback_Explicit***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_IDeserializationCallback_Explicit***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_IDeserializationCallback_Explicit = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_IDeserializationCallback_Explicit());
        var resultCallBackSample_IDeserializationCallback_Explicit = SerializeAndDeserialize(valueCallBackSample_IDeserializationCallback_Explicit, baselineCallBackSample_IDeserializationCallback_Explicit, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_IDeserializationCallback_Explicit, resultCallBackSample_IDeserializationCallback_Explicit);

        //CallBackSample_OnDeserialized_Private_Base
        string baselineCallBackSample_OnDeserialized_Private_Base = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnDeserialized_Private_Base***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnDeserialized_Private_Base***""><Data>string</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnDeserialized_Private_Base = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnDeserialized_Private_Base());
        var resultCallBackSample_OnDeserialized_Private_Base = SerializeAndDeserialize(valueCallBackSample_OnDeserialized_Private_Base, baselineCallBackSample_OnDeserialized_Private_Base, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnDeserialized_Private_Base, resultCallBackSample_OnDeserialized_Private_Base);

        //CallBackSample_OnDeserialized_Public_Derived
        string baselineCallBackSample_OnDeserialized_Public_Derived = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CallBackSample_OnDeserialized_Public_Derived***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CallBackSample_OnDeserialized_Public_Derived***""><Data>string</Data><Data2>string</Data2></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCallBackSample_OnDeserialized_Public_Derived = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CallBackSample_OnDeserialized_Public_Derived());
        var resultCallBackSample_OnDeserialized_Public_Derived = SerializeAndDeserialize(valueCallBackSample_OnDeserialized_Public_Derived, baselineCallBackSample_OnDeserialized_Public_Derived, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCallBackSample_OnDeserialized_Public_Derived, resultCallBackSample_OnDeserialized_Public_Derived);

        //CDC_Possitive
        string baselineCDC_Possitive = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CDC_Possitive***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CDC_Possitive***""><string>112</string></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCDC_Possitive = new SerializationTestTypes.ObjectContainer(SerializationTestTypes.CDC_Possitive.CreateInstance());
        var resultCDC_Possitive = SerializeAndDeserialize(valueCDC_Possitive, baselineCDC_Possitive, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCDC_Possitive, resultCDC_Possitive);

        //CDC_PrivateAdd
        string baselineCDC_PrivateAdd = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CDC_PrivateAdd***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CDC_PrivateAdd***""><string>222323</string><string>222323</string></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCDC_PrivateAdd = new SerializationTestTypes.ObjectContainer(SerializationTestTypes.CDC_PrivateAdd.CreateInstance());
        var resultCDC_PrivateAdd = SerializeAndDeserialize(valueCDC_PrivateAdd, baselineCDC_PrivateAdd, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCDC_PrivateAdd, resultCDC_PrivateAdd);

        //Base_Possitive_VirtualAdd
        string baselineBase_Possitive_VirtualAdd = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.Base_Possitive_VirtualAdd***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.Base_Possitive_VirtualAdd***""><string>222323</string><string>222323</string></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueBase_Possitive_VirtualAdd = new SerializationTestTypes.ObjectContainer(SerializationTestTypes.Base_Possitive_VirtualAdd.CreateInstance());
        var resultBase_Possitive_VirtualAdd = SerializeAndDeserialize(valueBase_Possitive_VirtualAdd, baselineBase_Possitive_VirtualAdd, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueBase_Possitive_VirtualAdd, resultBase_Possitive_VirtualAdd);

        //CDC_NewAddToPrivate
        string baselineCDC_NewAddToPrivate = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CDC_NewAddToPrivate***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CDC_NewAddToPrivate***""><string>223213</string><string>223213</string></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCDC_NewAddToPrivate = new SerializationTestTypes.ObjectContainer(SerializationTestTypes.CDC_NewAddToPrivate.CreateInstance());
        var resultCDC_NewAddToPrivate = SerializeAndDeserialize(valueCDC_NewAddToPrivate, baselineCDC_NewAddToPrivate, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCDC_NewAddToPrivate, resultCDC_NewAddToPrivate);

        //CDC_PrivateDefaultCtor
        string baselineCDC_PrivateDefaultCtor = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CDC_PrivateDefaultCtor***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CDC_PrivateDefaultCtor***""/><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCDC_PrivateDefaultCtor = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CDC_PrivateDefaultCtor(true));
        var resultCDC_PrivateDefaultCtor = SerializeAndDeserialize(valueCDC_PrivateDefaultCtor, baselineCDC_PrivateDefaultCtor, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCDC_PrivateDefaultCtor, resultCDC_PrivateDefaultCtor);

        //NonDCPerson
        string baselineNonDCPerson = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.NonDCPerson***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NonDCPerson***""><Age>20</Age><Name>jeff</Name></_data><_data2 i:type=""a:SerializationTestTypes.NonDCPerson***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NonDCPerson***""><Age>20</Age><Name>jeff</Name></_data2></ObjectContainer>";
        var valueNonDCPerson = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.NonDCPerson());
        var resultNonDCPerson = SerializeAndDeserialize(valueNonDCPerson, baselineNonDCPerson, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueNonDCPerson, resultNonDCPerson);

        //PersonSurrogated
        string baselinePersonSurrogated = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.PersonSurrogated***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.PersonSurrogated***""><Age>30</Age><Name>Jeffery</Name></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valuePersonSurrogated = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.PersonSurrogated());
        var resultPersonSurrogated = SerializeAndDeserialize(valuePersonSurrogated, baselinePersonSurrogated, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valuePersonSurrogated, resultPersonSurrogated);

        //DCSurrogate
        string baselineDCSurrogate = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DCSurrogate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCSurrogate***""/><_data2 i:type=""a:SerializationTestTypes.DCSurrogate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCSurrogate***""/></ObjectContainer>";
        var valueDCSurrogate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCSurrogate());
        var resultDCSurrogate = SerializeAndDeserialize(valueDCSurrogate, baselineDCSurrogate, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCSurrogate, resultDCSurrogate);

        //SerSurrogate
        string baselineSerSurrogate = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SerSurrogate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerSurrogate***""/><_data2 i:type=""a:SerializationTestTypes.SerSurrogate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerSurrogate***""/></ObjectContainer>";
        var valueSerSurrogate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerSurrogate());
        var resultSerSurrogate = SerializeAndDeserialize(valueSerSurrogate, baselineSerSurrogate, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSerSurrogate, resultSerSurrogate);

        //DCSurrogateExplicit
        string baselineDCSurrogateExplicit = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DCSurrogateExplicit***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCSurrogateExplicit***""/><_data2 i:type=""a:SerializationTestTypes.DCSurrogateExplicit***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCSurrogateExplicit***""/></ObjectContainer>";
        var valueDCSurrogateExplicit = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCSurrogateExplicit());
        var resultDCSurrogateExplicit = SerializeAndDeserialize(valueDCSurrogateExplicit, baselineDCSurrogateExplicit, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCSurrogateExplicit, resultDCSurrogateExplicit);

        //SerSurrogateExplicit
        string baselineSerSurrogateExplicit = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SerSurrogateExplicit***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerSurrogateExplicit***""/><_data2 i:type=""a:SerializationTestTypes.SerSurrogateExplicit***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerSurrogateExplicit***""/></ObjectContainer>";
        var valueSerSurrogateExplicit = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerSurrogateExplicit());
        var resultSerSurrogateExplicit = SerializeAndDeserialize(valueSerSurrogateExplicit, baselineSerSurrogateExplicit, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSerSurrogateExplicit, resultSerSurrogateExplicit);

        //DCSurrogateReturnPrivate
        string baselineDCSurrogateReturnPrivate = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.DCSurrogateReturnPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCSurrogateReturnPrivate***""/><_data2 i:type=""a:SerializationTestTypes.DCSurrogateReturnPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCSurrogateReturnPrivate***""/></ObjectContainer>";
        var valueDCSurrogateReturnPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCSurrogateReturnPrivate());
        var resultDCSurrogateReturnPrivate = SerializeAndDeserialize(valueDCSurrogateReturnPrivate, baselineDCSurrogateReturnPrivate, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCSurrogateReturnPrivate, resultDCSurrogateReturnPrivate);

        //SerSurrogateReturnPrivate
        string baselineSerSurrogateReturnPrivate = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SerSurrogateReturnPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerSurrogateReturnPrivate***""/><_data2 i:type=""a:SerializationTestTypes.SerSurrogateReturnPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerSurrogateReturnPrivate***""/></ObjectContainer>";
        var valueSerSurrogateReturnPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerSurrogateReturnPrivate());
        var resultSerSurrogateReturnPrivate = SerializeAndDeserialize(valueSerSurrogateReturnPrivate, baselineSerSurrogateReturnPrivate, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSerSurrogateReturnPrivate, resultSerSurrogateReturnPrivate);

        //NullableContainerContainsValue
        string baselineNullableContainerContainsValue = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.NullableContainerContainsValue***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NullableContainerContainsValue***""><Data><Data>Data</Data></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueNullableContainerContainsValue = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.NullableContainerContainsValue());
        var resultNullableContainerContainsValue = SerializeAndDeserialize(valueNullableContainerContainsValue, baselineNullableContainerContainsValue, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueNullableContainerContainsValue, resultNullableContainerContainsValue);

        //NullableContainerContainsNull
        string baselineNullableContainerContainsNull = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.NullableContainerContainsNull***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NullableContainerContainsNull***""><Data i:nil=""true""/></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueNullableContainerContainsNull = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.NullableContainerContainsNull());
        var resultNullableContainerContainsNull = SerializeAndDeserialize(valueNullableContainerContainsNull, baselineNullableContainerContainsNull, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueNullableContainerContainsNull, resultNullableContainerContainsNull);

        //NullablePrivateContainerContainsValue
        string baselineNullablePrivateContainerContainsValue = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.NullablePrivateContainerContainsValue***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NullablePrivateContainerContainsValue***""><Data i:type=""PrivateDCStruct""><Data>2147483647</Data></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueNullablePrivateContainerContainsValue = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.NullablePrivateContainerContainsValue());
        var resultNullablePrivateContainerContainsValue = SerializeAndDeserialize(valueNullablePrivateContainerContainsValue, baselineNullablePrivateContainerContainsValue, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueNullablePrivateContainerContainsValue, resultNullablePrivateContainerContainsValue);

        //NullablePrivateContainerContainsNull
        string baselineNullablePrivateContainerContainsNull = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.NullablePrivateContainerContainsNull***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NullablePrivateContainerContainsNull***""><Data i:type=""PrivateDCStruct""><Data>2147483647</Data></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueNullablePrivateContainerContainsNull = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.NullablePrivateContainerContainsNull());
        var resultNullablePrivateContainerContainsNull = SerializeAndDeserialize(valueNullablePrivateContainerContainsNull, baselineNullablePrivateContainerContainsNull, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueNullablePrivateContainerContainsNull, resultNullablePrivateContainerContainsNull);

        //NullablePrivateDataInDMContainerContainsValue
        string baselineNullablePrivateDataInDMContainerContainsValue = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.NullablePrivateDataInDMContainerContainsValue***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NullablePrivateDataInDMContainerContainsValue***""><Data><Data z:Id=""i2""><_data>No change</_data></Data></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueNullablePrivateDataInDMContainerContainsValue = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.NullablePrivateDataInDMContainerContainsValue());
        var resultNullablePrivateDataInDMContainerContainsValue = SerializeAndDeserialize(valueNullablePrivateDataInDMContainerContainsValue, baselineNullablePrivateDataInDMContainerContainsValue, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueNullablePrivateDataInDMContainerContainsValue, resultNullablePrivateDataInDMContainerContainsValue);

        //NullablePrivateDataInDMContainerContainsNull
        string baselineNullablePrivateDataInDMContainerContainsNull = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.NullablePrivateDataInDMContainerContainsNull***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.NullablePrivateDataInDMContainerContainsNull***""><Data i:type=""PublicDCStructContainsPrivateDataInDM""><Data z:Id=""i2""><_data>No change</_data></Data></Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueNullablePrivateDataInDMContainerContainsNull = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.NullablePrivateDataInDMContainerContainsNull());
        var resultNullablePrivateDataInDMContainerContainsNull = SerializeAndDeserialize(valueNullablePrivateDataInDMContainerContainsNull, baselineNullablePrivateDataInDMContainerContainsNull, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueNullablePrivateDataInDMContainerContainsNull, resultNullablePrivateDataInDMContainerContainsNull);

        //DCPublicDatasetPublic
        string baselineDCPublicDatasetPublic = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCPublicDatasetPublic***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCPublicDatasetPublic***""><dataSet><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>10</Data></MyTable></MyData></diffgr:diffgram></dataSet><dataSet2><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>10</Data></MyTable></MyData></diffgr:diffgram></dataSet2><dataTable><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:MainDataTable=""MyTable"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>10</Data></MyTable></MyData></diffgr:diffgram></dataTable><dataTable2><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:MainDataTable=""MyTable"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>10</Data></MyTable></MyData></diffgr:diffgram></dataTable2></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCPublicDatasetPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCPublicDatasetPublic(true));
        var resultDCPublicDatasetPublic = SerializeAndDeserialize(valueDCPublicDatasetPublic, baselineDCPublicDatasetPublic, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCPublicDatasetPublic, resultDCPublicDatasetPublic);

        //DCPublicDatasetPrivate
        string baselineDCPublicDatasetPrivate = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DCPublicDatasetPrivate***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCPublicDatasetPrivate***""><_dataSet><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>20</Data></MyTable></MyData></diffgr:diffgram></_dataSet><dataTable><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:MainDataTable=""MyTable"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>20</Data></MyTable></MyData></diffgr:diffgram></dataTable></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDCPublicDatasetPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCPublicDatasetPrivate(true));
        var resultDCPublicDatasetPrivate = SerializeAndDeserialize(valueDCPublicDatasetPrivate, baselineDCPublicDatasetPrivate, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDCPublicDatasetPrivate, resultDCPublicDatasetPrivate);

        ////???
        ////SerPublicDatasetPublic
        //string baselineSerPublicDatasetPublic = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SerPublicDatasetPublic***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerPublicDatasetPublic***""><dataSet><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></MyData></diffgr:diffgram></dataSet><dataTable><xs:schema id=""NewDataSet"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:MainDataTable=""MyTable"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><DocumentElement xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></DocumentElement></diffgr:diffgram></dataTable></_data><_data2 i:type=""a:SerializationTestTypes.SerPublicDatasetPublic***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerPublicDatasetPublic***""><dataSet><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></MyData></diffgr:diffgram></dataSet><dataTable><xs:schema id=""NewDataSet"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:MainDataTable=""MyTable"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><DocumentElement xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></DocumentElement></diffgr:diffgram></dataTable></_data2></ObjectContainer>";
        //var valueSerPublicDatasetPublic = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerPublicDatasetPublic(true));
        //var resultSerPublicDatasetPublic = SerializeAndDeserialize(valueSerPublicDatasetPublic, baselineSerPublicDatasetPublic, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSerPublicDatasetPublic, resultSerPublicDatasetPublic);

        ////???
        ////SerPublicDatasetPrivate
        //string baselineSerPublicDatasetPrivate = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data i:type=""a:SerializationTestTypes.SerPublicDatasetPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerPublicDatasetPrivate***""><_dataSet><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></MyData></diffgr:diffgram></_dataSet><dataTable><xs:schema id=""NewDataSet"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:MainDataTable=""MyTable"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><DocumentElement xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></DocumentElement></diffgr:diffgram></dataTable></_data><_data2 i:type=""a:SerializationTestTypes.SerPublicDatasetPrivate***"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerPublicDatasetPrivate***""><_dataSet><xs:schema id=""MyData"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""MyData"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><MyData xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></MyData></diffgr:diffgram></_dataSet><dataTable><xs:schema id=""NewDataSet"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns="""" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:MainDataTable=""MyTable"" msdata:UseCurrentLocale=""true""><xs:complexType><xs:choice minOccurs=""0"" maxOccurs=""unbounded""><xs:element name=""MyTable""><xs:complexType><xs:sequence><xs:element name=""Data"" type=""xs:string"" minOccurs=""0""/></xs:sequence></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><DocumentElement xmlns=""""><MyTable diffgr:id=""MyTable1"" msdata:rowOrder=""0"" diffgr:hasChanges=""inserted""><Data>Testing</Data></MyTable></DocumentElement></diffgr:diffgram></dataTable></_data2></ObjectContainer>";
        //var valueSerPublicDatasetPrivate = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerPublicDatasetPrivate(true));
        //var resultSerPublicDatasetPrivate = SerializeAndDeserialize(valueSerPublicDatasetPrivate, baselineSerPublicDatasetPrivate, setting);
        //SerializationTestTypes.ComparisonHelper.CompareRecursively(valueSerPublicDatasetPrivate, resultSerPublicDatasetPrivate);

        //CustomGeneric2
        string baselineCustomGeneric2_2 = $@"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.CustomGeneric2`1[[SerializationTestTypes.NonDCPerson, {assemblyName}]]***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.CustomGeneric2`1[[SerializationTestTypes.NonDCPerson, {assemblyName}]]***""><Data>data</Data></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueCustomGeneric2_2 = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CustomGeneric2<SerializationTestTypes.NonDCPerson>());
        var resultCustomGeneric2_2 = SerializeAndDeserialize(valueCustomGeneric2_2, baselineCustomGeneric2_2, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueCustomGeneric2_2, resultCustomGeneric2_2);

        //DTOContainer
        string baselineDTOContainer = @"<ObjectContainer xmlns=""http://schemas.datacontract.org/2004/07/SerializationTestTypes"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><_data z:Id=""i1"" i:type=""a:SerializationTestTypes.DTOContainer***"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns:a=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.DTOContainer***""><array1><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/><anyType i:type=""b:DateTimeOffset"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" xmlns:b=""http://schemas.datacontract.org/2004/07/System""><b:DateTime>0001-01-01T00:00:00Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></anyType><anyType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/></array1><arrayDTO xmlns:b=""http://schemas.datacontract.org/2004/07/System""><b:DateTimeOffset><b:DateTime>0001-01-01T00:00:00Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></b:DateTimeOffset><b:DateTimeOffset><b:DateTime>9999-12-31T23:59:59.9999999Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></b:DateTimeOffset></arrayDTO><dictDTO xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""><b:KeyValueOfDateTimeOffsetDateTimeOffset_ShTDFhl_P><b:Key xmlns:c=""http://schemas.datacontract.org/2004/07/System""><c:DateTime>0001-01-01T00:00:00Z</c:DateTime><c:OffsetMinutes>0</c:OffsetMinutes></b:Key><b:Value xmlns:c=""http://schemas.datacontract.org/2004/07/System""><c:DateTime>9999-12-31T23:59:59.9999999Z</c:DateTime><c:OffsetMinutes>0</c:OffsetMinutes></b:Value></b:KeyValueOfDateTimeOffsetDateTimeOffset_ShTDFhl_P><b:KeyValueOfDateTimeOffsetDateTimeOffset_ShTDFhl_P><b:Key xmlns:c=""http://schemas.datacontract.org/2004/07/System""><c:DateTime>9999-12-31T23:59:59.9999999Z</c:DateTime><c:OffsetMinutes>0</c:OffsetMinutes></b:Key><b:Value xmlns:c=""http://schemas.datacontract.org/2004/07/System""><c:DateTime>0001-01-01T00:00:00Z</c:DateTime><c:OffsetMinutes>0</c:OffsetMinutes></b:Value></b:KeyValueOfDateTimeOffsetDateTimeOffset_ShTDFhl_P></dictDTO><enumBase1 i:type=""b:SerializationTestTypes.MyEnum1***"" xmlns:b=""http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyEnum1***"">red</enumBase1><lDTO xmlns:b=""http://schemas.datacontract.org/2004/07/System""><b:DateTimeOffset><b:DateTime>9999-12-31T23:59:59.9999999Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></b:DateTimeOffset><b:DateTimeOffset><b:DateTime>0001-01-01T00:00:00Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></b:DateTimeOffset></lDTO><nDTO xmlns:b=""http://schemas.datacontract.org/2004/07/System""><b:DateTime>9999-12-31T23:59:59.9999999Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></nDTO><valType i:type=""b:DateTimeOffset"" xmlns:b=""http://schemas.datacontract.org/2004/07/System""><b:DateTime>0001-01-01T00:00:00Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></valType></_data><_data2 z:Ref=""i1"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/""/></ObjectContainer>";
        var valueDTOContainer = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DTOContainer(true));
        var resultDTOContainer = SerializeAndDeserialize(valueDTOContainer, baselineDTOContainer, setting);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(valueDTOContainer, resultDTOContainer);
    }

    [Fact]
    public static void DCS_BasicPerSerializerRoundTripAndCompare_Collections()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = false
        };

        var baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.ContainsLinkedList***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ContainsLinkedList***\"><Data><SimpleDCWithRef z:Id=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/></SimpleDCWithRef><SimpleDCWithRef z:Id=\"i3\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data z:Id=\"i4\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i4\"/></SimpleDCWithRef><SimpleDCWithRef z:Ref=\"i3\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Id=\"i5\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data z:Id=\"i6\"><Data>11:59:59 PM</Data></Data><RefData z:Id=\"i7\"><Data>11:59:59 PM</Data></RefData></SimpleDCWithRef><SimpleDCWithRef z:Id=\"i8\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data z:Ref=\"i6\"/><RefData z:Ref=\"i6\"/></SimpleDCWithRef><SimpleDCWithRef z:Id=\"i9\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data z:Id=\"i10\"><Data>11:59:59 PM</Data></Data><RefData z:Id=\"i11\"><Data>11:59:59 PM</Data></RefData></SimpleDCWithRef></Data></_data><_data2 i:type=\"a:SerializationTestTypes.ContainsLinkedList***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ContainsLinkedList***\"><Data><SimpleDCWithRef z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Ref=\"i3\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Ref=\"i3\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Ref=\"i5\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Ref=\"i8\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRef z:Ref=\"i9\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></Data></_data2></ObjectContainer>";
        var value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ContainsLinkedList(true));
        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SimpleCDC***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleCDC***\"><Item>One</Item><Item>Two</Item><Item>two</Item></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleCDC(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SimpleCDC2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleCDC2***\"><Item>One</Item><Item>Two</Item><Item>two</Item></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleCDC2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.ContainsSimpleCDC***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ContainsSimpleCDC***\"><data1 z:Id=\"i2\"><Item>One</Item><Item>Two</Item><Item>two</Item></data1><data2 z:Id=\"i3\"><Item>One</Item><Item>Two</Item><Item>two</Item></data2></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ContainsSimpleCDC(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DMInCollection1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DMInCollection1***\"><Data1 z:Id=\"i2\"><Data>11:59:59 PM</Data></Data1><List1><SimpleDC z:Ref=\"i2\"/><SimpleDC z:Id=\"i3\"><Data>11:59:59 PM</Data></SimpleDC><SimpleDC z:Ref=\"i3\"/></List1></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DMInCollection1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DMInCollection2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DMInCollection2***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><InnerContent>11:59:59 PM</InnerContent><InnerInnerContent>11:59:59 PM</InnerInnerContent><List1><SimpleDC z:Ref=\"i2\"/><SimpleDC z:Id=\"i3\"><Data>11:59:59 PM</Data></SimpleDC><SimpleDC z:Id=\"i4\"><Data>11:59:59 PM</Data></SimpleDC><SimpleDC z:Id=\"i5\"><Data>11:59:59 PM</Data></SimpleDC><SimpleDC z:Id=\"i6\"><Data>11:59:59 PM</Data></SimpleDC></List1><List2 i:type=\"b:System.Collections.Generic.List`1[[SerializationTestTypes.SimpleDC, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><SimpleDC z:Ref=\"i2\"/><SimpleDC z:Ref=\"i6\"/></List2><List3 i:type=\"b:System.Collections.Generic.List`1[[SerializationTestTypes.SimpleDC, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><SimpleDC z:Ref=\"i2\"/><SimpleDC z:Ref=\"i3\"/><SimpleDC z:Ref=\"i4\"/><SimpleDC z:Ref=\"i5\"/><SimpleDC z:Ref=\"i6\"/></List3><List4 i:type=\"b:System.Collections.Generic.List`1[[SerializationTestTypes.SimpleDC, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><SimpleDC z:Ref=\"i2\"/><SimpleDC z:Ref=\"i6\"/></List4></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DMInCollection2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DMInDict1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DMInDict1***\"><Data1 z:Id=\"i2\"><Data>11:59:59 PM</Data></Data1><Data2 z:Id=\"i3\"><Data>11:59:59 PM</Data></Data2><Dict1 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Id=\"i4\"><Data>11:59:59 PM</Data></b:Key><b:Value z:Id=\"i5\"><Data>cd4f6d1f-db5e-49c9-bb43-13e73508a549</Data></b:Value></b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Ref=\"i2\"/><b:Value z:Ref=\"i3\"/></b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Id=\"i6\"><Data>11:59:59 PM</Data></b:Key><b:Value z:Ref=\"i5\"/></b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Ref=\"i5\"/><b:Value z:Id=\"i7\"><Data>11:59:59 PM</Data></b:Value></b:KeyValueOfSimpleDCSimpleDCzETuxydO></Dict1><Dict2 i:type=\"c:System.Collections.Generic.Dictionary`2[[SerializationTestTypes.SimpleDC, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb],[SerializationTestTypes.SimpleDC, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:c=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Ref=\"i4\"/><b:Value z:Ref=\"i5\"/></b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Ref=\"i2\"/><b:Value z:Ref=\"i3\"/></b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Ref=\"i6\"/><b:Value z:Ref=\"i5\"/></b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:KeyValueOfSimpleDCSimpleDCzETuxydO><b:Key z:Ref=\"i5\"/><b:Value z:Ref=\"i7\"/></b:KeyValueOfSimpleDCSimpleDCzETuxydO></Dict2><InnerData1>11:59:59 PM</InnerData1><InnerInnerData1>cd4f6d1f-db5e-49c9-bb43-13e73508a549</InnerInnerData1><Kvp1 xmlns:b=\"http://schemas.datacontract.org/2004/07/System.Collections.Generic\"><b:key z:Ref=\"i5\"/><b:value z:Ref=\"i7\"/></Kvp1></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DMInDict1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DMWithRefInCollection1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DMWithRefInCollection1***\"><Data1 z:Id=\"i2\"><Data>11:59:59 PM</Data><RefData>11:59:59 PM</RefData></Data1><InnerData1>a6d053ed-f7d4-42fb-8e56-e4b425f26fa9</InnerData1><List1><SimpleDCWithSimpleDMRef z:Ref=\"i2\"/><SimpleDCWithSimpleDMRef z:Id=\"i3\"><Data>a6d053ed-f7d4-42fb-8e56-e4b425f26fa9</Data><RefData>11:59:59 PM</RefData></SimpleDCWithSimpleDMRef><SimpleDCWithSimpleDMRef z:Ref=\"i3\"/></List1></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DMWithRefInCollection1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DMWithRefInCollection2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DMWithRefInCollection2***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><InnerContent>11:59:59 PM</InnerContent><InnerInnerContent>11:59:59 PM</InnerInnerContent><List1><SimpleDCWithRef z:Id=\"i3\"><Data z:Ref=\"i2\"/><RefData z:Ref=\"i2\"/></SimpleDCWithRef><SimpleDCWithRef z:Id=\"i4\"><Data z:Id=\"i5\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i5\"/></SimpleDCWithRef><SimpleDCWithRef z:Id=\"i6\"><Data z:Id=\"i7\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i7\"/></SimpleDCWithRef><SimpleDCWithRef z:Id=\"i8\"><Data z:Id=\"i9\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i9\"/></SimpleDCWithRef><SimpleDCWithRef z:Id=\"i10\"><Data z:Id=\"i11\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i11\"/></SimpleDCWithRef></List1><List2 i:type=\"b:System.Collections.Generic.List`1[[SerializationTestTypes.SimpleDCWithRef, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><SimpleDCWithRef z:Ref=\"i3\"/><SimpleDCWithRef z:Ref=\"i10\"/></List2><List3 i:type=\"b:System.Collections.Generic.List`1[[SerializationTestTypes.SimpleDCWithRef, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><SimpleDCWithRef z:Ref=\"i3\"/><SimpleDCWithRef z:Ref=\"i4\"/><SimpleDCWithRef z:Ref=\"i6\"/><SimpleDCWithRef z:Ref=\"i8\"/><SimpleDCWithRef z:Ref=\"i10\"/></List3><List4 i:type=\"b:System.Collections.Generic.List`1[[SerializationTestTypes.SimpleDCWithRef, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><SimpleDCWithRef z:Ref=\"i3\"/><SimpleDCWithRef z:Ref=\"i10\"/></List4><List5><SimpleDC z:Ref=\"i2\"/><SimpleDC z:Id=\"i12\"><Data>11:59:59 PM</Data></SimpleDC><SimpleDC z:Ref=\"i2\"/></List5><List6 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:anyType z:Id=\"i13\" i:type=\"c:SerializationTestTypes.SimpleDC***\" xmlns:c=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDC***\"><Data>11:59:59 PM</Data></b:anyType><b:anyType z:Id=\"i14\" i:type=\"c:SerializationTestTypes.SimpleDCWithRef***\" xmlns:c=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDCWithRef***\"><Data z:Id=\"i15\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i15\"/></b:anyType><b:anyType z:Id=\"i16\" i:type=\"c:SerializationTestTypes.SimpleDCWithSimpleDMRef***\" xmlns:c=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDCWithSimpleDMRef***\"><Data>11:59:59 PM</Data><RefData>11:59:59 PM</RefData></b:anyType><b:anyType i:type=\"ArrayOfSimpleDC\"/><b:anyType i:type=\"ArrayOfSimpleDCWithRef\"/><b:anyType i:type=\"ArrayOfSimpleDCWithSimpleDMRef\"/><b:anyType i:type=\"ArrayOfSimpleDCWithRef\"><SimpleDCWithRef z:Ref=\"i3\"/><SimpleDCWithRef z:Ref=\"i4\"/><SimpleDCWithRef z:Ref=\"i6\"/><SimpleDCWithRef z:Ref=\"i8\"/><SimpleDCWithRef z:Ref=\"i10\"/></b:anyType><b:anyType i:type=\"ArrayOfSimpleDCWithRef\"><SimpleDCWithRef z:Ref=\"i3\"/><SimpleDCWithRef z:Ref=\"i10\"/></b:anyType><b:anyType i:type=\"ArrayOfSimpleDCWithRef\"><SimpleDCWithRef z:Ref=\"i3\"/><SimpleDCWithRef z:Ref=\"i4\"/><SimpleDCWithRef z:Ref=\"i6\"/><SimpleDCWithRef z:Ref=\"i8\"/><SimpleDCWithRef z:Ref=\"i10\"/></b:anyType><b:anyType i:type=\"ArrayOfSimpleDCWithRef\"><SimpleDCWithRef z:Ref=\"i3\"/><SimpleDCWithRef z:Ref=\"i10\"/></b:anyType><b:anyType i:type=\"ArrayOfSimpleDC\"><SimpleDC z:Ref=\"i2\"/><SimpleDC z:Ref=\"i12\"/><SimpleDC z:Ref=\"i2\"/></b:anyType><b:anyType z:Ref=\"i2\"/><b:anyType i:type=\"c:string\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">11:59:59 PM</b:anyType><b:anyType i:type=\"c:string\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">11:59:59 PM</b:anyType></List6></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DMWithRefInCollection2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DMWithRefInDict1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DMWithRefInDict1***\"><Data1 z:Id=\"i2\"><Data z:Id=\"i3\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i3\"/></Data1><Data2 z:Id=\"i4\"><Data z:Id=\"i5\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i5\"/></Data2><Dict1 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Id=\"i6\"><Data z:Id=\"i7\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i7\"/></b:Key><b:Value z:Id=\"i8\"><Data z:Id=\"i9\"><Data>6d807157-536f-4794-a157-e463a11029aa</Data></Data><RefData z:Ref=\"i9\"/></b:Value></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Ref=\"i2\"/><b:Value z:Ref=\"i4\"/></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Id=\"i10\"><Data z:Id=\"i11\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i11\"/></b:Key><b:Value z:Ref=\"i8\"/></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Ref=\"i8\"/><b:Value z:Id=\"i12\"><Data z:Id=\"i13\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i13\"/></b:Value></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO></Dict1><Dict2 i:type=\"c:System.Collections.Generic.Dictionary`2[[SerializationTestTypes.SimpleDCWithRef, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb],[SerializationTestTypes.SimpleDCWithRef, System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb]]\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:c=\"System.Private.CoreLib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e\"><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Ref=\"i6\"/><b:Value z:Ref=\"i8\"/></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Ref=\"i2\"/><b:Value z:Ref=\"i4\"/></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Ref=\"i10\"/><b:Value z:Ref=\"i8\"/></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO><b:Key z:Ref=\"i8\"/><b:Value z:Ref=\"i12\"/></b:KeyValueOfSimpleDCWithRefSimpleDCWithRefzETuxydO></Dict2><InnerData1 z:Ref=\"i3\"/><InnerInnerData1>6d807157-536f-4794-a157-e463a11029aa</InnerInnerData1><Kvp1 xmlns:b=\"http://schemas.datacontract.org/2004/07/System.Collections.Generic\"><b:key z:Ref=\"i8\"/><b:value z:Ref=\"i12\"/></Kvp1></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DMWithRefInDict1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);
    }

    [Fact]
    public static void DCS_BasicPerSerializerRoundTripAndCompare_ItRef()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = false
        };

        var rd = new Random();
        var baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.TestInheritence9***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence9***\"><base1 i:type=\"b:SerializationTestTypes.Derived2Serializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></base1><baseDC i:type=\"b:SerializationTestTypes.DerivedSerializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:c=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><c:string>Base1</c:string><c:string>Base2</c:string><c:string>Base3</c:string><c:string>Base4</c:string><c:string>Base5</c:string><c:string>Base6</c:string><c:string>Base7</c:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></baseDC><derived2><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived2><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 i:type=\"a:SerializationTestTypes.TestInheritence9***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence9***\"><base1 i:type=\"b:SerializationTestTypes.Derived2Serializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></base1><baseDC i:type=\"b:SerializationTestTypes.DerivedSerializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:c=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><c:string>Base1</c:string><c:string>Base2</c:string><c:string>Base3</c:string><c:string>Base4</c:string><c:string>Base5</c:string><c:string>Base6</c:string><c:string>Base7</c:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></baseDC><derived2><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived2><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data2></ObjectContainer>";
        var value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence9(true));
        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SimpleDC***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDC***\"><Data>11:59:59 PM</Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleDC(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SimpleDCWithSimpleDMRef***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDCWithSimpleDMRef***\"><Data>11:59:59 PM</Data><RefData>11:59:59 PM</RefData></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleDCWithSimpleDMRef(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SimpleDCWithRef***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDCWithRef***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleDCWithRef(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.ContainsSimpleDCWithRef***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ContainsSimpleDCWithRef***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i3\"/></Data><RefData z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ContainsSimpleDCWithRef(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SimpleDCWithIsRequiredFalse***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SimpleDCWithIsRequiredFalse***\"><Data>11:59:59 PM</Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SimpleDCWithIsRequiredFalse(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.Mixed1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Mixed1***\"><Data1 z:Id=\"i2\"><Data>11:59:59 PM</Data></Data1><Data2 z:Id=\"i3\"><Data z:Id=\"i4\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i4\"/></Data2><Data3 z:Id=\"i5\"><Data>11:59:59 PM</Data><RefData>11:59:59 PM</RefData></Data3><Data4><Data>11:59:59 PM</Data></Data4><Data5><Data><Data>11:59:59 PM</Data></Data><RefData><Data>11:59:59 PM</Data></RefData></Data5></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Mixed1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.SerIser***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerIser***\"><containedData z:Id=\"i1\" i:type=\"b:SerializationTestTypes.PublicDC***\" xmlns=\"\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.PublicDC***\"><Data xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\">55cb1688-dec7-4106-a6d8-7e57590cb20a</Data></containedData></_data><_data2 i:type=\"a:SerializationTestTypes.SerIser***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SerIser***\"><containedData z:Ref=\"i1\" xmlns=\"\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SerIser());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DCVersioned1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCVersioned1***\"><Data z:Id=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><b:Data>11:59:59 PM</b:Data></Data><RefData z:Ref=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCVersioned1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DCVersioned2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCVersioned2***\"><Data z:Id=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><b:Data>11:59:59 PM</b:Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCVersioned2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DCVersionedContainer1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCVersionedContainer1***\"><DataVersion1 z:Id=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\"><Data z:Id=\"i3\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><b:Data>11:59:59 PM</b:Data></Data><RefData z:Ref=\"i3\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"/></DataVersion1></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCVersionedContainer1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DCVersionedContainerVersion1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCVersionedContainerVersion1***\"><DataVersion1 z:Id=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\"><Data z:Id=\"i3\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><b:Data>11:59:59 PM</b:Data></Data><RefData z:Ref=\"i3\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"/></DataVersion1><DataVersion2 z:Id=\"i4\" xmlns=\"SerializationTestTypes.ExtensionData\"><Data z:Id=\"i5\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><b:Data>11:59:59 PM</b:Data></Data></DataVersion2><RefDataVersion1 z:Ref=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\"/><RefDataVersion2 z:Ref=\"i4\" xmlns=\"SerializationTestTypes.ExtensionData\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCVersionedContainerVersion1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DCVersionedContainerVersion2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCVersionedContainerVersion2***\"><DataVersion1 z:Id=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\"><Data z:Id=\"i3\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><b:Data>11:59:59 PM</b:Data></Data><RefData z:Ref=\"i3\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"/></DataVersion1><DataVersion2 z:Id=\"i4\" xmlns=\"SerializationTestTypes.ExtensionData\"><Data z:Id=\"i5\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><b:Data>11:59:59 PM</b:Data></Data></DataVersion2></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCVersionedContainerVersion2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DCVersionedContainerVersion3***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCVersionedContainerVersion3***\"><DCVersioned1 z:Id=\"i2\" i:type=\"b:SerializationTestTypes.DCVersioned1***\" xmlns=\"SerializationTestTypes.ExtensionData\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DCVersioned1***\"><Data z:Id=\"i3\" xmlns:c=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"><c:Data>11:59:59 PM</c:Data></Data><RefData z:Ref=\"i3\" xmlns:c=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\"/></DCVersioned1><NewRefDataVersion1 z:Ref=\"i2\" xmlns=\"SerializationTestTypes.ExtensionData\"/><RefDataVersion2 i:nil=\"true\" xmlns=\"SerializationTestTypes.ExtensionData\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DCVersionedContainerVersion3(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.BaseDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseDC***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2></_data><_data2 i:type=\"a:SerializationTestTypes.BaseDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseDC***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BaseDC(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.BaseSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></_data><_data2 i:type=\"a:SerializationTestTypes.BaseSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BaseSerializable(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedDC***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></_data><_data2 i:type=\"a:SerializationTestTypes.DerivedDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedDC***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedDC(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></_data><_data2 i:type=\"a:SerializationTestTypes.DerivedSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedSerializable(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedDCIsRefBaseSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedDCIsRefBaseSerializable***\"><data i:nil=\"true\"/><data2 i:nil=\"true\"/><days i:nil=\"true\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><Data33 i:nil=\"true\"/></_data><_data2 i:type=\"a:SerializationTestTypes.DerivedDCIsRefBaseSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedDCIsRefBaseSerializable***\"><data i:nil=\"true\"/><data2 i:nil=\"true\"/><days i:nil=\"true\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><Data33 i:nil=\"true\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedDCIsRefBaseSerializable());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedDCBaseSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedDCBaseSerializable***\"><data i:nil=\"true\"/><data2 i:nil=\"true\"/><days i:nil=\"true\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><Data33 i:nil=\"true\"/></_data><_data2 i:type=\"a:SerializationTestTypes.DerivedDCBaseSerializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedDCBaseSerializable***\"><data i:nil=\"true\"/><data2 i:nil=\"true\"/><days i:nil=\"true\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><Data33 i:nil=\"true\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedDCBaseSerializable());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived2DC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2DC***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data11>12:00:00 AM</data11><data12>12:00:00 AM</data12><data4>12:00:00 AM</data4></_data><_data2 i:type=\"a:SerializationTestTypes.Derived2DC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2DC***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data11>12:00:00 AM</data11><data12>12:00:00 AM</data12><data4>12:00:00 AM</data4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived2DC(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.BaseDCNoIsRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseDCNoIsRef***\"><_data/></_data><_data2 i:type=\"a:SerializationTestTypes.BaseDCNoIsRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseDCNoIsRef***\"><_data/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BaseDCNoIsRef());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedPOCOBaseDCNOISRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedPOCOBaseDCNOISRef***\"><_data/><Data22 i:nil=\"true\"/></_data><_data2 i:type=\"a:SerializationTestTypes.DerivedPOCOBaseDCNOISRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedPOCOBaseDCNOISRef***\"><_data/><Data22 i:nil=\"true\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedPOCOBaseDCNOISRef());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedIXmlSerializable_POCOBaseDCNOISRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedIXmlSerializable_POCOBaseDCNOISRef***\">12:00 AM</_data><_data2 i:type=\"a:SerializationTestTypes.DerivedIXmlSerializable_POCOBaseDCNOISRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedIXmlSerializable_POCOBaseDCNOISRef***\">12:00 AM</_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedIXmlSerializable_POCOBaseDCNOISRef());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedCDCFromBaseDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedCDCFromBaseDC***\"/><_data2 i:type=\"a:SerializationTestTypes.DerivedCDCFromBaseDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedCDCFromBaseDC***\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedCDCFromBaseDC());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data><_data2 i:type=\"a:SerializationTestTypes.Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived2Serializable(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived2SerializablePositive***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2SerializablePositive***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>11:59:59 PM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data><_data2 i:type=\"a:SerializationTestTypes.Derived2SerializablePositive***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2SerializablePositive***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>11:59:59 PM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived2SerializablePositive(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived2Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data><_data2 i:type=\"a:SerializationTestTypes.Derived2Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived2Derived2Serializable(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived3Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived3Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data><_data2 i:type=\"a:SerializationTestTypes.Derived3Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived3Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived3Derived2Serializable(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived31Derived2SerializablePOCO***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived31Derived2SerializablePOCO***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4><RefData z:Id=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/></RefData><SimpleDCWithRefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data><_data2 i:type=\"a:SerializationTestTypes.Derived31Derived2SerializablePOCO***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived31Derived2SerializablePOCO***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4><RefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived31Derived2SerializablePOCO(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived4Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived4Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data><_data2 i:type=\"a:SerializationTestTypes.Derived4Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived4Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived4Derived2Serializable(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived5Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived5Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data><_data2 i:type=\"a:SerializationTestTypes.Derived5Derived2Serializable***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived5Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived5Derived2Serializable(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived6Derived2SerializablePOCO***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived6Derived2SerializablePOCO***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4><RefData z:Id=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/></RefData><SimpleDCWithRefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data><_data2 i:type=\"a:SerializationTestTypes.Derived6Derived2SerializablePOCO***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived6Derived2SerializablePOCO***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4><RefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><SimpleDCWithRefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived6Derived2SerializablePOCO(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.BaseWithIsRefTrue***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseWithIsRefTrue***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BaseWithIsRefTrue(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedNoIsRef***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedNoIsRef***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedNoIsRef(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedNoIsRef2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedNoIsRef2***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedNoIsRef2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedNoIsRef3***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedNoIsRef3***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedNoIsRef3(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedNoIsRef4***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedNoIsRef4***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/><RefData4 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedNoIsRef4(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedNoIsRef5***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedNoIsRef5***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/><RefData4 z:Ref=\"i2\"/><RefData5 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedNoIsRef5(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedNoIsRefWithIsRefTrue6***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedNoIsRefWithIsRefTrue6***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/><RefData4 z:Ref=\"i2\"/><RefData5 z:Ref=\"i2\"/><RefData6 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedNoIsRefWithIsRefTrue6(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefFalse***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefFalse***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefFalse(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefFalse2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefFalse2***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefFalse2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefFalse3***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefFalse3***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefFalse3(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefFalse4***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefFalse4***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/><RefData4 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefFalse4(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefFalse5***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefFalse5***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/><RefData4 z:Ref=\"i2\"/><RefData5 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefFalse5(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefTrue6***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefTrue6***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/><RefData3 z:Ref=\"i2\"/><RefData4 z:Ref=\"i2\"/><RefData5 z:Ref=\"i2\"/><RefData6 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefTrue6(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefTrueExplicit***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefTrueExplicit***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefTrueExplicit(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.DerivedWithIsRefTrueExplicit2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefTrueExplicit2***\"><Data z:Id=\"i2\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i2\"/><RefData2 z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefTrueExplicit2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.BaseNoIsRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseNoIsRef***\"><Data z:Id=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data>11:59:59 PM</Data></Data></_data><_data2 i:type=\"a:SerializationTestTypes.BaseNoIsRef***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BaseNoIsRef***\"><Data z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BaseNoIsRef(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedWithIsRefFalseExplicit***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefFalseExplicit***\"><Data z:Id=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"><Data>11:59:59 PM</Data></Data><RefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data><_data2 i:type=\"a:SerializationTestTypes.DerivedWithIsRefFalseExplicit***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedWithIsRefFalseExplicit***\"><Data z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/><RefData z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedWithIsRefFalseExplicit(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence***\"><baseDC i:type=\"b:SerializationTestTypes.DerivedDC***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedDC***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.TestInheritence91***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence91***\"><base1 i:type=\"b:SerializationTestTypes.Derived2Serializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></base1><baseDC i:type=\"b:SerializationTestTypes.DerivedSerializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:c=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><c:string>Base1</c:string><c:string>Base2</c:string><c:string>Base3</c:string><c:string>Base4</c:string><c:string>Base5</c:string><c:string>Base6</c:string><c:string>Base7</c:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></baseDC><derived2 i:type=\"b:SerializationTestTypes.Derived3Derived2Serializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived3Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived2><derived3><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived3><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 i:type=\"a:SerializationTestTypes.TestInheritence91***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence91***\"><base1 i:type=\"b:SerializationTestTypes.Derived2Serializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></base1><baseDC i:type=\"b:SerializationTestTypes.DerivedSerializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedSerializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:c=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><c:string>Base1</c:string><c:string>Base2</c:string><c:string>Base3</c:string><c:string>Base4</c:string><c:string>Base5</c:string><c:string>Base6</c:string><c:string>Base7</c:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></baseDC><derived2 i:type=\"b:SerializationTestTypes.Derived3Derived2Serializable***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived3Derived2Serializable***\"><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived2><derived3><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived3><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence91(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence5***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence5***\"><baseDC i:nil=\"true\"/><derivedDC i:nil=\"true\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence5(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.TestInheritence10***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence10***\"/><_data2 i:type=\"a:SerializationTestTypes.TestInheritence10***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence10***\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence10(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence2***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.TestInheritence11***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence11***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 i:type=\"a:SerializationTestTypes.TestInheritence11***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence11***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence11(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence3***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence3***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence3(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.TestInheritence16***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence16***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 i:type=\"a:SerializationTestTypes.TestInheritence16***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence16***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence16(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence4***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence4***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence4(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.TestInheritence12***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence12***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 i:type=\"a:SerializationTestTypes.TestInheritence12***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence12***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence12(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence6***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence6***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2></baseDC><derived2DC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data11>12:00:00 AM</data11><data12>12:00:00 AM</data12><data4>12:00:00 AM</data4></derived2DC><derivedDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3></derivedDC></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence6(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence7***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence7***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2></baseDC><derived2DC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data11>12:00:00 AM</data11><data12>12:00:00 AM</data12><data4>12:00:00 AM</data4></derived2DC></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence7(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.TestInheritence14***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence14***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derived2DC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived2DC></_data><_data2 i:type=\"a:SerializationTestTypes.TestInheritence14***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence14***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><days xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>Base1</b:string><b:string>Base2</b:string><b:string>Base3</b:string><b:string>Base4</b:string><b:string>Base5</b:string><b:string>Base6</b:string><b:string>Base7</b:string></days></baseDC><derived2DC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data00>12:00:00 AM</data00><data122>12:00:00 AM</data122><data4>12:00:00 AM</data4></derived2DC></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence14(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.TestInheritence8***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.TestInheritence8***\"><baseDC><data>12:00:00 AM</data><data2>12:00:00 AM</data2></baseDC><derived2DC><data>12:00:00 AM</data><data2>12:00:00 AM</data2><data0>12:00:00 AM</data0><data1>12:00:00 AM</data1><data3>12:00:00 AM</data3><data11>12:00:00 AM</data11><data12>12:00:00 AM</data12><data4>12:00:00 AM</data4></derived2DC></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.TestInheritence8(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);
    }

    [Fact]
    public static void DCS_BasicPerSerializerRoundTripAndCompare_SelfRefCycles()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = false
        };

        var baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SelfRef1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SelfRef1***\"><Data z:Ref=\"i1\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        var value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SelfRef1(true));
        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SelfRef1DoubleDM***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SelfRef1DoubleDM***\"><Data z:Ref=\"i1\"/><Data2 z:Ref=\"i1\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SelfRef1DoubleDM(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SelfRef2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SelfRef2***\"><Data z:Id=\"i2\"><Data z:Ref=\"i2\"/></Data><RefData z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SelfRef2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.SelfRef3***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SelfRef3***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Ref=\"i3\"/></Data><RefData z:Ref=\"i3\"/></Data><RefData z:Ref=\"i2\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SelfRef3(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.Cyclic1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Cyclic1***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Ref=\"i2\"/></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Cyclic1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.Cyclic2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Cyclic2***\"><Data z:Id=\"i2\"><Data z:Ref=\"i1\"/></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Cyclic2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicA***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicA***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Ref=\"i1\"/></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicA(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicB***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicB***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Ref=\"i1\"/></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicB(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicC***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicC***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Ref=\"i1\"/></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicC(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicD***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicD***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Ref=\"i1\"/></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicD(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD1***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Ref=\"i2\"/></Data></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD2***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD2***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data i:nil=\"true\"/></Data></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD2(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD3***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD3***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Ref=\"i6\"/></Data></Data></Data></Data></Data></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD3(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD4***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD4***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Id=\"i10\"><Data z:Ref=\"i7\"/></Data></Data></Data></Data></Data></Data></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD4(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD5***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD5***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Ref=\"i2\"/></Data></Data></Data></Data></Data></Data></Data></Data><Data2 z:Ref=\"i6\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD5(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD6***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD6***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Ref=\"i2\"/></Data></Data></Data></Data></Data></Data></Data></Data><Data2 z:Ref=\"i6\"/><Data3 z:Ref=\"i3\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD6(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD7***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD7***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Ref=\"i2\"/></Data></Data></Data></Data><Data2 z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Ref=\"i2\"/></Data></Data></Data></Data2><Data3 z:Ref=\"i3\"/><Data4 z:Id=\"i10\"><Data z:Id=\"i11\"><Data z:Ref=\"i6\"/></Data></Data4></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD7(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCD8***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCD8***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Ref=\"i2\"/></Data></Data></Data></Data><Data2 z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Ref=\"i6\"/></Data></Data></Data></Data2><Data3 z:Ref=\"i7\"/><Data4 z:Id=\"i10\"><Data z:Id=\"i11\"><Data z:Ref=\"i2\"/></Data></Data4><Data5 z:Ref=\"i11\"/></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCD8(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.CyclicABCDNoCycles***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CyclicABCDNoCycles***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Ref=\"i6\"/></Data></Data></Data></Data></Data></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CyclicABCDNoCycles(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.A1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.A1***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Ref=\"i4\"/></Data><Data2 z:Id=\"i8\"><Data z:Ref=\"i6\"/></Data2></Data></Data></Data></Data><Data2 z:Id=\"i9\"><Data z:Id=\"i10\"><Data z:Id=\"i11\"><Data z:Id=\"i12\"><Data z:Id=\"i13\"><Data z:Id=\"i14\"><Data z:Id=\"i15\"><Data z:Ref=\"i12\"/></Data><Data2 z:Id=\"i16\"><Data z:Ref=\"i14\"/></Data2></Data></Data></Data></Data><Data2 z:Id=\"i17\"><Data i:nil=\"true\"/></Data2></Data></Data2></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.A1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.B1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.B1***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Ref=\"i3\"/></Data><Data2 z:Id=\"i7\"><Data z:Ref=\"i5\"/></Data2></Data></Data></Data></Data><Data2 z:Id=\"i8\"><Data z:Id=\"i9\"><Data z:Id=\"i10\"><Data z:Id=\"i11\"><Data z:Id=\"i12\"><Data z:Id=\"i13\"><Data z:Id=\"i14\"><Data z:Ref=\"i11\"/></Data><Data2 z:Id=\"i15\"><Data z:Ref=\"i13\"/></Data2></Data></Data></Data></Data><Data2 z:Id=\"i16\"><Data i:nil=\"true\"/></Data2></Data></Data2></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.B1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.C1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.C1***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Id=\"i6\"><Data z:Id=\"i7\"><Data z:Ref=\"i4\"/></Data><Data2 z:Id=\"i8\"><Data z:Ref=\"i6\"/></Data2></Data></Data></Data></Data><Data2 z:Id=\"i9\"><Data i:nil=\"true\"/></Data2></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.C1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.BB1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BB1***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Id=\"i5\"><Data z:Ref=\"i2\"/></Data><Data2 z:Id=\"i6\"><Data z:Ref=\"i4\"/></Data2></Data></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BB1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data z:Id=\"i1\" i:type=\"a:SerializationTestTypes.BBB1***\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BBB1***\"><Data z:Id=\"i2\"><Data z:Id=\"i3\"><Data z:Id=\"i4\"><Data z:Ref=\"i1\"/></Data><Data2 z:Id=\"i5\"><Data z:Ref=\"i3\"/></Data2></Data></Data></_data><_data2 z:Ref=\"i1\" xmlns:z=\"http://schemas.microsoft.com/2003/10/Serialization/\"/></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BBB1(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);
    }

    [Fact]
    public static void DCS_BasicPerSerializerRoundTripAndCompare_EnumStruct()
    {
        var dataContractSerializerSettings = new DataContractSerializerSettings()
        {
            DataContractResolver = new SerializationTestTypes.SimpleResolver(),
            IgnoreExtensionDataObject = false,
            KnownTypes = null,
            MaxItemsInObjectGraph = int.MaxValue,
            PreserveObjectReferences = false
        };

        var baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.SeasonsEnumContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SeasonsEnumContainer***\"><member1>Autumn</member1><member2>Spring</member2><member3>Winter</member3></_data><_data2 i:type=\"a:SerializationTestTypes.SeasonsEnumContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.SeasonsEnumContainer***\"><member1>Autumn</member1><member2>Spring</member2><member3>Winter</member3></_data2></ObjectContainer>";
        var value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.SeasonsEnumContainer());
        var actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Person***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Person***\"><Age>6</Age><Name>smith</Name></_data><_data2 i:type=\"a:SerializationTestTypes.Person***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Person***\"><Age>6</Age><Name>smith</Name></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Person("Hi"));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.CharClass***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CharClass***\"><c>0</c><c1>65535</c1><c2>0</c2><c3>99</c3></_data><_data2 i:type=\"a:SerializationTestTypes.CharClass***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.CharClass***\"><c>0</c><c1>65535</c1><c2>0</c2><c3>99</c3></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.CharClass());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.AllTypes***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.AllTypes***\"><a>false</a><array1><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/></array1><b>255</b><c>0</c><d>65535</d><e>79228162514264337593543950335</e><enumArrayData><MyEnum1>red</MyEnum1></enumArrayData><enumBase1 i:type=\"b:SerializationTestTypes.MyEnum1***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyEnum1***\">red</enumBase1><f>-1</f><f5>0001-01-01T00:00:00</f5><g>-79228162514264337593543950335</g><guidData>5642b5d2-87c3-a724-2390-997062f3f7a2</guidData><h>1</h><i>0</i><j>0</j><k>0</k><l>4.94065645841247E-324</l><lDTO xmlns:b=\"http://schemas.datacontract.org/2004/07/System\"/><m>1.7976931348623157E+308</m><n>-1.7976931348623157E+308</n><nDTO xmlns:b=\"http://schemas.datacontract.org/2004/07/System\"><b:DateTime>9999-12-31T23:59:59.9999999Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></nDTO><o>NaN</o><obj/><p>-INF</p><q>INF</q><r>0</r><s>1.401298E-45</s><strData i:nil=\"true\"/><t>-3.40282347E+38</t><timeSpan i:type=\"b:duration\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">P10675199DT2H48M5.4775807S</timeSpan><u>3.40282347E+38</u><uri>http://www.microsoft.com/</uri><v>NaN</v><valType i:type=\"PublicDCStruct\"><Data>Data</Data></valType><w>-INF</w><x>INF</x><q:xmlQualifiedName xmlns:q=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:b=\"http://www.microsoft.com\">b:WCF</q:xmlQualifiedName><y>0</y><z>2147483647</z><z1>-2147483648</z1><z2>0</z2><z3>9223372036854775807</z3><z4>-9223372036854775808</z4><z5/><z6>0</z6><z7>127</z7><z8>-128</z8><z9>0</z9><z91>32767</z91><z92>-32768</z92><z93>abc</z93><z94>0</z94><z95>65535</z95><z96>0</z96><z97>0</z97><z98>4294967295</z98><z99>0</z99><z990>0</z990><z991>18446744073709551615</z991><z992>0</z992></_data><_data2 i:type=\"a:SerializationTestTypes.AllTypes***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.AllTypes***\"><a>false</a><array1><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/></array1><b>255</b><c>0</c><d>65535</d><e>79228162514264337593543950335</e><enumArrayData><MyEnum1>red</MyEnum1></enumArrayData><enumBase1 i:type=\"b:SerializationTestTypes.MyEnum1***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyEnum1***\">red</enumBase1><f>-1</f><f5>0001-01-01T00:00:00</f5><g>-79228162514264337593543950335</g><guidData>5642b5d2-87c3-a724-2390-997062f3f7a2</guidData><h>1</h><i>0</i><j>0</j><k>0</k><l>4.94065645841247E-324</l><lDTO xmlns:b=\"http://schemas.datacontract.org/2004/07/System\"/><m>1.7976931348623157E+308</m><n>-1.7976931348623157E+308</n><nDTO xmlns:b=\"http://schemas.datacontract.org/2004/07/System\"><b:DateTime>9999-12-31T23:59:59.9999999Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></nDTO><o>NaN</o><obj/><p>-INF</p><q>INF</q><r>0</r><s>1.401298E-45</s><strData i:nil=\"true\"/><t>-3.40282347E+38</t><timeSpan i:type=\"b:duration\" xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/\">P10675199DT2H48M5.4775807S</timeSpan><u>3.40282347E+38</u><uri>http://www.microsoft.com/</uri><v>NaN</v><valType i:type=\"PublicDCStruct\"><Data>Data</Data></valType><w>-INF</w><x>INF</x><q:xmlQualifiedName xmlns:q=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:b=\"http://www.microsoft.com\">b:WCF</q:xmlQualifiedName><y>0</y><z>2147483647</z><z1>-2147483648</z1><z2>0</z2><z3>9223372036854775807</z3><z4>-9223372036854775808</z4><z5/><z6>0</z6><z7>127</z7><z8>-128</z8><z9>0</z9><z91>32767</z91><z92>-32768</z92><z93>abc</z93><z94>0</z94><z95>65535</z95><z96>0</z96><z97>0</z97><z98>4294967295</z98><z99>0</z99><z990>0</z990><z991>18446744073709551615</z991><z992>0</z992></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.AllTypes());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.AllTypes2***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.AllTypes2***\"><a>false</a><array1><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/></array1><b>255</b><c>0</c><d>65535</d><e>79228162514264337593543950335</e><enumArrayData><MyEnum1>red</MyEnum1></enumArrayData><enumBase1 i:type=\"b:SerializationTestTypes.MyEnum1***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyEnum1***\">red</enumBase1><f>-1</f><f5>0001-01-01T00:00:00</f5><g>-79228162514264337593543950335</g><guidData>cac76333-577f-7e1f-0389-789b0d97f395</guidData><h>1</h><i>0</i><j>0</j><k>0</k><l>4.94065645841247E-324</l><m>1.7976931348623157E+308</m><n>-1.7976931348623157E+308</n><nDTO xmlns:b=\"http://schemas.datacontract.org/2004/07/System\"><b:DateTime>9999-12-31T23:59:59.9999999Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></nDTO><o>NaN</o><obj/><p>-INF</p><q>INF</q><r>0</r><s>1.401298E-45</s><strData i:nil=\"true\"/><t>-3.40282347E+38</t><timeSpan>P10675199DT2H48M5.4775807S</timeSpan><u>3.40282347E+38</u><uri>http://www.microsoft.com/</uri><v>NaN</v><valType i:type=\"PublicDCStruct\"><Data>Data</Data></valType><w>-INF</w><x>INF</x><q:xmlQualifiedName xmlns:q=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:b=\"http://www.microsoft.com\">b:WCF</q:xmlQualifiedName><y>0</y><z>2147483647</z><z1>-2147483648</z1><z2>0</z2><z3>9223372036854775807</z3><z4>-9223372036854775808</z4><z5/><z6>0</z6><z7>127</z7><z8>-128</z8><z9>0</z9><z91>32767</z91><z92>-32768</z92><z93>abc</z93><z94>0</z94><z95>65535</z95><z96>0</z96><z97>0</z97><z98>4294967295</z98><z99>0</z99><z990>0</z990><z991>18446744073709551615</z991><z992>0</z992></_data><_data2 i:type=\"a:SerializationTestTypes.AllTypes2***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.AllTypes2***\"><a>false</a><array1><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><anyType xmlns=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/></array1><b>255</b><c>0</c><d>65535</d><e>79228162514264337593543950335</e><enumArrayData><MyEnum1>red</MyEnum1></enumArrayData><enumBase1 i:type=\"b:SerializationTestTypes.MyEnum1***\" xmlns:b=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.MyEnum1***\">red</enumBase1><f>-1</f><f5>0001-01-01T00:00:00</f5><g>-79228162514264337593543950335</g><guidData>cac76333-577f-7e1f-0389-789b0d97f395</guidData><h>1</h><i>0</i><j>0</j><k>0</k><l>4.94065645841247E-324</l><m>1.7976931348623157E+308</m><n>-1.7976931348623157E+308</n><nDTO xmlns:b=\"http://schemas.datacontract.org/2004/07/System\"><b:DateTime>9999-12-31T23:59:59.9999999Z</b:DateTime><b:OffsetMinutes>0</b:OffsetMinutes></nDTO><o>NaN</o><obj/><p>-INF</p><q>INF</q><r>0</r><s>1.401298E-45</s><strData i:nil=\"true\"/><t>-3.40282347E+38</t><timeSpan>P10675199DT2H48M5.4775807S</timeSpan><u>3.40282347E+38</u><uri>http://www.microsoft.com/</uri><v>NaN</v><valType i:type=\"PublicDCStruct\"><Data>Data</Data></valType><w>-INF</w><x>INF</x><q:xmlQualifiedName xmlns:q=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:b=\"http://www.microsoft.com\">b:WCF</q:xmlQualifiedName><y>0</y><z>2147483647</z><z1>-2147483648</z1><z2>0</z2><z3>9223372036854775807</z3><z4>-9223372036854775808</z4><z5/><z6>0</z6><z7>127</z7><z8>-128</z8><z9>0</z9><z91>32767</z91><z92>-32768</z92><z93>abc</z93><z94>0</z94><z95>65535</z95><z96>0</z96><z97>0</z97><z98>4294967295</z98><z99>0</z99><z990>0</z990><z991>18446744073709551615</z991><z992>0</z992></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.AllTypes2());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DictContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DictContainer***\"><dictionaryData xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:KeyValueOfbase64Binarybase64Binary><b:Key>S3sf7NHCbkyVtgYKERsK/Q==</b:Key><b:Value>kNwxmEzKsk2TNVihAl7PKQ==</b:Value></b:KeyValueOfbase64Binarybase64Binary><b:KeyValueOfbase64Binarybase64Binary><b:Key>R5hoXhAack+qrhmyR80IeA==</b:Key><b:Value>kYav59VD50mHdRsBJr2UPA==</b:Value></b:KeyValueOfbase64Binarybase64Binary><b:KeyValueOfbase64Binarybase64Binary><b:Key>3WgRcQBK5U2fPjjd+9oBRA==</b:Key><b:Value>r7SFJrYJVkqB25UjGj0Cdg==</b:Value></b:KeyValueOfbase64Binarybase64Binary></dictionaryData></_data><_data2 i:type=\"a:SerializationTestTypes.DictContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DictContainer***\"><dictionaryData xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:KeyValueOfbase64Binarybase64Binary><b:Key>S3sf7NHCbkyVtgYKERsK/Q==</b:Key><b:Value>kNwxmEzKsk2TNVihAl7PKQ==</b:Value></b:KeyValueOfbase64Binarybase64Binary><b:KeyValueOfbase64Binarybase64Binary><b:Key>R5hoXhAack+qrhmyR80IeA==</b:Key><b:Value>kYav59VD50mHdRsBJr2UPA==</b:Value></b:KeyValueOfbase64Binarybase64Binary><b:KeyValueOfbase64Binarybase64Binary><b:Key>3WgRcQBK5U2fPjjd+9oBRA==</b:Key><b:Value>r7SFJrYJVkqB25UjGj0Cdg==</b:Value></b:KeyValueOfbase64Binarybase64Binary></dictionaryData></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DictContainer());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.ListContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ListContainer***\"><listData xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>1/1/0001</b:string></listData></_data><_data2 i:type=\"a:SerializationTestTypes.ListContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ListContainer***\"><listData xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:string>1/1/0001</b:string></listData></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ListContainer());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.ArrayContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ArrayContainer***\"><listData xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:anyType i:type=\"c:string\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">1/1/0001</b:anyType><b:anyType i:type=\"c:string\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">Test</b:anyType><b:anyType i:type=\"c:guid\" xmlns:c=\"http://schemas.microsoft.com/2003/10/Serialization/\">c0a7310f-f369-481e-a990-39b121eae513</b:anyType></listData></_data><_data2 i:type=\"a:SerializationTestTypes.ArrayContainer***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.ArrayContainer***\"><listData xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:anyType i:type=\"c:string\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">1/1/0001</b:anyType><b:anyType i:type=\"c:string\" xmlns:c=\"http://www.w3.org/2001/XMLSchema\">Test</b:anyType><b:anyType i:type=\"c:guid\" xmlns:c=\"http://schemas.microsoft.com/2003/10/Serialization/\">c0a7310f-f369-481e-a990-39b121eae513</b:anyType></listData></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.ArrayContainer(true));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.EnumContainer1***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EnumContainer1***\"><myPrivateEnum1 i:type=\"ArrayOfEnum1\"><Enum1>red</Enum1></myPrivateEnum1></_data><_data2 i:type=\"a:SerializationTestTypes.EnumContainer1***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EnumContainer1***\"><myPrivateEnum1 i:type=\"ArrayOfEnum1\"><Enum1>red</Enum1></myPrivateEnum1></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.EnumContainer1());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.EnumContainer2***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EnumContainer2***\"><myPrivateEnum2 i:type=\"ArrayOfMyPrivateEnum2\"><MyPrivateEnum2>red</MyPrivateEnum2></myPrivateEnum2></_data><_data2 i:type=\"a:SerializationTestTypes.EnumContainer2***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EnumContainer2***\"><myPrivateEnum2 i:type=\"ArrayOfMyPrivateEnum2\"><MyPrivateEnum2>red</MyPrivateEnum2></myPrivateEnum2></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.EnumContainer2());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.EnumContainer3***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EnumContainer3***\"><myPrivateEnum3 i:type=\"ArrayOfMyPrivateEnum3\"><MyPrivateEnum3>red</MyPrivateEnum3></myPrivateEnum3></_data><_data2 i:type=\"a:SerializationTestTypes.EnumContainer3***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EnumContainer3***\"><myPrivateEnum3 i:type=\"ArrayOfMyPrivateEnum3\"><MyPrivateEnum3>red</MyPrivateEnum3></myPrivateEnum3></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.EnumContainer3());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.WithStatic***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.WithStatic***\"><str>instance string</str></_data><_data2 i:type=\"a:SerializationTestTypes.WithStatic***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.WithStatic***\"><str>instance string</str></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.WithStatic());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.DerivedFromPriC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedFromPriC***\"><a>0</a><b i:nil=\"true\"/><c>0</c><d>0</d></_data><_data2 i:type=\"a:SerializationTestTypes.DerivedFromPriC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.DerivedFromPriC***\"><a>0</a><b i:nil=\"true\"/><c>0</c><d>0</d></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.DerivedFromPriC(0));
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.EmptyDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EmptyDC***\"><a>10</a></_data><_data2 i:type=\"a:SerializationTestTypes.EmptyDC***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.EmptyDC***\"><a>10</a></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.EmptyDC());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Base***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Base***\"><A>0</A><B i:nil=\"true\"/></_data><_data2 i:type=\"a:SerializationTestTypes.Base***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Base***\"><A>0</A><B i:nil=\"true\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Base());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Derived***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived***\"><A>0</A><B i:nil=\"true\"/></_data><_data2 i:type=\"a:SerializationTestTypes.Derived***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Derived***\"><A>0</A><B i:nil=\"true\"/></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Derived());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.list***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.list***\"><next i:nil=\"true\"/><value>0</value></_data><_data2 i:type=\"a:SerializationTestTypes.list***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.list***\"><next i:nil=\"true\"/><value>0</value></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.list());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Arrays***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Arrays***\"><a1 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><a2 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>1</b:int></a2><a3 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>1</b:int><b:int>2</b:int><b:int>3</b:int><b:int>4</b:int></a3><a4 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int></a4></_data><_data2 i:type=\"a:SerializationTestTypes.Arrays***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Arrays***\"><a1 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"/><a2 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>1</b:int></a2><a3 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>1</b:int><b:int>2</b:int><b:int>3</b:int><b:int>4</b:int></a3><a4 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int><b:int>0</b:int></a4></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Arrays());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Array3***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Array3***\"><a1 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:ArrayOfint><b:int>1</b:int></b:ArrayOfint><b:ArrayOfint/></a1></_data><_data2 i:type=\"a:SerializationTestTypes.Array3***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Array3***\"><a1 xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:ArrayOfint><b:int>1</b:int></b:ArrayOfint><b:ArrayOfint/></a1></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Array3());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Properties***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Properties***\"><A>5</A></_data><_data2 i:type=\"a:SerializationTestTypes.Properties***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Properties***\"><A>5</A></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Properties());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.HaveNS***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.HaveNS***\"><ns><a>0</a></ns></_data><_data2 i:type=\"a:SerializationTestTypes.HaveNS***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.HaveNS***\"><ns><a>0</a></ns></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.HaveNS());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.OutClass***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.OutClass***\"><nc><a>10</a></nc></_data><_data2 i:type=\"a:SerializationTestTypes.OutClass***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.OutClass***\"><nc><a>10</a></nc></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.OutClass());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Temp***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Temp***\"><a>10</a></_data><_data2 i:type=\"a:SerializationTestTypes.Temp***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Temp***\"><a>10</a></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Temp());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Array22***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Array22***\"><p xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>1</b:int></p></_data><_data2 i:type=\"a:SerializationTestTypes.Array22***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Array22***\"><p xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><b:int>1</b:int></p></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Array22());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.Person2***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Person2***\"><age>0</age><name i:nil=\"true\"/><Uid>ff816178-54df-2ea8-6511-cfeb4d14ab5a</Uid><XQAArray xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><q:QName xmlns:q=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:c=\"http://www.PlayForFun.com\">c:Name1</q:QName><q:QName xmlns:q=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:c=\"http://www.FunPlay.com\">c:Name2</q:QName></XQAArray><anyData i:type=\"b:SerializationTestTypes.Kid\" xmlns:b=\"System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb\"><age>3</age><name i:nil=\"true\"/><FavoriteToy i:type=\"b:SerializationTestTypes.Blocks\"><color>Orange</color></FavoriteToy></anyData></_data><_data2 i:type=\"a:SerializationTestTypes.Person2***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.Person2***\"><age>0</age><name i:nil=\"true\"/><Uid>ff816178-54df-2ea8-6511-cfeb4d14ab5a</Uid><XQAArray xmlns:b=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\"><q:QName xmlns:q=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:c=\"http://www.PlayForFun.com\">c:Name1</q:QName><q:QName xmlns:q=\"http://schemas.microsoft.com/2003/10/Serialization/Arrays\" xmlns:c=\"http://www.FunPlay.com\">c:Name2</q:QName></XQAArray><anyData i:type=\"b:SerializationTestTypes.Kid\" xmlns:b=\"System.Runtime.Serialization.Xml.Tests, Version=4.1.3.0, Culture=neutral, PublicKeyToken=9d77cc7ad39b68eb\"><age>3</age><name i:nil=\"true\"/><FavoriteToy i:type=\"b:SerializationTestTypes.Blocks\"><color>Orange</color></FavoriteToy></anyData></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.Person2());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        baseline = "<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:SerializationTestTypes.BoxedPrim***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BoxedPrim***\"><p i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">false</p><p2 i:type=\"VT\"><b>10</b></p2></_data><_data2 i:type=\"a:SerializationTestTypes.BoxedPrim***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes.BoxedPrim***\"><p i:type=\"b:boolean\" xmlns:b=\"http://www.w3.org/2001/XMLSchema\">false</p><p2 i:type=\"VT\"><b>10</b></p2></_data2></ObjectContainer>";
        value = new SerializationTestTypes.ObjectContainer(new SerializationTestTypes.BoxedPrim());
        actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
        SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);

        var typelist = new List<Type>
        {
            typeof(SerializationTestTypes.MyEnum),
            typeof(SerializationTestTypes.MyEnum1),
            typeof(SerializationTestTypes.MyEnum2),
            typeof(SerializationTestTypes.MyEnum3),
            typeof(SerializationTestTypes.MyEnum4),
            typeof(SerializationTestTypes.MyEnum7),
            typeof(SerializationTestTypes.MyEnum8),
            typeof(SerializationTestTypes.MyPrivateEnum1),
            typeof(SerializationTestTypes.MyPrivateEnum2),
            typeof(SerializationTestTypes.MyPrivateEnum3)
        };

        foreach (var type in typelist)
        {
            var possibleValues = Enum.GetValues(type);
            var input = possibleValues.GetValue(new Random().Next(possibleValues.Length));
            baseline = $"<ObjectContainer xmlns=\"http://schemas.datacontract.org/2004/07/SerializationTestTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><_data i:type=\"a:{type}***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/{type}***\">{input}</_data><_data2 i:type=\"a:{type}***\" xmlns:a=\"http://schemas.datacontract.org/2004/07/{type}***\">{input}</_data2></ObjectContainer>";
            value = new SerializationTestTypes.ObjectContainer(input);
            actual = SerializeAndDeserialize(value, baseline, dataContractSerializerSettings);
            SerializationTestTypes.ComparisonHelper.CompareRecursively(value, actual);
        }
    }

    #endregion

    [Fact]
    public static void DCS_MyPersonSurrogate()
    {
        DataContractSerializer dcs = new DataContractSerializer(typeof(Family));
        dcs.SetSerializationSurrogateProvider(new MyPersonSurrogateProvider());
        MemoryStream ms = new MemoryStream();
        Family myFamily = new Family
        {
            Members = new NonSerializablePerson[]
            {
                new NonSerializablePerson("John", 34),
                new NonSerializablePerson("Jane", 32),
                new NonSerializablePerson("Bob", 5),
            }
        };
        dcs.WriteObject(ms, myFamily);
        ms.Position = 0;
        var newFamily = (Family)dcs.ReadObject(ms);
        Assert.StrictEqual(myFamily.Members.Length, newFamily.Members.Length);
        for (int i = 0; i < myFamily.Members.Length; ++i)
        {
            Assert.StrictEqual(myFamily.Members[i].Name, newFamily.Members[i].Name);
        }
    }

    [Fact]
    public static void DCS_FileStreamSurrogate()
    {
        using (var testFile = TempFile.Create())
        {
            const string TestFileData = "Some data for data contract surrogate test";

            // Create the serializer and specify the surrogate
            var dcs = new DataContractSerializer(typeof(MyFileStream));
            dcs.SetSerializationSurrogateProvider(MyFileStreamSurrogateProvider.Singleton);

            // Create and initialize the stream
            byte[] serializedStream;

            // Serialize the stream
            using (var stream1 = new MyFileStream(testFile.Path))
            {
                stream1.WriteLine(TestFileData);
                using (var memoryStream = new MemoryStream())
                {
                    dcs.WriteObject(memoryStream, stream1);
                    serializedStream = memoryStream.ToArray();
                }
            }

            // Deserialize the stream
            using (var stream = new MemoryStream(serializedStream))
            {
                using (var stream2 = (MyFileStream)dcs.ReadObject(stream))
                {
                    string fileData = stream2.ReadLine();
                    Assert.StrictEqual(TestFileData, fileData);
                }
            }
        }
    }

    private static T SerializeAndDeserialize<T>(T value, string baseline, DataContractSerializerSettings settings = null, Func<DataContractSerializer> serializerFactory = null, bool skipStringCompare = false)
    {
        DataContractSerializer dcs;
        if (serializerFactory != null)
        {
            dcs = serializerFactory();
        }
        else
        {
            dcs = (settings != null) ? new DataContractSerializer(typeof(T), settings) : new DataContractSerializer(typeof(T));
        }

        using (MemoryStream ms = new MemoryStream())
        {
            dcs.WriteObject(ms, value);
            ms.Position = 0;

            string actualOutput = new StreamReader(ms).ReadToEnd();

            if (!skipStringCompare)
            {
                Utils.CompareResult result = Utils.Compare(baseline, actualOutput);
                Assert.True(result.Equal, string.Format("{1}{0}Test failed for input: {2}{0}Expected: {3}{0}Actual: {4}",
                    Environment.NewLine, result.ErrorMessage, value, baseline, actualOutput));
            }

            ms.Position = 0;
            T deserialized = (T)dcs.ReadObject(ms);

            return deserialized;
        }
    }

    private static T DeserializeString<T>(string stringToDeserialize, bool shouldReportDeserializationExceptions = true, DataContractSerializerSettings settings = null, Func<DataContractSerializer> serializerFactory = null)
    {
        DataContractSerializer dcs;
        if (serializerFactory != null)
        {
            dcs = serializerFactory();
        }
        else
        {
            dcs = (settings != null) ? new DataContractSerializer(typeof(T), settings) : new DataContractSerializer(typeof(T));
        }

        byte[] bytesToDeserialize = Encoding.UTF8.GetBytes(stringToDeserialize);
        using (MemoryStream ms = new MemoryStream(bytesToDeserialize))
        {
            ms.Position = 0;
            T deserialized = (T)dcs.ReadObject(ms);

            return deserialized;
        }
    }

    private static string s_errorMsg = "The field/property {0} value of deserialized object is wrong";
    private static string getCheckFailureMsg(string propertyName)
    {
        return string.Format(s_errorMsg, propertyName);
    }
}
