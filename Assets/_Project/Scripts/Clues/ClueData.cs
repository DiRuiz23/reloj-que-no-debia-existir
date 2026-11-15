using UnityEngine;

namespace Reloj.Clues
{
    /// <summary>
    /// Definicion de una pista (I-27). Un asset por pista en Assets/_Project/Data/Clues.
    /// La pista se obtiene al activarse su flag de narrativa (ver <see cref="Flag"/>): asi funciona igual
    /// si la da un dialogo (setFlag en el .json) o un objeto examinable (<see cref="ClueExaminable"/>).
    /// </summary>
    [CreateAssetMenu(fileName = "Clue_", menuName = "Reloj/Clues/Clue Data")]
    public class ClueData : ScriptableObject
    {
        [Tooltip("Id unico y estable (se guarda en la partida, I-34). No cambiarlo despues.")]
        [SerializeField] string id;
        [SerializeField] string title;
        [TextArea(2, 6), SerializeField] string summary;
        [Tooltip("Nivel donde se consigue (solo informativo / para ordenar).")]
        [SerializeField, Min(0)] int level = 1;
        [SerializeField] Sprite icon;
        [Tooltip("Flag de narrativa que concede la pista. Vacio = 'clue_' + id.")]
        [SerializeField] string flag;

        public string Id => id;
        public string Title => title;
        public string Summary => summary;
        public int Level => level;
        public Sprite Icon => icon;
        public string Flag => string.IsNullOrEmpty(flag) ? "clue_" + id : flag;

        void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name.StartsWith("Clue_") ? name.Substring(5) : name;
        }
    }
}
