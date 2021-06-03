using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NetBox.FileFormats;
using NetBox.Generator;
using NetBox.Performance;
using NetBox.Terminal.Core;
using Xunit;

namespace NetBox
{
   public class ByteArrayExtensionsTest
   {
      [Theory]
      [InlineData(null, null)]
      [InlineData(new byte[] { }, "")]
      [InlineData(new byte[] { 0, 1, 2, 3, 4, 5 }, "000102030405")]
      public void ToHexString_Variable_Variable(byte[] input, string expected)
      {
         string actual = input.ToHexString();

         Assert.Equal(expected, actual);
      }
   }

   public class StringExtensionsTest
   {
      [Theory]
      [InlineData("<string>test text</string>", "test text")]
      public void StripHtml_Variable_Variable(string html, string stripped)
      {
         Assert.Equal(stripped, html.StripHtml());
      }

      [Fact]
      public void Base64_Encode_Decodes()
      {
         string s = "test string";
         string s64 = s.Base64Encode();
         string s2 = s64.Base64Decode();

         Assert.Equal(s, s2);
      }

      [Fact]
      public void ToMemoryStream_TestString_ReadsBack()
      {
         string input = "test stream";
         using (var ms = input.ToMemoryStream())
         {
            string s = Encoding.UTF8.GetString(ms.ToArray());
            Assert.Equal(input, s);
         }
      }

      [Fact]
      public void ToMemoryStream_EncodingTestString_ReadsBack()
      {
         string input = "test stream";
         using (var ms = input.ToMemoryStream(Encoding.ASCII))
         {
            string s = Encoding.ASCII.GetString(ms.ToArray());
            Assert.Equal(input, s);
         }
      }

      [Theory]
      [InlineData("the %variable%", "%", "%", true, "%variable%")]
      [InlineData("the %variable%", "%", "%", false, "variable")]
      [InlineData("this is a test", "Sean", "test", false, null)]
      [InlineData("this is a test", " is", "test", false, " a ")]
      public void FindTagged_Variations(string input, string startTag, string endTag, bool includeOuter, string expected)
      {
         Assert.Equal(expected, input.FindTagged(startTag, endTag, includeOuter));
      }

      [Fact]
      public void ReplaceTextBetween_ReturnsPastInStringIfStartTokenDoesNotExistInPassedInString()
      {
         string s = "this is a test";
         Assert.Equal("this is a test", s.ReplaceTagged("Sean", "test", "me", false));
      }

      [Fact]
      public void ReplaceTextBetween_ReturnsPastInStringIfEndTokenDoesNotExistInPassedInString()
      {
         string s = "this is a test";
         Assert.Equal("this is a test", s.ReplaceTagged("This", "Sean", "me", false));
      }

      [Fact]
      public void ReplaceTextBetween_RemovesOuterTokens()
      {
         string s = "This is a test";
         Assert.Equal("This unit test", s.ReplaceTagged(" is", "a ", " unit ", true));
      }

      [Fact]
      public void ReplaceTextBetween_DoesNotRemoveOuterTokens()
      {
         string s = "This is a test";
         Assert.Equal("This is unit a test", s.ReplaceTagged(" is", "a ", " unit ", false));
      }

      [Theory]
      [InlineData("One Two", "OneTwo")]
      [InlineData("one two Three", "OneTwoThree")]
      [InlineData("one tWo Three", "OneTwoThree")]
      [InlineData(null, null)]
      [InlineData("one tw", "OneTw")]
      public void SpacedToCamelCase_Variable_Variable(string input, string expected)
      {
         Assert.Equal(expected, input.SpacedToCamelCase());
      }

      [Theory]
      [InlineData(null, null)]
      [InlineData("O", "O")]
      [InlineData("o", "O")]
      [InlineData("one", "One")]
      [InlineData("tWo", "Two")]
      [InlineData("1234", "1234")]
      public void Capitalize_Variable_Variable(string input, string expected)
      {
         Assert.Equal(expected, input.Capitalize());
      }

      [Theory]
      [InlineData(null, null, null, null)]
      [InlineData("word", "ord", 1, null)]
      [InlineData("word", "ord", -3, null)]
      [InlineData("word", "word", -4, null)]
      [InlineData("word", "rd", -2, null)]
      [InlineData("word", "word", 0, null)]
      [InlineData("word", "word", null, null)]
      [InlineData("word", "wor", null, 3)]
      [InlineData("word", "wo", null, -2)]
      [InlineData("word", "", null, -10)]
      [InlineData("word", "or", 1, -1)]
      public void Slice_Variable_Variable(string input, string expected, int? start, int? end)
      {
         string result = input.Slice(start, end);

         Assert.Equal(expected, result);
      }

      [Theory]
      [InlineData(null, null, null, null)]
      [InlineData("key:value", null, "key:value", null)]
      [InlineData("key:value", ":", "key", "value")]
      [InlineData("key:value", "=", "key:value", null)]
      [InlineData("key:", ":", "key", "")]
      public void SplitByDelimiter_Variable_Variable(string input, string delimiter, string expectedKey, string expectedValue)
      {
         Tuple<string, string> result = input.SplitByDelimiter(delimiter);

         string key = result?.Item1;
         string value = result?.Item2;

         Assert.Equal(expectedKey, key);
         Assert.Equal(expectedValue, value);
      }

      [Fact]
      public void Empty_string_from_hex_returns_empty_byte_array()
      {
         byte[] r = "".FromHexToBytes();
         Assert.NotNull(r);
         Assert.Empty(r);
      }

      [Fact]
      public void Null_string_from_hex_returns_null()
      {
         byte[] r = ((string)null).FromHexToBytes();
         Assert.Null(r);
      }

      [Fact]
      public void Invalid_hex_from_hex_returns_null()
      {
         byte[] r = "zztop".FromHexToBytes();
         Assert.Null(r);
      }

      [Fact]
      public void One_byte_from_hex_returns_empty_byte_array()
      {
         byte[] r = "z".FromHexToBytes();
         Assert.Empty(r);

      }

      [Fact]
      public void RemoteLinesContaining()
      {
         string text = @"#Header
![dfdsfdf](000.png)
some text";

         string nt = text.RemoveLinesContaining("000.png").Trim();

         Assert.Equal(@"#Header
some text", nt, ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
      }

      [Theory]
      [InlineData(null, null)]
      [InlineData("the string", "the+string")]
      [InlineData("lk>i*", "lk%3Ei*")]
      public void UrlEncode_Variable_Variable(string decoded, string encoded)
      {
         string encodedNow = decoded.UrlEncode();

         Assert.Equal(encoded, encodedNow);
      }


      /*[Fact]
      public async Task I_can_download_web_page()
      {
         string content = await ("http://microsoft.com".HttpGetAsync());

         Assert.NotNull(content);
      }*/

      // ReSharper disable once MemberCanBePrivate.Global
      public class XmlDoc
      {
         // ReSharper disable once InconsistentNaming
         public string SV { get; set; }

         //public XmlEnum E { get; set; }
      }

      public enum XmlEnum
      {
         One,
         Two
      }

      private class HiddenDoc
      {
         // ReSharper disable once InconsistentNaming
         public string SV { get; set; }
      }

      // ReSharper disable once MemberCanBePrivate.Global
      public class NonXmlDoc
      {
         public NonXmlDoc(int i)
         {

         }
      }

   }

   public class DateTimeExtensionsTest
   {
      [Fact]
      public void RoundToDay_TestDateWithTime_TimeTrimmed()
      {
         Assert.Equal(new DateTime(2015, 09, 10), new DateTime(2015, 09, 10, 14, 17, 35).RoundToDay());
      }

      [Fact]
      public void ToHourMinuteString_Trivial_Trivial()
      {
         Assert.Equal("13:04", new DateTime(2014, 12, 3, 13, 4, 0).ToHourMinuteString());
      }

      [Theory]
      [InlineData("14:15", "14:18", 15, true)]
      [InlineData("14:30", "14:18", 15, false)]
      [InlineData("14:15", "14:15", 15, false)]
      [InlineData("14:15", "14:15", 15, true)]
      [InlineData("15:00", "14:59", 15, false)]
      [InlineData("14:00", "14:01", 15, true)]
      public void RoundToMinute_Variable_Variable(string expected, string actual, int round, bool roundToLeft)
      {
         DateTime actualDate = DateTime.UtcNow.RoundToDay().Add(TimeSpan.Parse(actual));
         DateTime expectedDate = DateTime.UtcNow.RoundToDay().Add(TimeSpan.Parse(expected));
         DateTime convertedDate = actualDate.RoundToMinute(round, roundToLeft);

         Assert.Equal(convertedDate, expectedDate);
      }

      [Fact]
      public void Convert_local_time_to_iso_string()
      {
         DateTime dt = DateTime.Now;

         string s = dt.ToIso8601DateString();

         Assert.Equal(dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"), s);
      }

      [Fact]
      public void Convert_utc_time_to_iso_string()
      {
         DateTime dt = DateTime.UtcNow;

         string s = dt.ToIso8601DateString();

         Assert.Equal(dt.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK"), s);
      }

   }

   public class CsvReaderWriterTest
   {
      private CsvWriter _writer;
      private CsvReader _reader;
      private MemoryStream _ms;

      public CsvReaderWriterTest()
      {
         _ms = new MemoryStream();
         _writer = new CsvWriter(_ms, Encoding.UTF8);
         _reader = new CsvReader(_ms, Encoding.UTF8);
      }

      private void SetReaderFromWriter()
      {
         _ms.Flush();
         _ms.Position = 0;
         _reader = new CsvReader(_ms, Encoding.UTF8);
      }

      [Fact]
      public void Write_2RowsOfDifferentSize_Succeeds()
      {
         _writer.Write("11", "12");
         _writer.Write("21", "22", "23");
      }

      [Fact]
      public void Write_2RowsOfSameSize_Succeeds()
      {
         _writer.Write("11", "12");
         _writer.Write("21", "22");
      }

      [Fact]
      public void Write_NoEscaping_JustQuotes()
      {
         _writer.Write("1", "-=--=,,**\r\n77$$");

         string result = Encoding.UTF8.GetString(_ms.ToArray());

         Assert.Equal("1,\"-=--=,,**\r77$$\"", result);
      }

      [Fact]
      public void Write_WithEscaping_EscapingAndQuoting()
      {
         _writer.Write("1", "two of \"these\"");

         string result = Encoding.UTF8.GetString(_ms.ToArray());

         Assert.Equal("1,\"two of \"\"these\"\"\"", result);

      }

      [Fact]
      public void WriteRead_WriteTwoRows_ReadsTwoRows()
      {
         _writer.Write("r1c1", "r1c2", "r1c3");
         _writer.Write("r2c1", "r2c2");
         SetReaderFromWriter();

         string[] r1 = _reader.ReadNextRow();
         string[] r2 = _reader.ReadNextRow();
         string[] r3 = _reader.ReadNextRow();

         Assert.Null(r3);
         Assert.Equal(2, r2.Length);
         Assert.Equal(3, r1.Length);

         Assert.Equal("r2c1", r2[0]);
      }

      [Fact]
      public void WriteRead_Multiline_Succeeds()
      {
         _writer.Write(@"mu
lt", "nm");
         _writer.Write("1", "2");
         SetReaderFromWriter();

         //validate first row
         string[] r = _reader.ReadNextRow();
         Assert.Equal(2, r.Length);
         Assert.Equal(@"mu
lt", r[0], false, true);
         Assert.Equal("nm", r[1]);

         //validate second row
         r = _reader.ReadNextRow();
         Assert.Equal(2, r.Length);
         Assert.Equal("1", r[0]);
         Assert.Equal("2", r[1]);

         //validate there is no more rows
         Assert.Null(_reader.ReadNextRow());
      }

      [Fact]
      public void WriteRead_OneColumnOneValue_Reads()
      {
         _writer.Write("RowKey");
         _writer.Write("rk");

         SetReaderFromWriter();

         string[] header = _reader.ReadNextRow();
         string[] values = _reader.ReadNextRow();

         Assert.NotNull(header);
         Assert.NotNull(values);

         Assert.Single(header);
         Assert.Equal("RowKey", header[0]);

         Assert.Single(values);
         Assert.Equal("rk", values[0]);
      }

      [Fact]
      public void WriteRead_EmptyUnquotedValue_Included()
      {
         _writer.Write("one", "", "three");
         SetReaderFromWriter();

         string[] row = _reader.ReadNextRow();
         Assert.Equal(3, row.Length);
         Assert.Equal("one", row[0]);
         Assert.Equal("", row[1]);
         Assert.Equal("three", row[2]);
      }

      [Fact]
      public void WriteRead_Case001_Fixed()
      {
         _writer.Write("RowKey", "col1", "col2", "col3");
         _writer.Write("rk1", "val11", "val12", "");
         _writer.Write("rk2", "", "val22", "val23");

         _ms.Flush();
         _ms.Position = 0;

         _reader = new CsvReader(_ms, Encoding.UTF8);

         string[] h = _reader.ReadNextRow();
         string[] r1 = _reader.ReadNextRow();
         string[] r2 = _reader.ReadNextRow();
         string[] nl = _reader.ReadNextRow();

         Assert.NotNull(h);
         Assert.NotNull(r1);
         Assert.NotNull(r2);
         Assert.Null(nl);
      }

      [Fact]
      public void Read_all_content_as_dictionary_with_column_names()
      {
         const string csv = @"col1,col2
1,11
2,22
";

         Dictionary<string, System.Collections.Generic.List<string>> f = CsvReader.ReadAllFromContent(csv);

         Assert.Equal(2, f.Count);
         Assert.Equal("col1", f.Keys.First());
         Assert.Equal("col2", f.Keys.Skip(1).First());
      }

      [Fact]
      public void Performance_Escaping_Stands()
      {
         const string ValueEscapeFind = "\"";
         const string ValueEscapeValue = "\"\"";

         const int loops = 10000;
         const string s = "kjkj\"jfjflj\"\"\"";
         long time1, time2;

         //experiment 1
         using (var m = new TimeMeasure())
         {
            for (int i = 0; i < loops; i++)
            {
               string s1 = s.Replace(ValueEscapeFind, ValueEscapeValue);
            }

            time1 = m.ElapsedTicks;
         }

         //experiment 2
         var rgx = new Regex("\"", RegexOptions.Compiled);
         using (var m = new TimeMeasure())
         {
            for (int i = 0; i < loops; i++)
            {
               string s1 = rgx.Replace(s, ValueEscapeValue);
            }

            time2 = m.ElapsedTicks;
         }

         //regex.replace is MUCH slower than string.replace

         Assert.NotEqual(time1, time2);
      }
   }

   public class RandomGeneratorTest
   {
      [Fact]
      public void RandomString_TwoStrings_NotEqual()
      {
         string s1 = RandomGenerator.RandomString;
         string s2 = RandomGenerator.RandomString;

         Assert.NotEqual(s1, s2);
      }

      [Theory]
      [InlineData(10)]
      [InlineData(100)]
      public void RandomString_SpecificLengthNoNulls_Matches(int length)
      {
         Assert.Equal(length, RandomGenerator.GetRandomString(length, false).Length);
      }

      [Fact]
      public void RandomBool_Anything_DoesntCrash()
      {
         bool b = RandomGenerator.RandomBool;
      }

      [Fact]
      public void RandomEnum_Random_Random()
      {
         EnumExample random = RandomGenerator.GetRandomEnum<EnumExample>();

         //not sure how to validate
      }

      [Fact]
      public void RandomEnumNonGeneric_Random_Random()
      {
         EnumExample random = (EnumExample)RandomGenerator.RandomEnum(typeof(EnumExample));

         //not sure how to validate
      }

      [Fact]
      public void RandomInt_Random_Random()
      {
         int i = RandomGenerator.RandomInt;
      }

      [Fact]
      public void RandomDate_Interval_Matches()
      {
         DateTime randomDate = RandomGenerator.GetRandomDate(DateTime.UtcNow, DateTime.UtcNow.AddDays(10));
      }

      [Fact]
      public void RandomDate_Random_Random()
      {
         DateTime randomDate = RandomGenerator.RandomDate;
      }


      [Theory]
      [InlineData(-10L, 100L)]
      [InlineData(5L, 10L)]
      [InlineData(-100L, -1L)]
      [InlineData(0, 67)]
      public void RandomLong_VaryingRange_InRange(long min, long max)
      {
         long random = RandomGenerator.GetRandomLong(min, max);

         Assert.True(random >= min);
         Assert.True(random <= max);
      }

      [Fact]
      public void RandomLong_TwoGenerations_NotEqual()
      {
         long l1 = RandomGenerator.RandomLong;
         long l2 = RandomGenerator.RandomLong;

         Assert.NotEqual(l1, l2);
      }

      [Fact]
      public void RandomUri_TwoGenerations_NotEqual()
      {
         Uri u1 = RandomGenerator.GetRandomUri(false);
         Uri u2 = RandomGenerator.RandomUri;

         Assert.NotEqual(u1, u2);
      }

      private enum EnumExample
      {
         One,
         Two,
         Three
      }
   }
}

public class StringTokenizerTest
{
   private readonly StringTokenizer _tokenizer = new StringTokenizer();

   [Fact]
   public void Smoke()
   {
      List<Token> tokens = _tokenizer.Tokenise("{this} is a {funny} number {0:D2}");
   }
}