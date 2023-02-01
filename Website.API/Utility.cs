using System.IO;

namespace Website.API
{
    /// <summary>
    /// Utility class to fetch schema.
    /// </summary>
    public class Utility
    {
        /// <summary>
        /// Gets schema as string from schema file.
        /// </summary>
        /// <value>
        /// Schema as string from schema file.
        /// </value>
        public static string GetSchema
        {
            get
            {
                var dirCurrAssemby = Path.GetDirectoryName(typeof(Utility).Assembly.Location);
                var file = Path.Combine(dirCurrAssemby!, "Website.graphql");
                return File.ReadAllText(file);
            }
        }
    }
}
