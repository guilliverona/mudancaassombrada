using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Controla o estado geral da partida: cronômetro até o amanhecer, vitória, derrota e reinício.
//
// EM REDE: só o host conta o tempo e decide quando a partida acaba. Os clientes recebem
// os valores por NetworkVariables e só desenham na tela. Assim todo mundo vê o MESMO
// cronômetro e o MESMO final.
public class GameManager : NetworkBehaviour
{
    // ": byte" = guarda o enum em 1 byte só (menos dados pela rede do que um int).
    enum Outcome : byte { Playing, Won, Dawn, Caught }

    [SerializeField] TruckZone truck;
    [SerializeField] float nightDuration = 180f; // segundos até amanhecer

    static GameManager instance;

    readonly NetworkVariable<float> timeLeft = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<int> delivered = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<Outcome> outcome = new(Outcome.Playing, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<int> stars = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    int totalFurniture;

    void Awake() => instance = this;

    void Start() => totalFurniture = FindObjectsByType<Furniture>().Length;

    public override void OnNetworkSpawn()
    {
        outcome.OnValueChanged += OnOutcomeChanged;
        if (IsServer) timeLeft.Value = nightDuration;
        ApplyOutcome(outcome.Value); // quem entra no meio de um game over também congela
    }

    public override void OnNetworkDespawn() => outcome.OnValueChanged -= OnOutcomeChanged;

    void OnOutcomeChanged(Outcome previous, Outcome current) => ApplyOutcome(current);

    // Roda em TODOS os computadores quando o resultado muda.
    // Congelar o tempo não trava a rede: o Netcode usa o tempo "real", sem escala.
    static void ApplyOutcome(Outcome o) => Time.timeScale = o == Outcome.Playing ? 1f : 0f;

    // Chamado pelo fantasma, que só pensa no host.
    public static void ServerPlayerCaught()
    {
        if (instance != null && instance.IsServer) instance.End(Outcome.Caught);
    }

    void Update()
    {
        if (!IsSpawned) return; // antes de conectar não existe partida

        if (outcome.Value != Outcome.Playing)
        {
            // Qualquer jogador pode pedir para jogar de novo; quem executa é o host.
            if (Keyboard.current.rKey.wasPressedThisFrame) RestartRpc();
            return;
        }

        if (!IsServer) return; // daqui pra baixo, só o host decide

        timeLeft.Value = Mathf.Max(0f, timeLeft.Value - Time.deltaTime);
        delivered.Value = truck.DeliveredCount;

        if (timeLeft.Value <= 0) End(Outcome.Dawn);
        else if (delivered.Value >= totalFurniture) End(Outcome.Won);
    }

    void End(Outcome o)
    {
        if (outcome.Value != Outcome.Playing) return; // já acabou, ignora
        if (o == Outcome.Won)
            stars.Value = Mathf.Clamp(Mathf.CeilToInt(timeLeft.Value / nightDuration * 5f), 1, 5);
        outcome.Value = o;
    }

    // Reiniciar SEM recarregar a cena: recarregar no meio de uma partida em rede
    // é complicado (todos precisam recarregar juntos e os objetos de rede renascer).
    // É mais simples colocar cada coisa de volta no lugar.
    [Rpc(SendTo.Server)]
    void RestartRpc()
    {
        if (outcome.Value == Outcome.Playing) return; // dois jogadores apertaram R: ignora o segundo

        foreach (var f in FindObjectsByType<Furniture>()) f.ServerReset();
        foreach (var g in FindObjectsByType<GhostHunter>()) g.ServerReset();
        foreach (var p in FindObjectsByType<NetworkPlayer>()) p.ResetPositionRpc();

        timeLeft.Value = nightDuration;
        delivered.Value = 0;
        outcome.Value = Outcome.Playing; // descongela todo mundo
    }

    // A "ficha de avaliação" do Seu Valdemar, como no roteiro. Mais tempo sobrando = mais estrelas.
    string Rating()
    {
        string comment = stars.Value switch
        {
            5 => "\"Nem parecia uma casa assombrada. Contrato renovado!\"",
            4 => "\"Ótimo serviço. Só não me arranhem o armário de novo.\"",
            3 => "\"Deu certo. Mas eu ouvi a gritaria daqui do escritório.\"",
            2 => "\"Quase amanheceu. O cliente reclamou do barulho.\"",
            _ => "\"No limite! Vou descontar a hora extra do salário.\"",
        };
        // new string('★', 3) = "★★★"
        return new string('★', stars.Value) + new string('☆', 5 - stars.Value) + "\n" + comment + "\n— Seu Valdemar";
    }

    // OnGUI é o sistema de interface mais antigo da Unity. Feio, mas ótimo para protótipos:
    // zero configuração. Depois trocamos por uma UI de verdade.
    void OnGUI()
    {
        GameFonts.ApplyToGUI();
        if (!IsSpawned) return;

        // HUD: cronômetro e móveis entregues, no canto superior esquerdo.
        var hud = new GUIStyle(GUI.skin.label) { fontSize = 24 };
        hud.normal.textColor = Color.white;
        int minutes = (int)(timeLeft.Value / 60);
        int seconds = (int)(timeLeft.Value % 60);
        // {1:00} = sempre 2 dígitos (5 vira 05)
        GUI.Label(new Rect(20, 20, 500, 40), string.Format("Amanhecer em {0}:{1:00}", minutes, seconds), hud);
        GUI.Label(new Rect(20, 55, 500, 40), $"Móveis no caminhão: {delivered.Value}/{totalFurniture}", hud);

        if (outcome.Value == Outcome.Playing) return;

        (string message, Color color) = outcome.Value switch
        {
            Outcome.Won => ("Mudança concluída!\n\n" + Rating(), new Color(0.4f, 1f, 0.5f)),
            Outcome.Dawn => ("Amanheceu...\nO serviço ficou pela metade.", new Color(1f, 0.6f, 0.2f)),
            _ => ("O Bibliotecário pegou alguém da equipe!", Color.red),
        };
        var style = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = color;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), message + "\n\nAperte R para jogar de novo", style);
    }
}
