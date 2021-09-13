using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stowage.Impl.Databricks
{
   public class NewCluster
   {

   }

   public class NotebookTask
   {
      [JsonPropertyName("notebook_path")]
      public string Path { get; set; }

      [JsonPropertyName("revision_timestamp")]
      public long RevisionTimestamp { get; set; }

      [JsonPropertyName("base_parameters")]
      public Dictionary<string, string> BaseParameters { get; set; }
   }

   public class SparkJarTask
   {
      /// <summary>
      /// The full name of the class containing the main method to be executed. This class must be contained in a JAR provided as a library.
      /// </summary>
      [JsonPropertyName("main_class_name")]
      public string MainClassName { get; set; }

      [JsonPropertyName("parameters")]
      public string[] Parameters { get; set; }
   }

   /// <summary>
   /// https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/jobs#--jobsettings
   /// </summary>
   public class JobSettings
   {
      [JsonPropertyName("name")]
      public string Name { get; set; }

      [JsonPropertyName("schedule")]
      public CronSchedule Schedule { get; set; }

      /// <summary>
      /// If existing_cluster_id, the ID of an existing cluster that will be used for all runs of this job. When running jobs on an existing cluster, you may need to manually restart the cluster if it stops responding. We suggest running jobs on new clusters for greater reliability.
      /// </summary>
      [JsonPropertyName("existing_cluster_id")]
      public string ExistingClusterId { get; set; }

      /// <summary>
      /// If new_cluster, a description of a cluster that will be created for each run.
      /// </summary>
      [JsonPropertyName("new_cluster")]
      public NewCluster NewCluster { get; set; }

      [JsonPropertyName("notebook_task")]
      public NotebookTask NotebookTask { get; set; }

      public bool IsNotebookTask => NotebookTask != null;

      public string NotebookTaskDisplay => NotebookTask?.Path;

      [JsonPropertyName("spark_jar_task")]
      public SparkJarTask JarTask { get; set; }

      public bool IsSparkJarTask => JarTask != null;
   }
}
