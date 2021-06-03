using System;

namespace Stowage.SelfTest
{
   class TestOutcome
   {
      public string TestName { get; set; }

      public bool AssertionFailure { get; set; }

      public string Expected { get; set; }

      public string Actual { get; set; }

      public Exception RuntimeException { get; set; }

      public bool Success => !AssertionFailure && RuntimeException == null;

      public override string ToString()
      {
         if(Success)
            return $"{TestName}: success";

         if(AssertionFailure)
            return $"{TestName}: assertion failure -  expected '{Expected}' but found '{Actual}'";

         if(RuntimeException != null)
            return $"{TestName}: runtime exception - {RuntimeException}";

         return null;
      }
   }
}
