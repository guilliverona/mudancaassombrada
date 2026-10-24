using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Controla o estado geral da partida: cronômetro até o amanhecer, vitória, derrota e reinício.
public class GameManager : MonoBehaviour
{
    [SerializeField] TruckZone truck;
    [SerializeField] float nightDuration = 180f; // segundos até amanhecer

    static GameManager instance;
    float timeLeft;
    int totalFurniture;
    string endMessage; // null = jogo rolando
    Color endColor;

    void Awake() => instance = this;

    void Start()
    {
        timeLeft = nightDuration;
        totalFurniture = FindObjectsByType<Furniture>().Length;
    }

    public static void PlayerCaught(string message)
    {
        if (instance != null) instance.EndGame(message, Color.red);
    }

    void Update()
    {
        if (endMessage != null)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                Time.timeScale = 1f; // IMPORTANTE: o timeScale continua 0 na próxima cena se não voltar
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0)
        {
            timeLeft = 0;
            EndGame("Amanheceu...\nO serviço ficou pela metade.", new Color(1f, 0.6f, 0.2f));
        }
        else if (truck.DeliveredCount >= totalFurniture)
        {
            EndGame("Mudança concluída!\n\n" + Rating(), new Color(0.4f, 1f, 0.5f));
        }
    }

    void EndGame(string message, Color color)
    {
        if (endMessage != null) return; // já acabou, ignora
        endMessage = message;
        endColor = color;
        Time.timeScale = 0f; // congela o tempo: física, Update com deltaTime, tudo para
    }

    // A "ficha de avaliação" do Seu Valdemar, como no roteiro. Mais tempo sobrando = mais estrelas.
    string Rating()
    {
        float ratio = timeLeft / nightDuration;
        int stars = Mathf.Clamp(Mathf.CeilToInt(ratio * 5f), 1, 5);
        string comment = stars switch
        {
            5 => "\"Nem parecia uma casa assombrada. Contrato renovado!\"",
            4 => "\"Ótimo serviço. Só não me arranhem o armário de novo.\"",
            3 => "\"Deu certo. Mas eu ouvi a gritaria daqui do escritório.\"",
            2 => "\"Quase amanheceu. O cliente reclamou do barulho.\"",
            _ => "\"No limite! Vou descontar a hora extra do salário.\"",
        };
        // new string('★', 3) = "★★★"
        return new string('★', stars) + new string('☆', 5 - stars) + "\n" + comment + "\n— Seu Valdemar";
    }

    // OnGUI é o sistema de interface mais antigo da Unity. Feio, mas ótimo para protótipos:
    // zero configuração. Depois trocamos por uma UI de verdade.
    void OnGUI()
    {
        // HUD: cronômetro e móveis entregues, no canto superior esquerdo.
        var hud = new GUIStyle(GUI.skin.label) { fontSize = 24 };
        hud.normal.textColor = Color.white;
        int minutes = (int)(timeLeft / 60);
        int seconds = (int)(timeLeft % 60);
        // {1:00} = sempre 2 dígitos (5 vira 05)
        GUI.Label(new Rect(20, 20, 500, 40), string.Format("Amanhecer em {0}:{1:00}", minutes, seconds), hud);
        GUI.Label(new Rect(20, 55, 500, 40), $"Móveis no caminhão: {truck.DeliveredCount}/{totalFurniture}", hud);

        if (endMessage == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = endColor;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), endMessage + "\n\nAperte R para jogar de novo", style);
    }
}
