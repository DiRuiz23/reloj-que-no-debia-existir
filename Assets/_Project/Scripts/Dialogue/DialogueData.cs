using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Dialogue
{
    [Serializable]
    public class DialogueChoice
    {
        public string text;
        /// <summary>Id del nodo al que lleva esta opcion.</summary>
        public string next;
        /// <summary>Flag que se activa al elegir esta opcion (opcional).</summary>
        public string setFlag;
    }

    [Serializable]
    public class DialogueAltIf
    {
        /// <summary>Si este flag esta activo al entrar al nodo...</summary>
        public string flag;
        /// <summary>...se muestra este otro nodo en su lugar.</summary>
        public string next;

        public bool IsSet => !string.IsNullOrEmpty(flag);
    }

    [Serializable]
    public class DialogueNode
    {
        public string id;
        public string speaker;
        public string text;
        /// <summary>true = nodo final: al terminar de leerlo se cierra la ventana.</summary>
        public bool end;
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        public DialogueAltIf altIf = new DialogueAltIf();
    }

    [Serializable]
    public class DialogueFile
    {
        public string id;
        /// <summary>Id del nodo inicial. Si se omite, es el primero de la lista.</summary>
        public string start;
        public List<DialogueNode> nodes = new List<DialogueNode>();

        [NonSerialized] Dictionary<string, DialogueNode> index;

        public DialogueNode GetNode(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) return null;
            if (index == null)
            {
                index = new Dictionary<string, DialogueNode>();
                foreach (var n in nodes)
                    if (n != null && !string.IsNullOrEmpty(n.id)) index[n.id] = n;
            }
            return index.TryGetValue(nodeId, out var node) ? node : null;
        }

        public DialogueNode StartNode =>
            !string.IsNullOrEmpty(start) ? GetNode(start) : (nodes.Count > 0 ? nodes[0] : null);
    }

    /// <summary>Lista de flags validos. La mantiene quien escribe los dialogos (Resources/Dialogues/_flags.json).</summary>
    [Serializable]
    public class DialogueFlagCatalog
    {
        public List<string> flags = new List<string>();
    }
}
