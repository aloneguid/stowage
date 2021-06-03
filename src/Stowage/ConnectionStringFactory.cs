using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stowage.Factories;

namespace Stowage
{
   static class ConnectionStringFactory
   {
      private const string TypeSeparator = "://";
      private static readonly List<IConnectionFactory> Factories = new List<IConnectionFactory>();

      static ConnectionStringFactory()
      {
         Register(new BuiltInConnectionFactory());
      }

      public static void Register(IConnectionFactory factory)
      {
         if(factory == null)
            throw new ArgumentNullException(nameof(factory));

         Factories.Add(factory);
      }

      public static IFileStorage Create(string connectionString)
      {
         if(connectionString is null)
            throw new ArgumentNullException(nameof(connectionString));

         var pcs = new ConnectionString(connectionString);

         IFileStorage instance = Factories
            .Select(f => f.Create(pcs))
            .FirstOrDefault(b => b != null);

         if(instance == null)
         {
            throw new ArgumentException(
               $"could not create any implementation based on the passed connection string (prefix: {pcs.Prefix}), did you register required external module?",
               nameof(connectionString));
         }

         return instance;
      }
   }
}
