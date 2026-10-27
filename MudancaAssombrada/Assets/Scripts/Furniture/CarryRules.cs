using UnityEngine;

namespace MudancaAssombrada
{
    // As REGRAS de peso e dupla, sem cena nem rede: quem levanta, quanto pesa, em que passo a dupla anda.
    // O Furniture e o FurnitureCarrier juntam os números (força, peso, velocidade, ids) e perguntam aqui;
    // os testes (Assets/Tests/EditMode/CarryRulesTests.cs) conferem cada regra sem abrir uma fase.
    //
    // Ponta vazia = 0: quem não está lá (ou a ponta FALSA do Imitador) entra com força, limite e
    // velocidade zero. Assim a mentira da isca não soma nada em conta nenhuma.
    public static class CarryRules
    {
        // Peso acima disto = móvel GRANDE (penteadeira, armário): na escada anda mais devagar e cada
        // degrau range. Só vale para a escada; quem levanta o quê sai da força (CanLift).
        public const float BulkyWeight = 2f;

        // Dá para levantar? A soma do que cada ponta levanta sozinha tem que vencer o peso (≥).
        // Senão, quem segura só arrasta.
        public static bool CanLift(float holderLimit, float helperLimit, float weight) =>
            holderLimit + helperLimit >= weight;

        // Quem segura sente o peso (é mais pesado que a força dele)? Então cabe alguém na outra ponta.
        public static bool FeelsWeight(float holderStrength, float weight) => weight > holderStrength;

        // A força somada das duas pontas (divide o peso). Nunca zero, para a conta não explodir.
        public static float CrewStrength(float holderStrength, float helperStrength) =>
            Mathf.Max(0.1f, holderStrength + helperStrength);

        // A dupla anda no passo do mais lento. Velocidade 0 = ninguém nessa ponta; ninguém = 1.
        public static float CrewSpeed(float holderSpeed, float helperSpeed)
        {
            if (holderSpeed <= 0f && helperSpeed <= 0f) return 1f;
            if (holderSpeed <= 0f) return helperSpeed;
            if (helperSpeed <= 0f) return holderSpeed;
            return Mathf.Min(holderSpeed, helperSpeed);
        }

        // Peso que o jogador SENTE: nunca menos que 1 (nada fica mais leve que "sem peso").
        public static float FeltWeight(float weight, float crewStrength) =>
            Mathf.Max(1f, weight / crewStrength);

        public static bool IsBulky(float weight) => weight > BulkyWeight;

        // Mostrar "Segurar a outra ponta"? Quem segura sente o peso, a ponta está livre e sem ponta falsa.
        public static bool HelpOffered(bool holderFeelsWeight, bool hasHelper, bool hasFakeHelper) =>
            holderFeelsWeight && !hasHelper && !hasFakeHelper;

        // No HOST: aceitar este colega na outra ponta? Além do que o aviso mostra, ele não pode ser o dono,
        // e não pode estar com as mãos ocupadas em OUTRO móvel (primeira ponta ou outra ponta).
        public static bool CanAcceptHelper(bool isHeld, bool holderFeelsWeight, bool hasHelper, bool hasFakeHelper,
                                           ulong sender, ulong owner, bool senderBusyElsewhere) =>
            isHeld && sender != owner && !senderBusyElsewhere && HelpOffered(holderFeelsWeight, hasHelper, hasFakeHelper);

        // No HOST: entregar este móvel a quem pediu? Só se estiver solto (dois pedidos: o primeiro leva)
        // e quem pediu não estiver com as mãos ocupadas em OUTRO móvel.
        public static bool CanAcceptPickUp(bool isHeld, bool senderBusyElsewhere) =>
            !isHeld && !senderBusyElsewhere;

        // Multiplicador de velocidade de quem carrega.
        //  - Arrastando (faltou gente): dragSpeed ÷ peso sentido.
        //  - Levantando: o passo da dupla (relativo à minha velocidade) ÷ peso sentido.
        //  - Móvel grande na escada: ÷ stairsSlowdown, por cima de tudo.
        public static float CarrySpeed(bool dragging, float dragSpeed, float crewSpeed, float mySpeed,
                                       float feltWeight, bool bulky, bool onStairs, float stairsSlowdown)
        {
            float speed = dragging ? dragSpeed / feltWeight : crewSpeed / Mathf.Max(0.1f, mySpeed) / feltWeight;
            if (bulky && onStairs) speed /= stairsSlowdown;
            return speed;
        }
    }
}
