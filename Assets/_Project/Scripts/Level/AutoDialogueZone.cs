using UnityEngine;
using Reloj.Dialogue;
using Reloj.Player;

namespace Reloj.Level
{
    [RequireComponent(typeof(Collider))]
    public class AutoDialogueZone : MonoBehaviour
    {
        [SerializeField] string resourcesId = "nivel1_introspeccion";
        bool played;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (played || other.GetComponentInParent<PlayerMotor>() == null) return;
            var runner = DialogueRunner.Instance;
            if (runner == null || !runner.CanStart) return;
            var file = DialogueLoader.LoadFromResources(resourcesId);
            if (file != null && runner.StartDialogue(file)) played = true;
        }
    }
}