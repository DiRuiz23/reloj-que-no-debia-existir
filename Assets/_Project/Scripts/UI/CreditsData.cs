using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloj.UI
{
    [Serializable]
    public class CreditPerson
    {
        public string name;
        public string role;
    }

    [Serializable]
    public class CreditTool
    {
        public string name;
        public string usage;
        public string url;
    }

    [Serializable]
    public class CreditAsset
    {
        public string name;
        public string author;
        public string source;      // URL (o "N/A (obra original)")
        public string license;
        public string tool;
        public string usage;
        public string sourcePath;  // carpeta donde vive el SOURCE.txt (ruta relativa a la raíz del proyecto)
    }

    /// <summary>
    /// Datos de la pantalla de créditos (I-xx).
    /// Autores y herramientas se editan a mano. La lista de recursos externos NO se edita a mano:
    /// la regenera Tools > Verificar licencias a partir de los SOURCE.txt del proyecto.
    /// </summary>
    [CreateAssetMenu(menuName = "Reloj/UI/CreditsData", fileName = "CreditsData")]
    public class CreditsData : ScriptableObject
    {
        public string gameTitle = "1910";
        public string studio = "PIXELNOVA GAME STUDIO";

        public List<CreditPerson> authors = new List<CreditPerson>();
        public List<CreditTool> tools = new List<CreditTool>();

        [Tooltip("Generada automáticamente por Tools > Verificar licencias. No editar a mano.")]
        public List<CreditAsset> assets = new List<CreditAsset>();
    }
}
