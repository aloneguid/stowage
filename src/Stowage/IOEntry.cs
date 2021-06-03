using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Stowage
{
   /// <summary>
   /// IOEntry item description
   /// </summary>
   public sealed class IOEntry : IEquatable<IOEntry>, ICloneable
   {
      /// <summary>
      /// Entry Path
      /// </summary>
      public IOPath Path { get; private set; }

      /// <summary>
      /// Gets the name of this blob, unique within the folder. In most providers this is the same as file name.
      /// </summary>
      public string Name => Path.Name;

      /// <summary>
      /// IOEntry size
      /// </summary>
      public long? Size { get; set; }

      /// <summary>
      /// MD5 content hash of the blob. Note that this property can be null if underlying storage has
      /// no information about the hash, or it's very expensive to calculate it, for instance it would require
      /// getting a whole content of the blob to hash it.
      /// </summary>
      public string MD5 { get; set; }

      /// <summary>
      /// Creation time when known
      /// </summary>
      public DateTimeOffset? CreatedTime { get; set; }

      /// <summary>
      /// Last modification time when known
      /// </summary>
      public DateTimeOffset? LastModificationTime { get; set; }

      /// <summary>
      /// Custom provider-specific properties. Key names are case-insensitive.
      /// </summary>
      public Dictionary<string, object> Properties { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

      /// <summary>
      /// Try to get property and cast it to a specified type
      /// </summary>
      public bool TryGetProperty<TValue>(string name, out TValue value, TValue defaultValue = default)
      {
         if(name == null || !Properties.TryGetValue(name, out object objValue))
         {
            value = defaultValue;
            return false;
         }

         if(objValue is TValue)
         {
            value = (TValue)objValue;
            return true;
         }

         value = defaultValue;
         return false;
      }

      /// <summary>
      /// User defined metadata. Key names are case-insensitive.
      /// </summary>
      public Dictionary<string, string> Metadata { get; private set; } = new(StringComparer.OrdinalIgnoreCase);

      /// <summary>
      /// Optional tag for you to use. This is never used by the library itself.
      /// </summary>
      public object Tag { get; set; }

      /// <summary>
      /// Tries to add properties in pairs when value is not null
      /// </summary>
      /// <param name="keyValues"></param>
      public void TryAddProperties(params object[] keyValues)
      {
         for(int i = 0; i < keyValues.Length; i += 2)
         {
            string key = (string)keyValues[i];
            object value = keyValues[i + 1];

            if(key != null && value != null)
            {
               if(value is string s && string.IsNullOrEmpty(s))
                  continue;

               Properties[key] = value;
            }
         }
      }

      /// <summary>
      /// Works just like <see cref="TryAddProperties(object[])"/> but prefixes all the keys
      /// </summary>
      /// <param name="prefix"></param>
      /// <param name="keyValues"></param>
      public void TryAddPropertiesWithPrefix(string prefix, params object[] keyValues)
      {
         if(string.IsNullOrEmpty(prefix))
            TryAddProperties(keyValues);

         object[] keyValuesWithPrefix = keyValues.Select((e, i) => i % 2 == 0 ? (prefix + (string)e) : e).ToArray();

         TryAddProperties(keyValuesWithPrefix);
      }

      /// <summary>
      /// Tries to add properties from dictionary by key names
      /// </summary>
      /// <param name="source"></param>
      /// <param name="keyNames"></param>
      public void TryAddPropertiesFromDictionary(IDictionary<string, string> source, params string[] keyNames)
      {
         if(source == null || keyNames == null)
            return;

         foreach(string key in keyNames)
         {
            if(source.TryGetValue(key, out string value))
            {
               Properties[key] = value;
            }
         }
      }

      /// <summary>
      /// Create a new instance
      /// </summary>
      /// <param name="path"></param>
      /// <param name="kind"></param>
      public IOEntry(string path)
      {
         SetFullPath(path);
      }

      /// <summary>
      /// Returns true if this item is a folder and it's a root folder
      /// </summary>
      public bool IsRootFolder => Path.IsRootPath;

      /// <summary>
      /// Full blob info, i.e type, id and path
      /// </summary>
      public override string ToString() => Path.Full;

      /// <summary>
      /// Equality check
      /// </summary>
      /// <param name="other"></param>
      public bool Equals(IOEntry other)
      {
         if(ReferenceEquals(other, null))
            return false;

         return
            other.Path.Equals(Path);
      }

      /// <summary>
      /// Equality check
      /// </summary>
      /// <param name="other"></param>
      public override bool Equals(object other)
      {
         if(ReferenceEquals(other, null))
            return false;
         if(ReferenceEquals(other, this))
            return true;
         if(other.GetType() != typeof(IOEntry))
            return false;

         return ((IOEntry)other).Path.Equals(Path);
      }

      /// <summary>
      /// Hash code calculation
      /// </summary>
      public override int GetHashCode()
      {
         return Path.GetHashCode();
      }

      /// <summary>
      /// Constructs a file blob by full ID
      /// </summary>
      public static implicit operator IOEntry(string fullPath)
      {
         return new IOEntry(fullPath);
      }

      /// <summary>
      /// Converts blob to string by using full path
      /// </summary>
      /// <param name="blob"></param>
      public static implicit operator string(IOEntry blob)
      {
         return blob.Path;
      }

      /// <summary>
      /// Converts blob attributes (user metadata to byte array)
      /// </summary>
      /// <returns></returns>
      public byte[] AttributesToByteArray()
      {
         using(var ms = new MemoryStream())
         {
            using(var b = new BinaryWriter(ms, Encoding.UTF8, true))
            {
               b.Write((byte)1); //version marker

               b.Write((int)Metadata?.Count);   //number of metadata items

               foreach(KeyValuePair<string, string> pair in Metadata)
               {
                  b.Write(pair.Key);
                  b.Write(pair.Value);
               }
            }

            return ms.ToArray();
         }
      }

      /// <summary>
      /// Appends attributes from byte array representation
      /// </summary>
      /// <param name="data"></param>
      public void AppendAttributesFromByteArray(byte[] data)
      {
         if(data == null)
            return;

         using(var ms = new MemoryStream(data))
         {
            using(var b = new BinaryReader(ms, Encoding.UTF8, true))
            {
               byte version = b.ReadByte();  //to be used with versioning
               if(version != 1)
               {
                  throw new ArgumentException($"version {version} is not supported", nameof(data));
               }

               int count = b.ReadInt32();
               if(count > 0)
               {
                  for(int i = 0; i < count; i++)
                  {
                     string key = b.ReadString();
                     string value = b.ReadString();

                     Metadata[key] = value;
                  }
               }
            }
         }
      }

      /// <summary>
      /// Changes full path of this blob without modifying any other property
      /// </summary>
      public void SetFullPath(string fullPath)
      {
         Path = fullPath;
      }

      /// <summary>
      /// Clones blob to best efforts
      /// </summary>
      /// <returns></returns>
      public object Clone()
      {
         var clone = (IOEntry)MemberwiseClone();
         clone.Metadata = new Dictionary<string, string>(Metadata, StringComparer.OrdinalIgnoreCase);
         clone.Properties = new Dictionary<string, object>(Properties, StringComparer.OrdinalIgnoreCase);
         return clone;
      }
   }
}