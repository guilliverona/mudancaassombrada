using System.Runtime.CompilerServices;

// Deixa os testes (Assets/Tests) enxergarem o que é "internal" no jogo,
// como o Campaign.SetChaptersForTests. O jogo em si continua sem ver.
[assembly: InternalsVisibleTo("MudancaAssombrada.Tests.EditMode")]
