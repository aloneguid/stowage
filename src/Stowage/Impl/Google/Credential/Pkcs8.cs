using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Stowage.Impl.Google.Credential
{
   internal class Pkcs8
   {
      public static RSAParameters DecodeRsaParameters(string pkcs8PrivateKey)
      {
         pkcs8PrivateKey = pkcs8PrivateKey.Trim();
         object[] objArray = pkcs8PrivateKey.StartsWith("-----BEGIN PRIVATE KEY-----") && pkcs8PrivateKey.EndsWith("-----END PRIVATE KEY-----") ? (object[])((object[])Pkcs8.Asn1.Decode(Convert.FromBase64String(pkcs8PrivateKey.Substring("-----BEGIN PRIVATE KEY-----".Length, pkcs8PrivateKey.Length - "-----BEGIN PRIVATE KEY-----".Length - "-----END PRIVATE KEY-----".Length))))[2] : throw new ArgumentException("PKCS8 data must be contained within '-----BEGIN PRIVATE KEY-----' and '-----END PRIVATE KEY-----'.", nameof(pkcs8PrivateKey));
         return new RSAParameters()
         {
            Modulus = Pkcs8.TrimLeadingZeroes((byte[])objArray[1]),
            Exponent = Pkcs8.TrimLeadingZeroes((byte[])objArray[2], false),
            D = Pkcs8.TrimLeadingZeroes((byte[])objArray[3]),
            P = Pkcs8.TrimLeadingZeroes((byte[])objArray[4]),
            Q = Pkcs8.TrimLeadingZeroes((byte[])objArray[5]),
            DP = Pkcs8.TrimLeadingZeroes((byte[])objArray[6]),
            DQ = Pkcs8.TrimLeadingZeroes((byte[])objArray[7]),
            InverseQ = Pkcs8.TrimLeadingZeroes((byte[])objArray[8])
         };
      }

      internal static byte[] TrimLeadingZeroes(byte[] bs, bool alignTo8Bytes = true)
      {
         int index = 0;
         while(index < bs.Length && bs[index] == (byte)0)
            ++index;
         int count = bs.Length - index;
         if(alignTo8Bytes)
         {
            int num = count & 7;
            if(num != 0)
               count += 8 - num;
         }
         if(count == bs.Length)
            return bs;
         byte[] numArray = new byte[count];
         if(count < bs.Length)
            Buffer.BlockCopy((Array)bs, bs.Length - count, (Array)numArray, 0, count);
         else
            Buffer.BlockCopy((Array)bs, 0, (Array)numArray, count - bs.Length, bs.Length);
         return numArray;
      }

      /// <summary>
      /// An incomplete ASN.1 decoder, only implements what's required
      /// to decode a Service Credential.
      /// </summary>
      internal class Asn1
      {
         public static object Decode(byte[] bs) => new Pkcs8.Asn1.Decoder(bs).Decode();

         internal enum Tag
         {
            Integer = 2,
            OctetString = 4,
            Null = 5,
            ObjectIdentifier = 6,
            Sequence = 16, // 0x00000010
         }

         internal class Decoder
         {
            private byte[] _bytes;
            private int _index;

            public Decoder(byte[] bytes)
            {
               this._bytes = bytes;
               this._index = 0;
            }

            public object Decode()
            {
               Pkcs8.Asn1.Tag tag = this.ReadTag();
               switch(tag)
               {
                  case Pkcs8.Asn1.Tag.Integer:
                     return (object)this.ReadInteger();
                  case Pkcs8.Asn1.Tag.OctetString:
                     return this.ReadOctetString();
                  case Pkcs8.Asn1.Tag.Null:
                     return this.ReadNull();
                  case Pkcs8.Asn1.Tag.ObjectIdentifier:
                     return (object)this.ReadOid();
                  case Pkcs8.Asn1.Tag.Sequence:
                     return (object)this.ReadSequence();
                  default:
                     throw new NotSupportedException(string.Format("Tag '{0}' not supported.", (object)tag));
               }
            }

            private byte NextByte() => this._bytes[this._index++];

            private byte[] ReadLengthPrefixedBytes() => this.ReadBytes(this.ReadLength());

            private byte[] ReadInteger() => this.ReadLengthPrefixedBytes();

            private object ReadOctetString() => new Pkcs8.Asn1.Decoder(this.ReadLengthPrefixedBytes()).Decode();

            private object ReadNull()
            {
               if(this.ReadLength() != 0)
                  throw new InvalidDataException("Invalid data, Null length must be 0.");
               return (object)null;
            }

            private int[] ReadOid()
            {
               byte[] numArray = this.ReadLengthPrefixedBytes();
               List<int> intList = new List<int>();
               bool flag = true;
               int num1 = 0;
               while(num1 < numArray.Length)
               {
                  int num2 = 0;
                  byte num3;
                  do
                  {
                     num3 = numArray[num1++];
                     if(((long)num2 & 4278190080L) != 0L)
                        throw new NotSupportedException("Oid subId > 2^31 not supported.");
                     num2 = num2 << 7 | (int)num3 & (int)sbyte.MaxValue;
                  }
                  while(((int)num3 & 128) != 0);
                  if(flag)
                  {
                     flag = false;
                     intList.Add(num2 / 40);
                     intList.Add(num2 % 40);
                  }
                  else
                     intList.Add(num2);
               }
               return intList.ToArray();
            }

            private object[] ReadSequence()
            {
               int num = this._index + this.ReadLength();
               if(num < 0 || num > this._bytes.Length)
                  throw new InvalidDataException("Invalid sequence, too long.");
               List<object> objectList = new List<object>();
               while(this._index < num)
                  objectList.Add(this.Decode());
               return objectList.ToArray();
            }

            private byte[] ReadBytes(int length)
            {
               if(length <= 0)
                  throw new ArgumentOutOfRangeException(nameof(length), "length must be positive.");
               if(this._bytes.Length - length < 0)
                  throw new ArgumentException("Cannot read past end of buffer.");
               byte[] numArray = new byte[length];
               Array.Copy((Array)this._bytes, this._index, (Array)numArray, 0, length);
               this._index += length;
               return numArray;
            }

            private Pkcs8.Asn1.Tag ReadTag()
            {
               int num = (int)this.NextByte() & 31;
               return num != 31 ? (Pkcs8.Asn1.Tag)num : throw new NotSupportedException("Tags of value > 30 not supported.");
            }

            private int ReadLength()
            {
               byte num1 = this.NextByte();
               if(((int)num1 & 128) == 0)
                  return (int)num1;
               if(num1 == byte.MaxValue)
                  throw new InvalidDataException("Invalid length byte: 0xff");
               int num2 = (int)num1 & (int)sbyte.MaxValue;
               if(num2 == 0)
                  throw new NotSupportedException("Lengths in Indefinite Form not supported.");
               int num3 = 0;
               for(int index = 0; index < num2; ++index)
               {
                  if(((long)num3 & 4286578688L) != 0L)
                     throw new NotSupportedException("Lengths > 2^31 not supported.");
                  num3 = num3 << 8 | (int)this.NextByte();
               }
               return num3;
            }
         }
      }
   }
}
