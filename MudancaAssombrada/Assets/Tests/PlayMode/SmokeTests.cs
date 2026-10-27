using System;
using System.Collections;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MudancaAssombrada.Tests
{
    // Testes de FUMAÇA (TESTES.md, parte 8): poucos e simples, para pegar o que quebra "por fora" das
    // regras (cena que não abre, referência perdida, erro no Console). Regra não se testa aqui: é lento.
    // Qualquer Debug.LogError ou exceção durante o teste já faz ele falhar (o Test Runner confere).
    //
    // O host é LOCAL (StartHost direto, sem Lobby nem Relay): não precisa de internet nem de login.
    public class SmokeTests
    {
        const string Menu = NetworkSession.MenuScene;
        const string Fase1 = "CasaDoEspelho";
        const float Timeout = 15f; // segundos esperando a cena ou o Player

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Desliga a rede e apaga o NetworkManager (ele sobrevive às trocas de cena):
            // o próximo teste começa do zero, como um Play novo.
            var net = NetworkManager.Singleton;
            if (net != null)
            {
                if (net.IsListening) net.Shutdown();
                yield return null;
                UnityEngine.Object.Destroy(net.gameObject);
            }
            Time.timeScale = 1f; // o GameManager congela o tempo no fim da partida
            yield return null;
        }

        static IEnumerator WaitUntil(Func<bool> done, string what)
        {
            float limit = Time.realtimeSinceStartup + Timeout;
            while (!done())
            {
                if (Time.realtimeSinceStartup > limit) Assert.Fail($"Passaram {Timeout} s e {what} não aconteceu.");
                yield return null;
            }
        }

        static IEnumerator OpenMenu()
        {
            SceneManager.LoadScene(Menu);
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == Menu, "a cena Menu abrir");
            yield return null; // um frame para o Start dos scripts do menu rodar
        }

        // Sobe um host local e abre a fase pela rede, como o LobbyManager faz ao começar a partida.
        static IEnumerator HostFase1()
        {
            yield return OpenMenu();
            var net = NetworkManager.Singleton;
            Assert.IsNotNull(net, "o menu não criou o NetworkManager");
            Assert.IsTrue(net.StartHost(), "o host local não subiu");
            net.SceneManager.LoadScene(Fase1, LoadSceneMode.Single);
            yield return WaitUntil(() => SceneManager.GetActiveScene().name == Fase1, "a Casa do Espelho abrir");
        }

        [UnityTest]
        public IEnumerator Menu_AbreSemErro_ECriaONetworkManager()
        {
            yield return OpenMenu();
            yield return new WaitForSecondsRealtime(1f); // deixa o menu rodar um pouco
            Assert.IsNotNull(NetworkManager.Singleton);
        }

        [UnityTest]
        public IEnumerator HostLocal_OPlayerNasce_EAHudAparece()
        {
            yield return HostFase1();
            yield return WaitUntil(() => NetworkPlayer.Local != null, "o Player nascer");
            Assert.AreEqual(1, NetworkPlayer.All.Count);

            yield return WaitUntil(() => UnityEngine.Object.FindAnyObjectByType<Hud>() != null, "a HUD aparecer");
            Assert.IsTrue(UnityEngine.Object.FindAnyObjectByType<Hud>().isActiveAndEnabled);
        }

        [UnityTest]
        public IEnumerator CasaDoEspelho_OGameManagerAchaOCapitulo()
        {
            yield return HostFase1();
            yield return WaitUntil(() => GameManager.Instance != null, "o GameManager aparecer");
            var chapter = GameManager.Instance.Chapter;
            Assert.IsNotNull(chapter, "a cena não está na Campanha (Campaign.FindByScene)");
            Assert.AreEqual(Fase1, chapter.sceneName);
            Assert.IsTrue(GameManager.Instance.HasClock);
        }
    }
}
