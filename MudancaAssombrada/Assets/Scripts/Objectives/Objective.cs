using System.Collections.Generic;
using UnityEngine;

namespace MudancaAssombrada
{
    // Um objetivo da fase. Cada fase monta os SEUS objetivos colocando componentes na cena:
    //   - DeliverFurnitureObjective: levar móveis (todos ou alguns) para o caminhão
    //   - ReachAreaObjective: chegar a um lugar (ex.: o sótão)
    //   - InteractObjective: usar coisas (ex.: desligar os instrumentos da Maestrina)
    //
    // ETAPAS: objetivos com o mesmo número de etapa acontecem juntos; a próxima etapa só
    // começa quando TODOS os da etapa atual terminam. Fase simples = tudo na etapa 0.
    // A fase final do roteiro, por exemplo, teria as etapas 0, 1 e 2.
    //
    // Quem avalia é o GameManager, só no host. Objetivo novo = uma classe nova que herda desta.
    public abstract class Objective : MonoBehaviour
    {
        static readonly List<Objective> all = new();
        public static IReadOnlyList<Objective> All => all;

        [Tooltip("Etapa da fase (0, 1, 2...). A etapa seguinte começa quando todos desta terminam.")]
        [SerializeField, Min(0)] int stage;

        public int Stage => stage;

        // Já foi cumprido? (Lido pelo GameManager no host.)
        public abstract bool IsComplete { get; }

        // O texto que aparece na HUD, com o progresso. Ex.: "Móveis no caminhão: 2/3".
        public abstract string Progress { get; }

        // Volta ao estado inicial quando a fase reinicia (só no host).
        public virtual void ServerReset() { }

        protected virtual void OnEnable() => all.Add(this);
        protected virtual void OnDisable() => all.Remove(this);
    }
}
