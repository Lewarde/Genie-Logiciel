// Client/JobProgressInfo.cs (Nouveau fichier)
using System.Text.Json.Serialization; // Pour JsonPropertyName si nécessaire

namespace Client
{
    public class JobProgressInfo
    {
        // Les noms des propriétés doivent correspondre EXACTEMENT aux clés JSON envoyées par le serveur
        // Ou utilisez [JsonPropertyName("NomDansLeJson")] si les noms diffèrent.
        public string Name { get; set; }
        public string State { get; set; }
        public int TotalFilesToCopy { get; set; } // Assurez-vous que le type correspond
        public long TotalFilesSize { get; set; }  // Assurez-vous que le type correspond
        public int NbFilesLeftToDo { get; set; } // Assurez-vous que le type correspond
        public int Progression { get; set; }
        public string CurrentSourceFile { get; set; }
        public string CurrentTargetFile { get; set; } // Si vous l'envoyez
    }
}