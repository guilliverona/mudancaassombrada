using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Controla o estado geral da partida. Por enquanto: game over e reinício.
public class GameManager : MonoBehaviour
{
    static GameManager instance;
    string gameOverMessage; // null = jogo rolando

    void Awake() => instance = this;

    public static void PlayerCaught(string message)
    {
        if (instance == null || instance.gameOverMessage != null) return;
        instance.gameOverMessage = message;
        Time.timeScale = 0f; // congela o tempo: física, Update com deltaTime, tudo para
    }

    void Update()
    {
        if (gameOverMessage != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Time.timeScale = 1f; // IMPORTANTE: o timeScale continua 0 na próxima cena se não voltar
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // OnGUI é o sistema de interface mais antigo da Unity. Feio, mas ótimo para protótipos:
    // zero configuração. Depois trocamos por uma UI de verdade.
    void OnGUI()
    {
        if (gameOverMessage == null) return;

        var style = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = Color.red;
        GUI.Label(new Rect(0, 0, Screen.width, Screen.height), gameOverMessage + "\n\nAperte R para tentar de novo", style);
    }
}
